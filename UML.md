# UML class diagram

[Read the project README](README.md)

```mermaid
classDiagram
    direction TB

    namespace Core {
        class Responder {
            <<abstract>>
            +int MinEnergy$
            +int MaxEnergy$
            +string Name
            +int Energy «get; private set;»
            +bool IsAvailable «get; private set;»
            #Responder(name: string, energy: int)
            +HandleIncident(incident: Incident) string*
            +IsEligibleFor(incident: Incident) bool
            ~AssignTo(incident: Incident) void
            ~Release() void
            ~AdjustEnergy(amount: int) void
        }

        class AnimalCatcher {
            +HandleIncident(incident: Incident) string
            +DriveTo(location: string) string
        }
        class DronePilot {
            +HandleIncident(incident: Incident) string
            +OperateDrone(incident: Incident) string
            +ClimbTo(location: string) string
        }
        class WildlifeCalmer {
            +HandleIncident(incident: Incident) string
            +CalmAnimal(incident: Incident) string
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
            +string CurrentStrategyName
            +IReadOnlyList~Responder~ Responders
            +IReadOnlyList~Incident~ Incidents
            +RegisterResponder(responder: Responder) void
            +ReportIncident(incident: Incident) void
            +AssignIncident(incident: Incident) Responder
            +AssignSpecificResponder(incident: Incident, responder: Responder) Responder
            +ResolveIncident(incident: Incident, note: string) void
            +ReleaseResponder(responder: Responder) void
            +AddResolutionCallback(callback: ResolutionCallback) void
            +ChangeStrategy(strategy: IAssignmentStrategy) void
            ~ResetForTests() void$
        }

        class IAssignmentStrategy {
            <<interface>>
            +string Name
            +SelectResponder(responders: IEnumerable~Responder~, incident: Incident) Responder
        }
        class FirstAvailableStrategy {
            +string Name
            +SelectResponder(responders: IEnumerable~Responder~, incident: Incident) Responder
        }
        class HighestEnergyAvailableStrategy {
            +string Name
            +SelectResponder(responders: IEnumerable~Responder~, incident: Incident) Responder
        }

        class ICanOperateDrone {
            <<interface>>
            +OperateDrone(incident: Incident) string
        }
        class ICanCalmAnimals {
            <<interface>>
            +CalmAnimal(incident: Incident) string
        }
        class ICanClimb {
            <<interface>>
            +ClimbTo(location: string) string
        }
        class ICanDriveRescueVehicle {
            <<interface>>
            +DriveTo(location: string) string
        }

        class SearchTool {
            <<static>>
            +FindMatches~T~(items: IEnumerable~T~, condition: Func~T,bool~) IEnumerable~T~$
            +FindFirstMatch~T~(items: IEnumerable~T~, condition: Func~T,bool~) T$
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
    }

    namespace Application {
        class IIncidentResponseService {
            <<interface>>
            +string CurrentStrategyName
            +RegisterResponder(responder: Responder) void
            +ReportIncident(description: string, location: string, severity: SeverityLevel, requiredCapabilities: Type[]) Incident
            +AssignIncident(incident: Incident) Responder
            +AssignSpecificResponder(incident: Incident, responder: Responder) Responder
            +ResolveIncident(incident: Incident, note: string) void
            +ReleaseResponder(responder: Responder) void
            +AddResolutionCallback(callback: ResolutionCallback) void
            +ChangeStrategy(strategy: IAssignmentStrategy) void
            +FindResponders(condition: Func~Responder,bool~) IEnumerable~Responder~
            +FindIncidents(condition: Func~Incident,bool~) IEnumerable~Incident~
        }
        class IncidentResponseService {
            -CommandCentre commandCentre
            +IncidentResponseService(commandCentre: CommandCentre)
        }
    }

    namespace Console {
        class Program {
            <<static>>
            +Main(args: string[]) void$
            -LogResolution(incident: Incident) void$
        }
        class DemoData {
            <<static>>
            +RegisterResponders(service: IIncidentResponseService) void$
            +ReportIncidents(service: IIncidentResponseService) IReadOnlyList~Incident~$
        }
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
    IIncidentResponseService <|.. IncidentResponseService : implements

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
    CommandCentre ..> ResponderUnavailableException : may throw
    FirstAvailableStrategy ..> NoSuitableResponderException : may throw
    HighestEnergyAvailableStrategy ..> NoSuitableResponderException : may throw
    FirstAvailableStrategy ..> SearchTool : filters eligible responders
    HighestEnergyAvailableStrategy ..> SearchTool : filters eligible responders
    SearchTool ..> Responder : filters
    SearchTool ..> Incident : filters

    IncidentResponseService --> CommandCentre : delegates to the one centre
    IncidentResponseService ..> SearchTool : responder and incident queries

    Program ..> CommandCentre : creates the singleton with a strategy
    Program ..> IIncidentResponseService : runs the scripted demonstration
    Program ..> IAssignmentStrategy : selects concrete policy
    Program ..> DemoData : seeds responders and incidents
```