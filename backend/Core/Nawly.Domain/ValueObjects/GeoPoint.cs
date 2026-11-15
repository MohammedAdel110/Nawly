namespace Nawly.Domain.ValueObjects;

/// <summary>
/// Geographic coordinate pair with Haversine distance calculation (SRS §5.3, §6.1).
/// </summary>
public sealed record GeoPoint
{
    private const double EarthRadiusKm = 6371.0;

    public double Latitude { get; }
    public double Longitude { get; }

    public GeoPoint(double latitude, double longitude)
    {
        if (latitude is < -90.0 or > 90.0)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");

        if (longitude is < -180.0 or > 180.0)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    public double HaversineDistanceKm(GeoPoint other, double roadFactor = 1.3)
    {
        var dLat = DegreesToRadians(other.Latitude - Latitude);
        var dLon = DegreesToRadians(other.Longitude - Longitude);

        var lat1 = DegreesToRadians(Latitude);
        var lat2 = DegreesToRadians(other.Latitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var straightLineKm = EarthRadiusKm * c;
        return Math.Round(straightLineKm * roadFactor, 3, MidpointRounding.AwayFromZero);
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
