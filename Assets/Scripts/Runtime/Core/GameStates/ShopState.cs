using UnityEngine;

/// <summary>
/// Draft shop state. Shows shop UI with 3 items (one per category),
/// runs a countdown timer, handles purchases, then advances to next round.
/// After timer expires or player closes, transitions to MarketOpenState
/// (or TierTransitionState if act changes, or RunSummaryState if run complete).
/// </summary>
public class ShopState : IGameState
{
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;
    private EventScheduler _eventScheduler;

    private float _timeRemaining;
    private bool _shopActive;
    private ShopItemDef[] _offering;
    private bool[] _purchased;
    private int _purchaseCount;

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

        // Generate shop items
        _offering = ShopGenerator.GenerateOffering();
        _purchased = new bool[_offering.Length];
        _purchaseCount = 0;
        _timeRemaining = GameConfig.ShopDurationSeconds;
        _shopActive = true;

        // Show shop UI
        if (ShopUIInstance != null)
        {
            ShopUIInstance.Show(ctx, _offering, (cardIndex) => OnPurchase(ctx, cardIndex));
        }

        // Publish shop opened event
        EventBus.Publish(new ShopOpenedEvent
        {
            RoundNumber = ctx.CurrentRound,
            AvailableItems = _offering
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Enter: Shop opened (Round {ctx.CurrentRound}), {_offering.Length} items, {GameConfig.ShopDurationSeconds}s timer");
        #endif
    }

    public void Update(RunContext ctx)
    {
        if (!_shopActive) return;

        _timeRemaining -= Time.deltaTime;

        // Update timer display
        if (ShopUIInstance != null)
        {
            ShopUIInstance.UpdateTimer(Mathf.Max(0f, _timeRemaining));
        }

        // Timer expired — close shop
        if (_timeRemaining <= 0f)
        {
            CloseShop(ctx);
        }
    }

    public void Exit(RunContext ctx)
    {
        if (ShopUIInstance != null)
        {
            ShopUIInstance.Hide();
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Exit: {_purchaseCount} items purchased");
        #endif
    }

    public void OnPurchase(RunContext ctx, int cardIndex)
    {
        if (!_shopActive) return;
        if (cardIndex < 0 || cardIndex >= _offering.Length) return;
        if (_purchased[cardIndex]) return;

        var item = _offering[cardIndex];

        // Deduct cost — DeductCash returns false if insufficient funds
        if (!ctx.Portfolio.DeductCash(item.Cost)) return;

        _purchased[cardIndex] = true;
        _purchaseCount++;

        // Track item in RunContext
        ctx.ActiveItems.Add(item.Id);

        // Publish purchase event
        EventBus.Publish(new ShopItemPurchasedEvent
        {
            ItemId = item.Id,
            Cost = item.Cost,
            RemainingCash = ctx.Portfolio.Cash
        });

        // Update UI
        if (ShopUIInstance != null)
        {
            ShopUIInstance.RefreshAfterPurchase(cardIndex);
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopState] Purchased: {item.Name} (${item.Cost}), Cash remaining: ${ctx.Portfolio.Cash:F0}");
        #endif
    }

    private void CloseShop(RunContext ctx)
    {
        if (!_shopActive) return;
        _shopActive = false;

        // Publish shop closed event
        EventBus.Publish(new ShopClosedEvent
        {
            ItemsPurchasedCount = _purchaseCount,
            CashRemaining = ctx.Portfolio.Cash
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
