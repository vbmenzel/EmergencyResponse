namespace EmergencyResponse.Core.Capabilities;

/// <summary>
/// The optional skill of driving the unit's rescue vehicle, which is what makes
/// large or road-blocking incidents reachable.
/// </summary>
public interface ICanDriveRescueVehicle
{
    /// <summary>Drives the rescue vehicle to the incident.</summary>
    /// <param name="location">Where to drive to.</param>
    /// <returns>A line describing the journey, for a host to present.</returns>
    string DriveTo(string location);
}
