namespace EverydayChain.Hub.Infrastructure.Options;

public class AutoTuneOptions {
    public const string SectionName = "AutoTune";

    public bool Enabled { get; set; } = true;
    public int InitialBatchSize { get; set; } = 200;
    public int MinBatchSize { get; set; } = 20;
    public int MaxBatchSize { get; set; } = 2_000;
    public int IncreaseStep { get; set; } = 50;
    public int DecreaseStep { get; set; } = 100;
    public double SlowThresholdMilliseconds { get; set; } = 400;
}
