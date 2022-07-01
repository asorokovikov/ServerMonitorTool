using ServerMonitorApp.Common;
using ServerMonitorApp.Hubs;
using ServerMonitorApp.Notifications;
using ServerMonitorApp.Database;
using Microsoft.AspNetCore.ResponseCompression;
using Fluxor;
using ServerMonitorCore;

var builder = WebApplication.CreateBuilder(args);

var currentAssembly = typeof(Program).Assembly;
builder.Services.AddFluxor(
    options =>
    {
        options.ScanAssemblies(currentAssembly);
#if DEBUG
        options.UseReduxDevTools();
#endif
    }
);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddResponseCompression(options => {
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }
    );
});

builder.Logging.AddLogger();
builder.Services.Configure<DatabaseConfiguration>(builder.Configuration.GetSection(nameof(DatabaseConfiguration)));
builder.Services.AddTransient<IRepository<ServerMetrics>, DefaultMetricsRepository>();
builder.Services.AddTransient<DefaultMetricsRepository>();
builder.Services.AddHostedService<MetricsProcessingService>();
builder.Services.AddNotification<LogMessage>();
builder.Services.AddNotification<ServerMetrics>();

var app = builder.Build();

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapHub<MonitorHub>("/monitorhub");
app.MapFallbackToPage("/_Host");
app.PrepareDatabase();
app.Run();
