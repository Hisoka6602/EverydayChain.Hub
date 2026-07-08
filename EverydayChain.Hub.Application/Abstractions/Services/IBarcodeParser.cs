using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 IBarcodeParser 类型。
/// </summary>
public interface IBarcodeParser
{
    /// <summary>
    /// 执行 Parse 方法。
    /// </summary>
    BarcodeParseResult Parse(string barcodeText);
}

