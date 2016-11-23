namespace Unosquare.Labs.LiteLib
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        private const string IntegerAffinity = "INTEGER";
        private const string NumericAffinity = "NUMERIC";
        private const string TextAffinity = "TEXT";
        private const string DateTimeAffinity = "DATETIME";

        private static readonly Dictionary<Type, string> TypeMappings = new Dictionary<Type, string>
        {
            {typeof (Int16), IntegerAffinity},
            {typeof (Int32), IntegerAffinity},
            {typeof (Int64), IntegerAffinity},
            {typeof (UInt16), IntegerAffinity},
            {typeof (UInt32), IntegerAffinity},
            {typeof (UInt64), IntegerAffinity},
            {typeof (byte), IntegerAffinity},
            {typeof (char), IntegerAffinity},
            {typeof (Decimal), NumericAffinity},
            {typeof (Boolean), NumericAffinity},
            {typeof (DateTime), DateTimeAffinity},
        };

        /// <summary>
        /// Gets the type mapping.
        /// </summary>
        /// <param name="propertyType">Type of the property.</param>
        /// <returns></returns>
        public static string GetTypeMapping(this Type propertyType)
        {
            return TypeMappings.ContainsKey(propertyType) ? TypeMappings[propertyType] : TextAffinity;
        }
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
