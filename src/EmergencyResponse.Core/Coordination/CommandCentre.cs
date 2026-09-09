using EmergencyResponse.Core.Assignment;
using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Core.Coordination;

/// <summary>
/// The municipality's single coordination centre. Holds the registered
/// responders and reported incidents, and owns the assignment policy.
/// </summary>
public sealed class CommandCentre
{
    private static readonly object instanceLock = new();
    private static CommandCentre? instance;

    private readonly object assignmentLock = new();
    private readonly List<Responder> responders = [];
    private readonly List<Incident> incidents = [];
    private readonly Dictionary<Incident, Responder> assignments = [];
    private readonly List<ResolutionCallback> resolutionCallbacks = [];
    private IAssignmentStrategy assignmentStrategy;

    private CommandCentre(IAssignmentStrategy strategy) => assignmentStrategy = strategy;

    /// <summary>
    /// Creates the one command centre with the policy it should use.
    /// </summary>
    /// <param name="strategy">The selection policy to start with.</param>
    /// <returns>The newly created centre.</returns>
    /// <remarks>
    /// A plain lock rather than a double-checked read: creation happens once, so
    /// there is no contention to optimise away.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="strategy"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// The centre already exists; use <see cref="ChangeStrategy"/> instead.
    /// </exception>
    public static CommandCentre GetInstance(IAssignmentStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        lock (instanceLock)
        {
            if (instance is not null)
            {
                throw new InvalidOperationException(
                    "The command centre already exists. Use " +
                    $"{nameof(ChangeStrategy)} to replace its assignment policy.");
            }

            instance = new CommandCentre(strategy);
            return instance;
        }
    }

    /// <summary>Returns the command centre.</summary>
    /// <returns>The existing centre.</returns>
    /// <exception cref="InvalidOperationException">
    /// The centre has not been created yet.
    /// </exception>
    public static CommandCentre GetInstance()
    {
        lock (instanceLock)
        {
            return instance ?? throw new InvalidOperationException(
                "The command centre has not been created. Call " +
                $"{nameof(GetInstance)} with an assignment strategy first.");
        }
    }

    /// <summary>The name of the policy currently in force.</summary>
    public string CurrentStrategyName
    {
        get
        {
            lock (assignmentLock)
            {
                return assignmentStrategy.Name;
            }
        }
    }

    /// <summary>
    /// The registered responders, in registration order, as a snapshot taken at
    /// the moment of the call.
    /// </summary>
    public IReadOnlyList<Responder> Responders
    {
        get
        {
            lock (assignmentLock)
            {
                return [.. responders];
            }
        }
    }

    /// <summary>
    /// The reported incidents, in report order, as a snapshot taken at the
    /// moment of the call.
    /// </summary>
    public IReadOnlyList<Incident> Incidents
    {
        get
        {
            lock (assignmentLock)
            {
                return [.. incidents];
            }
        }
    }

    /// <summary>Adds a responder to the roster.</summary>
    /// <param name="responder">The responder to register.</param>
    /// <exception cref="ArgumentNullException"><paramref name="responder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// This responder object is already registered. Identity is the object, not
    /// the name.
    /// </exception>
    public void RegisterResponder(Responder responder)
    {
        ArgumentNullException.ThrowIfNull(responder);

        lock (assignmentLock)
        {
            if (responders.Contains(responder))
            {
                throw new InvalidOperationException($"{responder.Name} is already registered.");
            }

            responders.Add(responder);
        }
    }

    /// <summary>Records a newly reported incident.</summary>
    /// <param name="incident">The incident to report.</param>
    /// <exception cref="ArgumentNullException"><paramref name="incident"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This incident is already reported.</exception>
    public void ReportIncident(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        lock (assignmentLock)
        {
            if (incidents.Contains(incident))
            {
                throw new InvalidOperationException(
                    $"Incident '{incident.Description}' has already been reported.");
            }

            incidents.Add(incident);
        }
    }

    /// <summary>
    /// Whether the responder is free, that is not currently out on an
    /// unresolved incident.
    /// </summary>
    /// <param name="responder">The responder to ask about.</param>
    /// <returns><see langword="true"/> if nothing is currently assigned to them.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="responder"/> is null.</exception>
    public bool IsAvailable(Responder responder)
    {
        ArgumentNullException.ThrowIfNull(responder);

        lock (assignmentLock)
        {
            return IsFree(responder);
        }
    }

    /// <summary>
    /// The responders who are free right now, in registration order, as a
    /// snapshot taken at the moment of the call.
    /// </summary>
    public IReadOnlyList<Responder> AvailableResponders
    {
        get
        {
            lock (assignmentLock)
            {
                return FreeResponders();
            }
        }
    }

    /// <summary>
    /// Who is handling the incident, or who handled it once it is resolved.
    /// </summary>
    /// <param name="incident">The incident to ask about.</param>
    /// <returns>The responder, or <see langword="null"/> if it was never assigned.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="incident"/> is null.</exception>
    public Responder? GetAssignedResponder(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        lock (assignmentLock)
        {
            return assignments.GetValueOrDefault(incident);
        }
    }

