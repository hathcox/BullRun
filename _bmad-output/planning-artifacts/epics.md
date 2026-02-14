# Bull Run — Epics & User Stories

> Generated from GDD Section 12 + full feature specifications
> Project: BullRun | Date: 2026-02-10

---

## Epic 1: Price Engine

**Description:** Stock price generation, trend system, noise, event integration
**Phase:** Phase 1 (Weeks 1-4)

### Story 1.1: Base Trend Generation

As a player, I want stock prices to have an underlying directional bias (bullish, bearish, or neutral) each round, so that price movement feels intentional and readable.

**Acceptance Criteria:**
- Each stock receives a hidden trend direction at round start
- Trend creates a visible slope on the price line over the round duration
- Trend strength varies by stock tier

### Story 1.2: Noise Layer

As a player, I want random variation on top of price trends, so that prices are not perfectly predictable even when the trend is known.

**Acceptance Criteria:**
- Random walk noise applied over base trend
- Noise amplitude scales with stock tier volatility (penny = very high, blue chip = low-med)
- Noise creates readable chart patterns without overwhelming the trend

### Story 1.3: Event Spike Integration

As a player, I want market events to cause sudden price spikes or drops, so that dramatic moments create trading opportunities.

**Acceptance Criteria:**
- Events inject sharp price movements that override the base trend temporarily
- Spike magnitude configurable per event type
- Spikes create obvious visual signals (sharp V-shapes, sudden plateaus)

### Story 1.4: Mean Reversion

As a player, I want prices to gradually return toward the trend line after event spikes, so that patient play is rewarded.

**Acceptance Criteria:**
- Post-spike prices revert toward trend at tier-appropriate speeds
- Penny stocks revert slowly or not at all; blue chips revert quickly
- Reversion speed is configurable per tier

### Story 1.5: Stock Tier Configuration

As a developer, I want stock behavior parameters defined per tier, so that each market tier feels distinct.

**Acceptance Criteria:**
- Penny: $0.10-$5, very high volatility, 3-4 stocks, wild swings
- Low-Value: $5-$50, high volatility, 3-4 stocks, trend-based with reversals
- Mid-Value: $50-$500, medium volatility, 2-3 stocks, sector correlation
- Blue Chip: $500-$5,000, low-med volatility, 2-3 stocks, stable with rare dramatic events
- All parameters defined as data (not hardcoded)

### Story 1.6: Debug Overlay

As a developer, I want a debug overlay (F1 toggle) showing trend direction, scheduled events, price tick probabilities, and win rate tracking, so that I can tune balance.

**Acceptance Criteria:**
- Toggle with F1 key
- Shows underlying trend direction for each stock
- Shows all scheduled events and their timing
- Shows probability distribution of next price tick
- Shows effective win rate over last 20 runs

---

## Epic 2: Trading System

**Description:** Buy/sell/short execution, portfolio management, P&L calculation
**Phase:** Phase 1 (Weeks 1-4)

### Story 2.1: Buy Execution

As a player, I want to click BUY to purchase shares at the current market price using my cash, so that I can take a long position.

**Acceptance Criteria:**
- Clicking BUY purchases shares at current price
- Cost = share price x quantity, deducted from cash
- Shares held until sold or auto-liquidated at market close
- Cannot buy if insufficient cash

### Story 2.2: Sell Execution

As a player, I want to click SELL to liquidate held shares at the current market price, so that I can realize profits or cut losses.

**Acceptance Criteria:**
- Clicking SELL sells held shares at current price
- Cash returned to capital pool
- Can sell partial or full position
- Profit/loss calculated from average buy price

### Story 2.3: Short Execution

As a player, I want to SHORT a stock by borrowing and selling at current price, so that I can profit when prices drop.

**Acceptance Criteria:**
- Shorting borrows shares and sells at current price
- Requires margin collateral (50% of position value held in reserve)
- Player profits if price drops, loses if price rises
- Shorts auto-closed at market close
- Available from Round 1 as a core mechanic

### Story 2.4: Portfolio Tracking

As a player, I want real-time tracking of all my positions with P&L calculations, so that I know my current financial state.

**Acceptance Criteria:**
- Track all open positions (long and short)
- Calculate unrealized P&L per position in real-time
- Track average buy/sell price per position
- Show total portfolio value (cash + positions)

