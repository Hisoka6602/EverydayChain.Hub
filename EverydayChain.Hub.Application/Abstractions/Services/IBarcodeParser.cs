using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 条码解析服务抽象，负责将扫描输入标准化为统一解析结果。
/// </summary>
public interface IBarcodeParser
{
    /// <summary>
    /// 解析条码文本并返回分类结果。
    /// </summary>
    /// <param name="barcodeText">待解析条码文本。</param>
    /// <returns>条码解析结果。</returns>
    BarcodeParseResult Parse(string barcodeText);
}