    /// <summary>
    /// Picks a free, capable responder using the current policy and records the
    /// assignment.
    /// </summary>
    /// <param name="incident">The incident that needs someone.</param>
    /// <returns>The responder now handling it.</returns>
    /// <remarks>
    /// Selection and recording happen inside one lock.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="incident"/> is null.</exception>
    /// <exception cref="NoSuitableResponderException">Nobody free is capable of it.</exception>
    /// <exception cref="ResponderUnavailableException">The incident already has someone.</exception>
    /// <exception cref="InvalidOperationException">The incident is already resolved.</exception>
    public Responder AssignIncident(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        lock (assignmentLock)
        {
            EnsureUnassigned(incident);
            Responder chosen = assignmentStrategy.SelectResponder(FreeResponders(), incident);
            Record(incident, chosen);
            return chosen;
        }
    }

    /// <summary>
    /// Sends one named responder to an incident, bypassing the policy.
    /// </summary>
    /// <param name="incident">The incident that needs someone.</param>
    /// <param name="responder">The responder to send.</param>
    /// <returns>The responder now handling it.</returns>
    /// <exception cref="ArgumentNullException">Either argument is null.</exception>
    /// <exception cref="ResponderUnavailableException">
    /// The responder is already out, exhausted, or lacks a required capability.
    /// </exception>
    /// <exception cref="InvalidOperationException">The incident is already resolved.</exception>
    public Responder AssignSpecificResponder(Incident incident, Responder responder)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(responder);

        lock (assignmentLock)
        {
            EnsureUnassigned(incident);
            EnsureCanTake(incident, responder);
            Record(incident, responder);
            return responder;
        }
    }

    /// <summary>Free means no unresolved incident is assigned to them.</summary>
    private bool IsFree(Responder responder) =>
        !assignments.Any(entry =>
            entry.Value == responder && entry.Key.Status == IncidentStatus.Assigned);

    /// <summary>The free responders, in registration order.</summary>
    private List<Responder> FreeResponders() => [.. responders.Where(IsFree)];

    /// <summary>Throws unless the incident is open and nobody is on it yet.</summary>
    private void EnsureUnassigned(Incident incident)
    {
        incident.EnsureNotResolved();

        if (assignments.TryGetValue(incident, out Responder? existing))
        {
            throw new ResponderUnavailableException(
                $"Incident '{incident.Description}' is already assigned to {existing.Name}.");
        }
    }

    /// <summary>Throws unless this specific responder could take the incident.</summary>
    private void EnsureCanTake(Incident incident, Responder responder)
    {
        if (!IsFree(responder))
        {
            throw new ResponderUnavailableException(
                $"{responder.Name} is already out on another incident.");
        }

        if (!responder.CanHandle(incident))
        {
            throw new ResponderUnavailableException(
                $"{responder.Name} cannot take '{incident.Description}'. " +
                $"Energy: {responder.Energy}, " +
                $"required capabilities: {incident.RequiredCapabilities.Count}.");
        }
    }

    /// <summary>
    /// Writes the assignment. The record and the incident's status are both the
    /// centre's to keep in step, and both happen under the same lock.
    /// </summary>
    private void Record(Incident incident, Responder responder)
    {
        assignments[incident] = responder;
        incident.MarkAssigned();
    }

    /// <summary>
    /// Closes an incident and notifies every registered callback.
    /// </summary>
    /// <param name="incident">The incident to close.</param>
    /// <param name="note">What happened, for the record.</param>
    /// <remarks>
    /// The callbacks are copied under the lock and invoked outside it.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="incident"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="note"/> is blank.</exception>
    /// <exception cref="InvalidOperationException">
    /// The incident has no responder assigned, or is already resolved.
    /// </exception>
    public void ResolveIncident(Incident incident, string note)
    {
        ArgumentNullException.ThrowIfNull(incident);

        ResolutionCallback[] callbacks;
        lock (assignmentLock)
        {
            incident.Resolve(note);
            callbacks = [.. resolutionCallbacks];
        }

        foreach (ResolutionCallback callback in callbacks)
        {
            callback(incident);
        }
    }

    /// <summary>
    /// Registers a callback to run after an incident is resolved.
    /// </summary>
    /// <param name="callback">What to do with a resolved incident.</param>
    /// <remarks>Callbacks run in registration order.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is null.</exception>
    public void AddResolutionCallback(ResolutionCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        lock (assignmentLock)
        {
            resolutionCallbacks.Add(callback);
        }
    }

    /// <summary>Replaces the selection policy.</summary>
    /// <param name="strategy">The policy to use from now on.</param>
    /// <remarks>
    /// Taken under the assignment lock, so a policy change cannot land halfway
    /// through a selection.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="strategy"/> is null.</exception>
    public void ChangeStrategy(IAssignmentStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        lock (assignmentLock)
        {
            assignmentStrategy = strategy;
        }
    }

    /// <summary>
    /// Discards the singleton so the next test starts from nothing.
    /// </summary>
    /// <remarks>For the test suite only: singleton state is process-wide.</remarks>
    internal static void ResetForTests()
    {
        lock (instanceLock)
        {
            instance = null;
        }
    }
}