### Story 2.5: Capital Management

As a player, I want a limited starting cash pool that compounds across rounds, so that early decisions have lasting consequences.

**Acceptance Criteria:**
- Starting capital: $1,000
- Unspent cash carries forward between rounds
- Shop purchases deduct from trading capital
- Capital compounding rewards good play across rounds

---

## Epic 3: Chart Rendering

**Description:** Real-time chart drawing, animations, stock selection UI
**Phase:** Phase 1 (Weeks 1-4)

### Story 3.1: Real-Time Price Chart

As a player, I want to see a price line drawing itself in real-time across the screen, so that I can observe and react to price movement.

**Acceptance Criteria:**
- Line chart draws left-to-right during trading phase
- Current price shown at chart head with glowing indicator
- Price axis on the right
- Time remaining shown as progress bar along chart bottom
- Maintains 60fps with animations

### Story 3.2: Trading HUD

As a player, I want to see my cash, portfolio value, round profit, and margin call target updating in real-time, so that I know my status at a glance.

**Acceptance Criteria:**
- Top bar shows: Cash Available | Total Portfolio Value (% change) | Current Round Profit | Margin Call Target (progress bar)
- All values update in real-time as prices change and trades execute
- Clear visual distinction between profit and loss states

### Story 3.3: Stock Selection Sidebar

As a player, I want to switch between 3-4 stocks using a sidebar, so that I can monitor and trade multiple instruments.

**Acceptance Criteria:**
- Left sidebar lists available stocks vertically
- Each entry shows: ticker symbol, current price, % change, mini-sparkline
- Clicking/selecting a stock switches the main chart
- Visual indicator for currently selected stock
- Number keys 1-4 for keyboard selection

### Story 3.4: Positions Panel

As a player, I want to see my current positions with real-time P&L in a sidebar, so that I can manage my trades.

**Acceptance Criteria:**
- Right sidebar shows all open positions
- Each position shows: shares held, average buy price, current P&L
- Short positions shown in distinct color (hot pink vs green)
- Positions update in real-time

---

## Epic 4: Round Management

**Description:** Timer, market open/close, auto-liquidation, margin call check
**Phase:** Phase 1-2 (Weeks 3-6)

### Story 4.1: Round Timer

As a player, I want a 60-second countdown timer during trading, so that I feel time pressure to make decisions.

**Acceptance Criteria:**
- Visible countdown timer (60 seconds default)
- Progress bar visualization
- Visual urgency cues as time runs low (color change, pulse)

### Story 4.2: Market Open Phase

As a player, I want a brief market preview showing available stocks, a news headline, and the profit target before trading starts, so that I can plan my strategy.

**Acceptance Criteria:**
- 5-10 second preview phase before trading
- Shows which stocks are available for the round
- Displays a news headline hinting at price direction
- Shows the profit target to hit

### Story 4.3: Auto-Liquidation

As a player, I want all positions automatically liquidated when the timer expires, so that rounds resolve cleanly.

**Acceptance Criteria:**
- All long positions sold at current market price at timer expiry
- All short positions covered at current market price
- Final P&L calculated after liquidation
- Clear visual/audio signal for market close

### Story 4.4: Margin Call Check

As a player, I want to fail the round if my profit falls below the target, so that there are real stakes each round.

**Acceptance Criteria:**
- Compare round profit against escalating margin call target
- If target not met, trigger MARGIN CALL and end the run
- Escalating targets per round (see GDD section 2.3 table)

### Story 4.5: Round Transition

As a player, I want a clear transition between rounds showing my results, so that I understand how I performed.

**Acceptance Criteria:**
- Round summary screen after market close
- Shows: round profit, target met/missed, total accumulated profit
- Transitions to Draft Shop (if target met) or Margin Call (if missed)

---

## Epic 5: Event System

**Description:** Event scheduling, effects on prices, visual/audio signals
**Phase:** Phase 1-2 (Weeks 4-7)

### Story 5.1: Event Scheduler

As a developer, I want an event system that schedules 2-4 events per round at randomized intervals, so that rounds have dynamic price catalysts.

**Acceptance Criteria:**
- Schedule 2-3 events in early rounds, 3-4 in late rounds
- Events fire at randomized intervals during the trading phase
- Event timing and types configurable per round/tier

### Story 5.2: Core Market Events

