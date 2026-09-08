using EmergencyResponse.Core.Incidents;

namespace EmergencyResponse.Core.Coordination;

/// <summary>
/// Notified after an incident has been resolved, so a host can log the outcome
/// and free the responder who handled it.
/// </summary>
/// <param name="incident">
/// The incident that was just resolved. Its status is
/// <see cref="IncidentStatus.Resolved"/> and its resolution note is set.
/// </param>
public delegate void ResolutionCallback(Incident incident);
