using System;

namespace Automatica.Core.Internals.OperatingModes
{
    /// <summary>
    /// How logics see the operating modes without a core API (logic plugins build against older core packages):
    /// the core dispatches the state as JSON on <see cref="Base.IO.DispatchableType.Control"/> / <see cref="StateId"/>,
    /// logics read it with <c>IDispatcher.GetValue</c> and ask for manual changes by dispatching to <see cref="RequestId"/>.
    /// The ids are copied in P3.Logic.Time (OperatingModes/OperatingModeWatcher.cs), never change them.
    /// </summary>
    public static class OperatingModeContract
    {
        /// <summary>State JSON, see <see cref="OperatingModeState"/></summary>
        public static readonly Guid StateId = new Guid("f78460f9-88bb-4ab5-85e0-0701d6cebfd3");

        /// <summary>Request JSON from logics: <c>{"mode":"vacation","state":true|false|null}</c></summary>
        public static readonly Guid RequestId = new Guid("f84abac5-dfd4-4efc-a4fa-cde8d11788c0");

        public const string PublicHoliday = "public-holiday";
        public const string Vacation = "vacation";
        public const string SchoolHolidays = "school-holidays";

        /// <summary>Days ahead in the published state (alarm clocks look ahead for the next alarm)</summary>
        public const int PublishedDays = 14;
    }
}
