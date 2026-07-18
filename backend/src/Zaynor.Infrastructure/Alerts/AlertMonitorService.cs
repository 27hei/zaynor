using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.UserItems;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Infrastructure.Alerts;

/// <summary>
/// The background job from spec Section 13: periodically re-checks prices for
/// every product with an active alert. Firing an alert deactivates it and
/// rewrites its condition to the triggered form the UI displays. A pleasant
/// side effect: each check runs through the aggregation engine, so tracked
/// products keep accumulating price history even when nobody searches them.
///
/// In-app surfacing only for now — email/push delivery arrives with real
/// infrastructure (spec Section 19, iOS app / notifications).
/// </summary>
public sealed class AlertMonitorService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertMonitorService> _logger;
    private readonly TimeSpan _interval;

    public AlertMonitorService(
        IServiceScopeFactory scopeFactory,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<AlertMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var minutes = int.TryParse(configuration["AlertMonitor:IntervalMinutes"], out var parsed) && parsed > 0
            ? parsed
            : 30;
        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Alert monitor started; checking every {Interval}", _interval);

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            using var timer = new PeriodicTimer(_interval);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ZaynorDbContext>();
            var aggregation = scope.ServiceProvider.GetRequiredService<IAggregationService>();

            var activeAlerts = await db.Alerts
                .Where(a => a.IsActive)
                .Join(db.Products, a => a.ProductId, p => p.Id, (a, p) => new { Alert = a, p.CanonicalName })
                .ToListAsync(cancellationToken);

            if (activeAlerts.Count == 0)
            {
                return;
            }

            var fired = 0;

            foreach (var productGroup in activeAlerts.GroupBy(x => x.CanonicalName))
            {
                var result = await aggregation.SearchAsync(productGroup.Key, cancellationToken);
                var lowest = result.Offers.FirstOrDefault(o => o.IsLowestPrice);
                if (lowest is null)
                {
                    continue;
                }

                foreach (var entry in productGroup)
                {
                    var baseline = AlertConditions.TryParseBaseline(entry.Alert.TargetCondition);
                    if (baseline is null || lowest.Price >= baseline)
                    {
                        continue;
                    }

                    entry.Alert.IsActive = false;
                    entry.Alert.TargetCondition = AlertConditions.BuildTriggered(
                        lowest.Price, lowest.Currency, entry.Alert.TargetCondition);
                    fired++;

                    _logger.LogInformation(
                        "Alert {AlertId} fired: {Product} dropped to {Price} {Currency} (baseline {Baseline})",
                        entry.Alert.Id, productGroup.Key, lowest.Price, lowest.Currency, baseline);
                }
            }

            if (fired > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Alert monitor pass complete: {Checked} active alerts, {Fired} fired",
                activeAlerts.Count, fired);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One bad pass must not kill the monitor (NFR4).
            _logger.LogError(ex, "Alert monitor pass failed; will retry on the next tick");
        }
    }
}
