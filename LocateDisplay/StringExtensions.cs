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
    }
}
