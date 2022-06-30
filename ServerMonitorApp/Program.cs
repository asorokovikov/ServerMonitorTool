using ServerMonitorApp.Common;
using ServerMonitorApp.Hubs;
using ServerMonitorApp.Notifications;
using ServerMonitorApp.Services;
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
builder.Services.AddNotificationManager<LogMessage>();
builder.Services.AddNotificationManager<ServerMetrics>();
builder.Services.AddHostedService<DatabaseService>();
builder.Services.AddSingleton<IMetricsRepository, DefaultMetricsRepository>(); 

builder.Services.Configure<DatabaseConfiguration>(builder.Configuration.GetSection(nameof(DatabaseConfiguration)));

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

app.Run();
