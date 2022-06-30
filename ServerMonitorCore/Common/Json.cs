using System.Text.Json;

namespace ServerMonitorCore.Common;
public static class Json {
        private static readonly JsonSerializerOptions options = new() { WriteIndented = true };
        public static string ToJson(this object value) => JsonSerializer.Serialize(value, options);
    }

