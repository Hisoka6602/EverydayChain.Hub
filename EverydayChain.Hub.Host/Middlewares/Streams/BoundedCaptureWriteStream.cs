using System.Text;

namespace EverydayChain.Hub.Host.Middlewares.Streams;

/// <summary>
/// 定义 BoundedCaptureWriteStream 类型。
/// </summary>
public sealed class BoundedCaptureWriteStream : Stream {
    /// <summary>
    /// 存储 DefaultCaptureBufferBytes 字段。
    /// </summary>
    private const int DefaultCaptureBufferBytes = 4096;

    /// <summary>
    /// 存储 innerStream 字段。
    /// </summary>
    private readonly Stream innerStream;

    /// <summary>
    /// 存储 capturedStream 字段。
    /// </summary>
    private readonly MemoryStream capturedStream;

    /// <summary>
    /// 存储 maxCaptureBytes 字段。
    /// </summary>
    private readonly int maxCaptureBytes;

    /// <summary>
    /// 存储 cachedCapturedText 字段。
    /// </summary>
    private string? cachedCapturedText;

    /// <summary>
    /// 执行 BoundedCaptureWriteStream 方法。
    /// </summary>
    public BoundedCaptureWriteStream(Stream innerStream, int maxCaptureBytes) {
        // 步骤：执行 BoundedCaptureWriteStream 方法的核心处理流程。
        this.innerStream = innerStream;
        this.maxCaptureBytes = maxCaptureBytes;
        capturedStream = new MemoryStream(Math.Min(maxCaptureBytes, DefaultCaptureBufferBytes));
    }

    /// <summary>
    /// 获取或设置 CanRead。
    /// </summary>
    public override bool CanRead => false;

    /// <summary>
    /// 获取或设置 CanSeek。
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    /// 获取或设置 CanWrite。
    /// </summary>
    public override bool CanWrite => innerStream.CanWrite;

    /// <summary>
    /// 获取当前流长度。
    /// </summary>
    public override long Length => throw new NotSupportedException("不支持读取 Length。");

    /// <summary>
    /// 获取或设置当前流位置。
    /// </summary>
    public override long Position {
        get => throw new NotSupportedException("不支持读取 Position。");
        set => throw new NotSupportedException("不支持设置 Position。");
    }

    /// <summary>
    /// 执行 GetCapturedText 方法。
    /// </summary>
    public string GetCapturedText(Encoding encoding) {
        // 步骤：执行 GetCapturedText 方法的核心处理流程。
        if (cachedCapturedText is not null) {
            return cachedCapturedText;
        }

        cachedCapturedText = encoding.GetString(capturedStream.GetBuffer(), 0, (int)capturedStream.Length);
        return cachedCapturedText;
    }

    /// <summary>
    /// 执行 Read 方法。
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count) {
        // 步骤：执行 Read 方法的核心处理流程。
        throw new NotSupportedException("不支持读取操作。");
    }

    /// <summary>
    /// 执行 Seek 方法。
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin) {
        // 步骤：执行 Seek 方法的核心处理流程。
        throw new NotSupportedException("不支持定位操作。");
    }

    /// <summary>
    /// 执行 SetLength 方法。
    /// </summary>
    public override void SetLength(long value) {
        // 步骤：执行 SetLength 方法的核心处理流程。
        throw new NotSupportedException("不支持设置长度。");
    }

    /// <summary>
    /// 执行 Write 方法。
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count) {
        // 步骤：执行 Write 方法的核心处理流程。
        innerStream.Write(buffer, offset, count);
        CaptureBytes(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// 执行 WriteAsync 方法。
    /// </summary>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
        // 步骤：执行 WriteAsync 方法的核心处理流程。
        await innerStream.WriteAsync(buffer, cancellationToken);
        CaptureBytes(buffer.Span);
    }

    /// <summary>
    /// 执行 Flush 方法。
    /// </summary>
    public override void Flush() {
        // 步骤：执行 Flush 方法的核心处理流程。
        innerStream.Flush();
    }

    /// <summary>
    /// 执行 FlushAsync 方法。
    /// </summary>
    public override Task FlushAsync(CancellationToken cancellationToken) {
        // 步骤：执行 FlushAsync 方法的核心处理流程。
        return innerStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// 执行 Dispose 方法。
    /// </summary>
    protected override void Dispose(bool disposing) {
        // 步骤：执行 Dispose 方法的核心处理流程。
        if (disposing) {
            capturedStream.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// 执行 CaptureBytes 方法。
    /// </summary>
    private void CaptureBytes(ReadOnlySpan<byte> bytes) {
        // 步骤：执行 CaptureBytes 方法的核心处理流程。
        if (bytes.IsEmpty || capturedStream.Length >= maxCaptureBytes) {
            return;
        }

        var remaining = maxCaptureBytes - (int)capturedStream.Length;
        var bytesToCapture = Math.Min(remaining, bytes.Length);
        capturedStream.Write(bytes[..bytesToCapture]);
        cachedCapturedText = null;
    }
}

