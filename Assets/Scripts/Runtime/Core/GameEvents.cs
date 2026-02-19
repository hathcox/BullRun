/// <summary>
/// All game event type definitions.
/// Events are published via EventBus for inter-system communication.
/// Naming convention: {Subject}{Verb}Event
/// </summary>

/// <summary>
/// Fired every frame when a stock's price is updated by the PriceGenerator.
/// </summary>
public struct PriceUpdatedEvent
{
    public int StockId;
    public float NewPrice;
    public float PreviousPrice;
    public float DeltaTime;
}

/// <summary>
/// Fired when a market event starts affecting stock prices.
/// AffectedStockIds is null for global events (all stocks affected).
/// </summary>
public struct MarketEventFiredEvent
{
    public MarketEventType EventType;
    public int[] AffectedStockIds;
    public float PriceEffectPercent;
    public string Headline;
    public string[] AffectedTickerSymbols;
    public bool IsPositive;
    public float Duration;
}

/// <summary>
/// Fired when the EventPopup finishes displaying (or skips) a market event.
/// ScreenEffects subscribes to this instead of MarketEventFiredEvent
/// so dramatic visual effects start AFTER the popup flies away and timeScale resumes.
/// </summary>
public struct EventPopupCompletedEvent
{
    public MarketEventType EventType;
    public bool IsPositive;
}

/// <summary>
/// Fired when a market event expires and stops affecting stock prices.
/// AffectedStockIds is null for global events.
/// </summary>
public struct MarketEventEndedEvent
{
    public MarketEventType EventType;
    public int[] AffectedStockIds;
    public string[] AffectedTickerSymbols;
}

/// <summary>
/// Fired when a trade (buy/sell/short) is executed successfully.
/// NOTE: StockId is string here (ticker/name) but PriceUpdatedEvent uses int StockId.
/// A stock registry mapping int↔string will be needed when these systems integrate fully.
/// </summary>
public struct TradeExecutedEvent
{
    public string StockId;
    public int Shares;
    public float Price;
    public bool IsBuy;
    public bool IsShort;
    /// <summary>
    /// Financial amount of the trade. Semantics vary by trade type:
    /// Buy: total cost paid (shares * price). Sell: total proceeds received (shares * price).
    /// Short: margin collateral held. Cover: buy-back cost (shares * price).
    /// </summary>
    public float TotalCost;
    /// <summary>
    /// Realized profit/loss for closing trades (sell, cover). Positive = profit, negative = loss.
    /// Zero for opening trades (buy, short open). Used by AudioManager for profit/loss SFX routing.
    /// </summary>
    public float ProfitLoss;
}

/// <summary>
/// Fired when a round ends (after auto-liquidation).
/// </summary>
public struct RoundEndedEvent
{
    public int RoundNumber;
    public float TotalProfit;
    public float FinalCash;
}

/// <summary>
/// Fired when a new run starts.
/// </summary>
public struct RunStartedEvent
{
    public float StartingCapital;
}

/// <summary>
/// Fired when the player selects a stock from the sidebar or via keyboard shortcut.
/// ChartRenderer subscribes to switch the displayed stock.
/// </summary>
public struct StockSelectedEvent
{
    public int StockId;
    public string TickerSymbol;
}

/// <summary>
/// Fired when the MarketOpen preview phase begins.
/// UI subscribes to display stock preview, headline, and profit target.
/// </summary>
public struct MarketOpenEvent
{
    public int RoundNumber;
    public int Act;
    public int[] StockIds;
    public string[] TickerSymbols;
    public float[] StartingPrices;
    public string[] TierNames;
    public float ProfitTarget;
    public string Headline;
    /// <summary>Story 13.6: Bond Rep earned at round start (0 if no bonds).</summary>
    public int BondRepEarned;
}

/// <summary>
/// Fired when a trading round starts.
/// </summary>
public struct RoundStartedEvent
{
    public int RoundNumber;
    public int Act;
    public string TierDisplayName;
    public float MarginCallTarget;
    public float TimeLimit;
}

/// <summary>
/// Fired when the trading phase ends (timer expired or manual end).
/// </summary>
public struct TradingPhaseEndedEvent
{
    public int RoundNumber;
    public bool TimeExpired;
}

/// <summary>
/// Fired when the market closes and all positions are auto-liquidated.
/// Published by MarketCloseState after liquidation completes.
/// </summary>
public struct MarketClosedEvent
{
    public int RoundNumber;
    public float RoundProfit;
    public float FinalCash;
    public int PositionsLiquidated;
}

/// <summary>
/// Fired when the player fails to meet the margin call target for a round.
/// The run ends immediately after this event.
/// </summary>
public struct MarginCallTriggeredEvent
{
    public int RoundNumber;
    public float RoundProfit;
    public float RequiredTarget;
    public float Shortfall;
}

/// <summary>
/// Fired when a round is completed successfully (margin call passed).
/// UI subscribes to display round results overlay.
/// FIX-14: ProfitTarget is a cumulative value target (total cash threshold), not profit delta.
/// </summary>
public struct RoundCompletedEvent
{
    public int RoundNumber;
    public float RoundProfit;
    public float ProfitTarget;
    public bool TargetMet;
    public float TotalCash;
    /// <summary>FIX-14: Reputation earned this round (base + performance bonus).</summary>
    public int RepEarned;
    /// <summary>FIX-14: Base Reputation component (scales with round number).</summary>
    public int BaseRep;
    /// <summary>FIX-14: Bonus Reputation component (performance excess over target).</summary>
    public int BonusRep;
}

