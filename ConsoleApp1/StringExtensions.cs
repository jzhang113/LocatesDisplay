using System;
using System.Globalization;

namespace LocatesParser
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (!String.IsNullOrEmpty(value) && value.Length > maxLength)
            {
                return value.Substring(0, maxLength);
            }

            return value;
        }

        public static string ToTitleCase(this string value)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
        }
    }
}
