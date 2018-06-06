using System;
using System.Globalization;

namespace LocateDisplay
{
    public static class StringExtensions
    {
        public static (string, string) SplitAtWord(this string value, int length)
        {
            if (value == null || value.Length <= length)
                return (value, "");

            if (value.IndexOf(" ", length) == -1)
                return (value.Substring(0, length), value.Substring(length));

            int split = value.IndexOf(" ", length);

            return (value.Substring(0, split), value.Substring(split));
        }

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
