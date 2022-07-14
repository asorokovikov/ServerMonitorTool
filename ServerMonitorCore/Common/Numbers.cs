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
}
