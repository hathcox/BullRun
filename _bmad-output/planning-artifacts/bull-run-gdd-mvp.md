# BULL RUN
### A Real-Time Stock Trading Roguelike — MVP Development Document

---

| Field | Detail |
|-------|--------|
| **Engine** | Unity 6.3 + URP |
| **Platform** | PC (Steam) |
| **Target Duration** | 16 weeks (~4 months) |
| **Run Length** | 5–10 minutes |
| **Price Point** | $9.99–$14.99 |
| **Genre** | Real-Time Trading Roguelike |

---

## 1. Vision & Elevator Pitch

> **THE PITCH:** Balatro meets Wolf of Wall Street. A real-time stock trading roguelike where you frantically buy, sell, and short stocks on a live chart across escalating market tiers—from penny stocks to blue chips—racing to hit profit targets before you get margin called. Between trading rounds, draft powerful upgrades from Trading Tools, Market Intel, and Passive Perks to build your strategy. Spend run earnings on meta-progression: unlock new instruments, upgrade your office from a cramped cubicle to a penthouse trading floor, and climb the Wall Street ladder. Each run is 5–10 minutes of pure adrenaline. Every run is different.

### 1.1 Design Pillars

**Frantic Real-Time Action.** Every second of the trading phase matters. Prices move, events fire, and you have limited capital and time to make decisions. This is not a spreadsheet—it is an arcade game wearing a suit.

**Roguelike Depth via Compounding Decisions.** Each draft pick between rounds shapes your strategy. Trading Tools change how you interact with the market, Market Intel gives you edges, and Passive Perks compound over rounds. No two runs play the same.

**The Wolf of Wall Street Power Fantasy.** Progression is aspirational. Your workspace transforms from a dim cubicle to a corner office to a penthouse. Numbers get bigger. Wins feel like champagne. Losses feel like a crash. The aesthetic is synthwave-meets-Art-Deco: neon greens and hot pinks against dark navy and gold.

**Accessible Complexity.** The core mechanic is buy low, sell high. A child can understand it. But shorting, event prediction, tool synergies, and escalating targets create a skill ceiling that rewards mastery.

---

## 2. Core Gameplay Loop

### 2.1 Run Structure Overview

Each run consists of **4 Acts** with **2 Rounds per Act**, totaling **8 Rounds**. Each Act corresponds to a market tier with increasing complexity, volatility, and profit targets. A full run takes approximately 7–10 minutes.

| Act | Market Tier | Rounds | Volatility | New Mechanics |
|-----|-------------|--------|------------|---------------|
| Act 1 | Penny Stocks | Rounds 1–2 | Low–Med | Core buy/sell/short |
| Act 2 | Low-Value Stocks | Rounds 3–4 | Medium | Sector events unlock |
| Act 3 | Mid-Value Stocks | Rounds 5–6 | Med–High | Complex instruments |
| Act 4 | Blue Chips | Rounds 7–8 | High | Market manipulation events |

### 2.2 Single Round Flow

Each round follows a strict three-phase structure, mirroring Balatro's hand-then-shop cadence:

#### Phase 1: Market Open (5–10 seconds)

The round begins with a brief market preview. The player sees which stocks are available for this round, a short news headline hinting at price direction, and the profit target they must hit. This is the moment of strategic assessment: which stocks look promising, what is the target, and how should they allocate their limited capital?

#### Phase 2: Trading Phase (45–75 seconds)

The core gameplay. A real-time price chart animates across the screen. Stock prices move based on underlying event systems and randomized volatility. The player executes trades—buying, selling, and shorting—using their available capital. Time pressure creates urgency: the market will close whether you are ready or not. Limited capital means every position matters; you cannot hedge everything.

> **KEY CONSTRAINT: LIMITED CAPITAL**
> The player starts each run with a small cash pool (e.g., $1,000). Buying stocks ties up capital. Selling frees it. Shorting requires margin (a percentage held in reserve). The player must compound gains across rounds to afford larger positions in later acts. This is the Balatro-equivalent of chip management—your resource is your money, and mismanaging it in early rounds means you cannot hit targets in later ones.

#### Phase 3: Market Close & Draft Shop (15–20 seconds)

When the timer expires, all positions are automatically liquidated at current market price. The round's profit or loss is calculated. If the player's total profit for this round falls below the Margin Call target, the run ends immediately.

If the target is met, the player enters the Draft Shop. Three upgrade slots are presented—one from each category: Trading Tools, Market Intel, and Passive Perks. The player may purchase any combination they can afford using their accumulated cash. Unspent cash carries forward as trading capital for the next round. This creates a tension: spend on upgrades to be more powerful, or hoard capital for larger trades.

### 2.3 Margin Call: The Failure State

