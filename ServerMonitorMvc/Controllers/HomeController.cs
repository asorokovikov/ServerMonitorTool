using Microsoft.AspNetCore.Mvc;
using ServerMonitorMvc.Models;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using ServerMonitorCore;
using ServerMonitorCore.Database;

namespace ServerMonitorMvc.Controllers;

public class HomeController : Controller {
    private readonly ILogger<HomeController> _logger;
    private readonly IRepository<ServerMetrics> _repository;

    public HomeController(ILogger<HomeController> logger, IRepository<ServerMetrics> repository) {
        _logger = logger;
        _repository = repository;
    }

    public async Task<IActionResult> Index() {
        var items = await _repository.GetAllAsync();
        var model = new MetricsReportModel(items.Take(10));
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}