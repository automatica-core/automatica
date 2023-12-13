// Copyright (c) 2010 Anton Tykhyy
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the Software
// is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS 
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
// AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
#if UNITTEST
using System.Linq ;
using System.Diagnostics ;
#endif

namespace Automatica.Core.Base.Calendar
{
    /// <summary>
    /// Cf. http://google-rfc-2445.googlecode.com/svn/trunk/rfc2445.html#4.3.10
    /// </summary>
    public sealed class RFC2445Recur
    {
        [TypeConverter(typeof(RuleConverter))]
        public partial struct Rule
        {
            #region --[Fields: Public]------------------------------------
            public Frequency Frequency;
            public DayOfWeek Wkst;
            public DateTime Until;
            public int Count;
            public int Interval;
            public int[] BySetPos;
            public int[] BySecond;
            public int[] ByMinute;
            public int[] ByHour;
            public int[] ByMonthDay;
            public int[] ByYearDay;
            public int[] ByWeekNo;
            public int[] ByMonth;
            public Tuple<DayOfWeek, int>[] ByDay;
            #endregion

            #region --[Methods]-------------------------------------------
            public bool IsValid => Frequency != Frequency.None && new Rule().TryParseInternal(ToString());

            public static Rule Parse(string s)
            {
                var result = new Rule();
                if (result.TryParseInternal(s))
                    return result;

                throw new FormatException();
            }

            public override string ToString()
            {
                var s = new StringBuilder();
                s.Append("FREQ=");
                s.Append(Frequency);

                if (Until != default)
                {
                    s.Append(";UNTIL=");
                    s.Append(Until);
                }

                if (Count != 0)
                {
                    s.Append(";COUNT=");
                    s.Append(Count);
                }

                if (Interval != 1)
                {
                    s.Append(";INTERVAL=");
                    s.Append(Interval);
                }

                if (Wkst != DayOfWeek.Monday)
                {
                    s.Append(";WKST=");
                    s.Append(Wkst);
                }

                if (ByDay != null && ByDay.Length != 0)
                {
                    s.Append(";BYDAY");

                    var sep = '=';

                    foreach (var byday in ByDay)
                    {
                        s.Append(sep);
                        sep = ',';

                        if (byday.Item2 != 0)
                            s.Append(byday.Item2);

                        s.Append(byday.Item1);
                    }
                }

                AppendByList(s, ";BYSETPOS", BySetPos);
                AppendByList(s, ";BYSECOND", BySecond);
                AppendByList(s, ";BYMINUTE", ByMinute);
                AppendByList(s, ";BYHOUR", ByHour);
                AppendByList(s, ";BYMONTHDAY", ByMonthDay);
                AppendByList(s, ";BYYEARDAY", ByYearDay);
                AppendByList(s, ";BYWEEKNO", ByWeekNo);
                AppendByList(s, ";BYMONTH", ByMonth);

                return s.ToString();
            }

            private static void AppendByList(StringBuilder s, string key, int[] array)
            {
                if (array != null && array.Length != 0)
                {
                    s.Append(key);

                    var sep = '=';

                    foreach (var elem in array)
                    {
                        s.Append(sep);
                        sep = ',';

                        s.Append(elem);
                    }
                }
            }

