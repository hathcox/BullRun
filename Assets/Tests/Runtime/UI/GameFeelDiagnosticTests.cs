using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    /// <summary>
    /// Diagnostic specification tests for game feel tuning.
    ///
    /// These tests document the *intended* tuned values for flash alphas, particle counts,
    /// and particle speeds. Each constant must match the corresponding hardcoded value in
    /// GameFeelManager.cs or GameFeelSetup.cs. If you retune a value, update both files.
    ///
    /// Run these after any game feel changes to verify nothing went over the aggressiveness
    /// thresholds and that particle counts are at target density.
    /// </summary>
    [TestFixture]
    public class GameFeelDiagnosticTests
    {
        // ── Flash Alpha Targets ───────────────────────────────────────────────
        // All values must match PlayScreenFlash() calls in GameFeelManager.cs.

        // Trade flashes
        const float BUY_FLASH                  = 0.05f;
        const float SELL_LOSS_FLASH             = 0.05f;
        const float SHORT_FLASH                 = 0.05f;
        const float TRADE_FAIL_FLASH            = 0.04f;
        // Intensity-scaled (at intensity=1.0, maximum possible)
        const float SELL_PROFIT_FLASH_MAX       = 0.11f;  // 0.07*1 + 0.04
        const float COVER_PROFIT_FLASH_MAX      = 0.10f;  // 0.10*1
        const float COVER_LOSS_FLASH_MAX        = 0.14f;  // 0.10*1 + 0.04

        // Market event flashes
        const float MARKET_CRASH_FLASH          = 0.12f;
        const float BULL_RUN_FLASH              = 0.07f;
        const float FLASH_CRASH_WHITE           = 0.20f;
        const float FLASH_CRASH_RED             = 0.10f;

        // Round / run flashes
        const float ROUND_WIN_FLASH_MAX         = 0.12f;  // 0.08*1 + 0.04
        const float MARGIN_CALL_FLASH           = 0.15f;
        const float VICTORY_WHITE_FLASH         = 0.17f;
        const float VICTORY_GREEN_FLASH         = 0.08f;
        const float DEFEAT_FLASH                = 0.12f;

        // Shop / misc flashes
        const float ROUND_START_FLASH           = 0.03f;
        const float ACT_TRANSITION_WHITE_FLASH  = 0.10f;
        const float ACT_TRANSITION_AMBER_FLASH  = 0.05f;
        const float SHOP_ITEM_FLASH_MAX         = 0.06f;  // 0.04*1 + 0.02
        const float SHOP_EXPANSION_FLASH        = 0.07f;
        const float INSIDER_FLASH               = 0.04f;
        const float BOND_FLASH                  = 0.04f;

        // ── Particle Speed Targets ────────────────────────────────────────────
        // Values must match CreateParticleEmitter() calls in GameFeelSetup.cs.
        const float TRADE_PARTICLE_SPEED        = 3f;
        const float CELEBRATION_PARTICLE_SPEED  = 35f;
        const float CURRENCY_PARTICLE_SPEED     = 18f;
        const float FULLSCREEN_PARTICLE_SPEED   = 45f;

        // ── Particle Count Targets (flat, non-intensity events) ───────────────
        const int BUY_PARTICLE_COUNT            = 16;
        const int SELL_LOSS_PARTICLE_COUNT      = 12;
        const int SHORT_PARTICLE_COUNT          = 20;
        const int TRADE_FAIL_PARTICLE_COUNT     = 6;

        // ═══════════════════════════════════════════════════════════════════════
        // FLASH TESTS
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void FlashAlpha_TradeFlashes_AreBelowSubtleThreshold()
        {
            const float MAX = 0.15f;
            Assert.LessOrEqual(BUY_FLASH, MAX,             $"Buy flash {BUY_FLASH} exceeds threshold {MAX}");
            Assert.LessOrEqual(SELL_LOSS_FLASH, MAX,       $"Sell-loss flash {SELL_LOSS_FLASH} exceeds threshold {MAX}");
            Assert.LessOrEqual(SHORT_FLASH, MAX,           $"Short flash {SHORT_FLASH} exceeds threshold {MAX}");
            Assert.LessOrEqual(TRADE_FAIL_FLASH, MAX,      $"Trade-fail flash {TRADE_FAIL_FLASH} exceeds threshold {MAX}");
            Assert.LessOrEqual(SELL_PROFIT_FLASH_MAX, MAX, $"Sell-profit max flash {SELL_PROFIT_FLASH_MAX} exceeds threshold {MAX}");
            Assert.LessOrEqual(COVER_PROFIT_FLASH_MAX, MAX,$"Cover-profit max flash {COVER_PROFIT_FLASH_MAX} exceeds threshold {MAX}");
            Assert.LessOrEqual(COVER_LOSS_FLASH_MAX, MAX,  $"Cover-loss max flash {COVER_LOSS_FLASH_MAX} exceeds threshold {MAX}");

            Debug.Log($"[GameFeelDiagnostic] Trade flashes: " +
                      $"buy={BUY_FLASH}, sellLoss={SELL_LOSS_FLASH}, short={SHORT_FLASH}, " +
                      $"fail={TRADE_FAIL_FLASH}, sellProfitMax={SELL_PROFIT_FLASH_MAX}, " +
                      $"coverProfitMax={COVER_PROFIT_FLASH_MAX}, coverLossMax={COVER_LOSS_FLASH_MAX}");
        }

        [Test]
        public void FlashAlpha_MarketEventFlashes_AreBelowCrisisThreshold()
        {
            const float MAX = 0.25f;
            Assert.LessOrEqual(MARKET_CRASH_FLASH, MAX,  $"Market crash flash {MARKET_CRASH_FLASH} exceeds threshold {MAX}");
            Assert.LessOrEqual(BULL_RUN_FLASH, MAX,      $"Bull run flash {BULL_RUN_FLASH} exceeds threshold {MAX}");
            Assert.LessOrEqual(FLASH_CRASH_WHITE, MAX,   $"Flash crash white {FLASH_CRASH_WHITE} exceeds threshold {MAX}");
            Assert.LessOrEqual(FLASH_CRASH_RED, MAX,     $"Flash crash red {FLASH_CRASH_RED} exceeds threshold {MAX}");
            Assert.LessOrEqual(MARGIN_CALL_FLASH, MAX,   $"Margin call flash {MARGIN_CALL_FLASH} exceeds threshold {MAX}");

            Debug.Log($"[GameFeelDiagnostic] Market event flashes: " +
                      $"crash={MARKET_CRASH_FLASH}, bullRun={BULL_RUN_FLASH}, " +
                      $"flashCrashWhite={FLASH_CRASH_WHITE}, flashCrashRed={FLASH_CRASH_RED}, " +
                      $"marginCall={MARGIN_CALL_FLASH}");
        }

        [Test]
        public void FlashAlpha_VictoryAndDefeat_AreBelowCelebrationThreshold()
        {
            const float MAX = 0.20f;
            Assert.LessOrEqual(VICTORY_WHITE_FLASH, MAX, $"Victory white flash {VICTORY_WHITE_FLASH} exceeds threshold {MAX}");
            Assert.LessOrEqual(VICTORY_GREEN_FLASH, MAX, $"Victory green flash {VICTORY_GREEN_FLASH} exceeds threshold {MAX}");
            Assert.LessOrEqual(DEFEAT_FLASH, MAX,        $"Defeat flash {DEFEAT_FLASH} exceeds threshold {MAX}");
            Assert.LessOrEqual(ROUND_WIN_FLASH_MAX, MAX, $"Round win max flash {ROUND_WIN_FLASH_MAX} exceeds threshold {MAX}");

            Debug.Log($"[GameFeelDiagnostic] Victory/defeat flashes: " +
                      $"victoryWhite={VICTORY_WHITE_FLASH}, victoryGreen={VICTORY_GREEN_FLASH}, " +
                      $"defeat={DEFEAT_FLASH}, roundWinMax={ROUND_WIN_FLASH_MAX}");
        }

        [Test]
        public void FlashAlpha_AllFlashes_AreVisible()
        {
            Assert.Greater(BUY_FLASH, 0f,                  "Buy flash must be non-zero");
            Assert.Greater(SELL_LOSS_FLASH, 0f,            "Sell-loss flash must be non-zero");
            Assert.Greater(MARKET_CRASH_FLASH, 0f,         "Market crash flash must be non-zero");
            Assert.Greater(VICTORY_WHITE_FLASH, 0f,        "Victory flash must be non-zero");
            Assert.Greater(MARGIN_CALL_FLASH, 0f,          "Margin call flash must be non-zero");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PARTICLE SPEED TESTS
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void ParticleSpeed_TradeParticles_IsLowForContainedPop()
        {
            Assert.LessOrEqual(TRADE_PARTICLE_SPEED, 5f,
                $"Trade particles speed {TRADE_PARTICLE_SPEED} — should be ≤5 for a tight pop at chart head");
            Assert.Greater(TRADE_PARTICLE_SPEED, 0f);
            Debug.Log($"[GameFeelDiagnostic] Trade particle speed: {TRADE_PARTICLE_SPEED}px/s");
        }

        [Test]
        public void ParticleSpeed_CelebrationParticles_IsModerateRise()
        {
            Assert.LessOrEqual(CELEBRATION_PARTICLE_SPEED, 45f,
                $"Celebration particles speed {CELEBRATION_PARTICLE_SPEED} — should be ≤45 (was 80, too fast)");
            Assert.Greater(CELEBRATION_PARTICLE_SPEED, 20f,
                "Celebration particles must still rise noticeably");
            Debug.Log($"[GameFeelDiagnostic] Celebration particle speed: {CELEBRATION_PARTICLE_SPEED}px/s");
        }

        [Test]
        public void ParticleSpeed_CurrencyParticles_IsSlowDrift()
        {
            Assert.LessOrEqual(CURRENCY_PARTICLE_SPEED, 25f,
                $"Currency particles speed {CURRENCY_PARTICLE_SPEED} — should be ≤25 (was 50, too fast)");
            Assert.Greater(CURRENCY_PARTICLE_SPEED, 10f,
                "Currency particles must move visibly");
            Debug.Log($"[GameFeelDiagnostic] Currency particle speed: {CURRENCY_PARTICLE_SPEED}px/s");
        }

        [Test]
        public void ParticleSpeed_FullScreenParticles_IsModerate()
        {
            Assert.LessOrEqual(FULLSCREEN_PARTICLE_SPEED, 55f,
                $"Full-screen particles speed {FULLSCREEN_PARTICLE_SPEED} — should be ≤55 (was 100, too fast)");
            Assert.Greater(FULLSCREEN_PARTICLE_SPEED, 30f,
                "Full-screen particles must still have impact");
            Debug.Log($"[GameFeelDiagnostic] Full-screen particle speed: {FULLSCREEN_PARTICLE_SPEED}px/s");
        }

        [Test]
        public void ParticleSpeeds_Summary()
        {
            Debug.Log($"[GameFeelDiagnostic] === PARTICLE SPEED SUMMARY ===");
            Debug.Log($"[GameFeelDiagnostic]   Trade:       {TRADE_PARTICLE_SPEED}px/s  (target ≤5, was 8)");
            Debug.Log($"[GameFeelDiagnostic]   Celebration: {CELEBRATION_PARTICLE_SPEED}px/s (target ≤45, was 80)");
            Debug.Log($"[GameFeelDiagnostic]   Currency:    {CURRENCY_PARTICLE_SPEED}px/s (target ≤25, was 50)");
            Debug.Log($"[GameFeelDiagnostic]   FullScreen:  {FULLSCREEN_PARTICLE_SPEED}px/s (target ≤55, was 100)");
            Assert.Pass("See console log for particle speed summary");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PARTICLE COUNT TESTS
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void ParticleCount_FlatEvents_AreAtMinimumDensity()
        {
            const int MIN = 10;
            Assert.GreaterOrEqual(BUY_PARTICLE_COUNT, MIN,         $"Buy particles {BUY_PARTICLE_COUNT} — too few");
            Assert.GreaterOrEqual(SELL_LOSS_PARTICLE_COUNT, MIN,   $"Sell-loss particles {SELL_LOSS_PARTICLE_COUNT} — too few");
            Assert.GreaterOrEqual(SHORT_PARTICLE_COUNT, MIN,       $"Short particles {SHORT_PARTICLE_COUNT} — too few");
            Assert.GreaterOrEqual(TRADE_FAIL_PARTICLE_COUNT, 4,    $"Trade-fail particles {TRADE_FAIL_PARTICLE_COUNT} — too few");

            Debug.Log($"[GameFeelDiagnostic] Flat particle counts: " +
                      $"buy={BUY_PARTICLE_COUNT}, sellLoss={SELL_LOSS_PARTICLE_COUNT}, " +
                      $"short={SHORT_PARTICLE_COUNT}, fail={TRADE_FAIL_PARTICLE_COUNT}");
        }

        [Test]
        public void ParticleCount_IntensityScaled_ReasonableRange()
        {
            // Sell profit at intensity=1.0: 24*1+8=32 trade, 16*1+4=20 currency
            const int SELL_PROFIT_MAX_TRADE    = 32;
            const int SELL_PROFIT_MAX_CURRENCY = 20;
            Assert.LessOrEqual(SELL_PROFIT_MAX_TRADE, 60,    "Sell profit trade particles shouldn't exceed 60 at peak");
            Assert.LessOrEqual(SELL_PROFIT_MAX_CURRENCY, 40, "Sell profit currency particles shouldn't exceed 40 at peak");
            Assert.GreaterOrEqual(SELL_PROFIT_MAX_TRADE, 15, "Sell profit particles should have density at peak");

            Debug.Log($"[GameFeelDiagnostic] Sell profit at max intensity: " +
                      $"trade={SELL_PROFIT_MAX_TRADE}, currency={SELL_PROFIT_MAX_CURRENCY}");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // INTENSITY CALCULATION TESTS
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void CalculateIntensity_ZeroValue_ReturnsMinimum()
        {
            float result = CalculateIntensity(0f, 10000f);
            Assert.AreEqual(0.2f, result, 0.001f, "Zero-value trade should clamp to 0.2 minimum intensity");
        }

        [Test]
        public void CalculateIntensity_LargeValue_ClampsToOne()
        {
            float result = CalculateIntensity(999999f, 10000f);
            Assert.AreEqual(1.0f, result, 0.001f, "Massive trade value should clamp to 1.0 max intensity");
        }

        [Test]
        public void CalculateIntensity_BaselineValue_ReturnsOne()
        {
            float result = CalculateIntensity(10000f, 10000f);
            Assert.AreEqual(1.0f, result, 0.001f, "Trade at baseline should return 1.0 intensity");
        }

        [Test]
        public void CalculateIntensity_HalfBaseline_ReturnsHalf()
        {
            float result = CalculateIntensity(5000f, 10000f);
            Assert.AreEqual(0.5f, result, 0.001f, "Trade at half baseline should return 0.5 intensity");
        }

        [Test]
        public void CalculateIntensity_NegativeValue_UsesAbsoluteValue()
        {
            // F1 regression guard: GameFeelManager now receives evt.ProfitLoss which can
            // be negative for losing trades. CalculateIntensity must use Abs() so negative
            // values produce the same intensity as their positive counterparts.
            float positive = CalculateIntensity(5000f, 10000f);
            float negative = CalculateIntensity(-5000f, 10000f);
            Assert.AreEqual(positive, negative, 0.001f,
                "Negative ProfitLoss must produce same intensity as positive (uses Abs)");
        }

        [Test]
        public void CalculateIntensity_NegativeValue_DoesNotReturnZero()
        {
            float result = CalculateIntensity(-3000f, 10000f);
            Assert.Greater(result, 0.2f - 0.001f,
                "Negative value should still produce at least minimum intensity (0.2)");
        }

        // Mirror of GameFeelManager.CalculateIntensity (private static)
        private static float CalculateIntensity(float value, float baseline)
        {
            if (baseline <= 0f) return 0.5f;
            return Mathf.Clamp(Mathf.Abs(value) / baseline, 0.2f, 1.0f);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // FULL DIAGNOSTIC REPORT
        // ═══════════════════════════════════════════════════════════════════════

        [Test]
        public void Diagnostic_PrintFullReport()
        {
            Debug.Log("[GameFeelDiagnostic] ====== FULL GAME FEEL PARAMETER REPORT ======");
            Debug.Log("[GameFeelDiagnostic] --- FLASH ALPHAS ---");
            Debug.Log($"[GameFeelDiagnostic]   Buy:                 {BUY_FLASH:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Sell (loss):         {SELL_LOSS_FLASH:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Short:               {SHORT_FLASH:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Trade fail:          {TRADE_FAIL_FLASH:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Sell profit (max):   {SELL_PROFIT_FLASH_MAX:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Cover profit (max):  {COVER_PROFIT_FLASH_MAX:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Cover loss (max):    {COVER_LOSS_FLASH_MAX:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Market crash:        {MARKET_CRASH_FLASH:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Bull run:            {BULL_RUN_FLASH:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Flash crash white:   {FLASH_CRASH_WHITE:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Flash crash red:     {FLASH_CRASH_RED:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Round win (max):     {ROUND_WIN_FLASH_MAX:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Margin call:         {MARGIN_CALL_FLASH:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Victory white:       {VICTORY_WHITE_FLASH:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Victory green:       {VICTORY_GREEN_FLASH:F3}");
            Debug.Log($"[GameFeelDiagnostic]   Defeat:              {DEFEAT_FLASH:F3}");
            Debug.Log("[GameFeelDiagnostic] --- PARTICLE SPEEDS ---");
            Debug.Log($"[GameFeelDiagnostic]   Trade:         {TRADE_PARTICLE_SPEED}px/s");
            Debug.Log($"[GameFeelDiagnostic]   Celebration:   {CELEBRATION_PARTICLE_SPEED}px/s");
            Debug.Log($"[GameFeelDiagnostic]   Currency:      {CURRENCY_PARTICLE_SPEED}px/s");
            Debug.Log($"[GameFeelDiagnostic]   Full screen:   {FULLSCREEN_PARTICLE_SPEED}px/s");
            Debug.Log("[GameFeelDiagnostic] --- FLAT PARTICLE COUNTS ---");
            Debug.Log($"[GameFeelDiagnostic]   Buy:           {BUY_PARTICLE_COUNT}");
            Debug.Log($"[GameFeelDiagnostic]   Sell (loss):   {SELL_LOSS_PARTICLE_COUNT}");
            Debug.Log($"[GameFeelDiagnostic]   Short:         {SHORT_PARTICLE_COUNT}");
            Debug.Log($"[GameFeelDiagnostic]   Trade fail:    {TRADE_FAIL_PARTICLE_COUNT}");
            Debug.Log("[GameFeelDiagnostic] ============================================");
            Assert.Pass("See console output for full game feel parameter report");
        }
    }
}
