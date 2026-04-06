using Xunit;

// Program.RunAsync and several tests redirect static Console.Out/Error; parallel execution races on restore.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