            internal bool TryParseInternal(string value)
            {
                value = value.ToUpperInvariant();
                var wkst = false;
                var inte = false;

                Interval = 1;
                Wkst = DayOfWeek.Monday;

                foreach (var rulepart in value.Split(';'))
                {
                    int index;
                    if ((index = rulepart.IndexOf('=') + 1) <= 0 ||
                        index == rulepart.Length) return false;

                    value = rulepart.Substring(index);
                    switch (rulepart.Substring(0, index - 1))
                    {
                        case "FREQ":
                            if (Frequency == default(Frequency) &&
                                Enum.TryParse(value, out Frequency)) continue;
                            else return false;

                        case "WKST":
                            if (wkst) return false;
                            /**/
                            wkst = true;

                            if (TryParseWeekday(value, out Wkst)) continue;
                            else return false;

                        case "INTERVAL":
                            if (inte) return false;
                            /**/
                            inte = true;

                            if (Int32.TryParse(value, out Interval) && Interval > 0) continue;
                            else return false;

                        case "UNTIL":
                            if (Until == default(DateTime) &&
                                Count == 0 &&
                                DateTime.TryParse(value, out Until) &&
                               (Until == Until.Date || Until.Kind == DateTimeKind.Utc)) continue;
                            else return false;

                        case "COUNT":
                            if (Until == default(DateTime) &&
                                Count == 0 &&
                                Int32.TryParse(value, out Count) && Count > 0) continue;
                            else return false;

                        case "BYDAY":
                            if (ByDay != null)
                                return false;

                            var parts = value.Split(',');
                            if (parts.Length == 0)
                                return false;

                            ByDay = new Tuple<DayOfWeek, int>[parts.Length];
                            for (int i = 0; i < parts.Length; ++i)
                            {
                                DayOfWeek day;

                                var s = parts[i];
                                if (s.Length < 2 || !TryParseWeekday(s.Substring(s.Length - 2), out day))
                                    return false;

                                index = s[0] == '+' ? 1 : 0;
                                if (s.Length > 2 && (!Int32.TryParse(s.Substring(index, s.Length - 2 - index), out index) || index < -53 || index > 53 || index == 0))
                                    return false;

                                ByDay[i] = Tuple.Create(day, index);
                            }

                            continue;

                        case "BYSECOND":
                            if (TryParseByList(value, 0, 59, ref BySecond)) continue;
                            else return false;

                        case "BYMINUTE":
                            if (TryParseByList(value, 0, 59, ref ByMinute)) continue;
                            else return false;

                        case "BYHOUR":
                            if (TryParseByList(value, 0, 23, ref ByHour)) continue;
                            else return false;

                        case "BYMONTHDAY":
                            if (TryParseByList(value, -31, 31, ref ByMonthDay)) continue;
                            else return false;

                        case "BYYEARDAY":
                            if (TryParseByList(value, -366, 366, ref ByYearDay)) continue;
                            else return false;

                        case "BYWEEKNO":
                            if (TryParseByList(value, -53, 53, ref ByWeekNo)) continue;
                            else return false;

                        case "BYMONTH":
                            if (TryParseByList(value, 1, 12, ref ByMonth)) continue;
                            else return false;

                        case "BYSETPOS":
                            if (TryParseByList(value, -366, 366, ref BySetPos)) continue;
                            else return false;

                        default:
                            continue;
                    }
                }

                if (Frequency == default(Frequency))
                    return false;

                if (BySetPos != null &&
                    ByMonth == null &&
                    ByWeekNo == null &&
                    ByYearDay == null &&
                    ByMonthDay == null &&
                    ByDay == null &&
                    ByHour == null &&
                    ByMinute == null &&
                    BySecond == null)
                    return false;

                if (Frequency != Frequency.YEARLY &&
                    ByWeekNo != null)
                    return false;

                return true;
            }

            private static bool TryParseWeekday(string value, out DayOfWeek result)
            {
                switch (value.ToUpperInvariant())
                {
                    case "SU": result = DayOfWeek.Sunday; return true;
                    case "MO": result = DayOfWeek.Monday; return true;
                    case "TU": result = DayOfWeek.Tuesday; return true;
                    case "WE": result = DayOfWeek.Wednesday; return true;
                    case "TH": result = DayOfWeek.Thursday; return true;
                    case "FR": result = DayOfWeek.Friday; return true;
                    case "SA": result = DayOfWeek.Saturday; return true;
                    default: result = default(DayOfWeek); return false;
                }
            }

            private static bool TryParseByList(string value, int from, int to, ref int[] result)
            {
                if (result != null)
                    return false;

                var elems = value.Split(',');
                var temp = new int[elems.Length];
                int x;

                if (elems.Length == 0)
                    return false;

                for (int i = 0; i < elems.Length; ++i)
                {
                    var s = elems[i];
                    if (s.Length > 0 && s[0] == '+') s = s.Substring(1);

                    if (!Int32.TryParse(s, out x) ||
                        x < from || x > to || from < 0 && x == 0) return false;

                    temp[i] = x;
                }

                result = temp;
                return true;
            }
            #endregion
        }

        private readonly Rule r;
        private readonly DateTime start;

        #region --[Methods: Tests]----------------------------------------
#if UNITTEST
        static int Main ()
        {
            try
            {
                var now = DateTime.Parse ("17.12.2010 16:25:21") ;

                foreach (var t in new Recur (now, "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1")
                    .Iterate (Direction.Forward).Take (5)) Debug.WriteLine (t) ;

                foreach (var t in new Recur (now, "FREQ=YEARLY;INTERVAL=2;BYMONTH=1;BYDAY=SU;BYHOUR=8,9;BYMINUTE=30")
                    .Iterate (Direction.Forward).Take (5)) Debug.WriteLine (t) ;

                return 1 ;

            }
            catch (Exception e)
            {
                Console.WriteLine (e) ;
                return 0 ;
            }
        }
#endif
        #endregion

        #region --[Methods: Public]---------------------------------------
        public RFC2445Recur(DateTime start, Rule r)
        {
            if (!r.IsValid) throw new ArgumentException();

            this.start = start;
            this.r = r;
        }

