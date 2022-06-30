using System.Collections.Immutable;

namespace ServerMonitorCore.Common;

public static class Collections {

    public static ImmutableList<TOut>
    MapToImmutableList<TIn, TOut>(this IEnumerable<TIn> enumerable, Func<TIn, TOut> convert) =>
    ImmutableList.CreateRange(enumerable.Select(convert));

    public static ImmutableArray<TOut>
    MapToImmutableArray<TIn, TOut>(this IEnumerable<TIn> enumerable, Func<TIn, TOut> convert) =>
    ImmutableArray.CreateRange(enumerable.Select(convert));

    public static IEnumerable<(int index, T value)>
    WithIndex<T>(this IEnumerable<T> items) {
        var index = 0;
        foreach (var item in items)
            yield return (index++, item);
    }

}
