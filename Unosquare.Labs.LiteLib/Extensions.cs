namespace Unosquare.Labs.LiteLib
{
    using System;

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Transform a DateTime to a SQLite UTC date.
        /// </summary>
        /// <param name="utcDate">The UTC date.</param>
        /// <returns></returns>
        public static DateTime ToSQLiteUtcDate(this DateTime utcDate)
        {
            var startupDifference = (int)DateTime.UtcNow.Subtract(DateTime.Now).TotalHours;
            return utcDate.AddHours(startupDifference);
        }
    }
}
