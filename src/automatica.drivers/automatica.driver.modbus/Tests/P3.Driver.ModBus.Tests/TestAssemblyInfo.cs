// the tests share a tcp port / dispatcher state, running test classes in parallel makes them flaky
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