As a player, I want earnings beat/miss events that cause stocks to rise or drop 15-30%, so that there are clear trading opportunities.

**Acceptance Criteria:**
- Earnings Beat: stock rises 15-30%, green news banner flash
- Earnings Miss: stock drops 15-30%, red news banner flash
- Available in all tiers

### Story 5.3: Tier-Specific Events

As a player, I want events unique to each market tier, so that each act feels mechanically distinct.

**Acceptance Criteria:**
- Penny: Pump & Dump (rapid rise then crash, suspicious volume spike)
- Penny/Low: SEC Investigation (gradual decline 20-40%, warning icon)
- Mid/Blue: Sector Rotation (one sector up, another down)
- Mid/Blue: Merger Rumor (target stock surges, rumor text crawl)

### Story 5.4: Global Market Events

As a player, I want rare market-wide events like crashes and bull runs, so that dramatic moments shake up the entire round.

**Acceptance Criteria:**
- Market Crash: all stocks drop sharply, screen shake + alarm (rare)
- Bull Run: all stocks rise steadily, green tint on chart (rare)
- Flash Crash: single stock drops then recovers, rapid red flash
- Short Squeeze: shorted stock spikes violently, warning on positions

### Story 5.5: Event Visual Signals

As a player, I want clear visual signals when events occur, so that I can react quickly under time pressure.

**Acceptance Criteria:**
- News banner flash (green/red) for earnings events
- Suspicious volume spike indicator for pump & dump
- Warning icons on affected stocks
- Sector highlight shifts for rotation events
- Screen shake for market-wide events
- News ticker crawl in bottom bar

---

## Epic 6: Run Structure

**Description:** Act/round progression, tier transitions, difficulty scaling
**Phase:** Phase 2 (Weeks 5-8)

### Story 6.1: Act-Round Progression

As a player, I want to progress through 4 acts with 2 rounds each (8 total), so that each run has a clear escalation arc.

**Acceptance Criteria:**
- 4 Acts: Penny Stocks, Low-Value, Mid-Value, Blue Chips
- 2 rounds per act, 8 rounds total
- Full run takes approximately 7-10 minutes

### Story 6.2: Tier Transitions

As a player, I want visual and audio changes between acts, so that progressing to a new market tier feels like entering a new stage.

**Acceptance Criteria:**
- Distinct visual theme per tier (volatility, stock count, behavior)
- Transition screen between acts showing the new tier name
- Audio shift to match tier intensity

### Story 6.3: Escalating Profit Targets

As a player, I want profit targets that increase across rounds with clear scaling, so that difficulty ramps predictably.

**Acceptance Criteria:**
- Round targets follow GDD table: $200, $350, $600, $900, $1500, $2200, $3500, $5000
- Scaling multipliers: 1.0x (Act 1), 1.5x (Act 2), 2.0x (Act 3), 2.5-3.0x (Act 4)
- Targets configurable for balance tuning

### Story 6.4: Stock Pool Management

As a player, I want different stocks available per act with appropriate behavior, so that each tier introduces new trading dynamics.

**Acceptance Criteria:**
- Act 1: 3-4 penny stocks with wild swings
- Act 2: 3-4 low-value stocks with trend reversals
- Act 3: 2-3 mid-value stocks with sector correlation
- Act 4: 2-3 blue chips with rare high-impact events

### Story 6.5: Win State

As a player, I want to be celebrated when I complete Round 8, so that winning a full run feels like a major achievement.

**Acceptance Criteria:**
- Special victory screen after Round 8 completion
- Run summary showing: total profit, rounds completed, items used
- Reputation earned calculation and display
- Transition to meta-hub

---

## Epic 7: Draft Shop

**Description:** Shop UI, item generation, purchase flow, item effect application
**Phase:** Phase 2 (Weeks 6-8)

### Story 7.1: Shop UI

As a player, I want a draft shop screen after each successful round presenting one item per category, so that I can upgrade my build.

**Acceptance Criteria:**
- Three item slots: Trading Tool, Market Intel, Passive Perk
- Each item shows: name, description, cost, rarity indicator
- Clear purchase button per item
- Display current cash available

### Story 7.2: Item Pool Generation

As a developer, I want items randomly selected from a rarity-weighted pool, so that shop offerings vary each run.

