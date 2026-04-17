using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// <see cref="BusinessTaskMaterializer"/> 行为测试。
/// </summary>
public class BusinessTaskMaterializerTests
{
    /// <summary>
    /// 物化时应写入默认状态与统一时间字段。
    /// </summary>
    [Fact]
    public void Materialize_WithValidRequest_ShouldAssignDefaultStatusAndTime()
    {
        var sut = new BusinessTaskMaterializer();
        var fixedTime = new DateTime(2026, 4, 13, 10, 20, 30, DateTimeKind.Local);
        var request = new BusinessTaskMaterializeRequest
        {
            TaskCode = "TASK-001",
            SourceTableCode = "WMS_PICK",
            BusinessKey = "ORDER-1|LINE-2",
            Barcode = " BC-001 ",
            SourceType = BusinessTaskSourceType.Split,
            WaveCode = "WAVE-001",
            WaveRemark = "首波次",
            MaterializedTimeLocal = fixedTime,
        };

        var entity = sut.Materialize(request);

        Assert.Equal(BusinessTaskStatus.Created, entity.Status);
        Assert.Equal(fixedTime, entity.CreatedTimeLocal);
        Assert.Equal(fixedTime, entity.UpdatedTimeLocal);
        Assert.Equal("TASK-001", entity.TaskCode);
        Assert.Equal("WMS_PICK", entity.SourceTableCode);
        Assert.Equal("ORDER-1|LINE-2", entity.BusinessKey);
        Assert.Equal("BC-001", entity.Barcode);
        Assert.Equal(BusinessTaskSourceType.Split, entity.SourceType);
        Assert.Equal("WAVE-001", entity.WaveCode);
        Assert.Equal("首波次", entity.WaveRemark);
    }

    /// <summary>
    /// 必填字段为空白时应抛出参数异常。
    /// </summary>
    [Theory]
    [InlineData(" ", "WMS_PICK", "K1", "TaskCode")]
    [InlineData("TASK-001", " ", "K1", "SourceTableCode")]
    [InlineData("TASK-001", "WMS_PICK", " ", "BusinessKey")]
    public void Materialize_WithBlankRequiredField_ShouldThrowArgumentException(
        string taskCode,
        string sourceTableCode,
        string businessKey,
        string expectedParamName)
    {
        var sut = new BusinessTaskMaterializer();
        var request = new BusinessTaskMaterializeRequest
        {
            TaskCode = taskCode,
            SourceTableCode = sourceTableCode,
            BusinessKey = businessKey,
        };

        var action = () => sut.Materialize(request);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(expectedParamName, exception.ParamName);
    }
}
