namespace ServerMonitorCore.Common;

public static class Strings {
    public static string Quoted(this string @string) => $"\"{@string}\"";

    public static string LastWord(this string @string, char delimiter = ' ') =>
        @string.Substring(@string.LastIndexOf(delimiter) + 1);

    public static bool IsNotEmpty(this string value) => !string.IsNullOrEmpty(value);
}
