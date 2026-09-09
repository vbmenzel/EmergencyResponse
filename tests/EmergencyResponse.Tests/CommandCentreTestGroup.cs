namespace EmergencyResponse.Tests;

/// Marker type that groups the command centre tests into one xUnit collection.
///
/// The command centre is a singleton, so its state is process-wide. Tests that
/// initialise it must not run alongside each other or alongside other
/// collections, or one test's centre becomes another test's surprise.
///
/// xUnit convention would name this CommandCentreCollection, but CA1711 rejects
/// a type ending in "Collection" that is not one. Only the string in the
/// attribute binds to [Collection("CommandCentre")], so the class name is free.
[CollectionDefinition("CommandCentre", DisableParallelization = true)]
public class CommandCentreTestGroup;
