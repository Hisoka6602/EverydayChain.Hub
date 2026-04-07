using EverydayChain.Hub.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EverydayChain.Hub.Domain.Aggregates.WmsPickToWcsAggregate;

/// <summary>
/// WMS 下发至 WCS 的分拣任务表实体。
/// </summary>
[Table("IDX_PICKTOWCS2")]
public class WmsPickToWcsEntity : IEntity<long>
{
    /// <summary>
    /// 自增主键。
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// 波次ID。
    /// </summary>
    [Column("WAVENO")]
    [StringLength(30)]
    public string? WaveNo { get; set; }

    /// <summary>
    /// 订单号。
    /// </summary>
    [Column("DOCNO")]
    [StringLength(30)]
    public string? DocumentNo { get; set; }

    /// <summary>
    /// 订单总整件箱数。
    /// </summary>
    [Column("QTY")]
    public int? Quantity { get; set; }

    /// <summary>
    /// 码头号（格口号）。
    /// </summary>
    [Column("SEQNO")]
    public int? ChuteNo { get; set; }

    /// <summary>
    /// 标记。
    /// 默认值：N。
    /// </summary>
    [Column("FLAG")]
    [StringLength(1)]
    public string? Flag { get; set; } = "N";

    /// <summary>
    /// WMS任务下发时间。
    /// </summary>
    [Column("ADDTIME")]
    public DateTime? AddTime { get; set; }

    /// <summary>
    /// WCS读取时间。
    /// </summary>
    [Column("EDITTIME")]
    public DateTime? EditTime { get; set; }

    /// <summary>
    /// 门店名称。
    /// </summary>
    [Column("MENDIAN")]
    [StringLength(300)]
    public string? StoreName { get; set; }

    /// <summary>
    /// WCS编号。
    /// 格式：格口号-门店集货ID。
    /// </summary>
    [Column("WCSNO")]
    [StringLength(30)]
    public string? WcsNo { get; set; }

    /// <summary>
    /// 箱号ID（分拣条码）。
    /// </summary>
    [Column("SKUID")]
    [StringLength(200)]
    public string? BoxBarcode { get; set; }

    /// <summary>
    /// 订单商品序号。
    /// </summary>
    [Column("SKUSEQ")]
    [StringLength(30)]
    public string? SkuSequence { get; set; }

    /// <summary>
    /// 订单商品总箱数。
    /// </summary>
    [Column("SKUQTY")]
    [StringLength(30)]
    public string? SkuQuantityText { get; set; }

    /// <summary>
    /// 商品代码。
    /// </summary>
    [Column("SKU")]
    [StringLength(30)]
    public string? SkuCode { get; set; }

    /// <summary>
    /// 拣货位。
    /// </summary>
    [Column("LOCATION")]
    [StringLength(30)]
    public string? LocationCode { get; set; }

    /// <summary>
    /// 未使用标记。
    /// 默认值：N。
    /// </summary>
    [Column("ZJFLAG")]
    [StringLength(1)]
    public string? ZjFlag { get; set; } = "N";

    /// <summary>
    /// 最小单位数量。
    /// </summary>
    [Column("SKUQTY1", TypeName = "NUMBER(18,8)")]
    public decimal? MinUnitQuantity { get; set; }

    /// <summary>
    /// 序号。
    /// </summary>
    [Column("ALLNUM")]
    [StringLength(30)]
    public string? SequenceNo { get; set; }

    /// <summary>
    /// 序号2。
    /// </summary>
    [Column("ALLNUM1")]
    [StringLength(30)]
    public string? SequenceNo1 { get; set; }

    /// <summary>
    /// 唯一序号。
    /// </summary>
    [Column("R_SYSID")]
    [StringLength(30)]
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    /// 长，单位厘米。
    /// </summary>
    [Column("LENGTH", TypeName = "NUMBER(18,8)")]
    public decimal? LengthCm { get; set; }

    /// <summary>
    /// 宽，单位厘米。
    /// </summary>
    [Column("WIDTH", TypeName = "NUMBER(18,8)")]
    public decimal? WidthCm { get; set; }

    /// <summary>
    /// 高，单位厘米。
    /// </summary>
    [Column("HIGH", TypeName = "NUMBER(18,8)")]
    public decimal? HeightCm { get; set; }

    /// <summary>
    /// 体积，单位立方厘米。
    /// </summary>
    [Column("CUBE", TypeName = "NUMBER(18,8)")]
    public decimal? VolumeCubicCm { get; set; }

    /// <summary>
    /// 重量，单位克。
    /// </summary>
    [Column("GROSSWEIGHT", TypeName = "NUMBER(18,8)")]
    public decimal? GrossWeightGram { get; set; }

    /// <summary>
    /// 门店ID。
    /// </summary>
    [Column("CONSIGNEEID")]
    [StringLength(30)]
    public string? ConsigneeId { get; set; }

    /// <summary>
    /// 波次备注。
    /// </summary>
    [Column("DESCR")]
    [StringLength(100)]
    public string? Description { get; set; }

    /// <summary>
    /// 条码扫描次数。
    /// </summary>
    [Column("SCANCOUNT")]
    public int? ScanCount { get; set; }

    /// <summary>
    /// 康利达读取时间。
    /// </summary>
    [Column("OPENTIME")]
    public DateTime? OpenTime { get; set; }

    /// <summary>
    /// 康利达扫描时间（最后一次扫描时间）。
    /// </summary>
    [Column("CLOSETIME")]
    public DateTime? CloseTime { get; set; }

    /// <summary>
    /// 康利达读取标记。
    /// 默认值：N，读取完改为Y。
    /// </summary>
    [Column("TASKPROCESS")]
    [StringLength(2)]
    public string? TaskProcess { get; set; } = "N";

    /// <summary>
    /// 康利达回传标记。
    /// 默认值：00，回传后改为Y。
    /// </summary>
    [Column("STATUS")]
    [StringLength(2)]
    public string? Status { get; set; } = "00";
}
