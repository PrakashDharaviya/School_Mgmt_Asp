namespace SchoolEduERP.Services;

public class FeeReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FeeReminderBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public FeeReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FeeReminderBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>("FeatureFlags:EnableFeeReminders");
        if (!enabled)
        {
            _logger.LogInformation("Fee reminders are disabled via feature flags.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var reminderService = scope.ServiceProvider.GetRequiredService<IFeeReminderService>();
                await reminderService.GenerateRemindersAsync();
                _logger.LogInformation("Fee reminder check completed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating fee reminders");
            }

            // Run every 6 hours
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