**Acceptance Criteria:**
- Rarity tiers: Common, Uncommon, Rare, Legendary
- Higher rarity items appear less frequently
- One item generated per category per shop visit
- Items drawn from the unlocked item pool

### Story 7.3: Purchase Flow

As a player, I want to buy items with my accumulated cash and carry unspent cash forward, so that there is tension between upgrading and capital.

**Acceptance Criteria:**
- Purchase deducts item cost from cash
- Can buy any combination (0-3 items) affordable
- Unspent cash becomes trading capital next round
- Cannot buy items that exceed available cash
- 15-20 second shop timer

### Story 7.4: Item Inventory Display

As a player, I want to see my collected items during trading rounds, so that I know what tools and perks are active.

**Acceptance Criteria:**
- Bottom bar shows active Trading Tools with hotkeys
- Passive Perks visible in a compact list
- Intel items show their information effect when applicable

---

## FIX Sprint: Bugs & Gameplay Critical Fixes

**Description:** Critical bug fixes and missing gameplay features that must be resolved before Epic 8
**Phase:** Inserted between Epic 7 and Epic 8

### Story FIX-1: Shop Click Fix & Timer Removal

As a player, I want shop item buttons to respond when I click them and have unlimited time to make my draft choices, so that I can actually purchase upgrades and make thoughtful decisions.

**Acceptance Criteria:**
- All three shop item purchase buttons respond to mouse clicks
- Shop has NO countdown timer — player browses at their own pace
- A "Continue" / "Next Round" button lets the player leave the shop when ready
- Timer text element is removed from shop UI
- No regressions in purchase flow

### Story FIX-2: Short Selling UI Bindings

As a player, I want to open short positions and cover them using keyboard controls, so that I can profit from price drops as a core trading mechanic.

**Acceptance Criteria:**
- Player can open a short position via keyboard (D key = Short)
- Player can cover a short position via keyboard (F key = Cover)
- Visual feedback on short/cover execution
- Feedback when short/cover is blocked (already long, no position, insufficient margin)

### Story FIX-3: Trade Quantity Selection

As a player, I want to choose how many shares to buy, sell, short, or cover at a time, so that I can control my position sizes and manage risk.

**Acceptance Criteria:**
- Preset quantity buttons: 1x, 5x, 10x, MAX
- MAX calculates maximum affordable/held shares dynamically
- Selected quantity persists until changed, resets each round
- All trade types (buy, sell, short, cover) use selected quantity

### Story FIX-4: Event Pop-Up Display with Pause & Directional Fly

As a player, I want market events to dramatically pop up on screen with a headline, briefly pause the action, and fly in the direction the stock will move, so that events feel impactful and I can react to them.

**Acceptance Criteria:**
- CRITICAL BUG FIX: NewsBanner, NewsTicker, ScreenEffects actually instantiated (missing init calls in GameRunner.Start)
- Large center-screen popup with event headline and direction arrow
- Brief game pause (Time.timeScale = 0) for ~1.2s so player can read
- Popup flies UP for positive events, DOWN for negative events
- Event queue prevents stacking pauses
- Green/red color coding consistent with existing system

### Story FIX-5: Single Stock Per Round

As a player, I want to focus on a single stock each round instead of choosing between 2-4 stocks, so that the game feels more intense and decision-making is clearer.

**Acceptance Criteria:**
- Each round spawns exactly 1 stock (regardless of tier)
- The left stock sidebar is removed from the UI
- The chart area expands to use the freed-up left space
- All trading actions automatically target the single active stock
- Stock selection keyboard shortcuts (1-4) are removed

### Story FIX-6: Trading Panel Overhaul — Buy/Sell Buttons with Quantity Presets

As a player, I want large BUY and SELL buttons with quantity presets (x5, x10, x15, x25), so that trading feels fast and intuitive with clear visual actions.

**Acceptance Criteria:**
- A trade panel with quantity presets (x5, x10, x15, x25) and large BUY/SELL buttons
- BUY: opens long or auto-covers short position
- SELL: closes long or auto-opens short if no position held
- Buttons are large and clearly labeled (BUY green, SELL red)
- Keyboard shortcuts: B=Buy action, S=Sell action (same smart routing as buttons)
- Trade feedback shows actual action: "BOUGHT", "SHORTED", "COVERED", "SOLD"

### Story FIX-7: Current Position Overlay — Bottom-Left of Chart