/// <summary>
/// Fired when an act transition occurs (new act begins).
/// UI subscribes to display act transition interstitial.
/// </summary>
public struct ActTransitionEvent
{
    public int NewAct;
    public int PreviousAct;
    public string TierDisplayName;
}

/// <summary>
/// Fired when the store opens after a successful round.
/// FIX-12: Carries Reputation balance (shop currency) instead of cash.
/// 13.2: Added section availability flags for the multi-panel store layout.
/// 13.9: AvailableRelics replaced legacy ShopItemDef[] AvailableItems.
/// </summary>
public struct ShopOpenedEvent
{
    public int RoundNumber;
    public RelicDef[] AvailableRelics;
    public int CurrentReputation;
    public bool ExpansionsAvailable;
    public bool TipsAvailable;
    public bool BondAvailable;
}

/// <summary>
/// Fired when the player purchases an item from the shop.
/// FIX-12: Cost and remaining balance are in Reputation, not cash.
/// </summary>
public struct ShopItemPurchasedEvent
{
    public string ItemId;
    public string ItemName;
    public int Cost;
    public int RemainingReputation;
}

/// <summary>
/// Fired when the player purchases an expansion from the shop.
/// </summary>
public struct ShopExpansionPurchasedEvent
{
    public string ExpansionId;
    public string DisplayName;
    public int Cost;
    public int RemainingReputation;
}

/// <summary>
/// Fired when the player purchases an insider tip from the shop (Story 13.5, AC 9).
/// </summary>
public struct InsiderTipPurchasedEvent
{
    public InsiderTipType TipType;
    public string RevealedText;
    public int Cost;
    public int RemainingReputation;
}

/// <summary>
/// Fired when the store closes (player clicked Next Round).
/// FIX-12: Remaining balance is in Reputation, not cash.
/// 13.2: Added per-section purchase counts for analytics.
/// </summary>
public struct ShopClosedEvent
{
    public string[] PurchasedItemIds;
    public int ReputationRemaining;
    public int RoundNumber;
    public int RelicsPurchased;
    public int ExpansionsPurchased;
    public int TipsPurchased;
    public int BondsPurchased;
}

/// <summary>
/// Fired when the player clicks the BUY or SELL button on the trade panel.
/// GameRunner subscribes to execute the smart trade routing.
/// </summary>
public struct TradeButtonPressedEvent
{
    public bool IsBuy;
}

/// <summary>
/// Fired by GameRunner after every trade attempt (success or failure).
/// TradeFeedback subscribes to display brief visual feedback text.
/// </summary>
public struct TradeFeedbackEvent
{
    public string Message;
    public bool IsSuccess;
    public bool IsBuy;
    public bool IsShort;
}

/// <summary>
/// Fired when a run ends, either by margin call or completing all rounds.
/// Contains full run statistics including victory status.
/// Audio system (Epic 11) subscribes for victory music.
/// Meta-progression (Epic 9) subscribes for reputation processing.
/// </summary>
public struct RunEndedEvent
{
    public int RoundsCompleted;
    public float FinalCash;
    public float TotalProfit;
    public bool WasMarginCalled;
    public bool IsVictory;
    public int ReputationEarned;
    public int ItemsCollected;
    public float PeakCash;
    public float BestRoundProfit;
}

/// <summary>
/// Fired when the player purchases a bond (Story 13.6, AC 15).
/// Bonds cost Cash (not Reputation).
/// </summary>
public struct BondPurchasedEvent
{
    public int Round;
    public float PricePaid;
    public int TotalBondsOwned;
    public float RemainingCash;
}

/// <summary>
/// Fired when the player sells a bond (Story 13.6, AC 15).
/// Sell price = purchase price × BondSellMultiplier (LIFO).
/// </summary>
public struct BondSoldEvent
{
    public float SellPrice;
    public int TotalBondsOwned;
    public float CashAfterSale;
}

/// <summary>
/// Fired at round start when bonds pay out Reputation (Story 13.6, AC 14, 15).
/// </summary>
public struct BondRepPaidEvent
{
    public int BondsOwned;
    public int RepEarned;
    public int TotalReputation;
}

/// <summary>
/// Fired every frame by GameRunner when a short position has an active countdown timer.
/// PositionPanel uses this to display auto-close countdown on short entries.
/// </summary>
public struct ShortCountdownEvent
{
    public string StockId;
    public float TimeRemaining;
    public bool IsCashOutWindow;
}

/// <summary>
/// Fired when the player sells an owned relic from the owned relics bar (Story 13.10, AC 15).
/// Refund amount is 50% of original cost (integer division = floor).
/// </summary>
public struct ShopItemSoldEvent
{
    public string RelicId;
    public int RefundAmount;
    public int RemainingReputation;
}

/// <summary>
/// Fired when the player rerolls the relic offering in the shop.
/// Added in Story 11.1 for audio triggers. AudioManager subscribes for the shop_reroll sound.
/// </summary>
public struct ShopRerollEvent
{
    public int RerollCount;
    public int Cost;
}

/// <summary>
/// Fired when the player clicks START GAME on the main menu.
/// Story 16.1: Triggers transition from MainMenuState to MarketOpenState.
/// </summary>
public struct StartGameRequestedEvent { }
