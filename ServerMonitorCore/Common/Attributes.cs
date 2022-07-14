using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServerMonitorCore.Common;

public sealed class NameAttribute : Attribute {
    public string Name { get; }
    public NameAttribute(string name) => Name = name;
}

public static class Attributes {
    // enumtype to (attributeType to (enumValue to attribute))
    private static readonly Dictionary<Type, Dictionary<Type, Dictionary<object, object?>>> AttributesCache = new();
    private static object _lockObject = new();

    private static Dictionary<object, object?> 
    GetEnumAttributeCache(this Type enumType, Type attributeType) {
        if (AttributesCache.TryGetValue(enumType, out var attributes)) {
            if (attributes.TryGetValue(attributeType, out var result))
                return result;
            result = new();
            attributes[attributeType] = result;
            return result;
        }

        lock(_lockObject) {
            attributes = new();
            attributes[attributeType] = new();
            AttributesCache[enumType] = attributes;
            return attributes[attributeType];
        }
    }

    public static string
    GetName<T>(this T value) where T : Enum {
        if (value.TryGetEnumAttributes<NameAttribute>(out var result)) 
            return result.Name;
        return value.ToString();
    }


    private static T GetAttribute<T>(this object value) {
        var attributes = value is PropertyInfo propertyInfo
            ? propertyInfo.GetCustomAttributes(typeof(T), true)
            : value.GetType().GetCustomAttributes(typeof(T), true);
        if (attributes.Length == 0)
            throw new InvalidOperationException($"Expecting attribute {typeof(T)} on {value}");
        return (T) attributes[0];
    }

    private static bool TryGetEnumAttributes<T>(this object @object, [NotNullWhen(true)] out T? result) {
        var enumType = @object.GetType();
        var attributesCache = enumType.GetEnumAttributeCache(attributeType: typeof(T));
        if (attributesCache.TryGetValue(@object, out var attribute)) {
            result = attribute != null ? (T)attribute : default;
            return result != null;
        }
        if (TryGetEnumAttributeSlow<T>(@object, out result)) {
            attributesCache[@object] = result;
            return true;
        }
        attributesCache[@object] = null;
        return false;
    }

    private static bool
    TryGetEnumAttributeSlow<T>(this object @object, [NotNullWhen(true)] out T? result) {
        result = default;
        var objectType = @object.GetType();
        var memberName = @object.ToString();
        if (string.IsNullOrEmpty(memberName))
            return false;
        var attributes = objectType
            .GetMember(memberName)
            .First(x => x.DeclaringType == objectType)
            .GetCustomAttributes(typeof(T), false);
        if (attributes?.Length > 0) {
            result = (T)attributes[0];
            return true;
        }
        return false;
    }

}