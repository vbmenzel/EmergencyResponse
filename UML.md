# UML class diagram

The diagram uses UML-style visibility (`+` public, `-` private, `#` protected,
`~` package/internal),
generalization, interface realization, aggregation, association, and dependency
arrows. Multiplicities label both ends of relationships. Method bodies are
intentionally omitted.

```mermaid
classDiagram
    direction LR

    class Responder {
        <<abstract>>
        +string Name
        +int Energy
        +bool IsAvailable
        +HandleIncident(incident: Incident) void*
        +IsEligibleFor(incident: Incident) bool
        ~AssignTo(incident: Incident) void
        ~Release() void
        ~AdjustEnergy(amount: int) void
    }

    class AnimalCatcher {
        +HandleIncident(incident: Incident) void
    }
    class DronePilot {
        +HandleIncident(incident: Incident) void
    }
    class WildlifeCalmer {
        +HandleIncident(incident: Incident) void
    }

    class Incident {
        +Guid Id
        +string Description
        +string Location
        +SeverityLevel Severity
        +IReadOnlyCollection~Type~ RequiredCapabilities
        +IncidentStatus Status «get; private set;»
        +Responder AssignedResponder «get; private set;»
        +string ResolutionNote «get; private set;»
        ~AssignResponder(responder: Responder) void
        ~Resolve(note: string) void
    }

    class CommandCentre {
        <<singleton>>
        -CommandCentre instance$
        -object instanceLock$
        -object assignmentLock
        -List~Responder~ responders
        -List~Incident~ incidents
        -List~ResolutionCallback~ resolutionCallbacks
        -IAssignmentStrategy assignmentStrategy
        -CommandCentre(strategy: IAssignmentStrategy)
        +GetInstance(strategy: IAssignmentStrategy) CommandCentre$
        +GetInstance() CommandCentre$
        +RegisterResponder(responder: Responder) void
        +ReportIncident(incident: Incident) void
        +AssignIncident(incident: Incident) void
        +ResolveIncident(incident: Incident, note: string) void
        +AddResolutionCallback(callback: ResolutionCallback) void
        +ChangeStrategy(strategy: IAssignmentStrategy) void
    }

    class IAssignmentStrategy {
        <<interface>>
        +SelectResponder(responders: IEnumerable~Responder~, incident: Incident) Responder
    }
    class FirstAvailableStrategy {
        +SelectResponder(responders: IEnumerable~Responder~, incident: Incident) Responder
    }
    class HighestEnergyAvailableStrategy {
        +SelectResponder(responders: IEnumerable~Responder~, incident: Incident) Responder
    }

    class ICanOperateDrone {
        <<interface>>
        +OperateDrone(incident: Incident) void
    }
    class ICanCalmAnimals {
        <<interface>>
        +CalmAnimal(incident: Incident) void
    }
    class ICanClimb {
        <<interface>>
        +ClimbTo(location: string) void
    }
    class ICanDriveRescueVehicle {
        <<interface>>
        +DriveTo(location: string) void
    }

    class SearchTool {
        <<static>>
        +FindMatches~T~(items: IEnumerable~T~, condition: Func~T,bool~) IEnumerable~T~$
    }
    class Program {
        <<static>>
        +Main(args: string[]) void$
    }
    class ResolutionCallback {
        <<delegate>>
        +Invoke(incident: Incident) void
    }
    class SeverityLevel {
        <<enumeration>>
        Low
        Medium
        High
        Critical
    }
    class IncidentStatus {
        <<enumeration>>
        Reported
        Assigned
        Resolved
    }
    class ResponderUnavailableException {
        <<exception>>
    }
    class NoSuitableResponderException {
        <<exception>>
    }

    Responder <|-- AnimalCatcher
    Responder <|-- DronePilot
    Responder <|-- WildlifeCalmer

    ICanDriveRescueVehicle <|.. AnimalCatcher : implements
    ICanOperateDrone <|.. DronePilot : implements
    ICanClimb <|.. DronePilot : implements
    ICanCalmAnimals <|.. WildlifeCalmer : implements

    IAssignmentStrategy <|.. FirstAvailableStrategy : implements
    IAssignmentStrategy <|.. HighestEnergyAvailableStrategy : implements

    CommandCentre "1" o-- "0..*" Responder : registered responders
    CommandCentre "1" o-- "0..*" Incident : reported incidents
    CommandCentre "1" o-- "0..*" ResolutionCallback : registered callbacks
    CommandCentre "1" --> "1" IAssignmentStrategy : current strategy
    Incident "0..*" --> "0..1" Responder : single assigned responder, released on resolve
    Incident "0..*" --> "1" SeverityLevel : severity
    Incident "0..*" --> "1" IncidentStatus : status
    ResolutionCallback ..> Incident : receives resolved incident
    Incident ..> ICanOperateDrone : may require
    Incident ..> ICanCalmAnimals : may require
    Incident ..> ICanClimb : may require
    Incident ..> ICanDriveRescueVehicle : may require
    Responder ..> ResponderUnavailableException : may throw
    FirstAvailableStrategy ..> NoSuitableResponderException : may throw
    HighestEnergyAvailableStrategy ..> NoSuitableResponderException : may throw
    SearchTool ..> Responder : filters
    SearchTool ..> Incident : filters
    Program ..> CommandCentre : composes and runs
    Program ..> IAssignmentStrategy : selects concrete policy
    Program ..> SearchTool : invokes generic search
```

`CommandCentre` has two separate locks by design: `instanceLock` makes
singleton construction thread-safe, while `assignmentLock` atomically selects
and reserves a responder and synchronizes a strategy replacement. The
`ChangeStrategy` operation is the chosen way to demonstrate both assignment
policies while retaining exactly one command centre.