        public RFC2445Recur(DateTime start, string s)
        {
            this.start = start;
            this.r = new Rule();

            if (!r.TryParseInternal(s)) throw new FormatException();
        }

        public RFC2445Recur(DateTime start, RFC2445Recur other)
        {
            this.start = start;
            this.r = other.r;
        }

        public DateTime Start => start;
        public Rule Freq => r;

        public override string ToString()
        {
            return $"DTSTART={start};{r.ToString()}";
        }

        public IEnumerable<DateTime> Iterate(Direction direction)
        {
            return DoIterate(
                Filter(r.BySetPos,
                Filter(r.BySecond, t => t.Second,
                Filter(r.ByMinute, t => t.Minute,
                Filter(r.ByHour, t => t.Hour,
                Filter(r.ByDay,
                Filter(r.ByMonthDay, t => t.Day,
                Filter(r.ByYearDay, t => t.DayOfYear,
                Filter(r.ByWeekNo, WeekOfYear,
                Filter(r.ByMonth, t => t.Month,
                GenerateInterval(start, direction)))))))))));
        }
        #endregion

        #region --[Methods: Private: Iteration]---------------------------
        private IEnumerable<DateTime> DoIterate(IEnumerable<DateTime> g)
        {
            int count = 0;

            foreach (var t in g)
            {
                if (r.Until != default(DateTime) && t > r.Until)
                    yield break;

                if (r.Count >= ++count)
                    yield break;

                yield return t;
            }
        }

        private int DoYfirstWeekStart(DateTime t)
        {
            var d = FirstOf(r.Wkst, new DateTime(t.Year, 1, 1));
            if (d > 4) d -= 7;

            return d;
        }

        private int WeekOfYear(DateTime t)
        {
            // produces 0 when t falls into the incomplete "0th" week, which filters out nicely
            return 1 + (t.DayOfYear - DoYfirstWeekStart(t)) / 7;
        }

        private static int FirstOf(DayOfWeek dow, DateTime of)
        {
            return 1 + (DoWoffset(dow, of) + 7) % 7;
        }

        private static int DoWoffset(DayOfWeek dow, DateTime date)
        {
            return (int)dow - (int)date.DayOfWeek;
        }

        private IEnumerable<DateTime> Filter(int[] list, IEnumerable<DateTime> g)
        {
            if (list == null || list.Length == 0) return g;
            else return DoFilter(g, list);
        }

        private IEnumerable<DateTime> DoFilter(IEnumerable<DateTime> g, int[] list)
        {
            var pool = new List<DateTime>();
            var last = default(DateTime);

            foreach (var t in g)
            {
                var curr = Truncate(t);
                if (curr != last)
                {
                    for (int i = 0; i < pool.Count; ++i)
                        if (Array.IndexOf(list, i + 1) >= 0 ||
                            Array.IndexOf(list, i - pool.Count) >= 0)
                            yield return pool[i];

                    last = curr;
                    pool.Clear();
                }

                pool.Add(t);
            }
        }

        private IEnumerable<DateTime> Filter(Tuple<DayOfWeek, int>[] list, IEnumerable<DateTime> g)
        {
            if (list == null || list.Length == 0) return g;
            else return DoFilter(g, list);
        }

        private IEnumerable<DateTime> DoFilter(IEnumerable<DateTime> g, Tuple<DayOfWeek, int>[] list)
        {
            foreach (var t in g)
            {
                var dow = t.DayOfWeek;

                foreach (var spec in list)
                    if (dow == spec.Item1)
                    {
                        if (spec.Item2 == 0)
                        {
                            yield return t;
                            continue;
                        }

                        DateTime tbase;
                        switch (r.Frequency)
                        {
                            case Frequency.YEARLY: tbase = spec.Item2 > 0 ? new DateTime(t.Year, 1, 1) : new DateTime(t.Year, 12, 31); break;
                            case Frequency.MONTHLY: tbase = spec.Item2 > 0 ? new DateTime(t.Year, t.Month, 1) : new DateTime(t.Year, t.Month, DateTime.DaysInMonth(t.Year, t.Month)); break;
                            default: continue;
                        }

                        int delta = spec.Item2 > 0 ? 1 : -1;

                        if (spec.Item2 == delta + (int)(t - tbase.AddDays((DoWoffset(dow, tbase) + delta * 7) % 7)).TotalDays / 7)
                            yield return t;
                    }
            }
        }

        private static IEnumerable<DateTime> Filter(int[] list, Func<DateTime, int> getter, IEnumerable<DateTime> g)
        {
            if (list == null || list.Length == 0) return g;
            else return DoFilter(g, getter, list);
        }

