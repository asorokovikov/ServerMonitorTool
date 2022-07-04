using ServerMonitorCore;
using ServerMonitorCore.Database;
using ServerMonitorCore.Hubs;
using ServerMonitorCore.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.Configure<DatabaseConfiguration>(builder.Configuration.GetSection(nameof(DatabaseConfiguration)));
builder.Services.AddTransient<IRepository<ServerMetrics>, DefaultMetricsRepository>();
builder.Services.AddTransient<DefaultMetricsRepository>();
builder.Services.AddHostedService<MetricsProcessingService>();
builder.Services.AddNotification<ServerMetrics>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapHub<MonitorHub>("/monitorhub");
app.PrepareDatabase();
app.Run();

