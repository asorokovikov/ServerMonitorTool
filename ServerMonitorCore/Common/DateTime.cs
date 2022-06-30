using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMonitorCore.Common;

public static class DateTimeHelper {

    public static string Humanize(this TimeSpan timeSpan) {
        string result;
        if (timeSpan <= TimeSpan.FromSeconds(60)) {
            result = $"{timeSpan.Seconds} seconds ago";
        }
        else if (timeSpan <= TimeSpan.FromMinutes(60)) {
            result = timeSpan.Minutes > 1 ? $"about {timeSpan.Minutes} minutes ago" : "about a minute ago";
        }
        else if (timeSpan <= TimeSpan.FromHours(24)) {
            result = timeSpan.Hours > 1 ? $"about {timeSpan.Hours} hours ago" : "about an hour ago";
        }
        else if (timeSpan <= TimeSpan.FromDays(30)) {
            result = timeSpan.Days > 1 ? $"about {timeSpan.Days} days ago" : "yesterday";
        }
        else if (timeSpan <= TimeSpan.FromDays(365)) {
            result = timeSpan.Days > 30 ? $"about {timeSpan.Days / 30} months ago" : "about a month ago";
        }
        else {
            result = timeSpan.Days > 365 ? $"about {timeSpan.Days / 365} years ago" : "about a year ago";
        }
        return result;
    }
}

