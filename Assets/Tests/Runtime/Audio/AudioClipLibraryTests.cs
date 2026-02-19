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
    }
}
