namespace ServerMonitorCore.Common;

public static class Numbers {

    private const float DefaultFloatDeviation = 0.000001f;

    public static int
    ToMBytes(this long bytes) => (int) (bytes / (1024 * 1024));

    public static ByteSize
    FromBytes(this int value) => ByteSize.FromBytes(value);

    public static ByteSize
    FromMBytes(this int value) => ByteSize.FromMBytes(value);

    public static bool
    IsLessZero(this float value, float deviation = DefaultFloatDeviation) => value < -deviation;

    public static bool
    IsLessZero(this double value, float deviation = DefaultFloatDeviation) => value < -deviation;

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