Each round has an escalating profit target. Early rounds require modest gains; later rounds demand substantial returns. If the player fails to meet the target in any single round, they receive a **MARGIN CALL** and the run ends. This mirrors Balatro's blind system: the targets are known in advance, creating a clear goal each round and a rising tension curve across the run.

| | Round Target | Act | Cumulative | Difficulty | Scaling |
|---|---|---|---|---|---|
| Round 1 | $200 | Act 1 | $200 | Tutorial | 1.0x |
| Round 2 | $350 | Act 1 | $550 | Easy | 1.0x |
| Round 3 | $600 | Act 2 | $1,150 | Medium | 1.5x |
| Round 4 | $900 | Act 2 | $2,050 | Medium | 1.5x |
| Round 5 | $1,500 | Act 3 | $3,550 | Hard | 2.0x |
| Round 6 | $2,200 | Act 3 | $5,750 | Hard | 2.0x |
| Round 7 | $3,500 | Act 4 | $9,250 | Expert | 2.5x |
| Round 8 | $5,000 | Act 4 | $14,250 | Final | 3.0x |

*Note: These values are starting points for playtesting. The scaling multiplier indicates how much faster targets grow relative to the player's compounding potential. Balancing this curve is the single most important tuning task during development.*

---

## 3. Trading Mechanics

### 3.1 Core Actions

The player has three fundamental actions available at all times during the Trading Phase:

**BUY:** Purchase shares of a stock at the current market price. Costs cash equal to (share price × quantity). Shares are held until sold or until market close (auto-liquidated). The player profits if the price rises above their purchase price.

**SELL:** Sell currently held shares at the current market price. Returns cash to the player's capital pool. Can be executed at any time during the trading phase.

**SHORT:** Bet against a stock. The player borrows shares and sells them at the current price, profiting if the price drops. Shorting requires margin collateral (e.g., 50% of position value held in reserve). Shorts are auto-closed at market close. If the price rises instead, the player loses the difference. Shorting is available from Round 1 as a core mechanic, not an unlock.

### 3.2 Stock Behavior by Tier

| Tier | Price Range | Volatility | Stocks Available | Event Frequency | Behavior |
|------|-------------|------------|------------------|-----------------|----------|
| Penny | $0.10–$5 | Very High | 3–4 | Frequent | Wild swings, pump & dump patterns |
| Low-Value | $5–$50 | High | 3–4 | Moderate | Trend-based with reversals |
| Mid-Value | $50–$500 | Medium | 2–3 | Moderate | Sector correlation, steadier trends |
| Blue Chip | $500–$5,000 | Low–Med | 2–3 | Low freq, high impact | Stable with rare dramatic events |

### 3.3 Price Generation System

Stock prices are generated using a layered system designed to create readable-but-unpredictable charts:

**Base Trend:** Each stock has a hidden directional bias for the round (bullish, bearish, or neutral), determined at round start. This creates the underlying slope of the price line.

**Noise Layer:** Random walk noise is applied on top of the base trend. Amplitude scales with stock tier volatility. This prevents prices from being perfectly predictable even when the trend is known.

**Event Spikes:** Market events inject sudden price movements—sharp spikes or drops—that override the base trend temporarily. Events are the primary driver of dramatic moments and strategic opportunities.

**Mean Reversion:** After event spikes, prices gradually revert toward the trend line. The speed of reversion varies by tier (penny stocks revert slowly or not at all; blue chips revert quickly). This rewards patient players who can identify temporary dislocations.

> **DESIGN NOTE: READABILITY OVER REALISM**
> The price system should produce charts that feel like real stock charts but are actually designed for gameplay clarity. Events should create obvious visual signals (sharp V-shapes, sudden plateaus) that skilled players learn to recognize and exploit. Realism is secondary to fun and readability.

### 3.4 Market Events

Events are the primary source of gameplay variety within rounds. Each round has 2–4 events that fire at randomized intervals during the trading phase. Events affect one or more stocks and create trading opportunities.

| Event Type | Effect | Signal to Player | Tier Availability |
|------------|--------|-------------------|-------------------|
| Earnings Beat | Stock rises 15–30% | Green news banner flash | All tiers |
| Earnings Miss | Stock drops 15–30% | Red news banner flash | All tiers |
| Pump & Dump | Rapid rise then crash | Suspicious volume spike | Penny only |
| SEC Investigation | Gradual decline 20–40% | Warning icon on stock | Penny, Low |
| Sector Rotation | One sector up, another down | Sector highlight shift | Mid, Blue |
| Merger Rumor | Target stock surges | Rumor text crawl | Mid, Blue |
| Market Crash | All stocks drop sharply | Screen shake + alarm | All (rare) |
| Bull Run | All stocks rise steadily | Green tint on chart bg | All (rare) |
| Insider Tip | Hint at next event direction | Whisper UI element | All (via Intel) |
| Flash Crash | Single stock drops then recovers | Rapid red flash | Low, Mid |
| IPO Hype | New stock appears mid-round | Sparkle animation | Stretch goal |
| Short Squeeze | Shorted stock spikes violently | Warning on short positions | All tiers |

