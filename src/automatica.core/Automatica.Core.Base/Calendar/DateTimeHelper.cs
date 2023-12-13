using System;

namespace Automatica.Core.Base.Calendar
{
    public static class DateTimeHelper
    {
        public static DateTime StartOfDay(this DateTime theDate)
        {
            return theDate.Date;
        }

        public static DateTime EndOfDay(this DateTime theDate)
        {
            return theDate.Date.AddDays(1).AddTicks(-1);
        }

        public static bool IsToday(this DateTime theDate)
        {
            var now = DateTime.Now;
            
            return theDate.Date == now.Date;
        }
    }
}
