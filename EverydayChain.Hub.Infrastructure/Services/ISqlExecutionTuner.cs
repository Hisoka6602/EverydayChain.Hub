namespace EverydayChain.Hub.Infrastructure.Services;

public interface ISqlExecutionTuner {
    int CurrentBatchSize { get; }
    void Record(TimeSpan elapsed, bool success);
}
