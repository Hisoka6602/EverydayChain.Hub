using System.Text;

namespace EverydayChain.Hub.Host.Middlewares.Streams;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BoundedCaptureWriteStream : Stream {
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int DefaultCaptureBufferBytes = 4096;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly Stream innerStream;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly MemoryStream capturedStream;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly int maxCaptureBytes;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private string? cachedCapturedText;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public BoundedCaptureWriteStream(Stream innerStream, int maxCaptureBytes) {
        // 步骤：按既定流程执行当前方法逻辑。
        this.innerStream = innerStream;
        this.maxCaptureBytes = maxCaptureBytes;
        capturedStream = new MemoryStream(Math.Min(maxCaptureBytes, DefaultCaptureBufferBytes));
    }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public override bool CanRead => false;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    /// 获取或设置当前属性值。
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
    /// 执行当前方法。
    /// </summary>
    public string GetCapturedText(Encoding encoding) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (cachedCapturedText is not null) {
            return cachedCapturedText;
        }

        cachedCapturedText = encoding.GetString(capturedStream.GetBuffer(), 0, (int)capturedStream.Length);
        return cachedCapturedText;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count) {
        // 步骤：按既定流程执行当前方法逻辑。
        throw new NotSupportedException("不支持读取操作。");
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin) {
        // 步骤：按既定流程执行当前方法逻辑。
        throw new NotSupportedException("不支持定位操作。");
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public override void SetLength(long value) {
        // 步骤：按既定流程执行当前方法逻辑。
        throw new NotSupportedException("不支持设置长度。");
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count) {
        // 步骤：按既定流程执行当前方法逻辑。
        innerStream.Write(buffer, offset, count);
        CaptureBytes(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
        // 步骤：按既定流程执行当前方法逻辑。
        await innerStream.WriteAsync(buffer, cancellationToken);
        CaptureBytes(buffer.Span);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public override void Flush() {
        // 步骤：按既定流程执行当前方法逻辑。
        innerStream.Flush();
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public override Task FlushAsync(CancellationToken cancellationToken) {
        // 步骤：按既定流程执行当前方法逻辑。
        return innerStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    protected override void Dispose(bool disposing) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (disposing) {
            capturedStream.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private void CaptureBytes(ReadOnlySpan<byte> bytes) {
        // 步骤：按既定流程执行当前方法逻辑。
        if (bytes.IsEmpty || capturedStream.Length >= maxCaptureBytes) {
            return;
        }

        var remaining = maxCaptureBytes - (int)capturedStream.Length;
        var bytesToCapture = Math.Min(remaining, bytes.Length);
        capturedStream.Write(bytes[..bytesToCapture]);
        cachedCapturedText = null;
    }
}

