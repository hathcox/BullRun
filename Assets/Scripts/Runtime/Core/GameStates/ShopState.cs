using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shop state orchestrating all four panels: relics, expansions, tips, bonds.
/// Handles purchases and rerolls, then advances to next round when player clicks Continue.
/// Transitions to MarketOpenState (or TierTransitionState if act changes,
/// or RunSummaryState if run complete).
/// Story 13.3: Uses RelicDef + uniform random selection + reroll mechanism.
/// </summary>
public class ShopState : IGameState
{
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;
    private EventScheduler _eventScheduler;

    private bool _shopActive;
    private RelicDef?[] _relicOffering;
    private bool[] _purchased;
    private List<string> _purchasedItemIds;
    private ShopTransaction _shopTransaction;
    private System.Random _random;
    private int _randomSeedOverride = -1;

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
            _randomSeedOverride = NextConfig.RandomSeed;
            NextConfig = null;
        }

        // Reset per-visit store state (AC 9: reroll cost resets each shop visit)
        ctx.CurrentShopRerollCount = 0;
        ctx.RevealedTips.Clear();

        // Generate relic offering with uniform random selection (AC 1, 2, 3)
        _random = _randomSeedOverride >= 0 ? new System.Random(_randomSeedOverride) : new System.Random();
        _relicOffering = ShopGenerator.GenerateRelicOffering(ctx.OwnedRelics, _random);

        _purchased = new bool[_relicOffering.Length];
        _purchasedItemIds = new List<string>();
        _shopTransaction = new ShopTransaction();
        _shopActive = true;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Generated relics: {RelicLabel(0)}, {RelicLabel(1)}, {RelicLabel(2)}");
        #endif

        // Show store UI with purchase, close, and reroll callbacks
        if (ShopUIInstance != null)
        {
            ShopUIInstance.ShowRelics(ctx, _relicOffering, (cardIndex) => OnPurchaseRequested(ctx, cardIndex));
            ShopUIInstance.SetOnCloseCallback(() => CloseShop(ctx));
            ShopUIInstance.SetOnRerollCallback(() => OnRerollRequested(ctx));
        }

        // Publish shop opened event — only include non-null items
        int availableCount = 0;
        for (int i = 0; i < _relicOffering.Length; i++)
            if (_relicOffering[i].HasValue) availableCount++;

        // Convert RelicDefs to ShopItemDefs for the event (backwards compat)
        var availableItems = new ShopItemDef[availableCount];
        int availIdx = 0;
        for (int i = 0; i < _relicOffering.Length; i++)
        {
            if (_relicOffering[i].HasValue)
            {
                var r = _relicOffering[i].Value;
                availableItems[availIdx++] = new ShopItemDef(r.Id, r.Name, r.Description, r.Cost, ItemRarity.Common, ItemCategory.TradingTool);
            }
        }

        EventBus.Publish(new ShopOpenedEvent
        {
            RoundNumber = ctx.CurrentRound,
            AvailableItems = availableItems,
            CurrentReputation = ctx.Reputation.Current,
            ExpansionsAvailable = false, // Placeholder — Stories 13.4+
            TipsAvailable = false,       // Placeholder — Story 13.5
            BondAvailable = false        // Placeholder — Story 13.6
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Enter: Store opened (Round {ctx.CurrentRound}), {availableItems.Length} relics, untimed");
        #endif
    }

    public void Update(RunContext ctx)
    {
        // Shop is untimed — player closes via Continue button
    }

    public void Exit(RunContext ctx)
    {
        // Safety net: if shop was still active (not closed via CloseShop), fire event
        if (_shopActive)
        {
            _shopActive = false;
            EventBus.Publish(new ShopClosedEvent
            {
                PurchasedItemIds = _purchasedItemIds?.ToArray() ?? System.Array.Empty<string>(),
                ReputationRemaining = ctx.Reputation.Current,
                RoundNumber = ctx.CurrentRound,
                RelicsPurchased = _purchasedItemIds?.Count ?? 0,
                ExpansionsPurchased = 0,
                TipsPurchased = 0,
                BondsPurchased = 0
            });
        }

        if (ShopUIInstance != null)
        {
            ShopUIInstance.Hide();
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Exit: {_purchasedItemIds?.Count ?? 0} items purchased");
        #endif
    }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    private string RelicLabel(int index)
    {
        if (index < 0 || index >= _relicOffering.Length) return "none";
        return _relicOffering[index].HasValue ? _relicOffering[index].Value.Name : "none";
    }
    #endif

    /// <summary>
    /// Called when player clicks a buy button. Delegates to ShopTransaction for atomic purchase.
    /// Uses RelicDef purchase flow (AC 5, 11, 12, 15).
    /// </summary>
    public void OnPurchaseRequested(RunContext ctx, int cardIndex)
    {
        if (!_shopActive) return;
        if (cardIndex < 0 || cardIndex >= _relicOffering.Length) return;
        if (_purchased[cardIndex]) return;
        if (!_relicOffering[cardIndex].HasValue) return;

        var relic = _relicOffering[cardIndex].Value;
        var result = _shopTransaction.PurchaseRelic(ctx, relic);

        if (result == ShopPurchaseResult.Success)
        {
            _purchased[cardIndex] = true;
            _purchasedItemIds.Add(relic.Id);

            // Update UI: mark purchased and refresh affordability
            if (ShopUIInstance != null)
            {
                ShopUIInstance.RefreshAfterPurchase(cardIndex);
            }
        }
    }

    /// <summary>
    /// Called when player clicks the reroll button (AC 7, 8, 9, 10).
    /// Deducts Rep, regenerates unsold relic slots with new random items.
    /// </summary>
    private void OnRerollRequested(RunContext ctx)
    {
        if (!_shopActive) return;

        if (!_shopTransaction.TryReroll(ctx))
        {
            return; // Insufficient funds
        }

        // Regenerate offering — only unsold slots get new items (AC 10)
        // Exclude currently displayed unsold relics so reroll yields fresh items
        var currentUnsoldIds = new List<string>();
        for (int i = 0; i < _relicOffering.Length; i++)
        {
            if (!_purchased[i] && _relicOffering[i].HasValue)
                currentUnsoldIds.Add(_relicOffering[i].Value.Id);
        }
        var newOffering = ShopGenerator.GenerateRelicOffering(ctx.OwnedRelics, currentUnsoldIds, _random);

        // Preserve sold slots
        for (int i = 0; i < _relicOffering.Length && i < newOffering.Length; i++)
        {
            if (_purchased[i])
            {
                newOffering[i] = _relicOffering[i]; // Keep sold item reference
            }
        }

        _relicOffering = newOffering;

        if (ShopUIInstance != null)
        {
            ShopUIInstance.RefreshRelicOffering(newOffering);
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Reroll #{ctx.CurrentShopRerollCount}: {RelicLabel(0)}, {RelicLabel(1)}, {RelicLabel(2)}");
        #endif
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
            RoundNumber = ctx.CurrentRound,
            RelicsPurchased = _purchasedItemIds.Count,
            ExpansionsPurchased = 0,
            TipsPurchased = 0,
            BondsPurchased = 0
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
    /// <summary>
    /// Optional random seed for deterministic relic generation. -1 = time-based (default).
    /// </summary>
    public int RandomSeed = -1;
}