        private static IEnumerable<DateTime> DoFilter(IEnumerable<DateTime> g, Func<DateTime, int> getter, int[] list)
        {
            foreach (var t in g)
                if (Array.IndexOf(list, getter(t)) >= 0)
                    yield return t;
        }

        private static Func<DateTime, int, DateTime> GetAdder(Frequency frequency)
        {
            switch (frequency)
            {
                case Frequency.YEARLY: return (t, n) => t.AddYears(n);
                case Frequency.MONTHLY: return (t, n) => t.AddMonths(n);
                case Frequency.WEEKLY: return (t, n) => t.AddDays(n * 7);
                case Frequency.DAILY: return (t, n) => t.AddDays(n);
                case Frequency.HOURLY: return (t, n) => t.AddHours(n);
                case Frequency.MINUTELY: return (t, n) => t.AddMinutes(n);
                case Frequency.SECONDLY: return (t, n) => t.AddSeconds(n);

                default: throw new InvalidOperationException();
            }
        }

        private DateTime Truncate(DateTime date)
        {
            const long TicksPerWeek = TimeSpan.TicksPerDay * 7;
            switch (r.Frequency)
            {
                case Frequency.YEARLY: return new DateTime(date.Year, 1, 1);
                case Frequency.MONTHLY: return new DateTime(date.Year, date.Month, 1);
                case Frequency.DAILY: return new DateTime((date.Ticks / TimeSpan.TicksPerDay) * TimeSpan.TicksPerDay);
                case Frequency.HOURLY: return new DateTime((date.Ticks / TimeSpan.TicksPerHour) * TimeSpan.TicksPerHour);
                case Frequency.MINUTELY: return new DateTime((date.Ticks / TimeSpan.TicksPerMinute) * TimeSpan.TicksPerMinute);
                case Frequency.SECONDLY: return new DateTime((date.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond);
                case Frequency.WEEKLY:
                    return new DateTime(((date.Ticks - TimeSpan.TicksPerDay * ((int)r.Wkst - (int)DayOfWeek.Monday)) / TicksPerWeek) * TicksPerWeek
                                                                     + TimeSpan.TicksPerDay * ((int)r.Wkst - (int)DayOfWeek.Monday));
                default: throw new InvalidOperationException();
            }
        }

        private IEnumerable<DateTime> GenerateInterval(DateTime start, Direction direction)
        {
            var minfreq = Frequency.YEARLY;
            if (r.ByMonth != null) minfreq = Frequency.MONTHLY;
            if (r.ByWeekNo != null) minfreq = Frequency.WEEKLY;
            if (r.ByDay != null ||
                r.ByMonthDay != null ||
                r.ByYearDay != null) minfreq = Frequency.DAILY;
            if (r.ByHour != null) minfreq = Frequency.HOURLY;
            if (r.ByMinute != null) minfreq = Frequency.MINUTELY;
            if (r.BySecond != null) minfreq = Frequency.SECONDLY;

            var freqinter = direction == Direction.Forward ? r.Interval : -r.Interval;
            var freqadder = GetAdder(r.Frequency);

            if ((int)minfreq >= (int)r.Frequency)
            {
                while (true)
                {
                    yield return start;
                    start = freqadder(start, freqinter);
                }
            }
            else
            {
                var mininter = direction == Direction.Forward ? 1 : -1;
                var minadder = GetAdder(minfreq);
                var limit = freqadder(Truncate(start), mininter);

                while (true)
                {
                    yield return start;
                    start = minadder(start, mininter);

                    if (r.Interval > 1 && direction.Compare(start, limit) >= 0)
                    {
                        start = freqadder(limit, freqinter - mininter);
                        limit = freqadder(start, mininter);
                    }
                }
            }
        }
        #endregion

        sealed class RuleConverter : TypeConverter
        {
            #region --[Implementation: TypeConverter]---------------------
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (Type.GetTypeCode(sourceType) == TypeCode.String)
                    return true;

                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context,
                System.Globalization.CultureInfo culture, object value)
            {
                if (value is string)
                {
                    var result = new Rule();
                    if (result.TryParseInternal((string)value))
                        return result;

                    throw new FormatException();
                }

                return base.ConvertFrom(context, culture, value);
            }
            #endregion
        }
    }

    public static class DirectionExtensions
    {
        public static int Compare(this Direction d, DateTime a, DateTime b)
        {
            var c = DateTime.Compare(a, b);
            return d == Direction.Forward ? c : -c;
        }
    }

    public enum Direction
    {
        Forward, Backward,
    }

    public enum Frequency
    {
        None, SECONDLY, MINUTELY, HOURLY, DAILY, WEEKLY, MONTHLY, YEARLY,
    }
}