using Raven.Migrations;

namespace Engi.Substrate.Server;

public class RavenMigrationService : BackgroundService
{
    private readonly MigrationRunner runner;

    public RavenMigrationService(MigrationRunner runner)
    {
        this.runner = runner;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(runner.Run);
    }
}
