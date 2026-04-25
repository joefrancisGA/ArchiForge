namespace ArchLucid.Persistence.Tests;

/// <summary>
///     Serializes coordinator tests so <see cref="System.Diagnostics.ActivityListener" />-based correlation
///     tests do not observe <c>DataArchival.RunOnce</c> activities from other classes calling
///     <see cref="ArchLucid.Persistence.Archival.DataArchivalCoordinator.RunOnceAsync" />.
/// </summary>
[CollectionDefinition(nameof(DataArchivalCoordinatorCollection))]
public sealed class DataArchivalCoordinatorCollection : ICollectionFixture<DataArchivalCoordinatorCollectionMarker>;

/// <summary>Per-collection marker only; holds no state.</summary>
public sealed class DataArchivalCoordinatorCollectionMarker;
