namespace MunicipalAnimalEmergencyResponse.Core.Incidents;

/// <summary>
/// Priority of a reported animal incident.
/// </summary>
/// <remarks>
/// The members are ordered from least to most urgent so that searches can
/// compare them directly, for example <c>incident.Severity &gt;=
/// SeverityLevel.High</c>. Do not reorder them.
/// </remarks>
public enum SeverityLevel
{
    /// <summary>A nuisance that can wait for a responder to come free.</summary>
    Low,

    /// <summary>Disruptive to the public, but nobody is in danger.</summary>
    Medium,

    /// <summary>Blocking traffic or risking injury to a person or the animal.</summary>
    High,

    /// <summary>Immediate danger. Needs the next available responder.</summary>
    Critical
}
