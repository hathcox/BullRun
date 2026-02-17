using System.Collections.Generic;

/// <summary>
/// Generates insider tips for the shop phase (Story 13.5).
/// Plain C# class (not MonoBehaviour) for testability.
/// Tips reveal fuzzy information about the next round's parameters.
/// </summary>
public class InsiderTipGenerator
{
    /// <summary>
    /// A generated tip offering: the definition (type, cost) plus pre-computed revealed text.
    /// </summary>
    public struct TipOffering
    {
        public InsiderTipDef Definition;
        public string RevealedText;

        public TipOffering(InsiderTipDef definition, string revealedText)
        {
            Definition = definition;
            RevealedText = revealedText;
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
            string revealedText = CalculateRevealedText(def, tierConfig, nextAct, random);
            offerings[i] = new TipOffering(def, revealedText);
        }

        return offerings;
    }

    private string CalculateRevealedText(InsiderTipDef def, StockTierConfig tierConfig, int nextAct, System.Random random)
    {
        float fuzz = GameConfig.InsiderTipFuzzPercent;

        switch (def.Type)
        {
            case InsiderTipType.PriceForecast:
            {
                // Average price ≈ midpoint of tier range, fuzzed ±10%
                float avgPrice = (tierConfig.MinPrice + tierConfig.MaxPrice) / 2f;
                float fuzzed = ApplyFuzz(avgPrice, fuzz, random);
                return string.Format(def.DescriptionTemplate, FormatPrice(fuzzed));
            }

            case InsiderTipType.PriceFloor:
            {
                // Min price from tier config, fuzzed ±10%
                float fuzzed = ApplyFuzz(tierConfig.MinPrice, fuzz, random);
                return string.Format(def.DescriptionTemplate, FormatPrice(fuzzed));
            }

            case InsiderTipType.PriceCeiling:
            {
                // Max price from tier config, fuzzed ±10%
                float fuzzed = ApplyFuzz(tierConfig.MaxPrice, fuzz, random);
                return string.Format(def.DescriptionTemplate, FormatPrice(fuzzed));
            }

            case InsiderTipType.TrendDirection:
            {
                // Categorical — no fuzz. Derive from tier trend strength config.
                string label = ClassifyTrendDirection(tierConfig, random);
                return string.Format(def.DescriptionTemplate, label);
            }

            case InsiderTipType.EventForecast:
            {
                // Categorical — classify events. Use act to determine early/late.
                bool isLateRound = (nextAct >= 3);
                string forecast = ClassifyEventForecast(tierConfig, isLateRound, random);
                return string.Format(def.DescriptionTemplate, forecast);
            }

            case InsiderTipType.EventCount:
            {
                // Exact count — no fuzz (integer). Simulate event scheduling.
                bool isLateRound = (nextAct >= 3);
                int count = CalculateEventCount(tierConfig, isLateRound, random);
                return string.Format(def.DescriptionTemplate, count);
            }

            case InsiderTipType.VolatilityWarning:
            {
                // Categorical — classify from noise amplitude.
                string level = ClassifyVolatility(tierConfig.NoiseAmplitude);
                return string.Format(def.DescriptionTemplate, level);
            }

            case InsiderTipType.OpeningPrice:
            {
                // Random starting price in tier range, fuzzed ±10%
                float startPrice = tierConfig.MinPrice + (float)random.NextDouble() * (tierConfig.MaxPrice - tierConfig.MinPrice);
                float fuzzed = ApplyFuzz(startPrice, fuzz, random);
                return string.Format(def.DescriptionTemplate, FormatPrice(fuzzed));
            }

            default:
                return "Unknown tip";
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

    /// <summary>
    /// Derives trend direction from tier config's trend strength.
    /// Higher max trend strength → more likely directional (bull/bear).
    /// Lower trend strength → more likely neutral.
    /// Random determines bull vs bear when directional.
    /// </summary>
    private static string ClassifyTrendDirection(StockTierConfig tierConfig, System.Random random)
    {
        // Use max trend strength to determine how directional the market is.
        // Penny (0.025) → very directional, BlueChip (0.006) → more neutral.
        float strength = tierConfig.MaxTrendStrength;
        int neutralChance = strength >= 0.015f ? 10 : strength >= 0.005f ? 25 : 40;

        int roll = random.Next(100);
        if (roll < neutralChance) return "NEUTRAL";
        // Remaining split evenly between bull and bear
        return random.Next(2) == 0 ? "BULLISH" : "BEARISH";
    }

    /// <summary>
    /// Classifies expected event sentiment from tier config's event frequency modifier.
    /// Higher frequency → more chaotic → more likely MIXED or MOSTLY BAD.
    /// Lower frequency → calmer market → more likely MOSTLY GOOD.
    /// </summary>
    private static string ClassifyEventForecast(StockTierConfig tierConfig, bool isLateRound, System.Random random)
    {
        // EventFrequencyModifier: Penny=1.5, LowValue=1.2, MidValue=1.0, BlueChip=0.5
        float freq = tierConfig.EventFrequencyModifier;
        int goodChance, badChance;

        if (freq >= 1.3f) { goodChance = 20; badChance = 50; }      // High freq → chaotic
        else if (freq >= 0.9f) { goodChance = 35; badChance = 35; }  // Normal → balanced
        else { goodChance = 50; badChance = 20; }                     // Low freq → calmer

        // Late rounds skew slightly more negative
        if (isLateRound) { goodChance -= 10; badChance += 10; }

        int roll = random.Next(100);
        if (roll < goodChance) return "MOSTLY GOOD";
        if (roll < goodChance + badChance) return "MOSTLY BAD";
        return "MIXED";
    }

    private static int CalculateEventCount(StockTierConfig tierConfig, bool isLateRound, System.Random random)
    {
        int minEvents = isLateRound ? EventSchedulerConfig.MinEventsLateRounds : EventSchedulerConfig.MinEventsEarlyRounds;
        int maxEvents = isLateRound ? EventSchedulerConfig.MaxEventsLateRounds : EventSchedulerConfig.MaxEventsEarlyRounds;
        float scaled = random.Next(minEvents, maxEvents + 1) * tierConfig.EventFrequencyModifier;
        return scaled < 1 ? 1 : (int)(scaled + 0.5f);
    }

    private static string ClassifyVolatility(float noiseAmplitude)
    {
        if (noiseAmplitude >= 0.12f) return "EXTREME";
        if (noiseAmplitude >= 0.05f) return "HIGH";
        return "LOW";
    }

    private static string FormatPrice(float price)
    {
        return price.ToString("F2");
    }
}
