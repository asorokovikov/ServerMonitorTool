using System.Diagnostics.CodeAnalysis;

namespace ServerMonitorCore.Common;

static class ThrowHelper {

    [DoesNotReturn]
    internal static void ThrowArgumentGreaterOrEqualZeroException<T>(T value, string? argumentName = null) =>
        throw new ArgumentException($"Expecting value {argumentName} to be greater or equal zero but was {value}");

    [DoesNotReturn]
    internal static void ThrowArgumentGreaterZeroException<T>(T value, string? argumentName = null) =>
        throw new ArgumentException($"Expecting value {argumentName} to be greater zero but was {value}");

    [DoesNotReturn]
    internal static void ThrowArgumentEmptyException<T>(T value, string? argumentName = null) =>
        throw new ArgumentException($"Value {argumentName} is not expected to be empty");
}