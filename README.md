<div align="center">

# Emergency Response

**One command centre, five responders, and no way to send the same person to two places at once.**

[![build](https://github.com/vbmenzel/EmergencyResponse/actions/workflows/ci.yml/badge.svg)](https://github.com/vbmenzel/EmergencyResponse/actions/workflows/ci.yml)
![tests](https://img.shields.io/badge/tests-92%20passing-brightgreen)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![warnings](https://img.shields.io/badge/warnings-0%20tolerated-f28d1a)

<img src="docs/images/concurrency.gif" alt="Seven concurrent callouts, first all landing on one responder, then spread across five once a lock spans both the decision and the record" width="960">

<sub>Seven callouts arrive at once. Without a lock spanning both steps, all seven land on the same person.<br>With one, five go to five different people and two are refused, which is correct once everybody is out.</sub>

</div>

## What this is

A command centre for the municipal Emergency Response Unit, which exists because
a goat got onto the library roof and the paper rota sent the same person to two
places at once.

A C# console application: responders with skills, incidents with requirements, a
single command centre that matches them up without double-booking anybody.

## Running it

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build          # 0 warnings, or it fails: see "Warnings are errors"
dotnet test           # 92 tests
dotnet run --project src/EmergencyResponse.ConsoleDemo
```

The demo is scripted, not interactive. It prints seven sections: the roster, a
generic search, a callout handled end to end, two failures handled cleanly, the
assignment policy being swapped, a race condition and its fix, and the board at
the end of the shift.

Any one section also runs on its own, named. Every recording on this page is one
of these commands, unedited:

```bash
dotnet run --project src/EmergencyResponse.ConsoleDemo -- concurrency
```

The names are `roster`, `search`, `callbacks`, `exceptions`, `policy`,
`concurrency` and `board`, in that order. A section run alone still prints the
number it carries in the full run, and only the roster and the incident board
are set up first, so every section works on its own.

It will not always print the same thing as it does in a full run, though. The
two resolution callbacks are registered in section 3, so section 6 on its own
resolves its callouts silently, where in a full run each one is announced twice.
The work is the same either way. Only the listeners differ, which is the point
callbacks are there to make.

![The roster and the incident board](docs/images/roster.gif)

## The design

Two projects. `EmergencyResponse.Core` holds the domain and knows nothing about
consoles. `EmergencyResponse.ConsoleDemo` is the only thing that prints.

```
src/EmergencyResponse.Core/          domain, rules, the command centre
src/EmergencyResponse.ConsoleDemo/   the scripted demonstration
tests/EmergencyResponse.Tests/       92 tests
```

One callout, from the report to the callbacks:

```mermaid
flowchart TD
    A(["Incident reported"]) --> B["CommandCentre.AssignIncident"]

    subgraph held ["one lock, held across all three steps"]
        direction TB
        C["Who is free?<br>derived from the assignment dictionary"]
        D["IAssignmentStrategy.SelectResponder"]
        E["Record the assignment"]
        C --> D --> E
    end

    B --> C
    D -.->|nobody fits| X(["NoSuitableResponderException"])
    E --> F["Responder.HandleIncident"]
    F --> G["CommandCentre.ResolveIncident"]
    G --> H(["Every ResolutionCallback fires,<br>in registration order"])
```

[**Read the full class diagram**](docs/UML.md), which has every type, member and
relationship. GitHub renders it inline.

### Why the command centre is a singleton

There is one municipal coordination centre in real life, so there is one here.
A second instance would keep its own view of who is busy, which is the exact
failure the paper rota had. The constructor is private, the only way in is
`CommandCentre.GetInstance(strategy)`, and creation is guarded by a static lock
so the concurrency demo cannot accidentally produce two.

Calling `GetInstance(strategy)` a second time throws rather than quietly
ignoring the new policy. We do this because silently discarding an argument hides bugs.

### Who knows who is busy

Neither the responder nor the incident. `CommandCentre` owns a
`Dictionary<Incident, Responder>`, and availability is **derived** from it: a
responder is free when no unresolved incident names them.

### Skills are interfaces, not subclasses

An incident declares what it needs, `Responder.CanHandle` checks it, and nobody
carries a method that makes no sense for them.

|                  | `ICanClimb` | `ICanOperateDrone` | `ICanCalmAnimals` | `ICanDriveRescueVehicle` |
| ---------------- | :---------: | :----------------: | :---------------: | :----------------------: |
| `DronePilot`     |      ✅      |         ✅          |                   |                          |
| `WildlifeCalmer` |             |                    |         ✅         |                          |
| `AnimalCatcher`  |             |                    |                   |            ✅             |

A goat on the library roof needs `ICanClimb`, so it can only go to a
`DronePilot`. Three alpacas on the motorway need `ICanDriveRescueVehicle`, so it
can only go to an `AnimalCatcher`. Nothing needs a subclass per combination.

<details>
<summary><b>Access modifiers</b></summary>

<br>

`Energy` has a private setter. `AdjustEnergy`, `MarkAssigned` and `Resolve` are
`internal`, so only `Core` can drive the lifecycle. The console can ask
questions and issue commands; it cannot reach in and set a field. Energy is
valid from **0 to 100** inclusive, `AdjustEnergy` clamps rather than throwing (a
brutal callout should exhaust somebody, but not crash the dispatch), and a
responder on 0 takes no new work.

</details>

<details>
<summary><b>Collections</b></summary>

<br>

`List<Responder>` and `List<Incident>`, because order carries meaning:
responders stay in registration order, which is what gives
`FirstAvailableStrategy` a stable definition of "first", and incidents stay in
report order. A `Dictionary` keyed by name would lose that and would wrongly
imply names are unique. Both lists are private, and every read hands back a
snapshot copy taken under the lock, so nothing can enumerate one while another
thread appends to it.

</details>

### Swapping the assignment policy

`CommandCentre` never names a concrete strategy. It takes an
`IAssignmentStrategy` on creation and `ChangeStrategy` replaces it, which is the mechanism the demo uses (the singleton rules out simply building a second
centre). Same centre, same roster, different answer:

![The same roster answering to two different policies](docs/images/policy.gif)

`CommandCentre.cs` is not touched between those two picks. Only the object it
was handed changed.

<details>
<summary><b>Why that makes the selection logic easier to change later</b></summary>

<br>

The centre depends on the interface and never on an algorithm, so changing how a
responder is chosen and changing how the centre works are two different jobs in
two different files. A new policy is a new class: nothing that already works
gets edited, and we wont have to grow a `switch` inside the centre for every new
rule that get's added.

The parts that must nt break are the locking, the assignment record and the
guards, and a policy change never touches them, so the no-double-booking
guarantee does not have to be proved again every time the selection rule
changes. The field is typed `IAssignmentStrategy` so the compiler will not let
the centre depend on a concrete strategy by accident.

</details>

## Callbacks

![One callout end to end, with both callback forms](docs/images/callbacks.gif)

Resolving an incident fires every registered `ResolutionCallback`, in
registration order. The demo registers two: a named method that logs the
outcome, and a lambda that reports who is free again. The callbacks are copied
under the lock and invoked outside it, because they are host code and running
arbitrary work while holding a lock is how you get a deadlock.

## The race condition

This is the recording at the top of this page. Seven callouts arrive at once,
twice.

**Without synchronisation**, each task asks who is free, and only afterwards
records its choice. Every task reads the roster before any task writes to it, so they all see the same person as available and all seven land on Bo Nielsen. 
Note that this is not a missing lock. The dictionary write *is* locked. What is
missing is a lock spanning **both** steps, so another thread slips into the gap
between deciding and recording. Individually safe operations do not compose into
a safe operation.

**With synchronisation**, the same seven go through `AssignIncident`, which
holds one lock across choosing AND recording:

```csharp
lock (assignmentLock)
{
    EnsureIncidentIsOpen(incident);
    Responder chosen = assignmentStrategy.SelectResponder(AvailableRespondersNoLock(), incident);
    Record(incident, chosen);
}
```

Five callouts go to five different people and two are refused with
`NoSuitableResponderException`, which is correct once everybody is out. No
responder can be in two places, because no other thread can observe the roster
between the decision and the record.

The concurrency lives in the host: `Main` is `async Task`, the callouts start
together and are awaited with `Task.WhenAll`. `CommandCentre` stays synchronous
on purpose, because you cannot `await` inside a `lock`.

The one `Task.Delay` in the solution widens the race window so the failure is
observable. It is in the demonstration and nowhere near `Core`.

## A second UI

![The raylib dispatch board](docs/images/raylib-board.png)

`src/EmergencyResponse.RaylibUi` is a desktop window built with
[raylib-cs](https://github.com/chrisdill/raylib-cs). It references `Core` and
nothing else, and shares no code with the console. Click a responder to send
them by name, or dispatch by policy. Swap the policy, call in new incidents,
close them off.

It is here as proof that `Core` is not entangled with the console: both hosts
get a string back from `HandleIncident`, one writes it and the other draws it.
The console demonstration is still the canonical showcase.

```bash
dotnet run --project src/EmergencyResponse.RaylibUi
```

Bundles Maple Mono NF under the SIL Open Font License 1.1.

## Warnings are errors

`TreatWarningsAsErrors` is on, and `.editorconfig` promotes the naming rules to
errors via `IDE1006`. So a missing XML doc comment, a nullability warning or a
stray `_underscored` field breaks the build rather than accumulating quietly.

The [build workflow](.github/workflows/ci.yml) runs the same build on every push
and pull request, then runs the tests and the demonstration itself, so a warning
fails CI and not just somebody's machine.

## Regenerating the artwork

The recording at the top of this page is checked in, and so is the script that
produces it. Neither is drawn by hand.

```bash
dotnet build
for tape in docs/tapes/*.tape; do vhs "$tape"; done
for s in roster callbacks policy concurrency; do
    dotnet run --project src/EmergencyResponse.ConsoleDemo -- "$s" > "docs/snapshots/$s.txt"
done
```

[`docs/tapes/`](docs/tapes) holds one [VHS](https://github.com/charmbracelet/vhs)
script per recording, and each one runs a single section of the demonstration by
name. Nothing is piped or trimmed, which is why the colour survives: the program
drops its colour when it sees a redirected stdout.

[`docs/snapshots/`](docs/snapshots) holds the same four sections as plain text,
and the build workflow fails if the program stops producing them. GIFs cannot be
compared: recording the same tape twice gives the same picture but different
bytes, because the frame timings jitter. The text does not, so it is the text
that is checked, and a failure there means a recording needs making again.

`docs/images/social-preview.png` is the card GitHub shows when a link to this
repository is posted somewhere. Behind the title is a full run of the
demonstration, in Maple Mono, the same face the raylib board draws with. It
belongs in **Settings, General, Social preview**, which is the only place GitHub
reads it from.
