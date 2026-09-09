using EmergencyResponse.Core.Assignment;
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
