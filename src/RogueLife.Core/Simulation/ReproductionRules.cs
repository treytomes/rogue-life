using System;

namespace RogueLife;

/// <summary>Immutable parameters for the version 2 Oink reproduction rules.</summary>
internal static class ReproductionRules
{
    internal static readonly TimeSpan Day = TimeSpan.FromDays(1);
    internal static readonly TimeSpan Gestation = TimeSpan.FromDays(3);
    internal static readonly TimeSpan PostBirthCooldown = TimeSpan.FromDays(3);
    internal const float MaximumNeed = 0.5f;
    internal const double MatingRadius = 2.0;
    internal const double DensityRadius = 8.0;
    internal const double PreferredDensity = 8.0;
    internal const double PlacementRadius = 8.0;
    internal const double StartingConceptionProbability = 0.10;
    internal const double PeakConceptionProbability = 0.90;
    internal const double PeakLifespanFraction = 0.60;
    internal const double MaximumLifespanDays = 24.0;

    internal static double BaselineConceptionProbability(TimeSpan age, TimeSpan maturityAge)
    {
        double ageDays = age.TotalDays;
        double maturityDays = maturityAge.TotalDays;
        double peakAgeDays = MaximumLifespanDays * PeakLifespanFraction;

        if (ageDays < maturityDays || ageDays >= MaximumLifespanDays)
            return 0.0;

        if (ageDays <= peakAgeDays)
        {
            double t = (ageDays - maturityDays) / (peakAgeDays - maturityDays);
            return StartingConceptionProbability
                + (PeakConceptionProbability - StartingConceptionProbability) * SmoothStep(t);
        }

        double decline = (ageDays - peakAgeDays) / (MaximumLifespanDays - peakAgeDays);
        return PeakConceptionProbability * (1.0 - SmoothStep(decline));
    }

    internal static double DensityMultiplier(int nearbyOinks)
    {
        double ratio = nearbyOinks / PreferredDensity;
        return 1.0 / (1.0 + (ratio * ratio));
    }

    private static double SmoothStep(double value)
    {
        double t = Math.Clamp(value, 0.0, 1.0);
        return t * t * (3.0 - (2.0 * t));
    }
}
