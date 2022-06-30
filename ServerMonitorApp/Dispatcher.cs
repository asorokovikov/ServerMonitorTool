using Fluxor;
using ServerMonitorApp.Reports;
using static ServerMonitorApp.Reports.MetricsReport;

namespace ServerMonitorApp;

public record DispatchAction<T>(Func<T, T> Reducer);

public static class DispatcherHelper {
    public static void Dispatch<T>(this IDispatcher dispatcher, Func<T, T> reducer) {
        dispatcher.Dispatch(new DispatchAction<T>(reducer));
    }
}

public static class ReducerHelper {

    // TODO: Fluxor cannot find this method. Need to check
    //[ReducerMethod]
    //public static T OnDispatch<T>(T state, DispatchAction<T> action) =>
    //    action.Reducer(state);

    [ReducerMethod]
    public static Reports.MetricsReport
    OnDispatch(Reports.MetricsReport state, DispatchAction<Reports.MetricsReport> action) =>
        action.Reducer(state);

    [ReducerMethod]
    public static MetricsReportSettings
    OnDispatch(MetricsReportSettings state, DispatchAction<MetricsReportSettings> action) =>
        action.Reducer(state);

    [ReducerMethod]
    public static MetricsReportLogs
    OnDispatch(MetricsReportLogs state, DispatchAction<MetricsReportLogs> action) =>
        action.Reducer(state);

}
