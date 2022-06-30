using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMonitorCore.Common;

public static class Strings {
    public static string Quoted(this string @string) => $"\"{@string}\"";
}