---

## 4. Draft Shop System

After each successful round, the player enters the Draft Shop. The shop presents one item from each of three categories. Items are purchased with the player's accumulated cash—the same cash used as trading capital. This creates a core tension: every dollar spent on upgrades is a dollar not available for trading in the next round.

### 4.1 Trading Tools (Active Abilities)

Trading Tools modify how the player interacts with the market. They change the action space—giving new ways to trade or enhancing existing mechanics. These are the equivalent of Balatro's Jokers: the build-defining items.

| Tool | Effect | Cost | Rarity |
|------|--------|------|--------|
| Stop-Loss Order | Auto-sell a stock if it drops below a set threshold, preventing catastrophic losses | $150 | Common |
| Limit Order | Auto-buy a stock when it hits a target price, enabling hands-free entry points | $150 | Common |
| Margin Boost | Increase available margin for shorting by 50%, enabling larger short positions | $300 | Uncommon |
| Speed Trader | Reduce trade execution delay by 50%, enabling faster reactions | $200 | Common |
| Dark Pool Access | Execute one trade per round at guaranteed mid-price (no slippage) | $400 | Rare |
| Options Contract | Pay a premium to lock in a buy/sell price for later in the round | $350 | Uncommon |
| Algorithmic Bot | Automatically executes a simple strategy (e.g., buy dips on strongest stock) | $500 | Rare |
| Leverage (2x) | Double the size of your next trade. Double the profit or double the loss | $250 | Uncommon |
| Portfolio Hedge | Reduce losses on all positions by 25% for one round | $300 | Uncommon |
| Flash Trade | Execute 3 instant trades in rapid succession (ignores normal execution speed) | $200 | Common |

### 4.2 Market Intel (Information Advantages)

Market Intel items give the player information edges—knowledge about upcoming events, stock trends, or hidden values. These reduce uncertainty and reward strategic play. Equivalent to Balatro's Tarot cards: targeted advantages.

| Intel | Effect | Cost | Rarity |
|-------|--------|------|--------|
| Analyst Report | Reveals the base trend direction (bull/bear/neutral) for one stock next round | $100 | Common |
| Insider Tip | Preview one market event that will occur next round | $200 | Common |
| Earnings Calendar | Know exactly when earnings events will fire during next round | $150 | Common |
| Sector Forecast | Reveals which sector will outperform next round | $250 | Uncommon |
| Price Floor Intel | Shows the lowest price a stock will reach next round | $300 | Uncommon |
| Price Ceiling Intel | Shows the highest price a stock will reach next round | $300 | Uncommon |
| Short Interest Data | Reveals if a short squeeze is likely on any stock next round | $200 | Uncommon |
| Market Maker Feed | See real-time buy/sell volume during trading (reveals momentum shifts) | $400 | Rare |
| Crystal Ball | Preview the full price chart shape for one stock (without exact values) | $500 | Rare |
| Wiretap | Know ALL events for next round in advance | $600 | Legendary |

### 4.3 Passive Perks (Persistent Modifiers)

Passive Perks are permanent modifiers that last for the rest of the run. They compound across rounds, making late-game builds feel powerful. Equivalent to Balatro's Planet cards: incremental power growth.

| Perk | Effect | Cost | Rarity |
|------|--------|------|--------|
| Interest Accrual | Earn 5% interest on unspent cash at end of each round | $200 | Common |
| Volume Discount | All trades cost 10% less in fees (stacks) | $150 | Common |
| Dividend Income | Held stocks generate small passive income during trading | $250 | Uncommon |
| Risk Appetite | Profit targets reduced by 10% (permanent, stacks up to 3x) | $300 | Uncommon |
| Market Intuition | Events are telegraphed 3 seconds earlier (visual cue appears sooner) | $200 | Common |
| Portfolio Insurance | First loss each round is halved | $350 | Uncommon |
| Golden Parachute | Survive one Margin Call (consumed on use) | $500 | Rare |
| Compound Interest | All profits earn an additional 15% bonus at round end | $400 | Rare |
| Wolf Instinct | Start each round with a brief price preview (2-second future glimpse) | $450 | Rare |
| Master of the Universe | All shop items cost 25% less for the rest of the run | $600 | Legendary |

> **SHOP DESIGN PRINCIPLE:** The shop should present meaningful choices, not obvious ones. Ideal state: the player agonizes over which item to pick because multiple options synergize with their current build. Bad state: one option is always clearly superior. Item costs should make the spend-vs-save tension real—buying a $500 Legendary means entering the next round with $500 less trading capital.

