namespace ServerMonitorCore.Common;

public static class UniqueString {

    public static string GetString(string prefix = "", int length = 8) =>
        prefix + Guid.NewGuid().ToString().Replace("-", "")[..length];
}