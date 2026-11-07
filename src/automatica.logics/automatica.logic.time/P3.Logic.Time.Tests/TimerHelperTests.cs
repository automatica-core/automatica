using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace P3.Logic.Time.Tests
{
    public class TimerHelperTests
    {
        public static IEnumerable<object[]> TruthTable => new[]
        {
            new object[] { null, false },
            new object[] { true, true },
            new object[] { false, false },
            new object[] { 1, true },
            new object[] { 0, false },
            new object[] { -1, true },
            new object[] { 1L, true },
            new object[] { (byte)0, false },
            new object[] { 0.0001d, true },
            new object[] { 0d, false },
            new object[] { -0d, false },
            new object[] { double.NaN, true },   // NaN != 0
            new object[] { 2.5f, true },
            new object[] { 1m, true },
            new object[] { 0m, false },
            new object[] { "true", true },
            new object[] { "True", true },
            new object[] { "TRUE", true },
            new object[] { "false", false },
            new object[] { "1", true },
            new object[] { "0", false },
            new object[] { "0.0", false },
            new object[] { "0.5", true },
            new object[] { "1e3", true },
            new object[] { "-2", true },
            new object[] { "", false },
            new object[] { " ", false },
            new object[] { "yes", false },
            new object[] { "on", false },
            new object[] { "NaN", true },
            new object[] { 'a', false },          // char is IConvertible but ToDouble throws
            new object[] { new object(), false },
            new object[] { DateTime.UnixEpoch, false }, // ToDouble throws
        };

        [Theory]
        [MemberData(nameof(TruthTable))]
        public void IsTruthy(object value, bool expected)
        {
            Assert.Equal(expected, TimerHelper.IsTruthy(value));
        }

        [Fact]
        public void IsTruthy_CommaDecimal_IsNotParsedWithTheCurrentCulture()
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-AT");
                // invariant parsing: "0,0" is not a number -> false, "1,5" is not a number -> false
                Assert.False(TimerHelper.IsTruthy("0,0"));
                Assert.False(TimerHelper.IsTruthy("1,5"));
                Assert.True(TimerHelper.IsTruthy("1.5"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        [Theory]
        [InlineData(double.NaN, 1d)]
        [InlineData(double.NegativeInfinity, 1d)]
        [InlineData(-1000d, 1d)]
        [InlineData(0d, 1d)]
        [InlineData(0.5d, 1d)]
        [InlineData(1d, 1d)]
        [InlineData(1.5d, 1.5d)]
        [InlineData(1000d, 1000d)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(int.MaxValue + 1d, int.MaxValue)]
        [InlineData(double.PositiveInfinity, int.MaxValue)]
        public void ClampInterval(double input, double expected)
        {
            var result = TimerHelper.ClampInterval(input);

            Assert.Equal(expected, result);
            // a System.Timers.Timer accepts the result
            using var timer = new System.Timers.Timer();
            timer.Interval = result;
        }
    }
}
