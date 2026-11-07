// LogicTest uses process-wide singletons (dispatcher, fake time provider), so the tests must not run in parallel
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
