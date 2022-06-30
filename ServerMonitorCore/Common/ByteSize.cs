namespace ServerMonitorCore.Common;

public readonly struct
ByteSize {
    private const long BytesInKilobyte = 1024;
    private const long BytesInMegabyte = 1048576;
    private const long BytesInGigabyte = 1073741824;

    private const string ByteSymbol = "B";
    private const string KilobyteSymbol = "KB";
    private const string MegabyteSymbol = "MB";
    private const string GigabyteSymbol = "GB";

    public enum Unit {
        Bytes = 0,
        Kilobytes = 1,
        Megabytes = 2,
        Gigabytes = 3,
    }

    public ByteSize(double bytes) => Bytes = bytes.VerifyGreaterOrEqualZero();

    public static ByteSize
    FromBytes(long value) => new (value);

    public static ByteSize
    FromMBytes(long value) => new (value * BytesInMegabyte);

    public double Bytes { get; }
    public double Kilobytes => Bytes / BytesInKilobyte;
    public double Megabytes => Bytes / BytesInMegabyte;
    public double Gigabytes => Bytes / BytesInGigabyte;

    public string
    Humanize(Unit unit) {
        switch (unit) {
            case Unit.Gigabytes:
            return $"{Gigabytes:N1} {GigabyteSymbol}";
            case Unit.Megabytes:
            return $"{Megabytes:N1} {MegabyteSymbol}";
            case Unit.Kilobytes:
            return $"{Kilobytes:N1} {KilobyteSymbol}";
            default:
            return $"{Bytes:N1} {ByteSymbol}";
        }
    }

    public string
    Humanize() => Humanize(LargestUnit);

    public Unit LargestUnit {
        get {
            if (Math.Abs(Gigabytes) >= 1)
                return Unit.Gigabytes;
            if (Math.Abs(Megabytes) >= 1)
                return Unit.Megabytes;
            if (Math.Abs(Kilobytes) >= 1)
                return Unit.Kilobytes;
            return Unit.Bytes;
        }
    }
}