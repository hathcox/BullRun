using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BullRun.Tests.Chart
{
    [TestFixture]
    public class TipOverlayRendererTests
    {
        // Chart bounds matching typical test values
        private const float ChartLeft = -4f;
        private const float ChartRight = 4f;
        private const float ChartBottom = -2f;
        private const float ChartTop = 2f;

        // Padded bounds (10% padding on each side)
        private float PaddedBottom => ChartBottom + (ChartTop - ChartBottom) * 0.1f;
        private float PaddedTop => ChartTop - (ChartTop - ChartBottom) * 0.1f;

        // --- Lifecycle Test Infrastructure ---

        private GameObject _testRoot;

        private struct TestOverlayObjects
        {
            public LineRenderer FloorLine;
            public LineRenderer CeilingLine;
            public MeshFilter ForecastMF;
            public MeshFilter DipMF;
            public MeshFilter PeakMF;
            public LineRenderer ReversalLine;
            public LineRenderer[] EventMarkerLines;
            public Text FloorLabel;
            public Text CeilingLabel;
            public Text ForecastLabel;
            public Text DipLabel;
            public Text PeakLabel;
            public Text ReversalLabel;
            public Text[] EventMarkerLabels;
            public Text ArrowText;
            public Text DirLabel;
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null) Object.DestroyImmediate(_testRoot);
            EventBus.Clear();
        }

        private TipOverlayRenderer CreateTestRenderer(out TestOverlayObjects obj)
        {
            _testRoot = new GameObject("TestRoot");

            // Canvas for Text components
            var canvasGo = new GameObject("TestCanvas");
            canvasGo.transform.SetParent(_testRoot.transform);
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRect = canvasGo.GetComponent<RectTransform>();

            obj = new TestOverlayObjects();

            // LineRenderers
            obj.FloorLine = CreateLR("Floor");
            obj.CeilingLine = CreateLR("Ceiling");
            obj.ReversalLine = CreateLR("Reversal");
            obj.EventMarkerLines = new LineRenderer[ChartVisualConfig.MaxEventTimingMarkers];
            for (int i = 0; i < ChartVisualConfig.MaxEventTimingMarkers; i++)
                obj.EventMarkerLines[i] = CreateLR($"Marker_{i}");

            // MeshFilters
            obj.ForecastMF = CreateMF("Forecast");
            obj.DipMF = CreateMF("Dip");
            obj.PeakMF = CreateMF("Peak");

            // Text labels (parent to canvas)
            obj.FloorLabel = CreateText("FloorLabel", canvasGo.transform);
            obj.CeilingLabel = CreateText("CeilingLabel", canvasGo.transform);
            obj.ForecastLabel = CreateText("ForecastLabel", canvasGo.transform);
            obj.DipLabel = CreateText("DipLabel", canvasGo.transform);
            obj.PeakLabel = CreateText("PeakLabel", canvasGo.transform);
            obj.ReversalLabel = CreateText("ReversalLabel", canvasGo.transform);
            obj.EventMarkerLabels = new Text[ChartVisualConfig.MaxEventTimingMarkers];
            for (int i = 0; i < ChartVisualConfig.MaxEventTimingMarkers; i++)
                obj.EventMarkerLabels[i] = CreateText($"MarkerLabel_{i}", canvasGo.transform);
            obj.ArrowText = CreateText("Arrow", canvasGo.transform);
            obj.DirLabel = CreateText("DirLabel", canvasGo.transform);

            // Set all inactive (mimics ChartSetup behavior)
            obj.FloorLine.gameObject.SetActive(false);
            obj.CeilingLine.gameObject.SetActive(false);
            obj.ForecastMF.gameObject.SetActive(false);
            obj.DipMF.gameObject.SetActive(false);
            obj.PeakMF.gameObject.SetActive(false);
            obj.ReversalLine.gameObject.SetActive(false);
            obj.ArrowText.gameObject.SetActive(false);
            obj.DirLabel.gameObject.SetActive(false);
            obj.FloorLabel.gameObject.SetActive(false);
            obj.CeilingLabel.gameObject.SetActive(false);
            obj.ForecastLabel.gameObject.SetActive(false);
            obj.DipLabel.gameObject.SetActive(false);
            obj.PeakLabel.gameObject.SetActive(false);
            obj.ReversalLabel.gameObject.SetActive(false);
            for (int i = 0; i < ChartVisualConfig.MaxEventTimingMarkers; i++)
            {
                obj.EventMarkerLines[i].gameObject.SetActive(false);
                obj.EventMarkerLabels[i].gameObject.SetActive(false);
            }

            var chartRenderer = new ChartRenderer();
            var bounds = new Rect(ChartLeft, ChartBottom,
                ChartRight - ChartLeft, ChartTop - ChartBottom);

            var renderer = _testRoot.AddComponent<TipOverlayRenderer>();
            renderer.Initialize(
                chartRenderer, bounds, canvasRect,
                obj.FloorLine, obj.CeilingLine,
                obj.ForecastMF, obj.DipMF, obj.PeakMF,
                obj.ReversalLine, obj.EventMarkerLines,
                obj.FloorLabel, obj.CeilingLabel, obj.ForecastLabel,
                obj.DipLabel, obj.PeakLabel, obj.ReversalLabel,
                obj.EventMarkerLabels, obj.ArrowText, obj.DirLabel);

            return renderer;
        }

        private LineRenderer CreateLR(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_testRoot.transform);
            return go.AddComponent<LineRenderer>();
        }

        private MeshFilter CreateMF(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<MeshRenderer>();
            return go.AddComponent<MeshFilter>();
        }

        private Text CreateText(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.AddComponent<RectTransform>();
            return go.AddComponent<Text>();
        }

        // --- Lifecycle Tests (AC 12) ---

        [Test]
        public void Lifecycle_AllOverlaysHidden_AfterInitialize()
        {
            CreateTestRenderer(out var obj);

            Assert.IsFalse(obj.FloorLine.gameObject.activeSelf, "Floor line should be hidden");
            Assert.IsFalse(obj.CeilingLine.gameObject.activeSelf, "Ceiling line should be hidden");
            Assert.IsFalse(obj.ForecastMF.gameObject.activeSelf, "Forecast band should be hidden");
            Assert.IsFalse(obj.DipMF.gameObject.activeSelf, "Dip zone should be hidden");
            Assert.IsFalse(obj.PeakMF.gameObject.activeSelf, "Peak zone should be hidden");
            Assert.IsFalse(obj.ReversalLine.gameObject.activeSelf, "Reversal line should be hidden");
            Assert.IsFalse(obj.ArrowText.gameObject.activeSelf, "Direction arrow should be hidden");
            Assert.IsFalse(obj.FloorLabel.gameObject.activeSelf, "Floor label should be hidden");
            Assert.IsFalse(obj.CeilingLabel.gameObject.activeSelf, "Ceiling label should be hidden");
            for (int i = 0; i < ChartVisualConfig.MaxEventTimingMarkers; i++)
            {
                Assert.IsFalse(obj.EventMarkerLines[i].gameObject.activeSelf,
                    $"Event marker {i} should be hidden");
            }
        }

        [Test]
        public void Lifecycle_FloorOverlayShown_AfterTipOverlaysActivatedEvent()
        {
            CreateTestRenderer(out var obj);

            var overlays = new List<TipOverlayData>
            {
                new TipOverlayData
                {
                    Type = InsiderTipType.PriceFloor,
                    Label = "FLOOR ~$3.00",
                    PriceLevel = 3.0f
                }
            };
            EventBus.Publish(new TipOverlaysActivatedEvent { Overlays = overlays });

            Assert.IsTrue(obj.FloorLine.gameObject.activeSelf,
                "Floor line should be visible after activation");
            Assert.IsTrue(obj.FloorLabel.gameObject.activeSelf,
                "Floor label should be visible after activation");
            // Other overlays remain hidden
            Assert.IsFalse(obj.CeilingLine.gameObject.activeSelf,
                "Ceiling should remain hidden when only floor activated");
            Assert.IsFalse(obj.ForecastMF.gameObject.activeSelf,
                "Forecast should remain hidden when only floor activated");
        }

        [Test]
        public void Lifecycle_OverlaysCleared_AfterRoundStartedEvent()
        {
            CreateTestRenderer(out var obj);

            // Activate a floor overlay
            EventBus.Publish(new TipOverlaysActivatedEvent
            {
                Overlays = new List<TipOverlayData>
                {
                    new TipOverlayData
                    {
                        Type = InsiderTipType.PriceFloor,
                        Label = "FLOOR ~$3.00",
                        PriceLevel = 3.0f
                    }
                }
            });
            Assert.IsTrue(obj.FloorLine.gameObject.activeSelf, "Precondition: floor active");

            // Fire RoundStartedEvent to clear overlays
            EventBus.Publish(new RoundStartedEvent { RoundNumber = 1, TimeLimit = 60f });

            Assert.IsFalse(obj.FloorLine.gameObject.activeSelf,
                "Floor should be hidden after round start");
            Assert.IsFalse(obj.FloorLabel.gameObject.activeSelf,
                "Floor label should be hidden after round start");
        }

        [Test]
        public void Lifecycle_OverlaysCleared_AfterShopOpenedEvent()
        {
            CreateTestRenderer(out var obj);

            // Activate a ceiling overlay
            EventBus.Publish(new TipOverlaysActivatedEvent
            {
                Overlays = new List<TipOverlayData>
                {
                    new TipOverlayData
                    {
                        Type = InsiderTipType.PriceCeiling,
                        Label = "CEILING ~$5.00",
                        PriceLevel = 5.0f
                    }
                }
            });
            Assert.IsTrue(obj.CeilingLine.gameObject.activeSelf, "Precondition: ceiling active");

            // Fire ShopOpenedEvent to clear overlays
            EventBus.Publish(new ShopOpenedEvent { RoundNumber = 1 });

            Assert.IsFalse(obj.CeilingLine.gameObject.activeSelf,
                "Ceiling should be hidden after shop opened");
            Assert.IsFalse(obj.CeilingLabel.gameObject.activeSelf,
                "Ceiling label should be hidden after shop opened");
        }

        [Test]
        public void Lifecycle_EventMarkersCapped_WhenMoreThan15Provided()
        {
            CreateTestRenderer(out var obj);

            // Create 20 markers (more than the max 15)
            var markers = new float[20];
            for (int i = 0; i < 20; i++)
                markers[i] = i / 20f;

            EventBus.Publish(new TipOverlaysActivatedEvent
            {
                Overlays = new List<TipOverlayData>
                {
                    new TipOverlayData
                    {
                        Type = InsiderTipType.EventTiming,
                        TimeMarkers = markers
                    }
                }
            });

            // Count active markers — should be capped at 15
            int activeCount = 0;
            for (int i = 0; i < ChartVisualConfig.MaxEventTimingMarkers; i++)
            {
                if (obj.EventMarkerLines[i].gameObject.activeSelf)
                    activeCount++;
            }

            Assert.AreEqual(ChartVisualConfig.MaxEventTimingMarkers, activeCount,
                "Event markers should be capped at MaxEventTimingMarkers when more provided");
        }

        [Test]
        public void Lifecycle_MultipleOverlayTypes_AllActivated()
        {
            CreateTestRenderer(out var obj);

            EventBus.Publish(new TipOverlaysActivatedEvent
            {
                Overlays = new List<TipOverlayData>
                {
                    new TipOverlayData
                    {
                        Type = InsiderTipType.PriceFloor,
                        Label = "FLOOR ~$3.00",
                        PriceLevel = 3.0f
                    },
                    new TipOverlayData
                    {
                        Type = InsiderTipType.PriceCeiling,
                        Label = "CEILING ~$5.00",
                        PriceLevel = 5.0f
                    },
                    new TipOverlayData
                    {
                        Type = InsiderTipType.ClosingDirection,
                        Label = "CLOSING UP",
                        DirectionSign = 1
                    }
                }
            });

            Assert.IsTrue(obj.FloorLine.gameObject.activeSelf, "Floor line should be visible");
            Assert.IsTrue(obj.CeilingLine.gameObject.activeSelf, "Ceiling line should be visible");
            Assert.IsTrue(obj.ArrowText.gameObject.activeSelf, "Direction arrow should be visible");
            Assert.IsTrue(obj.DirLabel.gameObject.activeSelf, "Direction label should be visible");
        }

        // --- PriceToWorldY Tests ---

        [Test]
        public void PriceToWorldY_MidPrice_ReturnsMidPaddedHeight()
        {
            float minPrice = 100f;
            float priceRange = 50f;
            float midPrice = 125f;

            float y = TipOverlayRenderer.PriceToWorldY(midPrice, minPrice, priceRange,
                PaddedBottom, PaddedTop);

            float expected = (PaddedBottom + PaddedTop) * 0.5f;
            Assert.AreEqual(expected, y, 0.001f,
                "Price at midpoint of range should return center of padded area");
        }

        [Test]
        public void PriceToWorldY_MinPrice_ReturnsPaddedBottom()
        {
            float minPrice = 100f;
            float priceRange = 50f;

            float y = TipOverlayRenderer.PriceToWorldY(minPrice, minPrice, priceRange,
                PaddedBottom, PaddedTop);

            Assert.AreEqual(PaddedBottom, y, 0.001f,
                "Price at min should return paddedBottom");
        }

        [Test]
        public void PriceToWorldY_MaxPrice_ReturnsPaddedTop()
        {
            float minPrice = 100f;
            float priceRange = 50f;
            float maxPrice = 150f;

            float y = TipOverlayRenderer.PriceToWorldY(maxPrice, minPrice, priceRange,
                PaddedBottom, PaddedTop);

            Assert.AreEqual(PaddedTop, y, 0.001f,
                "Price at max should return paddedTop");
        }

        [Test]
        public void PriceToWorldY_ZeroPriceRange_ReturnsMidpoint()
        {
            float minPrice = 100f;
            float priceRange = 0f;

            float y = TipOverlayRenderer.PriceToWorldY(100f, minPrice, priceRange,
                PaddedBottom, PaddedTop);

            float expected = (PaddedBottom + PaddedTop) * 0.5f;
            Assert.AreEqual(expected, y, 0.001f,
                "Zero price range should return midpoint of padded area");
        }

        [Test]
        public void PriceToWorldY_PriceOutsideRange_ExtrapolatesCorrectly()
        {
            float minPrice = 100f;
            float priceRange = 50f;
            float aboveMax = 200f;

            float y = TipOverlayRenderer.PriceToWorldY(aboveMax, minPrice, priceRange,
                PaddedBottom, PaddedTop);

            Assert.Greater(y, PaddedTop,
                "Price above max should extrapolate beyond paddedTop");
        }

        // --- NormalizedTimeToWorldX Tests ---

        [Test]
        public void NormalizedTimeToWorldX_Zero_ReturnsChartLeft()
        {
            float x = TipOverlayRenderer.NormalizedTimeToWorldX(0f, ChartLeft, ChartRight);

            Assert.AreEqual(ChartLeft, x, 0.001f,
                "Normalized time 0 should return chart left edge");
        }

        [Test]
        public void NormalizedTimeToWorldX_One_ReturnsChartRight()
        {
            float x = TipOverlayRenderer.NormalizedTimeToWorldX(1f, ChartLeft, ChartRight);

            Assert.AreEqual(ChartRight, x, 0.001f,
                "Normalized time 1 should return chart right edge");
        }

        [Test]
        public void NormalizedTimeToWorldX_Half_ReturnsMidpoint()
        {
            float x = TipOverlayRenderer.NormalizedTimeToWorldX(0.5f, ChartLeft, ChartRight);

            float expected = (ChartLeft + ChartRight) * 0.5f;
            Assert.AreEqual(expected, x, 0.001f,
                "Normalized time 0.5 should return chart midpoint");
        }

        // --- TimeZoneToWorldX Tests ---

        [Test]
        public void TimeZoneToWorldX_ClampedToChartBounds()
        {
            // Zone extends beyond 0-1 range
            var (xLeft, xRight) = TipOverlayRenderer.TimeZoneToWorldX(
                0.1f, 0.5f, ChartLeft, ChartRight);

            Assert.AreEqual(ChartLeft, xLeft, 0.001f,
                "Zone extending below 0 should be clamped to chart left");
            Assert.Greater(xRight, ChartLeft,
                "Right edge should be within chart bounds");
        }

        [Test]
        public void TimeZoneToWorldX_FullWidth_SpansEntireChart()
        {
            var (xLeft, xRight) = TipOverlayRenderer.TimeZoneToWorldX(
                0.5f, 0.5f, ChartLeft, ChartRight);

            Assert.AreEqual(ChartLeft, xLeft, 0.001f,
                "Center 0.5 with halfWidth 0.5 should span to chart left");
            Assert.AreEqual(ChartRight, xRight, 0.001f,
                "Center 0.5 with halfWidth 0.5 should span to chart right");
        }

        [Test]
        public void TimeZoneToWorldX_ZeroHalfWidth_ReturnsPointWidth()
        {
            var (xLeft, xRight) = TipOverlayRenderer.TimeZoneToWorldX(
                0.5f, 0f, ChartLeft, ChartRight);

            float expected = (ChartLeft + ChartRight) * 0.5f;
            Assert.AreEqual(expected, xLeft, 0.001f,
                "Zero-width zone should collapse to a single point");
            Assert.AreEqual(expected, xRight, 0.001f,
                "Zero-width zone should collapse to a single point");
        }

        // --- Overlay Positioning Tests ---

        [Test]
        public void FloorLine_PriceAtMin_PositionedAtPaddedBottom()
        {
            float minPrice = 100f;
            float priceRange = 50f;

            float y = TipOverlayRenderer.PriceToWorldY(minPrice, minPrice, priceRange,
                PaddedBottom, PaddedTop);

            Assert.AreEqual(PaddedBottom, y, 0.001f,
                "Floor at min price should be positioned at padded bottom");
        }

        [Test]
        public void CeilingLine_PriceAtMax_PositionedAtPaddedTop()
        {
            float minPrice = 100f;
            float priceRange = 50f;

            float y = TipOverlayRenderer.PriceToWorldY(150f, minPrice, priceRange,
                PaddedBottom, PaddedTop);

            Assert.AreEqual(PaddedTop, y, 0.001f,
                "Ceiling at max price should be positioned at padded top");
        }

        [Test]
        public void ForecastBand_CenteredOnPrice_SpansPlusMinusHalfWidth()
        {
            float minPrice = 100f;
            float priceRange = 50f;
            float forecastCenter = 125f;
            float halfWidth = 10f;

            float yTop = TipOverlayRenderer.PriceToWorldY(forecastCenter + halfWidth,
                minPrice, priceRange, PaddedBottom, PaddedTop);
            float yBottom = TipOverlayRenderer.PriceToWorldY(forecastCenter - halfWidth,
                minPrice, priceRange, PaddedBottom, PaddedTop);
            float yCenter = TipOverlayRenderer.PriceToWorldY(forecastCenter,
                minPrice, priceRange, PaddedBottom, PaddedTop);

            Assert.Greater(yTop, yCenter,
                "Forecast band top should be above center");
            Assert.Less(yBottom, yCenter,
                "Forecast band bottom should be below center");

            float bandHeight = yTop - yBottom;
            float expectedHeight = (halfWidth * 2f / priceRange) * (PaddedTop - PaddedBottom);
            Assert.AreEqual(expectedHeight, bandHeight, 0.001f,
                "Forecast band height should match proportional price range");
        }

        [Test]
        public void EventMarker_AtNormalizedTime_XMatchesLerp()
        {
            float normalizedTime = 0.3f;

            float x = TipOverlayRenderer.NormalizedTimeToWorldX(normalizedTime,
                ChartLeft, ChartRight);

            float expected = Mathf.Lerp(ChartLeft, ChartRight, normalizedTime);
            Assert.AreEqual(expected, x, 0.001f,
                "Event marker X should match Lerp at normalized time");
        }

        // --- Edge Case Tests ---

        [Test]
        public void PriceToWorldY_NegativePriceRange_ReturnsMidpoint()
        {
            float y = TipOverlayRenderer.PriceToWorldY(100f, 100f, -5f,
                PaddedBottom, PaddedTop);

            float expected = (PaddedBottom + PaddedTop) * 0.5f;
            Assert.AreEqual(expected, y, 0.001f,
                "Negative price range should return midpoint");
        }

        [Test]
        public void EventMarkerCount_MaxIs15()
        {
            Assert.AreEqual(15, ChartVisualConfig.MaxEventTimingMarkers,
                "Max event timing markers should be 15");
        }

        [Test]
        public void TimeZoneToWorldX_NarrowZone_CorrectExtents()
        {
            float center = 0.5f;
            float halfWidth = 0.1f;

            var (xLeft, xRight) = TipOverlayRenderer.TimeZoneToWorldX(
                center, halfWidth, ChartLeft, ChartRight);

            float expectedLeft = Mathf.Lerp(ChartLeft, ChartRight, 0.4f);
            float expectedRight = Mathf.Lerp(ChartLeft, ChartRight, 0.6f);
            Assert.AreEqual(expectedLeft, xLeft, 0.001f);
            Assert.AreEqual(expectedRight, xRight, 0.001f);
        }

        [Test]
        public void PriceToWorldY_QuarterPrice_ReturnsQuarterPaddedHeight()
        {
            float minPrice = 100f;
            float priceRange = 100f;
            float quarterPrice = 125f; // 25% of range

            float y = TipOverlayRenderer.PriceToWorldY(quarterPrice, minPrice, priceRange,
                PaddedBottom, PaddedTop);

            float expected = Mathf.Lerp(PaddedBottom, PaddedTop, 0.25f);
            Assert.AreEqual(expected, y, 0.001f,
                "Price at 25% of range should map to 25% of padded height");
        }
    }
}
