using System;

namespace CustomExtensions
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

        public static string TruncateAtWord(this string value, int length)
        {
            if (value == null || value.Length < length)
                return value;

            if (value.IndexOf(" ", length) == -1)
                return value.Substring(0, length);

            return value.Substring(0, value.IndexOf(" ", length));
        }
    }
}
