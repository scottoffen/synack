namespace Synack;

/// <summary>
/// A read-only stream wrapper that prepends a fixed byte buffer to an underlying stream.
/// </summary>
/// <remarks>
/// Data is read from the prepended buffer first. Once the prefix is exhausted,
/// reads continue from the underlying stream seamlessly.
/// </remarks>
internal sealed class PrependStream : Stream
{
    private readonly Stream _inner;
    private readonly byte[] _prefix;
    private int _prefixOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrependStream"/> class.
    /// </summary>
    /// <param name="prefix">The byte buffer to prepend before the underlying stream.</param>
    /// <param name="inner">The underlying stream to read from after the prefix is exhausted.</param>
    public PrependStream(byte[] prefix, Stream inner)
    {
        _prefix = new byte[prefix.Length];
        Buffer.BlockCopy(prefix, 0, _prefix, 0, prefix.Length);
        _prefixOffset = 0;
        _inner = inner;
    }

    /// <summary>
    /// Gets a value indicating whether the stream supports reading.
    /// Returns the value from the underlying stream.
    /// </summary>
    public override bool CanRead => _inner.CanRead;

    /// <summary>
    /// Always returns false. Seeking is not supported.
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    /// Always returns false. Writing is not supported.
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    /// Throws <see cref="NotSupportedException"/>. Length is not supported.
    /// </summary>
    public override long Length => throw new NotSupportedException();

    /// <summary>
    /// Throws <see cref="NotSupportedException"/>. Position is not supported.
    /// </summary>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Flushes the underlying stream. The prefix buffer is unaffected.
    /// </summary>
    public override void Flush() => _inner.Flush();

    /// <summary>
    /// Reads data from the prefix buffer first, then continues with the underlying stream if space allows.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="offset">The byte offset in the buffer to begin writing.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>The number of bytes read into the buffer.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = 0;

        if (_prefixOffset < _prefix.Length)
        {
            int bytesFromPrefix = Math.Min(count, _prefix.Length - _prefixOffset);
            Buffer.BlockCopy(_prefix, _prefixOffset, buffer, offset, bytesFromPrefix);
            _prefixOffset += bytesFromPrefix;
            bytesRead += bytesFromPrefix;

            offset += bytesFromPrefix;
            count -= bytesFromPrefix;
        }

        if (count > 0)
        {
            bytesRead += _inner.Read(buffer, offset, count);
        }

        return bytesRead;
    }

    /// <summary>
    /// Asynchronously reads data from the prefix buffer first, then continues with the underlying stream if space allows.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="offset">The byte offset in the buffer to begin writing.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for data.</param>
    /// <returns>A task representing the asynchronous read operation, containing the number of bytes read.</returns>
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int bytesRead = 0;

        if (_prefixOffset < _prefix.Length)
        {
            int bytesFromPrefix = Math.Min(count, _prefix.Length - _prefixOffset);
            Buffer.BlockCopy(_prefix, _prefixOffset, buffer, offset, bytesFromPrefix);
            _prefixOffset += bytesFromPrefix;
            bytesRead += bytesFromPrefix;

            offset += bytesFromPrefix;
            count -= bytesFromPrefix;
        }

        if (count > 0)
        {
            bytesRead += await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        }

        return bytesRead;
    }

    /// <summary>
    /// Throws <see cref="NotSupportedException"/>. Seeking is not supported.
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    /// <summary>
    /// Throws <see cref="NotSupportedException"/>. Setting length is not supported.
    /// </summary>
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <summary>
    /// Throws <see cref="NotSupportedException"/>. Writing is not supported.
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
