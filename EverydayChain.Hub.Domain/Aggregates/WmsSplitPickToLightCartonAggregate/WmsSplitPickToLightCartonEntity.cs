using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EverydayChain.Hub.Domain.Aggregates.WmsSplitPickToLightCartonAggregate {

    /// <summary>
    /// WMS 下发至亮灯拆零箱任务表实体。
    /// </summary>
    [Table("IDX_PICKTOLIGHT_CARTON1")]
    public class WmsSplitPickToLightCartonEntity {

        /// <summary>
        /// 订单号。
        /// </summary>
        [Column("DOCNO")]
        [StringLength(20)]
        public string? DocumentNo { get; set; }

        /// <summary>
        /// 拆零区域。
        /// </summary>
        [Column("WORKINGAREA")]
        [StringLength(20)]
        public string? WorkingAreaCode { get; set; }

        /// <summary>
        /// 箱号ID（分拣条码）。
        /// </summary>
        [Column("CARTONNO")]
        [StringLength(30)]
        public string? CartonNo { get; set; }

        /// <summary>
        /// 分拣位置。
        /// </summary>
        [Column("SORTATIONLOCATION")]
        [StringLength(20)]
        public string? SortationLocationCode { get; set; }

        /// <summary>
        /// 使用标记。
        /// 默认值：N。
        /// </summary>
        [Column("USEFLAG")]
        [StringLength(1)]
        public string? UseFlag { get; set; } = "N";

        /// <summary>
        /// 附加标记。
        /// 默认值：N。
        /// </summary>
        [Column("ADDITIONAL")]
        [StringLength(1)]
        public string? AdditionalFlag { get; set; } = "N";

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
        /// 码头号（格口号）。
        /// </summary>
        [Column("SEQNO")]
        public int? ChuteNo { get; set; }

        /// <summary>
        /// WMS任务下发时间。
        /// </summary>
        [Column("ADDTIME")]
        public DateTime? AddTime { get; set; }

        /// <summary>
        /// WMS订单下发人员ID。
        /// </summary>
        [Column("ADDWHO")]
        [StringLength(20)]
        public string? AddedByUserId { get; set; }

        /// <summary>
        /// 康利达读取标记。
        /// 默认值：N，读取完改为Y。
        /// </summary>
        [Column("TASKPROCESS")]
        [StringLength(2)]
        public string? TaskProcess { get; set; } = "N";

        /// <summary>
        /// 波次号。
        /// </summary>
        [Column("WAVENO")]
        [StringLength(20)]
        public string? WaveNo { get; set; }

        /// <summary>
        /// 门店ID。
        /// </summary>
        [Column("STOREID")]
        [StringLength(20)]
        public string? StoreId { get; set; }

        /// <summary>
        /// 停止标识或停止状态。
        /// 当前业务语义待进一步确认。
        /// </summary>
        [Column("STOP")]
        [StringLength(20)]
        public string? StopCode { get; set; }

        /// <summary>
        /// 康利达回传标记。
        /// 默认值：00，回传后改为Y。
        /// </summary>
        [Column("STATUS")]
        [StringLength(2)]
        public string? Status { get; set; } = "00";

        /// <summary>
        /// 仓库ID。
        /// </summary>
        [Column("WAREHOUSEID")]
        [StringLength(20)]
        public string? WarehouseId { get; set; }

        /// <summary>
        /// 门店名称。
        /// </summary>
        [Column("MENDIAN")]
        [StringLength(200)]
        public string? StoreName { get; set; }

        /// <summary>
        /// WCS编号。
        /// 格式：格口号-门店集货ID。
        /// </summary>
        [Column("WCSNO")]
        [StringLength(20)]
        public string? WcsNo { get; set; }

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
    }
}