---

## 5. Meta-Progression System

Meta-progression is what keeps players coming back after a run ends. It provides a sense of long-term advancement and unlocks new strategic options over time. The meta-currency is called **Reputation**, earned from every run proportional to performance (rounds completed, total profit, achievements hit).

### 5.1 Meta-Progression Layers

| System | Type | Examples | Purpose |
|--------|------|----------|---------|
| Office Upgrades | Visual / cosmetic | Cubicle → Open desk → Corner office → Executive suite → Penthouse | Convey status, reward spending, Wolf of Wall Street fantasy |
| Stock Unlocks | Horizontal expansion | New stock tiers, sectors, and individual instruments added to the pool | New strategies emerge each run, replayability |
| Shop Expansion | Horizontal expansion | New Trading Tools, Intel items, and Perks added to the draft pool | Deeper build variety, more synergies |
| Broker Perks | Persistent power (capped) | +5% starting capital, +1 trade slot, slightly faster execution | Smooth difficulty for newer players, hard-capped to prevent trivialization |
| Character Unlocks | Lateral variety | The Day Trader, The Analyst, The Short Seller, The Insider—each with a unique passive | Replay with fundamentally different playstyles |
| Lifestyle Milestones | Achievement-gated | Car, apartment, yacht, helicopter—each with a brief visual moment | Long-term aspirational goals |

### 5.2 Office Progression (The Visual Reward)

The player's office is the meta-hub—the screen they see between runs. It physically transforms as they spend Reputation, serving as a constant visual reminder of progress. Each tier changes the background art, ambient audio, and celebration animations.

| Tier | Rep Cost | Visual Description | Ambient Details |
|------|----------|-------------------|-----------------|
| 1: The Cubicle | Start | Cramped desk, single CRT monitor, fluorescent lighting, coffee-stained mug | Office chatter, keyboard clacking, distant phone rings |
| 2: The Desk | 500 | Open-plan trading floor, dual monitors, better chair, city view through shared windows | Trading floor ambient noise, stock ticker sounds |
| 3: The Office | 2,000 | Private corner office, triple monitors, mahogany desk, skyline view | Muffled trading floor, personal phone line, soft jazz |
| 4: The Suite | 5,000 | Executive floor, Bloomberg terminals, whiskey decanter, personal assistant NPC | Quiet luxury, ice clinking, leather chair creaks |
| 5: The Penthouse | 15,000 | Rooftop trading room, panoramic city views, helicopter pad visible, art on walls | Wind, helicopter blades, champagne corks (on wins) |

### 5.3 MVP Meta-Progression Scope

For the MVP launch, implement the following subset. All other meta-progression systems are post-launch additions.

**Include in MVP:** Reputation currency, 3 office tiers (Cubicle, Desk, Office), 5–8 stock unlocks, 5–8 shop item unlocks, 3–5 Broker Perks (capped). One playable character.

**Post-launch:** Tiers 4–5 (Suite, Penthouse), character unlocks, lifestyle milestones, modular difficulty system, daily challenges, leaderboards.

---

## 6. UI/UX Design

### 6.1 Screen Layout: Trading Phase

The trading phase screen must convey dense information while remaining readable under time pressure. The layout prioritizes the chart as the center of attention, with supporting information in the periphery.

**Center (60% of screen):** The price chart. A real-time line chart drawing itself left-to-right. The current price is shown at the chart head with a glowing indicator. Price axis on the right. Time remaining shown as a progress bar along the bottom of the chart.

**Top Bar:** Cash available | Total Portfolio Value (with % change) | Current Round Profit | Margin Call Target (with progress bar toward it)

**Left Sidebar:** Available stocks listed vertically. Each shows: ticker symbol, current price, % change, mini-sparkline. Clicking/selecting a stock switches the main chart to that stock.

**Right Sidebar:** Current positions. Each held stock shows: shares held, average buy price, current P&L. Short positions shown in a distinct color (hot pink vs. green).

**Bottom Bar:** Active Trading Tools. News ticker crawl showing incoming events. Trade execution buttons (or mapped to controller triggers).

### 6.2 Juice and Feel

The trading interface must feel alive and responsive. Every action should have immediate, satisfying feedback:

**Trade Execution:** Brief screen pulse, paper-shuffle sound effect, cash counter animates. Profitable sells trigger a green flash and number pop. Losses trigger a subtle red vignette.

**Big Wins:** When profit exceeds 2x the target, trigger a champagne-cork sound, confetti particles, and a brief slow-motion effect on the chart.

**Margin Call:** Dramatic red flash, phone-ringing sound effect, screen cracks, slow-motion collapse of the portfolio value number. This should feel devastating but cinematic.

