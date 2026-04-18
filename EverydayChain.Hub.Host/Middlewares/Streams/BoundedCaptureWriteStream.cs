using System.Text;

namespace EverydayChain.Hub.Host.Middlewares.Streams;

/// <summary>
/// 写透传并按上限捕获内容的响应流。
/// </summary>
public sealed class BoundedCaptureWriteStream : Stream {
    /// <summary>
    /// 默认捕获缓冲区容量。
    /// </summary>
    private const int DefaultCaptureBufferBytes = 4096;

    /// <summary>
    /// 原始响应流。
    /// </summary>
    private readonly Stream innerStream;

    /// <summary>
    /// 捕获缓冲流。
    /// </summary>
    private readonly MemoryStream capturedStream;

    /// <summary>
    /// 捕获上限字节数。
    /// </summary>
    private readonly int maxCaptureBytes;

    /// <summary>
    /// 已解码缓存文本。
    /// </summary>
    private string? cachedCapturedText;

    /// <summary>
    /// 初始化写透传捕获流。
    /// </summary>
    /// <param name="innerStream">原始响应流。</param>
    /// <param name="maxCaptureBytes">最大捕获字节数。</param>
    public BoundedCaptureWriteStream(Stream innerStream, int maxCaptureBytes) {
        this.innerStream = innerStream;
        this.maxCaptureBytes = maxCaptureBytes;
        capturedStream = new MemoryStream(Math.Min(maxCaptureBytes, DefaultCaptureBufferBytes));
    }

    /// <summary>
    /// 当前流不支持读取。
    /// </summary>
    public override bool CanRead => false;

    /// <summary>
    /// 当前流不支持定位。
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    /// 当前流支持写入。
    /// </summary>
    public override bool CanWrite => innerStream.CanWrite;

    /// <summary>
    /// 原始流长度。
    /// </summary>
    public override long Length => innerStream.Length;

    /// <summary>
    /// 原始流位置。
    /// </summary>
    public override long Position {
        get => innerStream.Position;
        set => throw new NotSupportedException("不支持设置 Position。");
    }

    /// <summary>
    /// 获取已捕获文本。
    /// </summary>
    /// <param name="encoding">文本编码。</param>
    /// <returns>已捕获响应文本。</returns>
    public string GetCapturedText(Encoding encoding) {
        if (cachedCapturedText is not null) {
            return cachedCapturedText;
        }

        cachedCapturedText = encoding.GetString(capturedStream.GetBuffer(), 0, (int)capturedStream.Length);
        return cachedCapturedText;
    }

    /// <summary>
    /// 读取操作不支持。
    /// </summary>
    /// <param name="buffer">目标缓冲区。</param>
    /// <param name="offset">写入偏移。</param>
    /// <param name="count">读取长度。</param>
    /// <returns>读取字节数。</returns>
    public override int Read(byte[] buffer, int offset, int count) {
        throw new NotSupportedException("不支持读取操作。");
    }

    /// <summary>
    /// 定位操作不支持。
    /// </summary>
    /// <param name="offset">偏移量。</param>
    /// <param name="origin">起始位置。</param>
    /// <returns>位置。</returns>
    public override long Seek(long offset, SeekOrigin origin) {
        throw new NotSupportedException("不支持定位操作。");
    }

    /// <summary>
    /// 设置长度操作不支持。
    /// </summary>
    /// <param name="value">目标长度。</param>
    public override void SetLength(long value) {
        throw new NotSupportedException("不支持设置长度。");
    }

    /// <summary>
    /// 同步写入并捕获响应内容。
    /// </summary>
    /// <param name="buffer">源缓冲区。</param>
    /// <param name="offset">读取偏移。</param>
    /// <param name="count">写入长度。</param>
    public override void Write(byte[] buffer, int offset, int count) {
        innerStream.Write(buffer, offset, count);
        CaptureBytes(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// 异步写入并捕获响应内容。
    /// </summary>
    /// <param name="buffer">写入内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
        await innerStream.WriteAsync(buffer, cancellationToken);
        CaptureBytes(buffer.Span);
    }

    /// <summary>
    /// 同步刷新原始流。
    /// </summary>
    public override void Flush() {
        innerStream.Flush();
    }

    /// <summary>
    /// 异步刷新原始流。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public override Task FlushAsync(CancellationToken cancellationToken) {
        return innerStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// 释放捕获流资源。
    /// </summary>
    /// <param name="disposing">是否释放托管资源。</param>
    protected override void Dispose(bool disposing) {
        if (disposing) {
            capturedStream.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// 捕获指定字节片段。
    /// </summary>
    /// <param name="bytes">字节片段。</param>
    private void CaptureBytes(ReadOnlySpan<byte> bytes) {
        if (bytes.IsEmpty || capturedStream.Length >= maxCaptureBytes) {
            return;
        }

        var remaining = maxCaptureBytes - (int)capturedStream.Length;
        var bytesToCapture = Math.Min(remaining, bytes.Length);
        capturedStream.Write(bytes[..bytesToCapture]);
        cachedCapturedText = null;
    }
}
