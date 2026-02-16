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

    // Expansion panel state (Story 13.4)
    private ExpansionManager _expansionManager;
    private ExpansionDef[] _expansionOffering;
    private int _expansionsPurchasedCount;

    // Insider tips panel state (Story 13.5)
    private InsiderTipGenerator _tipGenerator;
    private InsiderTipGenerator.TipOffering[] _tipOffering;
    private bool[] _tipPurchased;
    private int _tipsPurchasedCount;

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
        _expansionsPurchasedCount = 0;

        // Generate expansion offering (Story 13.4)
        _expansionManager = new ExpansionManager(ctx);
        _expansionOffering = _expansionManager.GetAvailableForShop(GameConfig.ExpansionsPerShopVisit, _random);

        // Generate insider tip offering (Story 13.5)
        _tipGenerator = new InsiderTipGenerator();
        int nextRound = ctx.CurrentRound + 1;
        int nextAct = RunContext.GetActForRound(nextRound);
        _tipOffering = _tipGenerator.GenerateTips(ctx.InsiderTipSlots, nextRound, nextAct, _random);
        _tipPurchased = new bool[_tipOffering.Length];
        _tipsPurchasedCount = 0;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Generated relics: {RelicLabel(0)}, {RelicLabel(1)}, {RelicLabel(2)}");
        #endif

        // Show store UI with purchase, close, and reroll callbacks
        if (ShopUIInstance != null)
        {
            ShopUIInstance.ShowRelics(ctx, _relicOffering, (cardIndex) => OnPurchaseRequested(ctx, cardIndex));
            ShopUIInstance.SetOnCloseCallback(() => CloseShop(ctx));
            ShopUIInstance.SetOnRerollCallback(() => OnRerollRequested(ctx));

            // Populate expansion panel (Story 13.4)
            ShopUIInstance.ShowExpansions(ctx, _expansionOffering, (cardIndex) => OnExpansionPurchaseRequested(ctx, cardIndex));

            // Populate insider tips panel (Story 13.5)
            ShopUIInstance.ShowTips(ctx, _tipOffering, (cardIndex) => OnTipPurchaseRequested(ctx, cardIndex));
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
            ExpansionsAvailable = _expansionOffering.Length > 0,
            TipsAvailable = _tipOffering != null && _tipOffering.Length > 0,
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
                ExpansionsPurchased = _expansionsPurchasedCount,
                TipsPurchased = _tipsPurchasedCount,
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
    /// Called when player clicks an expansion buy button (Story 13.4, AC 3, 5, 8).
    /// Delegates to ShopTransaction.PurchaseExpansion for atomic purchase.
    /// </summary>
    private void OnExpansionPurchaseRequested(RunContext ctx, int cardIndex)
    {
        if (!_shopActive) return;
        if (_expansionOffering == null) return;
        if (cardIndex < 0 || cardIndex >= _expansionOffering.Length) return;

        var expansion = _expansionOffering[cardIndex];
        if (ctx.OwnedExpansions.Contains(expansion.Id)) return;

        var result = _shopTransaction.PurchaseExpansion(ctx, expansion.Id, expansion.Name, expansion.Cost);

        if (result == ShopPurchaseResult.Success)
        {
            _expansionsPurchasedCount++;

            if (ShopUIInstance != null)
            {
                ShopUIInstance.RefreshExpansionAfterPurchase(cardIndex);
            }
        }
    }

    /// <summary>
    /// Called when player clicks a tip buy button (Story 13.5, AC 6, 8, 9).
    /// Delegates to ShopTransaction.PurchaseTip for atomic purchase.
    /// Fires InsiderTipPurchasedEvent on success.
    /// </summary>
    private void OnTipPurchaseRequested(RunContext ctx, int cardIndex)
    {
        if (!_shopActive) return;
        if (_tipOffering == null) return;
        if (cardIndex < 0 || cardIndex >= _tipOffering.Length) return;
        if (_tipPurchased[cardIndex]) return;

        var offering = _tipOffering[cardIndex];
        var tip = new RevealedTip(offering.Definition.Type, offering.RevealedText);
        var result = _shopTransaction.PurchaseTip(ctx, tip, offering.Definition.Cost);

        if (result == ShopPurchaseResult.Success)
        {
            _tipPurchased[cardIndex] = true;
            _tipsPurchasedCount++;

            EventBus.Publish(new InsiderTipPurchasedEvent
            {
                TipType = offering.Definition.Type,
                RevealedText = offering.RevealedText,
                Cost = offering.Definition.Cost,
                RemainingReputation = ctx.Reputation.Current
            });

            if (ShopUIInstance != null)
            {
                ShopUIInstance.RefreshTipAfterPurchase(cardIndex);
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
            RoundNumber = ctx.CurrentRound,
            RelicsPurchased = _purchasedItemIds.Count,
            ExpansionsPurchased = _expansionsPurchasedCount,
            TipsPurchased = _tipsPurchasedCount,
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