**Chart Animations:** The price line should have a glowing head that leaves a brief trail. Event spikes should be accompanied by visual pulses on the chart background. Volume spikes shown as subtle bar height increases below the chart.

**Dynamic Music:** Synthwave soundtrack that responds to market state. Calm ambient during stable periods. Driving beat during high volatility. Distorted/tense during crashes. Triumphant flourish when hitting the profit target.

### 6.3 Input Mapping

| Action | Keyboard/Mouse | Controller |
|--------|----------------|------------|
| Buy | Left Click on BUY / Spacebar | Right Trigger (RT) |
| Sell | Left Click on SELL / S key | Left Trigger (LT) |
| Short | Right Click / Shift+Space | Right Bumper (RB) |
| Cover Short | Right Click held stock / Shift+S | Left Bumper (LB) |
| Cycle Stocks | Number keys 1–4 / Mouse click sidebar | D-Pad Left/Right |
| Use Trading Tool | Q / E / R (tool slots) | Face Buttons (A/B/X/Y) |
| Adjust Quantity | Scroll wheel / Arrow keys | D-Pad Up/Down |
| Pause (shop only) | Escape | Start |

---

## 7. Art Direction & Audio

### 7.1 Visual Style

The visual identity blends synthwave aesthetics with Wall Street Art Deco. The palette centers on dark navy backgrounds with neon green for gains, hot pink for losses, and gold for premium/rare elements. Typography should evoke Bloomberg terminals and 1980s financial displays—monospaced numbers for prices, clean sans-serif for UI labels.

The overall look should feel like a high-end trading terminal redesigned by the set designer of Drive or Blade Runner 2049: sleek, dark, with bursts of saturated color that convey information and emotion simultaneously. The goal is a distinctive visual identity that screenshots well and is immediately recognizable on a Steam store page.

### 7.2 Audio Design

**Soundtrack:** 5–8 synthwave/retrowave tracks. Calm tracks for the meta-hub and shop. Driving tracks for trading phases. A distinct boss-fight-energy track for Act 4 Blue Chips. Music should be licensed or commissioned—budget $500–$1,500 for a small indie synthwave artist collaboration or royalty-free pack.

**SFX:** Satisfying mechanical sounds for trade execution (cash register click, stamp thud). Digital blips for price changes. Alarm sounds for events. A distinctive margin-call phone ring. Champagne cork for big wins. These can be sourced from royalty-free libraries and lightly processed.

**Ambient:** Each office tier has a unique ambient soundscape that plays in the meta-hub. The trading phase has subtle trading-floor ambience beneath the music.

---

## 8. Technical Architecture

### 8.1 Unity 6.3 + URP Setup

The project uses Unity 6.3 with the Universal Render Pipeline for 2D rendering with selective post-processing effects (bloom for neon elements, vignette for dramatic moments, chromatic aberration for crashes). The game is entirely 2D with UI-driven gameplay—no 3D models or complex rendering required.

### 8.2 Core Systems

| System | Responsibility | Key Classes |
|--------|---------------|-------------|
| Game State Manager | Manages run state, round transitions, meta-hub flow | GameManager, RunState, RoundState |
| Price Engine | Generates stock prices from trend + noise + events | PriceGenerator, StockData, TrendCurve |
| Trading System | Executes buy/sell/short, manages portfolio, calculates P&L | TradeExecutor, Portfolio, Position |
| Event System | Schedules and fires market events during rounds | EventScheduler, MarketEvent, EventEffects |
| Chart Renderer | Draws real-time price chart with animations | ChartRenderer, LineDrawer, PriceLabel |
| Shop System | Generates draft items, handles purchases | ShopGenerator, ShopItem, ItemDatabase |
| Upgrade System | Applies trading tools, intel, and perks to gameplay | UpgradeManager, ActiveTool, PassivePerk |
| Meta-Progression | Tracks Reputation, unlocks, office tier, persistent data | MetaManager, UnlockTree, SaveData |
| UI Manager | Manages all UI panels, transitions, animations | UIManager, TradingHUD, ShopUI, MetaHubUI |
| Audio Manager | Dynamic music, SFX, ambient based on game state | AudioManager, MusicState, SFXTrigger |
| Save System | Serializes meta-progression to disk | SaveManager, SaveData (JSON serialization) |

### 8.3 Data Architecture

All game data (stocks, events, shop items, upgrade definitions, margin call targets) should be defined in ScriptableObjects for easy iteration. Price generation parameters, event probabilities, and shop item pools should be tunable without code changes. The save system should use JSON serialization for meta-progression data, stored in `Application.persistentDataPath`.

### 8.4 Key Technical Risks

