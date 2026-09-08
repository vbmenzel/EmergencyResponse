# Emergency Response

## Project brief

The municipality needs a small command-centre application for its **Emergency Response Unit**. Citizens report unusual animal-related incidents, and the command centre assigns an available responder with suitable skills.

Most calls are not life-threatening, but they are certainly memorable: an escaped alpaca blocks the motorway, a swan has taken control of a bus stop, a goat is on the library roof, or a seagull has stolen an entire hot-dog stand.

The old paper-based system has caused responders to be sent to two incidents at once. Build a C# console application that keeps track of responders and incidents, assigns work safely, and records when incidents are resolved.

## Functional requirements

### 1. Responder hierarchy

Create an abstract base class for all responders.

- Each responder must have a name, an encapsulated energy level, and an availability status.
- Energy must not be set to an invalid value from outside the class. Decide and document your valid range.
- The base class must contain at least one abstract method, such as `HandleIncident(Incident incident)`.
- Create at least three concrete responder types. They must override the abstract method differently.

Suggested types:

- `AnimalCatcher` — handles escaped animals with nets, treats, and calm instructions.
- `DronePilot` — scouts rooftops, trees, and inaccessible locations.
- `WildlifeCalmer` — handles stressed, loud, or territorial animals.

Explain the access modifiers chosen for the important members. For example, a responder's availability should not be freely changed by unrelated code.

### 2. Skills as interfaces

Design at least two interfaces representing optional abilities. These must be independent of the responder inheritance hierarchy.

Possible examples:

- `ICanOperateDrone`
- `ICanCalmAnimals`
- `ICanClimb`
- `ICanDriveRescueVehicle`

Implement them only on responder types where they make sense. Demonstrate that a responder may have several skills.

### 3. Incidents and command centre

Create an `Incident` model containing at least:

- Description
- Location
- Severity level
- Resolution status

Create a command-centre class that stores registered responders and reported incidents in collections of your choice. Be prepared to justify the collection types you select.

The municipality has exactly one live command centre. Model this with the Singleton pattern: make the command centre's constructor inaccessible from outside the class and expose one shared instance. Make its creation thread-safe with a private static instance field and a static `lock`, since the later threading simulation may access it from several tasks. Keep the singleton focused on representing the one municipal command centre; do not use it as a shortcut for unrelated global state.

An incident should also record its assigned responder (or the command centre should keep an equally clear assignment record). This makes it possible to release the correct responder when the incident is resolved.

Suggested incident examples:

- "Three alpacas on the motorway"
- "Territorial swan occupying a bus stop"
- "Goat stranded on the library roof"
- "Seagull has stolen a hot-dog stand"
- "Cat in a tree that insists it is not an emergency"

### 4. Generic search/filter tool

Create at least one reusable generic method that can search or filter both responders and incidents. It should accept a collection and a condition, then return matching items.

Example shape:

```csharp
IEnumerable<T> FindMatches<T>(IEnumerable<T> items, Func<T, bool> condition)
```

Use it in at least one responder query and one incident query.

### 5. Exceptions

Create and use at least two custom exception classes relevant to this domain.

Suggested examples:

- `ResponderUnavailableException` — raised when code tries to assign an already-busy responder.
- `NoSuitableResponderException` — raised when no available responder fits an incident.

Catch exceptions in the program flow and present helpful output. The application must not crash during normal invalid operations.

### 6. Resolution callback

When an incident is marked as resolved, trigger a callback using an `Action<...>` or a custom delegate.

Use the callback for a meaningful result, such as logging the outcome and/or making the assigned responder available again. Demonstrate both:

- a named method as a callback;
- a lambda expression as a callback.

### 7. Loose coupling: assignment strategy

The command centre must not hard-code how it selects a responder. Define an interface such as:

```csharp
public interface IAssignmentStrategy
{
    Responder SelectResponder(IEnumerable<Responder> responders, Incident incident);
}
```

Pass an implementation into the command centre through its constructor. The constructor can remain private because of Singleton; expose a static factory/access method that accepts the strategy on the first call and passes it into that private constructor. For example, `CommandCentre.GetInstance(IAssignmentStrategy strategy)`. This preserves both requirements: only one command centre exists, and its selection policy originates outside the class.

For the two-strategy demonstration, add a deliberate `ChangeStrategy(IAssignmentStrategy strategy)` method, or run the program twice with a different first-call strategy. Document which option you choose. The command centre must store the interface type, never a concrete strategy type.

Implement at least these two strategies and demonstrate that the command centre works with either one without being changed:

- `FirstAvailableStrategy`
- `HighestEnergyAvailableStrategy`

Short justification: the command centre depends on an abstraction rather than one selection algorithm. A new policy can be introduced or exchanged without modifying the command-centre class.

### 8. Threading extension

Simulate several incidents arriving at the same time with `Task` objects or threads.

First, demonstrate the race condition in a clearly separated unsafe demo: two concurrent assignments can both see one responder as available before either marks that responder as busy. This can assign the same person twice. A tiny temporary delay between the check and status update is acceptable only to make this timing problem repeatable.

Then protect the complete assignment operation — selecting an eligible responder and marking that responder busy — with suitable synchronization, for example `lock`. After the fix, prove in the console output that one responder cannot be assigned to two unresolved incidents at the same time.

Do not add artificial delays to production logic. A small delay is acceptable only in a clearly labelled demonstration or test to make the race condition easier to observe.

## Required design documentation

Before implementation, create a UML class diagram. It must show:

- classes and significant members;
- inheritance from the abstract responder class;
- interface implementations;
- the command centre's relationship to its responder and incident collections;
- its dependency on `IAssignmentStrategy`.
- the singleton responsibility of the command centre (one shared instance and a private constructor).

Update the diagram if the design changes, and include the final diagram in the submission.

## Suggested minimum demonstration

1. Register at least four responders and five silly incidents.
2. Search for available responders and unresolved high-severity incidents using the generic method.
3. Assign and resolve an incident, showing both resolution callbacks.
4. Attempt an invalid assignment and handle a custom exception cleanly.
5. Run concurrent incident assignments and show that synchronization prevents double booking.
6. Create a second command centre with the other assignment strategy, or replace the strategy through a supported design, and show that the selection result changes without editing `CommandCentre`.
   Since this project uses a singleton command centre, use the supported strategy-change mechanism instead of creating a second command centre.

## Deliverables

- C# source code
- Final UML class diagram
- A `README.md` explaining build/run instructions and the main design decisions
- XML documentation comments on important public classes and methods
- Meaningful Git commits throughout development
- A brief note in the README explaining why `CommandCentre` is a singleton: there is one municipal coordination centre, and a thread-safe shared instance prevents accidental duplicate centres in the simulation.
- If the threading extension is chosen, a short written explanation of the race condition and why the synchronization prevents it.

## Scope advice

This is deliberately a small C#/.NET console application. Use English identifiers and follow the course code standard. Aim for three responder types, two or three interfaces, two assignment strategies, and a focused scripted console demonstration. Finish the core requirements before adding menu systems, persistence, or elaborate simulation features.
