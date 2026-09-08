using System.Collections.ObjectModel;
using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Core.Incidents;

/// <summary>
/// One reported emergency, from the moment a citizen calls it in until it is
/// closed with a resolution note.
/// </summary>
/// <remarks>
/// An incident owns the consistency of its own lifecycle. At most one
/// responder is ever assigned, and a resolved incident is never reopened. The
/// methods that change state are <see langword="internal"/>, so only the
/// coordination code inside this assembly can drive the transitions.
/// </remarks>
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
    /// The single responder handling this incident, or <see langword="null"/>
    /// while it is still unassigned.
    /// </summary>
    public Responder? AssignedResponder { get; private set; }

    /// <summary>
    /// How the incident was closed, or <see langword="null"/> until it is
    /// resolved.
    /// </summary>
    public string? ResolutionNote { get; private set; }

    /// <summary>
    /// Records the responder who will handle this incident.
    /// </summary>
    /// <param name="responder">The responder taking the incident.</param>
    /// <exception cref="ArgumentNullException"><paramref name="responder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The incident is already resolved.</exception>
    /// <exception cref="ResponderUnavailableException">
    /// Another responder is already assigned to this incident.
    /// </exception>
    internal void AssignResponder(Responder responder)
    {
        ArgumentNullException.ThrowIfNull(responder);

        if (Status == IncidentStatus.Resolved)
        {
            throw new InvalidOperationException(
                $"Incident '{Description}' is resolved and cannot be assigned again.");
        }

        if (AssignedResponder is not null)
        {
            throw new ResponderUnavailableException(
                $"Incident '{Description}' is already assigned to {AssignedResponder.Name}.");
        }

        AssignedResponder = responder;
        Status = IncidentStatus.Assigned;
    }

    /// <summary>
    /// Closes the incident with a note describing the outcome.
    /// </summary>
    /// <param name="note">What happened, for the record.</param>
    /// <remarks>
    /// Resolving does not free the assigned responder. That is the job of a
    /// resolution callback registered with the command centre, which is what
    /// makes the release observable in the console demonstration.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="note"/> is blank.</exception>
    /// <exception cref="InvalidOperationException">
    /// The incident has no responder assigned, or is already resolved.
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