**Chart Performance:** Rendering a smooth, real-time line chart with animations at 60fps. Mitigation: use Unity's LineRenderer or a custom mesh-based approach; profile early. Consider a dedicated chart asset from the Asset Store if custom implementation proves costly.

**Price Generation Balance:** Creating price curves that feel fair but challenging. Mitigation: expose all parameters as ScriptableObjects; build a debug mode that shows underlying trend and events; playtest extensively with data logging.

**Shop Balance:** Ensuring no single item or combo trivializes the game. Mitigation: flag powerful combos during testing; implement rarity tiers to control access; add a synergy tracking spreadsheet from day one.

---

## 9. Development Timeline (16 Weeks)

### Phase 1: Core Loop Prototype (Weeks 1–4)

**Goal:** A playable single round of trading that feels good. If the core 60-second trading loop is not fun by the end of Week 4, the concept needs fundamental rethinking before proceeding.

1. **Week 1:** Unity project setup, URP configuration, basic UI scaffolding. Implement a static price chart that draws a pre-defined curve. Build the BUY/SELL buttons and cash tracker.
2. **Week 2:** Implement the Price Engine (trend + noise). Make the chart draw in real-time. Connect BUY/SELL to actual position tracking. Display P&L in real-time.
3. **Week 3:** Add SHORT mechanic with margin. Add 3–4 stocks with a sidebar selector. Implement the round timer (60 seconds). Add auto-liquidation at market close.
4. **Week 4:** Add 3–4 basic market events (earnings beat/miss, flash crash, bull run). Add the Margin Call target and failure state. First complete playable round. Playtest and iterate.

> **GATE CHECK: END OF PHASE 1**
> At the end of Week 4, the following question must be honestly answered: is one round of trading fun to play? Does the time pressure create genuine tension? Does hitting the profit target feel satisfying? If the answer is no, stop and iterate on the core loop before building anything else. Do not proceed to Phase 2 until the answer is yes.

### Phase 2: Run Structure & Shop (Weeks 5–8)

**Goal:** A complete run from Round 1 to Round 8 (or Margin Call), with the draft shop between rounds and escalating difficulty.

1. **Week 5:** Implement the 4-Act structure with tier transitions. Build the escalating Margin Call target system. Add visual/audio differentiation per tier.
2. **Week 6:** Build the Draft Shop UI. Implement 5 Trading Tools, 5 Intel items, 5 Perks (15 total). Wire shop purchases to actual gameplay effects.
3. **Week 7:** Expand event pool to 10–12 events. Add stock behavior differentiation per tier. Implement the capital compounding flow across rounds (money carries forward).
4. **Week 8:** Run summary screen. Win state (complete Round 8). Full run playtesting. Balance pass on targets, prices, and shop costs. Data logging for balance.

### Phase 3: Meta-Progression & Polish (Weeks 9–12)

**Goal:** The game has a reason to play more than once. Meta-progression, unlocks, and the first layer of visual identity.

1. **Week 9:** Implement Reputation currency. Build the meta-hub screen with 3 office tiers (Cubicle, Desk, Office). Add 5–8 unlockable shop items gated behind Reputation.
2. **Week 10:** Add 5–8 unlockable stock types. Implement 3–5 Broker Perks. Save/load system for meta-progression. Instant restart flow (run ends → meta-hub → new run in under 10 seconds).
3. **Week 11:** Art pass: finalize color palette, chart visuals, UI skin. Implement juice (screen shake, particles, number animations, trade execution feel). Dynamic music system.
4. **Week 12:** SFX integration. Tutorial / first-run experience (light touch: a guided first round, not a lengthy tutorial). Expand total shop item pool to 25–30 items.

### Phase 4: Ship Prep (Weeks 13–16)

**Goal:** The game is ready for Early Access or full launch on Steam.

1. **Week 13:** Extended playtesting. Balance the difficulty curve with real data. Fix exploits and degenerate strategies. Ensure no single item combo breaks the game.
2. **Week 14:** Controller support and Steam Deck verification. Settings menu (volume, screen resolution, accessibility options). Performance optimization pass.
3. **Week 15:** Steam integration (achievements for 5–10 milestones, cloud saves, store page assets). Build trailer footage from gameplay. Write store description.
4. **Week 16:** Final QA. Build submission. Launch.

---

## 10. Scope Management

### 10.1 MVP Feature Set (Must Ship)

