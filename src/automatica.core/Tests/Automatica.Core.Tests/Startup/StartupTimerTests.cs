using System;
using System.Threading.Tasks;
using Automatica.Core.Runtime.Core;
using Xunit;

namespace Automatica.Core.Tests.Startup
{
    public class StartupTimerTests
    {
        [Fact]
        public void Summary_ListsPhasesInOrder()
        {
            var timer = new StartupTimer();
            timer.AddPhase("load plugins", TimeSpan.FromSeconds(1.25));
            timer.AddPhase("start 3 drivers", TimeSpan.FromSeconds(42));

            var summary = timer.Summary("Startup", TimeSpan.FromSeconds(44));

            Assert.Equal("Startup took 44s: load plugins 1.3s, start 3 drivers 42s", summary);
        }

        [Fact]
        public void Summary_KeepsOnlySlowItems_SlowestFirst()
        {
            var timer = new StartupTimer();
            timer.AddItem("start fast", TimeSpan.FromMilliseconds(500));
            timer.AddItem("start knx", TimeSpan.FromSeconds(3));
            timer.AddItem("start ebus", TimeSpan.FromSeconds(25));

            var summary = timer.Summary("Startup", TimeSpan.FromSeconds(30));

            Assert.EndsWith("; slowest: start ebus 25s, start knx 3.0s", summary);
            Assert.DoesNotContain("start fast", summary);
        }

        [Fact]
        public void Summary_UsesInvariantCulture()
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-AT");
                var timer = new StartupTimer();
                timer.AddPhase("license", TimeSpan.FromSeconds(1.5));

                Assert.Contains("license 1.5s", timer.Summary("Startup", TimeSpan.FromSeconds(1.5)));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = culture;
            }
        }

        [Fact]
        public async Task Measure_RecordsPhase_WhenActionThrows()
        {
            var timer = new StartupTimer();

            await Assert.ThrowsAsync<InvalidOperationException>(() => timer.Measure("configure", () => throw new InvalidOperationException()));

            Assert.Contains("configure", timer.Summary("Startup", timer.Total));
        }

        [Fact]
        public async Task Measure_ReturnsResult()
        {
            var timer = new StartupTimer();

            var result = await timer.Measure("load node instances", () => Task.FromResult(42));

            Assert.Equal(42, result);
            Assert.Contains("load node instances", timer.Summary("Startup", timer.Total));
        }

        [Fact]
        public async Task MeasureItem_IsSafeForParallelDrivers()
        {
            var timer = new StartupTimer();
            var tasks = new Task[50];
            for (var i = 0; i < tasks.Length; i++)
            {
                var name = $"start driver {i}";
                tasks[i] = timer.MeasureItem(name, () => Task.Delay(1));
                timer.AddItem(name + " slow", TimeSpan.FromSeconds(2 + i % 3));
            }
            await Task.WhenAll(tasks);

            var summary = timer.Summary("Startup", timer.Total);

            // only the 10 slowest items are listed
            Assert.Equal(10, summary.Split("slow ").Length - 1);
        }
    }
}
