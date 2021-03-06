﻿using System;

public static class TimespanExtensions
{
    public static string ToHumanReadableString(this TimeSpan t)
    {
        if (t.TotalSeconds < 1)
        {
            return $@"{t:s\.ff} seconds";
        }
        if (t.TotalSeconds == 1)
        {
            return "1 second";
        }
        if (t.TotalMinutes < 1)
        {
            return $"{t:%s} seconds";
        }
        if (t.TotalMinutes == 1)
        {
            return "1 minute";
        }
        if (t.TotalHours < 1)
        {
            return $"{t:%m} minutes";
        }
        if (t.TotalHours == 1)
        {
            return "1 hour";
        }
        if (t.TotalDays < 1)
        {
            return $"{t:%h} hours";
        }
        if (t.TotalDays == 1)
        {
            return "1 day";
        }

        return $@"{t:%d} days";
    }
}