| Feature | Scope | Priority |
|---------|-------|----------|
| Core trading (buy/sell/short) | 3 actions, real-time chart, limited capital | P0 — Critical |
| Run structure | 4 acts, 8 rounds, escalating targets | P0 — Critical |
| Margin Call failure state | Miss target = run over | P0 — Critical |
| Draft shop (3 categories) | 25–30 total items across categories | P0 — Critical |
| Market events | 12–15 event types | P0 — Critical |
| Price generation system | Trend + noise + event spikes | P0 — Critical |
| Meta-progression hub | Reputation currency, office upgrades | P0 — Critical |
| Unlockable content | 5–8 stocks, 5–8 shop items | P0 — Critical |
| Broker Perks | 3–5 small persistent boosts | P1 — Important |
| 3 office visual tiers | Cubicle, Desk, Office | P1 — Important |
| Save/load system | JSON-based meta-persistence | P1 — Important |
| Synthwave soundtrack | 5–8 tracks (licensed or commissioned) | P1 — Important |
| SFX and juice | Trade feel, win/loss feedback, events | P1 — Important |
| Tutorial / first run | Guided first round, light touch | P1 — Important |
| Controller support | Full gamepad mapping | P1 — Important |
| Steam integration | Achievements, cloud saves | P2 — Nice to have |

### 10.2 Cut List (Do Not Build for MVP)

These features are explicitly deferred to post-launch updates. Adding any of these to the MVP scope risks the 4-month timeline.

1. Multiple playable characters (add in first content update)
2. Modular difficulty / Heat system (add after first-win data exists)
3. Office tiers 4–5 (Suite, Penthouse)
4. Lifestyle milestones (car, yacht, helicopter)
5. IPO mechanic (stretch goal, not MVP)
6. Daily challenges
7. Leaderboards
8. Multiplayer or async competition
9. Elaborate narrative or cutscenes
10. Localization
11. More than 30 shop items total
12. Options/derivatives trading (complex instruments)

### 10.3 Post-Launch Roadmap (If MVP Succeeds)

| Update | Content | Timeline | Goal |
|--------|---------|----------|------|
| Update 1 | 2 new characters, 10 new shop items, Office tiers 4–5 | Launch + 4 weeks | Retention spike |
| Update 2 | Heat/difficulty system, daily challenges, leaderboards | Launch + 8 weeks | Endgame depth |
| Update 3 | IPO mechanic, lifestyle milestones, new event types | Launch + 12 weeks | Content expansion |
| Update 4 | Options trading, new stock tier, community-requested features | Launch + 16 weeks | Long-tail engagement |

---

## 11. Balance Framework

Balance is the highest-risk area of development. The game must feel challenging but fair—where failure feels like a mistake the player made, not randomness they couldn't control. The following framework provides starting points for tuning.

### 11.1 Core Economy

| Parameter | Starting Value | Tuning Notes |
|-----------|---------------|--------------|
| Starting Capital | $1,000 | Should feel tight in Act 1, comfortable by Act 3 if compounded well |
| Round Duration | 60 seconds | Test 45s and 75s variants; shorter = more frantic, longer = more strategic |
| Stocks per Round | 3–4 (Penny/Low), 2–3 (Mid/Blue) | Fewer stocks = more focused decisions |
| Events per Round | 2–3 (early), 3–4 (late) | Events are the primary driver of price movement |
| Short Margin Requirement | 50% of position value | Lower = more accessible shorting; higher = riskier shorts |
| Shop Item Costs | $100–$600 | Should represent a meaningful fraction of current capital |
| Reputation per Run (loss) | 10 + (5 × rounds completed) | Ensures even failed runs feel productive |
| Reputation per Run (win) | 100 + profit bonus | Winning should feel significantly more rewarding |

### 11.2 Target Win Rate

The ideal win rate for a roguelike is approximately 10–15% for experienced players in the early meta-progression phase, rising to 25–30% once core upgrades are unlocked. New players should expect to reach Act 2–3 consistently within 5–10 runs and achieve their first win within 15–25 runs. Balatro targets a similar curve. Log every run's outcome, round reached, and profit per round from day one of playtesting to track this.

### 11.3 Balance Testing Protocol

Implement a debug overlay (toggled with F1) that shows: the underlying trend direction for each stock, all scheduled events and their timing, the probability distribution of the next price tick, current effective win rate over the last 20 runs, and total money earned vs. spent in the shop. This data is essential for tuning and should be built in Phase 1.

---

## 12. Story Generation Guide

This section maps the design document into agile stories suitable for a Kanban or Scrum board. Stories are organized by epic and prioritized for the development phases outlined in Section 9.

### 12.1 Epic Structure