As a player, I want to see my current position (shares, direction, P&L) overlaid on the bottom-left of the stock chart, so that I always know my exposure at a glance.

**Acceptance Criteria:**
- Compact position panel overlaid on bottom-left of chart area
- Shows: share count, direction (LONG/SHORT/FLAT), average entry price, unrealized P&L
- P&L updates in real-time; green for profit, red for loss
- Semi-transparent so chart is still visible
- Old right-side PositionPanel is removed

### Story FIX-8: Chart Price Gridlines

As a player, I want horizontal lines drawn across the chart at each price label value, so that I have a visual frame of reference for price levels.

**Acceptance Criteria:**
- Horizontal lines at each Y-axis price label position across full chart width
- Lines update dynamically as price range changes
- Lines are subtle (thin, low-opacity) so they don't obscure the price line
- Lines match the number of price labels (5)

---

## Epic 8: Item/Upgrade System

**Description:** Trading Tools, Intel, Perks implementation and balancing
**Phase:** Phase 2-3 (Weeks 6-10)

### Story 8.1: Trading Tools Framework

As a developer, I want an active ability framework that supports tools with diverse effects, so that new tools can be added easily.

**Acceptance Criteria:**
- Tools have activation triggers (manual use, automatic condition)
- Tool effects can modify trades, prices, or timing
- Tools mapped to Q/E/R keys (or controller face buttons)
- Framework supports per-round usage limits

### Story 8.2: Core Trading Tools

As a player, I want essential trading tools (Stop-Loss, Limit Order, Speed Trader, Flash Trade, Leverage 2x), so that I have strategic options during trading.

**Acceptance Criteria:**
- Stop-Loss: auto-sell if stock drops below threshold ($150, Common)
- Limit Order: auto-buy at target price ($150, Common)
- Speed Trader: 50% faster trade execution ($200, Common)
- Flash Trade: 3 instant rapid trades ($200, Common)
- Leverage 2x: double next trade size and outcome ($250, Uncommon)

### Story 8.3: Advanced Trading Tools

As a player, I want powerful rare tools (Dark Pool, Options Contract, Algo Bot, Portfolio Hedge, Margin Boost), so that late-game builds feel impactful.

**Acceptance Criteria:**
- Margin Boost: 50% more margin for shorting ($300, Uncommon)
- Options Contract: lock buy/sell price for later ($350, Uncommon)
- Portfolio Hedge: 25% loss reduction for one round ($300, Uncommon)
- Dark Pool: one guaranteed mid-price trade per round ($400, Rare)
- Algorithmic Bot: auto-executes simple buy-dip strategy ($500, Rare)

### Story 8.4: Market Intel Items

As a player, I want information advantage items that reveal hidden market data, so that I can make better-informed trades.

**Acceptance Criteria:**
- Analyst Report: reveals trend direction for one stock ($100, Common)
- Insider Tip: preview one upcoming event ($200, Common)
- Earnings Calendar: know when earnings fire ($150, Common)
- Sector Forecast: reveals outperforming sector ($250, Uncommon)
- Price Floor/Ceiling Intel: shows extreme prices ($300 each, Uncommon)
- Short Interest Data: reveals squeeze likelihood ($200, Uncommon)
- Market Maker Feed: real-time buy/sell volume ($400, Rare)
- Crystal Ball: preview chart shape without values ($500, Rare)
- Wiretap: know ALL events in advance ($600, Legendary)

### Story 8.5: Passive Perks

As a player, I want permanent run modifiers that compound across rounds, so that my build grows more powerful over time.

**Acceptance Criteria:**
- Interest Accrual: 5% interest on unspent cash per round ($200, Common)
- Volume Discount: 10% cheaper trades, stacks ($150, Common)
- Market Intuition: events telegraphed 3s earlier ($200, Common)
- Dividend Income: held stocks generate passive income ($250, Uncommon)
- Risk Appetite: targets reduced 10%, stacks 3x ($300, Uncommon)
- Portfolio Insurance: first loss each round halved ($350, Uncommon)
- Compound Interest: 15% profit bonus at round end ($400, Rare)
- Golden Parachute: survive one Margin Call ($500, Rare)
- Wolf Instinct: 2-second price preview at round start ($450, Rare)
- Master of the Universe: 25% shop discount ($600, Legendary)

