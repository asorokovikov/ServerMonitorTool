
namespace ServerMonitorCore.Common;

public static class Verify {

    public static string
    VerifyNotEmpty(this string value, string? argumentName = null) {
            if (string.IsNullOrWhiteSpace(value))
                ThrowHelper.ThrowArgumentEmptyException(value, argumentName);
            return value;
    }

    public static int
    VerifyGreaterZero(this int? number, string? argument = null) =>
        (number.HasValue && number.Value > 0)
        ? number.Value
        : throw new ArgumentException(argument);

    public static int
    VerifyGreaterOrEqualZero(this int value, string? argumentName = null) {
        if (value < 0)
            ThrowHelper.ThrowArgumentGreaterOrEqualZeroException(value, argumentName);
        return value;
    }

    public static float
    VerifyGreaterOrEqualZero(this float value, string? argumentName = null) {
        if (value.IsLessZero())
            ThrowHelper.ThrowArgumentGreaterOrEqualZeroException(value, argumentName);
        return value;
    }

    public static int
    VerifyGreaterZero(this int value, string? argumentName = null) {
        if (value < 0)
            ThrowHelper.ThrowArgumentGreaterZeroException(value, argumentName);
        return value;
    }

    public static double
    VerifyGreaterOrEqualZero(this double value, string? argumentName = null) {
        if (value.IsLessZero())
            ThrowHelper.ThrowArgumentGreaterOrEqualZeroException(value, argumentName);
        return value;
    }
}
    