using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Audio
{
    [TestFixture]
    public class AudioClipLibraryTests
    {
        private AudioClipLibrary _lib;

        [SetUp]
        public void SetUp()
        {
            _lib = new AudioClipLibrary();
        }

        [Test]
        public void TryGetClip_ReturnsNull_WhenLibraryEmpty()
        {
            _lib.BuildLookup();
            Assert.IsNull(_lib.TryGetClip("BuySuccess"));
        }

        [Test]
        public void TryGetClip_ReturnsNull_WhenNameNotFound()
        {
            _lib.BuildLookup();
            Assert.IsNull(_lib.TryGetClip("NonExistentClip"));
        }

        [Test]
        public void TryGetClip_ReturnsNull_BeforeBuildLookup()
        {
            Assert.IsNull(_lib.TryGetClip("BuySuccess"));
        }

        [Test]
        public void TryGetClip_ReturnsClip_WhenFieldPopulated()
        {
            var clip = AudioClip.Create("test", 1, 1, 44100, false);
            _lib.BuySuccess = clip;
            _lib.BuildLookup();

            Assert.AreSame(clip, _lib.TryGetClip("BuySuccess"));
        }

        [Test]
        public void BuildLookup_IncludesAllPopulatedFields()
        {
            var clip1 = AudioClip.Create("clip1", 1, 1, 44100, false);
            var clip2 = AudioClip.Create("clip2", 1, 1, 44100, false);
            _lib.BuySuccess = clip1;
            _lib.SellProfit = clip2;
            _lib.BuildLookup();

            Assert.AreSame(clip1, _lib.TryGetClip("BuySuccess"));
            Assert.AreSame(clip2, _lib.TryGetClip("SellProfit"));
        }

        [Test]
        public void BuildLookup_ExcludesNullFields()
        {
            _lib.BuySuccess = AudioClip.Create("test", 1, 1, 44100, false);
            // SellProfit is null
            _lib.BuildLookup();

            Assert.IsNotNull(_lib.TryGetClip("BuySuccess"));
            Assert.IsNull(_lib.TryGetClip("SellProfit"));
        }

        [Test]
        public void PopulateFromEntries_MapsSnakeCaseToPascalCase()
        {
            var clip = AudioClip.Create("buy_success", 1, 1, 44100, false);
            var entries = new[]
            {
                new AudioClipHolder.AudioClipEntry { Name = "buy_success", Clip = clip }
            };

            _lib.PopulateFromEntries(entries);

            Assert.AreSame(clip, _lib.BuySuccess);
            Assert.AreSame(clip, _lib.TryGetClip("BuySuccess"));
        }

        [Test]
        public void PopulateFromEntries_HandlesNameOverrides()
        {
            var clip = AudioClip.Create("short_cashout_window", 1, 1, 44100, false);
            var entries = new[]
            {
                new AudioClipHolder.AudioClipEntry { Name = "short_cashout_window", Clip = clip }
            };

            _lib.PopulateFromEntries(entries);

            Assert.AreSame(clip, _lib.ShortCashoutWindowOpen);
        }

        [Test]
        public void PopulateFromEntries_HandlesDoubleUnderscoreInFileName()
        {
            var clip = AudioClip.Create("event_popup_dismiss__down", 1, 1, 44100, false);
            var entries = new[]
            {
                new AudioClipHolder.AudioClipEntry { Name = "event_popup_dismiss__down", Clip = clip }
            };

            _lib.PopulateFromEntries(entries);

            Assert.AreSame(clip, _lib.EventPopupDismissDown);
        }

        [Test]
        public void PopulateFromEntries_SkipsNullClipEntries()
        {
            var entries = new[]
            {
                new AudioClipHolder.AudioClipEntry { Name = "buy_success", Clip = null }
            };

            _lib.PopulateFromEntries(entries);

            Assert.IsNull(_lib.BuySuccess);
        }

        [Test]
        public void PopulateFromEntries_HandlesNullArray()
        {
            Assert.DoesNotThrow(() => _lib.PopulateFromEntries(null));
        }

        [Test]
        public void PopulateFromEntries_HandlesNumericSegments()
        {
            var clip = AudioClip.Create("timer_warning_15s", 1, 1, 44100, false);
            var entries = new[]
            {
                new AudioClipHolder.AudioClipEntry { Name = "timer_warning_15s", Clip = clip }
            };

            _lib.PopulateFromEntries(entries);

            Assert.AreSame(clip, _lib.TimerWarning15s);
        }

        [Test]
        public void AllPublicFieldsAreAudioClipType()
        {
            var fields = typeof(AudioClipLibrary).GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(AudioClip))
                    continue;
                // Non-AudioClip public fields are unexpected
                Assert.Fail($"Unexpected public field type: {field.Name} is {field.FieldType}");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // UI Audio Clip Mappings (Story: hover/click audio integration)
        // Verify that every new sound file maps to the correct library field.
        // A rename of either the .mp3 or the field name will break these.
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void PopulateFromEntries_Maps_relic_hover_to_RelicHover()
        {
            var clip = AudioClip.Create("relic_hover", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "relic_hover", Clip = clip } });
            Assert.AreSame(clip, _lib.RelicHover);
        }

        [Test]
        public void PopulateFromEntries_Maps_ui_button_hover_to_UiButtonHover()
        {
            var clip = AudioClip.Create("ui_button_hover", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "ui_button_hover", Clip = clip } });
            Assert.AreSame(clip, _lib.UiButtonHover);
        }

        [Test]
        public void PopulateFromEntries_Maps_ui_tab_switch_to_UiTabSwitch()
        {
            var clip = AudioClip.Create("ui_tab_switch", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "ui_tab_switch", Clip = clip } });
            Assert.AreSame(clip, _lib.UiTabSwitch);
        }

        [Test]
        public void PopulateFromEntries_Maps_ui_navigate_to_UiNavigate()
        {
            var clip = AudioClip.Create("ui_navigate", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "ui_navigate", Clip = clip } });
            Assert.AreSame(clip, _lib.UiNavigate);
        }

        [Test]
        public void PopulateFromEntries_Maps_ui_cancel_to_UiCancel()
        {
            var clip = AudioClip.Create("ui_cancel", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "ui_cancel", Clip = clip } });
            Assert.AreSame(clip, _lib.UiCancel);
        }

        [Test]
        public void PopulateFromEntries_Maps_stats_count_up_to_StatsCountUp()
        {
            var clip = AudioClip.Create("stats_count_up", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "stats_count_up", Clip = clip } });
            Assert.AreSame(clip, _lib.StatsCountUp);
        }

        [Test]
        public void PopulateFromEntries_Maps_results_dismiss_to_ResultsDismiss()
        {
            var clip = AudioClip.Create("results_dismiss", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "results_dismiss", Clip = clip } });
            Assert.AreSame(clip, _lib.ResultsDismiss);
        }

        [Test]
        public void PopulateFromEntries_Maps_profit_popup_to_ProfitPopup()
        {
            var clip = AudioClip.Create("profit_popup", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "profit_popup", Clip = clip } });
            Assert.AreSame(clip, _lib.ProfitPopup);
        }

        [Test]
        public void PopulateFromEntries_Maps_loss_popup_to_LossPopup()
        {
            var clip = AudioClip.Create("loss_popup", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "loss_popup", Clip = clip } });
            Assert.AreSame(clip, _lib.LossPopup);
        }

        [Test]
        public void PopulateFromEntries_Maps_rep_earned_to_RepEarned()
        {
            var clip = AudioClip.Create("rep_earned", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "rep_earned", Clip = clip } });
            Assert.AreSame(clip, _lib.RepEarned);
        }

        [Test]
        public void PopulateFromEntries_Maps_streak_milestone_to_StreakMilestone()
        {
            var clip = AudioClip.Create("streak_milestone", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "streak_milestone", Clip = clip } });
            Assert.AreSame(clip, _lib.StreakMilestone);
        }

        [Test]
        public void PopulateFromEntries_Maps_token_launch_to_TokenLaunch()
        {
            var clip = AudioClip.Create("token_launch", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "token_launch", Clip = clip } });
            Assert.AreSame(clip, _lib.TokenLaunch);
        }

        [Test]
        public void PopulateFromEntries_Maps_token_land_to_TokenLand()
        {
            var clip = AudioClip.Create("token_land", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "token_land", Clip = clip } });
            Assert.AreSame(clip, _lib.TokenLand);
        }

        [Test]
        public void PopulateFromEntries_Maps_token_burst_to_TokenBurst()
        {
            var clip = AudioClip.Create("token_burst", 1, 1, 44100, false);
            _lib.PopulateFromEntries(new[] { new AudioClipHolder.AudioClipEntry { Name = "token_burst", Clip = clip } });
            Assert.AreSame(clip, _lib.TokenBurst);
        }
    }
}