### Story 8.6: Item Balance and Synergy Tracking

As a developer, I want to track item pick rates and win rates per item, so that I can identify overpowered combos and underused items.

**Acceptance Criteria:**
- Log which items are picked each run
- Track win rate per item ownership
- Flag items with <5% pick rate for rework
- Flag combo win rates that exceed baseline by >2x

---

## Epic 9: Meta-Progression

**Description:** Reputation, office upgrades, unlocks, save/load
**Phase:** Phase 3 (Weeks 9-12)

### Story 9.1: Reputation Currency

As a player, I want to earn Reputation from every run proportional to my performance, so that even failed runs feel productive.

**Acceptance Criteria:**
- Reputation earned: 10 + (5 x rounds completed) on loss
- Reputation earned: 100 + profit bonus on win
- Reputation displayed on meta-hub and run summary screens

### Story 9.2: Office Visual Progression

As a player, I want my office to visually upgrade from Cubicle to Desk to Office as I spend Reputation, so that I feel a sense of long-term progress.

**Acceptance Criteria:**
- Tier 1 Cubicle (start): cramped desk, CRT monitor, fluorescent lighting
- Tier 2 Desk (500 Rep): open-plan floor, dual monitors, city view
- Tier 3 Office (2000 Rep): private corner office, triple monitors, skyline view
- Each tier has unique ambient audio
- Office is the meta-hub screen between runs

### Story 9.3: Stock Unlocks

As a player, I want to unlock 5-8 new stock instruments with Reputation, so that future runs have more variety.

**Acceptance Criteria:**
- 5-8 new stocks gated behind Reputation thresholds
- Unlocked stocks appear in the available pool for future runs
- New stocks add strategic options (new sectors, behaviors)

### Story 9.4: Shop Item Unlocks

As a player, I want to unlock 5-8 new shop items with Reputation, so that my build options expand over time.

**Acceptance Criteria:**
- 5-8 new items across all three categories
- Gated behind Reputation thresholds
- Unlocked items enter the draft shop pool for future runs

### Story 9.5: Broker Perks

As a player, I want small persistent boosts purchased with Reputation, so that the game gets slightly easier as I progress.

**Acceptance Criteria:**
- 3-5 Broker Perks available (e.g., +5% starting capital, +1 trade slot, faster execution)
- Hard-capped to prevent trivialization
- Purchased with Reputation on meta-hub

### Story 9.6: Save/Load System

As a player, I want my meta-progression saved to disk and loaded on startup, so that my progress persists between sessions.

**Acceptance Criteria:**
- JSON serialization of all meta-progression data
- Saved to Application.persistentDataPath
- Loads on startup, saves on changes
- Handles missing/corrupted save files gracefully

---

## Epic 10: UI/UX Polish

**Description:** HUD layout, animations, juice, screen transitions
**Phase:** Phase 3 (Weeks 11-12)

### Story 10.1: Trade Execution Feedback

As a player, I want satisfying visual and audio feedback when executing trades, so that every action feels responsive.

**Acceptance Criteria:**
- Brief screen pulse on trade execution
- Paper-shuffle/cash register sound effect
- Cash counter animates on change
- Profitable sells trigger green flash and number pop
- Losses trigger subtle red vignette

### Story 10.2: Big Win Celebrations

As a player, I want dramatic celebrations when I greatly exceed the profit target, so that exceptional performance feels rewarding.

**Acceptance Criteria:**
- Trigger when profit exceeds 2x target
- Champagne-cork sound effect
- Confetti particles
- Brief slow-motion effect on chart

### Story 10.3: Margin Call Drama

As a player, I want a dramatic Margin Call sequence when I fail, so that losing feels devastating but cinematic.

**Acceptance Criteria:**
- Red flash on screen
- Phone-ringing sound effect
- Screen cracks visual effect
- Slow-motion collapse of portfolio value number
- Transition to run summary

### Story 10.4: Chart Visual Polish

As a player, I want the price chart to feel alive with glowing effects and event pulses, so that the trading interface is visually engaging.

**Acceptance Criteria:**
- Price line has glowing head with brief trail
- Event spikes accompanied by visual pulses on chart background
- Volume spikes shown as subtle bar height increases below chart
- URP post-processing: bloom for neon, vignette for drama, chromatic aberration for crashes