| Epic | Description | Phase |
|------|-------------|-------|
| E1: Price Engine | Stock price generation, trend system, noise, event integration | Phase 1 (Weeks 1–4) |
| E2: Trading System | Buy/sell/short execution, portfolio management, P&L calculation | Phase 1 (Weeks 1–4) |
| E3: Chart Rendering | Real-time chart drawing, animations, stock selection UI | Phase 1 (Weeks 1–4) |
| E4: Round Management | Timer, market open/close, auto-liquidation, margin call check | Phase 1–2 (Weeks 3–6) |
| E5: Event System | Event scheduling, effects on prices, visual/audio signals | Phase 1–2 (Weeks 4–7) |
| E6: Run Structure | Act/round progression, tier transitions, difficulty scaling | Phase 2 (Weeks 5–8) |
| E7: Draft Shop | Shop UI, item generation, purchase flow, item effect application | Phase 2 (Weeks 6–8) |
| E8: Item/Upgrade System | Trading Tools, Intel, Perks implementation and balancing | Phase 2–3 (Weeks 6–10) |
| E9: Meta-Progression | Reputation, office upgrades, unlocks, save/load | Phase 3 (Weeks 9–12) |
| E10: UI/UX Polish | HUD layout, animations, juice, screen transitions | Phase 3 (Weeks 11–12) |
| E11: Audio | Music integration, SFX, dynamic audio system, ambient | Phase 3 (Weeks 11–12) |
| E12: Ship Prep | Steam integration, controller, settings, QA, build | Phase 4 (Weeks 13–16) |

### 12.2 Sample Stories (Phase 1)

Each story follows the format: *As a [player], I want [action], so that [outcome].* Stories should be small enough to complete in 1–3 days.

| ID | Epic | Story | Estimate |
|----|------|-------|----------|
| S001 | E3: Chart | As a player, I want to see a price line drawing itself in real-time across the screen, so that I can observe price movement | 2 days |
| S002 | E1: Price | As a player, I want stock prices to move with visible trends and random variation, so that I can attempt to predict direction | 2 days |
| S003 | E2: Trading | As a player, I want to click BUY to purchase shares at the current price using my cash, so that I can take a position | 1 day |
| S004 | E2: Trading | As a player, I want to click SELL to liquidate held shares at current price, so that I can realize profits or cut losses | 1 day |
| S005 | E2: Trading | As a player, I want to SHORT a stock, reserving margin, and profit when the price drops, so that I can bet against stocks | 2 days |
| S006 | E3: Chart | As a player, I want to see my portfolio value and cash updating in real-time during trading, so that I know my current status | 1 day |
| S007 | E4: Round | As a player, I want a 60-second timer that counts down during trading, so that I feel time pressure | 0.5 days |
| S008 | E4: Round | As a player, I want all positions auto-liquidated when the timer expires, so that the round resolves cleanly | 1 day |
| S009 | E3: Chart | As a player, I want to switch between 3–4 stocks using a sidebar, so that I can monitor and trade multiple instruments | 1.5 days |
| S010 | E5: Events | As a player, I want market events to fire during trading that spike or crash prices, so that there are moments of opportunity and danger | 2 days |
| S011 | E4: Round | As a player, I want to see a profit target and fail the round if I miss it, so that there are stakes to each round | 1 day |
| S012 | E1: Price | As a developer, I want a debug overlay showing trend direction and event timing, so that I can tune the balance | 1 day |

*Continue generating stories for Phases 2–4 following the same format, using the epic structure and phase breakdown as guides. Each phase should generate approximately 15–25 stories.*

---

## 13. Risks & Mitigations

| Risk | Severity | Description | Mitigation |
|------|----------|-------------|------------|
| Core loop not fun | Critical | The 60-second trading phase does not produce the desired adrenaline or tension | Phase 1 gate check at Week 4. Do not proceed without a fun core loop. Iterate until it works or kill the project. |
| Balance curve wrong | High | Margin Call targets are too easy (boring) or too hard (frustrating) | Log every run from day one. Track win rates, round-reached distribution, and profit per round. Adjust weekly. |
| Scope creep | High | Adding features beyond the MVP cut list | Refer to Section 10.2 ruthlessly. If a feature is on the cut list, it does not get built. No exceptions until MVP ships. |
| Chart rendering perf | Medium | Real-time chart drawing drops below 60fps | Profile in Week 2. Consider Asset Store chart solution if custom implementation is slow. Keep particle effects budgeted. |
| Shop items unbalanced | Medium | Certain combos trivialize the game or certain items are never picked | Track item pick rates and win rates per item. Adjust costs and effects monthly. Remove or rework items with <5% pick rate. |
| Insider Trading competition | Low | Insider Trading launches Feb 18 and captures the niche | Different sub-genre (real-time vs turn-based). Differentiate on pacing and accessibility. Monitor reviews for gaps to fill. |
| Audio budget overrun | Low | Commissioned soundtrack costs exceed budget | Have a fallback plan: royalty-free synthwave packs exist for $50–$200. Commission only the main theme; license the rest. |

---

> *Ship small. Ship fast. Iterate on data.*
>
> *The prototype proves the concept. The data guides the balance. The players define the roadmap.*
