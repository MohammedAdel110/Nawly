namespace Nawly.Abstraction.IServices.Dispatch;

public sealed record DispatchCandidate(
    Guid CourierId,
    double Latitude,
    double Longitude,
    decimal RatingAvg,
    DateTimeOffset? LastDeliveredAt,
    int ActiveDeliveriesCount,
    int MaxConcurrentDeliveries);

public interface IDispatchStrategy
{
    IReadOnlyList<Guid> SelectCandidatesForWave(
        IReadOnlyList<DispatchCandidate> candidates,
        double storeLatitude,
        double storeLongitude,
        int wave);
}
