namespace EverydayChain.Hub.Infrastructure.Options;

public class ShardingOptions {
    public const string SectionName = "Sharding";

    public string ConnectionString { get; set; } = "Server=localhost,1433;Database=EverydayChainHub;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=true";
    public string Schema { get; set; } = "dbo";
    public string BaseTableName { get; set; } = "sorting_task_trace";
    public int AutoCreateMonthsAhead { get; set; } = 1;
}
