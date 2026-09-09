using System.Collections.ObjectModel;
using EmergencyResponse.Core.Exceptions;

namespace EmergencyResponse.Core.Incidents;

/// <summary>
/// One reported emergency, from the moment a citizen calls it in until it is
/// closed with a resolution note.
/// </summary>
public sealed class Incident
{
    /// <summary>
    /// Creates a newly reported incident.
    /// </summary>
    /// <param name="description">What was reported, in plain words.</param>
    /// <param name="location">Where the incident is.</param>
    /// <param name="severity">How urgent the incident is.</param>
    /// <param name="requiredCapabilities">
    /// The optional skill interfaces a responder must implement to take this
    /// incident, for example <c>typeof(ICanClimb)</c>. Empty means any
    /// available responder will do.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="description"/> or <paramref name="location"/> is blank,
    /// or an entry in <paramref name="requiredCapabilities"/> is null or is not
    /// an interface type.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="requiredCapabilities"/> is null.
    /// </exception>
    public Incident(
        string description,
        string location,
        SeverityLevel severity,
        params Type[] requiredCapabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        ArgumentNullException.ThrowIfNull(requiredCapabilities);

        foreach (Type? capability in requiredCapabilities)
        {
            if (capability is null || !capability.IsInterface)
            {
                throw new ArgumentException(
                    "A required capability must be an interface type.",
                    nameof(requiredCapabilities));
            }
        }

        Id = Guid.NewGuid();
        Description = description;
        Location = location;
        Severity = severity;

        // Copied so a caller cannot mutate the array afterwards and change what this incident requires.
        RequiredCapabilities = new ReadOnlyCollection<Type>([.. requiredCapabilities]);
    }

    /// <summary>Identity of this incident, assigned when it is reported.</summary>
    public Guid Id { get; }

    /// <summary>What was reported.</summary>
    public string Description { get; }

    /// <summary>Where the incident is.</summary>
    public string Location { get; }

    /// <summary>How urgent the incident is.</summary>
    public SeverityLevel Severity { get; }

    /// <summary>
    /// The skill interfaces a responder must implement to be eligible for this
    /// incident. Empty when any available responder will do.
    /// </summary>
    public IReadOnlyCollection<Type> RequiredCapabilities { get; }

    /// <summary>Where this incident is in its lifecycle.</summary>
    public IncidentStatus Status { get; private set; } = IncidentStatus.Reported;

    /// <summary>
    /// How the incident was closed, or <see langword="null"/> until it is
    /// resolved.
    /// </summary>
    public string? ResolutionNote { get; private set; }

    /// <summary>Throws if this incident is closed, without changing anything.</summary>
    /// <exception cref="InvalidOperationException">The incident is already resolved.</exception>
    internal void EnsureNotResolved()
    {
        if (Status == IncidentStatus.Resolved)
        {
            throw new InvalidOperationException(
                $"Incident '{Description}' is resolved and cannot be assigned again.");
        }
    }

    /// <summary>Moves the incident from reported to assigned.</summary>
    /// <remarks>
    /// Who it is assigned to is recorded by the command centre, not here. The
    /// incident tracks only its own lifecycle.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The incident is not awaiting assignment.</exception>
    internal void MarkAssigned()
    {
        if (Status != IncidentStatus.Reported)
        {
            throw new InvalidOperationException(
                $"Incident '{Description}' cannot be assigned from status {Status}.");
        }

        Status = IncidentStatus.Assigned;
    }

    /// <summary>
    /// Closes the incident with a note describing the outcome.
    /// </summary>
    /// <param name="note">What happened, for the record.</param>
    /// <exception cref="ArgumentException"><paramref name="note"/> is blank.</exception>
    /// <exception cref="InvalidOperationException">
    /// The incident is not currently assigned.
    /// </exception>
    internal void Resolve(string note)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(note);

        if (Status != IncidentStatus.Assigned)
        {
            throw new InvalidOperationException(
                $"Incident '{Description}' cannot be resolved from status {Status}.");
        }

        ResolutionNote = note;
        Status = IncidentStatus.Resolved;
    }
}
