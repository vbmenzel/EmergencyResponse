using EmergencyResponse.Core.Incidents;

namespace EmergencyResponse.Core.Capabilities;

/// <summary>
/// The optional skill of flying a drone to survey a scene from above.
/// </summary>
public interface ICanOperateDrone
{
    /// <summary>Flies a drone over the incident and reports what it sees.</summary>
    /// <param name="incident">The incident to survey.</param>
    /// <returns>A line describing the aerial survey, for a host to present.</returns>
    string OperateDrone(Incident incident);
}
