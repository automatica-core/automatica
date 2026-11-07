// LogicTest uses process-wide singletons (DispatcherMock, FakeTimeProvider), the test classes must not run in parallel
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
