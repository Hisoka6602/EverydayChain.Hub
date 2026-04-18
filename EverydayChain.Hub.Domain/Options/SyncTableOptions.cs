namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 单表同步配置。
/// </summary>
public class SyncTableOptions
{
    /// <summary>表编码。</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>是否启用。</summary>
    public bool Enabled { get; set; }

    /// <summary>源端 Schema。</summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>源端表名。</summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>目标逻辑表名。</summary>
    public string TargetLogicalTable { get; set; } = string.Empty;

    /// <summary>游标列名。</summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>起始本地时间字符串。</summary>
    public string StartTimeLocal { get; set; } = string.Empty;

    /// <summary>分页大小。</summary>
    public int PageSize { get; set; } = 5000;

    /// <summary>轮询间隔（秒）。</summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>最大滞后分钟数。</summary>
    public int MaxLagMinutes { get; set; } = 10;

    /// <summary>同步优先级（High/Low）。</summary>
    public string Priority { get; set; } = "Low";

    /// <summary>唯一键集合。</summary>
    public List<string> UniqueKeys { get; set; } = [];

    /// <summary>排除列集合。</summary>
    public List<string> ExcludedColumns { get; set; } = [];

    /// <summary>删除同步配置（仅作用于本地 SQL Server 目标端）。</summary>
    public SyncDeleteOptions Delete { get; set; } = new();

    /// <summary>保留期治理配置（仅作用于本地 SQL Server 目标端）。</summary>
    public SyncRetentionOptions Retention { get; set; } = new();

    /// <summary>
    /// 同步执行模式（可填写项：KeyedMerge、StatusDriven；默认 KeyedMerge）。
    /// KeyedMerge：按游标窗口增量读取，执行幂等 Upsert 合并与缺失删除识别。
    /// StatusDriven：按状态列读取待处理行，仅追加写入，可选回写远端状态。
    /// 未配置时默认 KeyedMerge，存量配置无需修改。
    /// </summary>
    public string SyncMode { get; set; } = "KeyedMerge";

    /// <summary>
    /// 状态列名（仅 StatusDriven 模式使用；可填写范围：源端真实列名，仅允许字母、数字、下划线；默认 TASKPROCESS）。
    /// </summary>
    public string StatusColumnName { get; set; } = "TASKPROCESS";

    /// <summary>
    /// 待处理状态值（仅 StatusDriven 模式使用；可填写范围：字符串或 null；
    /// null 时生成 IS NULL 查询条件，非 null 时生成等值条件；默认 N）。
    /// </summary>
    public string? PendingStatusValue { get; set; } = "N";

    /// <summary>
    /// 完成状态值（仅 StatusDriven 且 ShouldWriteBackRemoteStatus=true 时使用；
    /// 可填写范围：字符串；默认 Y）。
    /// </summary>
    public string CompletedStatusValue { get; set; } = "Y";

    /// <summary>
    /// 是否回写远端状态（仅 StatusDriven 模式使用；可填写项：true/false；
    /// true：本地追加完成后按 ROWID 将远端状态更新为 CompletedStatusValue；
    /// false：仅本地追加，不执行任何远端状态更新；默认 true）。
    /// </summary>
    public bool ShouldWriteBackRemoteStatus { get; set; } = true;

    /// <summary>
    /// 状态驱动批次读取大小（仅 StatusDriven 模式使用；可填写范围：1~100000；默认 5000）。
    /// </summary>
    public int StatusBatchSize { get; set; } = 5000;

    /// <summary>
    /// 回写完成时间审计列名（仅 SyncMode=StatusDriven 且 ShouldWriteBackRemoteStatus=true 生效；
    /// 可填写范围：源端真实列名，仅允许字母、数字、下划线；留空表示不回写完成时间审计列）。
    /// </summary>
    public string? WriteBackCompletedTimeColumnName { get; set; }

    /// <summary>
    /// 回写批次号审计列名（仅 SyncMode=StatusDriven 且 ShouldWriteBackRemoteStatus=true 生效；
    /// 可填写范围：源端真实列名，仅允许字母、数字、下划线；留空表示不回写批次号审计列）。
    /// </summary>
    public string? WriteBackBatchIdColumnName { get; set; }

    /// <summary>
    /// 业务来源类型（可填写项：Split、FullCase；默认 Unknown）。
    /// </summary>
    public string SourceType { get; set; } = "Unknown";

    /// <summary>
    /// 业务键列名（仅业务任务状态驱动投影链路使用；可填写范围：源端真实列名，仅允许字母、数字、下划线）。
    /// </summary>
    public string BusinessKeyColumn { get; set; } = string.Empty;

    /// <summary>
    /// 条码列名（可填写范围：源端真实列名，仅允许字母、数字、下划线；留空表示不映射条码）。
    /// </summary>
    public string? BarcodeColumn { get; set; }

    /// <summary>
    /// 波次号列名（可填写范围：源端真实列名，仅允许字母、数字、下划线；留空表示不映射波次号）。
    /// </summary>
    public string? WaveCodeColumn { get; set; }

    /// <summary>
    /// 波次备注列名（可填写范围：源端真实列名，仅允许字母、数字、下划线；留空表示不映射波次备注）。
    /// </summary>
    public string? WaveRemarkColumn { get; set; }
}
