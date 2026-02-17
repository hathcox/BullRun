using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class CRTThemeDataTests
    {
        private const float Tolerance = 0.01f;

        // ── Helper: assert Color channels within tolerance ──────────────

        private static void AssertColorEqual(Color expected, Color actual, string name)
        {
            Assert.AreEqual(expected.r, actual.r, Tolerance, $"{name}.r");
            Assert.AreEqual(expected.g, actual.g, Tolerance, $"{name}.g");
            Assert.AreEqual(expected.b, actual.b, Tolerance, $"{name}.b");
            Assert.AreEqual(expected.a, actual.a, Tolerance, $"{name}.a");
        }

        // ── 3.1  Color value tests ─────────────────────────────────────

        [Test]
        public void Background_MatchesHex050a0a()
        {
            // #050a0a → (5/255, 10/255, 10/255, 1)
            AssertColorEqual(new Color(0.020f, 0.039f, 0.039f, 1f), CRTThemeData.Background, "Background");
        }

        [Test]
        public void Panel_MatchesHex061818_At90Alpha()
        {
            // #061818 @ 90% → (6/255, 24/255, 24/255, 0.9)
            AssertColorEqual(new Color(0.024f, 0.094f, 0.094f, 0.9f), CRTThemeData.Panel, "Panel");
        }

        [Test]
        public void Border_MatchesHex224444()
        {
            // #224444 → (34/255, 68/255, 68/255, 1)
            AssertColorEqual(new Color(0.133f, 0.267f, 0.267f, 1f), CRTThemeData.Border, "Border");
        }

        [Test]
        public void TextHigh_MatchesHex3daa6e()
        {
            // TextHigh = ColorPalette.Green
            AssertColorEqual(ColorPalette.Green, CRTThemeData.TextHigh, "TextHigh");
        }

        [Test]
        public void TextLow_MatchesHex245046()
        {
            // TextLow = ColorPalette.GreenDim
            AssertColorEqual(ColorPalette.GreenDim, CRTThemeData.TextLow, "TextLow");
        }

        [Test]
        public void Warning_MatchesHexcc9400()
        {
            // Warning = ColorPalette.Amber
            AssertColorEqual(ColorPalette.Amber, CRTThemeData.Warning, "Warning");
        }

        [Test]
        public void Danger_MatchesHexe65555()
        {
            // Danger = ColorPalette.Red
            AssertColorEqual(ColorPalette.Red, CRTThemeData.Danger, "Danger");
        }

        [Test]
        public void ButtonBuy_MatchesHex3daa6e()
        {
            // ButtonBuy = ColorPalette.Green
            AssertColorEqual(ColorPalette.Green, CRTThemeData.ButtonBuy, "ButtonBuy");
        }

        [Test]
        public void ButtonSell_MatchesHexe65555()
        {
            // ButtonSell = ColorPalette.Red
            AssertColorEqual(ColorPalette.Red, CRTThemeData.ButtonSell, "ButtonSell");
        }

        [Test]
        public void ButtonShort_MatchesHexcc9400()
        {
            // ButtonShort = ColorPalette.Amber
            AssertColorEqual(ColorPalette.Amber, CRTThemeData.ButtonShort, "ButtonShort");
        }

        // ── 3.2  ApplyLabelStyle tests ─────────────────────────────────

        [Test]
        public void ApplyLabelStyle_Highlight_SetsTextHighColor()
        {
            var go = new GameObject("TestText");
            var text = go.AddComponent<Text>();

            CRTThemeData.ApplyLabelStyle(text, highlight: true);

            AssertColorEqual(CRTThemeData.TextHigh, text.color, "ApplyLabelStyle(highlight=true)");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyLabelStyle_Dim_SetsTextLowColor()
        {
            var go = new GameObject("TestText");
            var text = go.AddComponent<Text>();

            CRTThemeData.ApplyLabelStyle(text, highlight: false);

            AssertColorEqual(CRTThemeData.TextLow, text.color, "ApplyLabelStyle(highlight=false)");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyLabelStyle_NullText_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CRTThemeData.ApplyLabelStyle(null, true));
        }

        // ── 3.3  ApplyPanelStyle tests ─────────────────────────────────

        [Test]
        public void ApplyPanelStyle_SetsPanelColor()
        {
            var go = new GameObject("TestImage");
            var image = go.AddComponent<Image>();

            CRTThemeData.ApplyPanelStyle(image);

            AssertColorEqual(CRTThemeData.Panel, image.color, "ApplyPanelStyle panel color");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyPanelStyle_AddsBorderOutline()
        {
            var go = new GameObject("TestImage");
            var image = go.AddComponent<Image>();

            CRTThemeData.ApplyPanelStyle(image);

            var outline = go.GetComponent<Outline>();
            Assert.IsNotNull(outline, "Outline component should be added");
            AssertColorEqual(CRTThemeData.Border, outline.effectColor, "ApplyPanelStyle border color");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyPanelStyle_NullImage_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CRTThemeData.ApplyPanelStyle(null));
        }
    }
}
