using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// BusinessTaskProjectionService 行为测试。
/// </summary>
public class BusinessTaskProjectionServiceTests
{
    /// <summary>
    /// 投影应将 TaskCode 固定派生为 BusinessKey。
    /// </summary>
    [Fact]
    public void Project_ShouldSetTaskCodeFromBusinessKey()
    {
        var service = new BusinessTaskProjectionService();
        var request = new BusinessTaskProjectionRequest
        {
            Rows =
            [
                new BusinessTaskProjectionRow
                {
                    SourceTableCode = "WmsSplitPickToLightCarton",
                    SourceType = BusinessTaskSourceType.Split,
                    BusinessKey = "CARTON001",
                    Barcode = "CARTON001",
                    WaveCode = "W1",
                    WaveRemark = "R1",
                    ProjectedTimeLocal = DateTime.Now
                }
            ]
        };

        var result = service.Project(request);

        Assert.Single(result.Entities);
        var entity = result.Entities[0];
        Assert.Equal("CARTON001", entity.TaskCode);
        Assert.Equal("CARTON001", entity.BusinessKey);
        Assert.Equal(BusinessTaskSourceType.Split, entity.SourceType);
    }
}
