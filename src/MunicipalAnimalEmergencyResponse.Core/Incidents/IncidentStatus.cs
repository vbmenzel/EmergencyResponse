namespace MunicipalAnimalEmergencyResponse.Core.Incidents;

/// <summary>
/// Lifecycle state of a reported incident.
/// </summary>
/// <remarks>
/// An incident moves forwards only: <see cref="Reported"/> to
/// <see cref="Assigned"/> to <see cref="Resolved"/>. A resolved incident is
/// never reopened or reassigned.
/// </remarks>
public enum IncidentStatus
{
    /// <summary>Reported by a citizen. No responder assigned yet.</summary>
    Reported,

    /// <summary>A responder has been assigned and is dealing with it.</summary>
    Assigned,

    /// <summary>Closed, with a resolution note recorded.</summary>
    Resolved
}
