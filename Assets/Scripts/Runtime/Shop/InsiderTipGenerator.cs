using System.Collections.Generic;

/// <summary>
/// Generates insider tips for the shop phase (Story 13.5).
/// Plain C# class (not MonoBehaviour) for testability.
/// Tips reveal fuzzy information about the next round's parameters.
/// </summary>
public class InsiderTipGenerator
{
    /// <summary>
    /// A generated tip offering: the definition (type, cost) plus pre-computed display text and numeric value.
    /// </summary>
    public struct TipOffering
    {
        public InsiderTipDef Definition;
        public string DisplayText;
        public float NumericValue;

        public TipOffering(InsiderTipDef definition, string displayText, float numericValue = 0f)
        {
            Definition = definition;
            DisplayText = displayText;
            NumericValue = numericValue;
        }
    }

    /// <summary>
    /// Generates a set of tip offerings for the next round.
    /// Selects slotCount random tip types (no duplicates), calculates fuzzed values.
    /// </summary>
    public TipOffering[] GenerateTips(int slotCount, int nextRound, int nextAct, System.Random random = null)
    {
        random = random ?? new System.Random();

        var tier = RunContext.GetTierForAct(nextAct);
        var tierConfig = StockTierData.GetTierConfig(tier);

        // Select random tip types (no duplicates) via Fisher-Yates
        var allTypes = new List<InsiderTipDef>(InsiderTipDefinitions.All);
        for (int i = allTypes.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = allTypes[i];
            allTypes[i] = allTypes[j];
            allTypes[j] = temp;
        }

        int count = slotCount < allTypes.Count ? slotCount : allTypes.Count;
        var offerings = new TipOffering[count];

        for (int i = 0; i < count; i++)
        {
            var def = allTypes[i];
            var (revealedText, numericValue) = CalculateDisplayText(def, tierConfig, nextAct, random);
            offerings[i] = new TipOffering(def, revealedText, numericValue);
        }

        return offerings;
    }

    private (string text, float numericValue) CalculateDisplayText(InsiderTipDef def, StockTierConfig tierConfig, int nextAct, System.Random random)
    {
        float fuzz = GameConfig.InsiderTipFuzzPercent;

        switch (def.Type)
        {
            case InsiderTipType.PriceForecast:
            {
                float avgPrice = (tierConfig.MinPrice + tierConfig.MaxPrice) / 2f;
                float fuzzed = ApplyFuzz(avgPrice, fuzz, random);
                return (string.Format(def.DescriptionTemplate, FormatPrice(fuzzed)), fuzzed);
            }

            case InsiderTipType.PriceFloor:
            {
                float fuzzed = ApplyFuzz(tierConfig.MinPrice, fuzz, random);
                return (string.Format(def.DescriptionTemplate, FormatPrice(fuzzed)), fuzzed);
            }

            case InsiderTipType.PriceCeiling:
            {
                float fuzzed = ApplyFuzz(tierConfig.MaxPrice, fuzz, random);
                return (string.Format(def.DescriptionTemplate, FormatPrice(fuzzed)), fuzzed);
            }

            case InsiderTipType.EventCount:
            {
                bool isLateRound = (nextAct >= 3);
                int count = CalculateEventCount(tierConfig, isLateRound, random);
                return (string.Format(def.DescriptionTemplate, count), 0f);
            }

            case InsiderTipType.DipMarker:
                return (def.DescriptionTemplate, 0f);

            case InsiderTipType.PeakMarker:
                return (def.DescriptionTemplate, 0f);

            case InsiderTipType.ClosingDirection:
            {
                bool closesHigher = random.NextDouble() < 0.6;
                return (string.Format(def.DescriptionTemplate, closesHigher ? "HIGHER" : "LOWER"), 0f);
            }

            case InsiderTipType.EventTiming:
                return (def.DescriptionTemplate, 0f);

            case InsiderTipType.TrendReversal:
                return (def.DescriptionTemplate, 0f);

            default:
                return ("Unknown tip", 0f);
        }
    }

    /// <summary>
    /// Applies ±fuzzPercent to a value. E.g., fuzzPercent=0.10 means ±10%.
    /// </summary>
    public static float ApplyFuzz(float value, float fuzzPercent, System.Random random)
    {
        float multiplier = 1f + ((float)random.NextDouble() * 2f - 1f) * fuzzPercent;
        return value * multiplier;
    }

    private static int CalculateEventCount(StockTierConfig tierConfig, bool isLateRound, System.Random random)
    {
        int minEvents = isLateRound ? EventSchedulerConfig.MinEventsLateRounds : EventSchedulerConfig.MinEventsEarlyRounds;
        int maxEvents = isLateRound ? EventSchedulerConfig.MaxEventsLateRounds : EventSchedulerConfig.MaxEventsEarlyRounds;
        float scaled = random.Next(minEvents, maxEvents + 1) * tierConfig.EventFrequencyModifier;
        return scaled < 1 ? 1 : (int)(scaled + 0.5f);
    }

    private static string FormatPrice(float price)
    {
        return price.ToString("F2");
    }
}
