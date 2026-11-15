using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Dispatch;
using Nawly.Domain.ValueObjects;

namespace Nawly.Services.ServicesImplementation.Dispatch.StrategyPattern;

/// <summary>
/// Wave-based courier dispatch scorer balancing road-adjusted Haversine distance,
/// fairness since last delivery, and courier rating (SRS §5.5.2).
/// </summary>
public sealed class WaveDispatchStrategy(IClock clock) : IDispatchStrategy
{
    private const int Wave1Limit = 3;
    private const int Wave2Limit = 5;

    public IReadOnlyList<Guid> SelectCandidatesForWave(
        IReadOnlyList<DispatchCandidate> candidates,
        double storeLatitude,
        double storeLongitude,
        int wave)
    {
        if (candidates.Count == 0 || wave < 1)
            return [];

        var storePoint = new GeoPoint(storeLatitude, storeLongitude);
        var now = clock.UtcNow;

        var ranked = candidates
            .Where(c => c.ActiveDeliveriesCount < c.MaxConcurrentDeliveries)
            .Select(c => new
            {
                c.CourierId,
                Score = ScoreCandidate(c, storePoint, now)
            })
            .OrderBy(x => x.Score)
            .Select(x => x.CourierId)
            .ToList();

        return wave switch
        {
            1 => ranked.Take(Wave1Limit).ToList(),
            2 => ranked.Skip(Wave1Limit).Take(Wave2Limit).ToList(),
            _ => ranked.Skip(Wave1Limit + Wave2Limit).ToList()
        };
    }

    private static double ScoreCandidate(DispatchCandidate candidate, GeoPoint storePoint, DateTimeOffset utcNow)
    {
        var courierPoint = new GeoPoint(candidate.Latitude, candidate.Longitude);
        var distanceKm = storePoint.HaversineDistanceKm(courierPoint, roadFactor: 1.3);

        var idleMinutes = candidate.LastDeliveredAt.HasValue
            ? Math.Clamp((utcNow - candidate.LastDeliveredAt.Value).TotalMinutes, 0d, 120d)
            : 120d;

        var fairnessBonus = idleMinutes * 0.02d;
        var ratingBonus = (double)candidate.RatingAvg * 0.15d;

        return distanceKm - fairnessBonus - ratingBonus;
    }
}