### Story 10.5: Screen Transitions

As a player, I want smooth transitions between game phases, so that the flow feels polished and professional.

**Acceptance Criteria:**
- Smooth transition: Meta-hub → Market Open → Trading → Market Close → Shop → next round
- Instant restart flow: run ends → meta-hub → new run in under 10 seconds
- No jarring cuts between phases

### Story 10.6: Tutorial / First Run

As a player, I want a light guided experience on my first run, so that I learn the core mechanics without a lengthy tutorial.

**Acceptance Criteria:**
- Guided first round pointing out key UI elements
- Teach buy/sell/short in context
- Show profit target and margin call concept
- Light touch — not a lengthy tutorial

---

## Epic 11: Audio

**Description:** Music integration, SFX, dynamic audio system, ambient
**Phase:** Phase 3 (Weeks 11-12)

### Story 11.1: Dynamic Music System

As a player, I want a synthwave soundtrack that responds to market state, so that the audio enhances the gameplay tension.

**Acceptance Criteria:**
- Calm ambient during stable periods
- Driving beat during high volatility
- Distorted/tense during crashes
- Triumphant flourish when hitting profit target
- Distinct boss-fight-energy track for Act 4

### Story 11.2: Trading Sound Effects

As a player, I want satisfying mechanical sounds for all trading actions, so that the interface feels tactile.

**Acceptance Criteria:**
- Cash register click / stamp thud for trade execution
- Digital blips for price changes
- Alarm sounds for events
- Distinctive margin-call phone ring
- Champagne cork for big wins

### Story 11.3: Ambient Soundscapes

As a player, I want unique ambient audio for each office tier, so that the meta-hub reflects my progression.

**Acceptance Criteria:**
- Cubicle: office chatter, keyboard clacking, distant phones
- Desk: trading floor noise, stock ticker sounds
- Office: muffled floor, personal phone line, soft jazz
- Trading phase has subtle trading-floor ambience beneath music

### Story 11.4: Event Audio Cues

As a player, I want distinct audio cues for different market events, so that I can react by sound even when focused on the chart.

**Acceptance Criteria:**
- Unique sound per event category (earnings, crash, sector rotation, etc.)
- Sounds are distinct and learnable
- Volume balanced to not overwhelm music

---

## Epic 12: Ship Prep

**Description:** Steam integration, controller support, settings, QA, build
**Phase:** Phase 4 (Weeks 13-16)

### Story 12.1: Controller Support

As a player, I want full gamepad support including Steam Deck verification, so that I can play comfortably with a controller.

**Acceptance Criteria:**
- Full input mapping per GDD section 6.3 (RT=Buy, LT=Sell, RB=Short, etc.)
- All UI navigable with controller
- Steam Deck verified layout

### Story 12.2: Steam Integration

As a player, I want Steam achievements and cloud saves, so that my progress is tracked and backed up.

**Acceptance Criteria:**
- 5-10 achievements for key milestones
- Cloud save support for meta-progression
- Store page assets (screenshots, descriptions)

### Story 12.3: Settings Menu

As a player, I want a settings menu for volume, resolution, and accessibility, so that I can configure the game to my preferences.

**Acceptance Criteria:**
- Volume controls (music, SFX, ambient)
- Screen resolution and display mode
- Accessibility options (colorblind modes, text size)

### Story 12.4: Performance Optimization

As a developer, I want the game to run at a consistent 60fps on target hardware, so that the trading experience feels smooth.

**Acceptance Criteria:**
- Profile and optimize chart rendering
- Particle effect budget maintained
- No frame drops during high-event rounds
- Memory usage within acceptable bounds

### Story 12.5: Balance Pass

As a developer, I want a data-driven balance pass on difficulty, items, and economy, so that the game feels fair and engaging.

**Acceptance Criteria:**
- Analyze playtest data: win rates, round-reached distribution, profit per round
- Adjust margin call targets based on actual player performance
- Fix exploits and degenerate item strategies
- Target win rate: 10-15% experienced early, 25-30% with unlocks

### Story 12.6: Final QA and Build

As a developer, I want comprehensive QA testing and a release build, so that the game launches without critical bugs.

**Acceptance Criteria:**
- Full playthrough testing across all acts
- Edge case testing (zero cash, max items, all events)
- Build for Steam submission
- No critical or major bugs remaining
