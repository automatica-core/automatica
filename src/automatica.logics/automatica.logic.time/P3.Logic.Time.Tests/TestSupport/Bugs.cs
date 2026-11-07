namespace P3.Logic.Time.Tests.TestSupport
{
    /// <summary>
    /// Skip reasons of the tests that expose known bugs. Build with -p:DefineConstants=VERIFY_BUGS to run them (they fail until fixed).
    /// </summary>
    internal static class Bugs
    {
#if VERIFY_BUGS
        public const string WeekdayMaskIgnored = null;
        public const string HourlyGraceCutsWindow = null;
        public const string UtcTimeOfDayOnLocalDate = null;
        public const string DstShiftsDailyEntries = null;
        public const string MonoflopDelayChangeIgnored = null;
        public const string WeeklyWithoutWkstThrows = null;
        public const string InvalidRecurrenceThrows = null;
        public const string TimerDstInterval = null;
#else
        public const string WeekdayMaskIgnored =
            "BUG: AdvancedTimer ignores the BYDAY weekday mask once the entry's first occurrence is in the past (RFC2445Recur.Iterate yields one date without COUNT, AdvancedTimerRule.cs:244-277 moves it to today)";

        public const string HourlyGraceCutsWindow =
            "BUG: AdvancedTimer hourly windows longer than 5 minutes switch off after 5 minutes (5 minute grace in AdvancedTimerRule.cs:315 moves the running window to the next hour)";

        public const string UtcTimeOfDayOnLocalDate =
            "BUG: AdvancedTimer puts the UTC time of day on the LOCAL date (AdvancedTimerRule.cs:265-291), so a window whose UTC date differs from the local date (around local midnight, east of UTC) lands on the wrong day";

        public const string DstShiftsDailyEntries =
            "BUG: AdvancedTimer keeps the UTC time of day of an entry (AdvancedTimerRule.cs:236-291), so a daily entry created in winter runs one hour later on the wall clock in summer (Europe/Vienna)";

        public const string MonoflopDelayChangeIgnored =
            "BUG: Monoflop never sets _timerRunning = true (MonoflopRule.cs:61), so a delay change while the pulse runs does not restart it with the new delay";

        public const string WeeklyWithoutWkstThrows =
            "BUG: AdvancedTimer Start throws InvalidOperationException for FREQ=WEEKLY;BYDAY=.. without WKST (RFC2445Recur.Truncate uses r.Wkst.Value, Automatica.Core.Base Calendar/RFC2445Recur.cs:586), the whole timer does not start";

        public const string InvalidRecurrenceThrows =
            "BUG: AdvancedTimer Start throws FormatException for an unparsable recurrence rule (new RFC2445Recur at AdvancedTimerRule.cs:228 throws before the IsValid check), one bad entry stops all entries";

        public const string TimerDstInterval =
            "BUG: Timer schedules its next tick with the wall clock difference (TimerRule.cs:111, nextChange - now in local wall time), so on DST days the tick is an hour late (spring) or early (fall, and the early tick then switches at the boundary)";
#endif
    }
}
