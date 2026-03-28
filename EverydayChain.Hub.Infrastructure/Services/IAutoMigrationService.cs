namespace EverydayChain.Hub.Infrastructure.Services;

public interface IAutoMigrationService {
    Task RunAsync(CancellationToken cancellationToken);
}
