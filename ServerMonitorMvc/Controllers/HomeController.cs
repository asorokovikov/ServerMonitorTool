using Microsoft.AspNetCore.Mvc;
using ServerMonitorMvc.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using ServerMonitorCore;
using ServerMonitorCore.Database;
using ServerMonitorCore.Hubs;

namespace ServerMonitorMvc.Controllers;

public class HomeController : Controller {
    private readonly ILogger<HomeController> _logger;
    private readonly IMetricsRepository _repository;
    private readonly IHubContext<MonitorHub, IMonitorHubClient> _hubContext;

    public HomeController(
        ILogger<HomeController> logger, 
        IMetricsRepository repository, 
        IHubContext<MonitorHub, IMonitorHubClient> hubContext
    ) {
        _logger = logger;
        _repository = repository;
        _hubContext = hubContext;
    }

    public async Task<IActionResult> Index() {
        var items = await _repository.GetLatestMetrics();
        var model = new MetricsReportModel(items, 5);
        Response.Headers.Add("Refresh", "5");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(int updateInterval) {
        updateInterval = Math.Max(1, updateInterval);
        MonitorHub.CurrentConfiguration = new(updateInterval);
        _logger.LogWarning($"Sending {nameof(ConfigurationMessage)} to servers: {MonitorHub.CurrentConfiguration}");
        await _hubContext.Clients.All.ReceiveConfiguration(MonitorHub.CurrentConfiguration);
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}