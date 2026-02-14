using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draft shop state. Shows shop UI with 3 items (one per category),
/// handles purchases, then advances to next round when player clicks Continue.
/// Transitions to MarketOpenState (or TierTransitionState if act changes,
/// or RunSummaryState if run complete).
/// </summary>
public class ShopState : IGameState
{
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;
    private EventScheduler _eventScheduler;

    private bool _shopActive;
    private ShopItemDef?[] _nullableOffering;
    private bool[] _purchased;
    private List<string> _purchasedItemIds;
    private ShopTransaction _shopTransaction;

    public static ShopStateConfig NextConfig;

    /// <summary>
    /// Static reference to the ShopUI MonoBehaviour, set by UISetup during F5.
    /// </summary>
    public static ShopUI ShopUIInstance;

    public void Enter(RunContext ctx)
    {
        if (NextConfig != null)
        {
            _stateMachine = NextConfig.StateMachine;
            _priceGenerator = NextConfig.PriceGenerator;
            _tradeExecutor = NextConfig.TradeExecutor;
            _eventScheduler = NextConfig.EventScheduler;
            NextConfig = null;
        }

        // Generate shop items with weighted rarity, unlock filtering, and duplicate prevention
        var random = new System.Random();
        _nullableOffering = ShopGenerator.GenerateOffering(ctx.ActiveItems, ShopItemDefinitions.DefaultUnlockedItems, random);
        _purchased = new bool[_nullableOffering.Length];
        _purchasedItemIds = new List<string>();
        _shopTransaction = new ShopTransaction();
        _shopActive = true;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Generated items: {ItemLabel(0)}, {ItemLabel(1)}, {ItemLabel(2)}");
        #endif

        // Show shop UI with purchase and close callbacks
        if (ShopUIInstance != null)
        {
            ShopUIInstance.Show(ctx, _nullableOffering, (cardIndex) => OnPurchaseRequested(ctx, cardIndex));
            ShopUIInstance.SetOnCloseCallback(() => CloseShop(ctx));
        }

        // Publish shop opened event — only include non-null items
        int availableCount = 0;
        for (int i = 0; i < _nullableOffering.Length; i++)
            if (_nullableOffering[i].HasValue) availableCount++;
        var availableItems = new ShopItemDef[availableCount];
        int availIdx = 0;
        for (int i = 0; i < _nullableOffering.Length; i++)
            if (_nullableOffering[i].HasValue) availableItems[availIdx++] = _nullableOffering[i].Value;

        EventBus.Publish(new ShopOpenedEvent
        {
            RoundNumber = ctx.CurrentRound,
            AvailableItems = availableItems,
            CurrentReputation = ctx.Reputation.Current
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Enter: Shop opened (Round {ctx.CurrentRound}), {availableItems.Length} items, untimed");
        #endif
    }

    public void Update(RunContext ctx)
    {
        // Shop is untimed — player closes via Continue button
    }

    public void Exit(RunContext ctx)
    {
        if (ShopUIInstance != null)
        {
            ShopUIInstance.Hide();
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Exit: {_purchasedItemIds?.Count ?? 0} items purchased");
        #endif
    }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    private string ItemLabel(int index)
    {
        if (index < 0 || index >= _nullableOffering.Length) return "none";
        return _nullableOffering[index].HasValue ? _nullableOffering[index].Value.Name : "none";
    }
    #endif

    /// <summary>
    /// Called when player clicks a buy button. Delegates to ShopTransaction for atomic purchase.
    /// </summary>
    public void OnPurchaseRequested(RunContext ctx, int cardIndex)
    {
        if (!_shopActive) return;
        if (cardIndex < 0 || cardIndex >= _nullableOffering.Length) return;
        if (_purchased[cardIndex]) return;
        if (!_nullableOffering[cardIndex].HasValue) return;

        var item = _nullableOffering[cardIndex].Value;
        var result = _shopTransaction.TryPurchase(ctx, item);

        if (result == ShopPurchaseResult.Success)
        {
            _purchased[cardIndex] = true;
            _purchasedItemIds.Add(item.Id);

            // Update UI: mark purchased and refresh affordability
            if (ShopUIInstance != null)
            {
                ShopUIInstance.RefreshAfterPurchase(cardIndex);
            }
        }
    }

    /// <summary>
    /// Closes the shop (player clicked Continue button).
    /// Publishes ShopClosedEvent, advances round, and transitions to next state.
    /// </summary>
    private void CloseShop(RunContext ctx)
    {
        if (!_shopActive) return;
        _shopActive = false;

        // Publish shop closed event with purchased item details
        EventBus.Publish(new ShopClosedEvent
        {
            PurchasedItemIds = _purchasedItemIds.ToArray(),
            ReputationRemaining = ctx.Reputation.Current,
            RoundNumber = ctx.CurrentRound
        });

        // Advance to next round
        Debug.Assert(ctx.Portfolio.PositionCount == 0,
            "[ShopState] AdvanceRound called with open positions — liquidate first!");
        int previousAct = ctx.CurrentAct;
        bool actChanged = ctx.AdvanceRound();
        ctx.Portfolio.StartRound(ctx.Portfolio.Cash);

        if (_stateMachine == null) return;

        // Check if run is complete after advancing
        if (ctx.IsRunComplete())
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[ShopState] Run complete — transitioning to RunSummary (win)");
            #endif

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = false,
                RoundProfit = 0f,
                RequiredTarget = 0f,
                StateMachine = _stateMachine,
                PriceGenerator = _priceGenerator,
                TradeExecutor = _tradeExecutor,
                EventScheduler = _eventScheduler
            };
            _stateMachine.TransitionTo<RunSummaryState>();
            return;
        }

        // Route through TierTransitionState when act changes
        if (actChanged)
        {
            TierTransitionState.NextConfig = new TierTransitionStateConfig
            {
                StateMachine = _stateMachine,
                PriceGenerator = _priceGenerator,
                TradeExecutor = _tradeExecutor,
                EventScheduler = _eventScheduler,
                PreviousAct = previousAct
            };
            _stateMachine.TransitionTo<TierTransitionState>();
            return;
        }

        // Continue to next MarketOpenState
        MarketOpenState.NextConfig = new MarketOpenStateConfig
        {
            StateMachine = _stateMachine,
            PriceGenerator = _priceGenerator,
            TradeExecutor = _tradeExecutor,
            EventScheduler = _eventScheduler
        };
        _stateMachine.TransitionTo<MarketOpenState>();
    }
}

/// <summary>
/// Configuration passed to ShopState before transition.
/// </summary>
public class ShopStateConfig
{
    public GameStateMachine StateMachine;
    public PriceGenerator PriceGenerator;
    public TradeExecutor TradeExecutor;
    public EventScheduler EventScheduler;
}
