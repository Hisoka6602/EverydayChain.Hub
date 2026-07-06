using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IBarcodeParser
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    BarcodeParseResult Parse(string barcodeText);
}

