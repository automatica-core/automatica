using Automatica.Core.Base.Calendar;
using JetBrains.Annotations;
using System.Linq;
using System;
using Xunit;

namespace Automatica.Core.Tests.Calendar;

[TestSubject(typeof(RFC2445Recur))]
public class RFC2445RecurTest
{
    [Fact]
    public void TestRFC2445RecurConstructorWithRule()
    {
        var rule = new RFC2445Recur.Rule { Frequency = Frequency.DAILY, Count = 5, Interval = 1};
        var start = DateTime.Now;
        var rfc2445Recur = new RFC2445Recur(start, rule);

        Assert.Equal(start, rfc2445Recur.Start);
        Assert.Equal(rule, rfc2445Recur.Freq);
    }

    [Fact]
    public void TestRFC2445RecurConstructorWithString()
    {
        var start = DateTime.Now;
        var rfc2445Recur = new RFC2445Recur(start, "FREQ=DAILY;COUNT=5");

        Assert.Equal(start, rfc2445Recur.Start);
        Assert.Equal(Frequency.DAILY, rfc2445Recur.Freq.Frequency);
        Assert.Equal(5, rfc2445Recur.Freq.Count);
    }

    [Fact]
    public void TestRFC2445RecurConstructorWithOtherRFC2445Recur()
    {
        var start = DateTime.Now;
        var rfc2445Recur1 = new RFC2445Recur(start, "FREQ=DAILY;COUNT=5");
        var rfc2445Recur2 = new RFC2445Recur(start, rfc2445Recur1);

        Assert.Equal(rfc2445Recur1.Start, rfc2445Recur2.Start);
        Assert.Equal(rfc2445Recur1.Freq, rfc2445Recur2.Freq);
    }

    [Fact]
    public void TestRFC2445RecurIterate()
    {
        var start = DateTime.Now;
        var rfc2445Recur = new RFC2445Recur(start, "FREQ=DAILY;COUNT=5");
        var dates = rfc2445Recur.Iterate(Direction.Forward).ToList();

        Assert.Equal(6, dates.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(start.AddDays(i), dates[i]);
        }
    }

    [Fact]
    public void TestRFC2445RecurIterate2()
    {
        var start = DateTime.Now;
        var end = DateTime.Now.AddHours(2);
        var rfc2445Recur = new RFC2445Recur(start, "FREQ=DAILY");
        var rfc2445RecurEnd = new RFC2445Recur(end, "FREQ=DAILY");
        var startDates = rfc2445Recur.Iterate(Direction.Forward).ToList();
        var endDates = rfc2445RecurEnd.Iterate(Direction.Forward).ToList();

        Assert.Single(startDates);
        Assert.Single(endDates);

    }

    [Fact]
    public void TestRFC2445RecIterateByDay_ExcepctToday()
    {
        var start = DateTime.Now;
        var recurrenceRule = new RFC2445Recur.Rule();
        recurrenceRule.ByDay = new[]
        {
            new Tuple<DayOfWeek, int>(DayOfWeek.Sunday, DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? -1 : 0),
            new Tuple<DayOfWeek, int>(DayOfWeek.Monday,  DateTime.Now.DayOfWeek == DayOfWeek.Monday ? -1 : 0),
            new Tuple<DayOfWeek, int>(DayOfWeek.Tuesday,  DateTime.Now.DayOfWeek == DayOfWeek.Tuesday ? -1 : 0),
            new Tuple<DayOfWeek, int>(DayOfWeek.Wednesday, DateTime.Now.DayOfWeek == DayOfWeek.Wednesday ?  -1 : 0),
            new Tuple<DayOfWeek, int>(DayOfWeek.Thursday,  DateTime.Now.DayOfWeek == DayOfWeek.Thursday ? -1 : 0),
            new Tuple<DayOfWeek, int>(DayOfWeek.Friday,  DateTime.Now.DayOfWeek == DayOfWeek.Friday ? -1 : 0),
            new Tuple<DayOfWeek, int>(DayOfWeek.Saturday, DateTime.Now.DayOfWeek == DayOfWeek.Saturday ?  -1 : 0)
        };

        recurrenceRule.Frequency = Frequency.DAILY;
        
        
        var rfc2445Recur = new RFC2445Recur(start, recurrenceRule);

        var dates = rfc2445Recur.Iterate(Direction.Forward).ToList();
        Assert.Empty(dates);
    }

    [Fact]
    public void TestRFC2445RecIterateByDay_OnlyToday()
    {
        var start = DateTime.Now;
        var recurrenceRule = new RFC2445Recur.Rule();
        recurrenceRule.ByDay = new[]
        {
            new Tuple<DayOfWeek, int>(DayOfWeek.Sunday, DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? 0 : -1),
            new Tuple<DayOfWeek, int>(DayOfWeek.Monday,  DateTime.Now.DayOfWeek == DayOfWeek.Monday ? 0 : -1),
            new Tuple<DayOfWeek, int>(DayOfWeek.Tuesday,  DateTime.Now.DayOfWeek == DayOfWeek.Tuesday ? 0 : -1),
            new Tuple<DayOfWeek, int>(DayOfWeek.Wednesday, DateTime.Now.DayOfWeek == DayOfWeek.Wednesday ?  0 : -1),
            new Tuple<DayOfWeek, int>(DayOfWeek.Thursday,  DateTime.Now.DayOfWeek == DayOfWeek.Thursday ? 0 : -1),
            new Tuple<DayOfWeek, int>(DayOfWeek.Friday,  DateTime.Now.DayOfWeek == DayOfWeek.Friday ? 0 : -1),
            new Tuple<DayOfWeek, int>(DayOfWeek.Saturday, DateTime.Now.DayOfWeek == DayOfWeek.Saturday ?  0 : -1)
        };
        recurrenceRule.Frequency = Frequency.DAILY;

        var rfc2445Recur = new RFC2445Recur(start, recurrenceRule);

        var dates = rfc2445Recur.Iterate(Direction.Forward).ToList();
        Assert.Single(dates);
    }
}