using System.Text;
using Synack.Streams;

namespace Synack.Tests.Streams;

public class PrependStreamTests
{
    private class DelegatingStream : Stream
    {
        public Action? OnDispose { get; set; }
        public Func<ValueTask>? OnDisposeAsync { get; set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get; set; }

        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) OnDispose?.Invoke();
        }

        public override async ValueTask DisposeAsync()
        {
            if (OnDisposeAsync is not null)
                await OnDisposeAsync().ConfigureAwait(false);

            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Fact]
    public void CanRead_ReturnsInnerCanRead()
    {
        var inner = new MemoryStream();
        var stream = new PrependStream(Array.Empty<byte>(), inner);
        stream.CanRead.ShouldBe(inner.CanRead);
    }

    [Fact]
    public void CanSeek_ReturnsFalse()
    {
        var stream = new PrependStream(Array.Empty<byte>(), new MemoryStream());
        stream.CanSeek.ShouldBeFalse();
    }

    [Fact]
    public void CanWrite_ReturnsFalse()
    {
        var stream = new PrependStream(Array.Empty<byte>(), new MemoryStream());
        stream.CanWrite.ShouldBeFalse();
    }

    [Fact]
    public void Length_ThrowsNotSupportedException()
    {
        var stream = new PrependStream(Array.Empty<byte>(), new MemoryStream());
        Should.Throw<NotSupportedException>(() => _ = stream.Length);
    }

    [Fact]
    public void PositionGet_ThrowsNotSupportedException()
    {
        var stream = new PrependStream(Array.Empty<byte>(), new MemoryStream());
        Should.Throw<NotSupportedException>(() => _ = stream.Position);
    }

    [Fact]
    public void PositionSet_ThrowsNotSupportedException()
    {
        var stream = new PrependStream(Array.Empty<byte>(), new MemoryStream());
        Should.Throw<NotSupportedException>(() => stream.Position = 0);
    }

    [Fact]
    public void Seek_ThrowsNotSupportedException()
    {
        var stream = new PrependStream(Array.Empty<byte>(), new MemoryStream());
        Should.Throw<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public void SetLength_ThrowsNotSupportedException()
    {
        var stream = new PrependStream(Array.Empty<byte>(), new MemoryStream());
        Should.Throw<NotSupportedException>(() => stream.SetLength(0));
    }

    [Fact]
    public void Write_ThrowsNotSupportedException()
    {
        var stream = new PrependStream(Array.Empty<byte>(), new MemoryStream());
        Should.Throw<NotSupportedException>(() => stream.Write(Array.Empty<byte>(), 0, 0));
    }

    [Fact]
    public void Flush_DelegatesToInnerStream()
    {
        var flushed = false;
        var inner = new MemoryStreamWithFlush(() => flushed = true);
        var stream = new PrependStream(Array.Empty<byte>(), inner);

        stream.Flush();

        flushed.ShouldBeTrue();
    }

    [Fact]
    public void Read_ReadsFromPrefixThenInnerStream()
    {
        var prefix = Encoding.ASCII.GetBytes("abc");
        var inner = new MemoryStream(Encoding.ASCII.GetBytes("def"));
        var stream = new PrependStream(prefix, inner);

        var buffer = new byte[6];
        var bytesRead = stream.Read(buffer, 0, 6);

        bytesRead.ShouldBe(6);
        Encoding.ASCII.GetString(buffer).ShouldBe("abcdef");
    }

    [Fact]
    public async Task ReadAsync_ReadsFromPrefixThenInnerStream()
    {
        var prefix = Encoding.ASCII.GetBytes("abc");
        var inner = new MemoryStream(Encoding.ASCII.GetBytes("def"));
        var stream = new PrependStream(prefix, inner);

        var buffer = new byte[6];

        var bytesRead1 = await stream.ReadAsync(buffer, 0, 2);
        bytesRead1.ShouldBe(2);
        Encoding.ASCII.GetString(buffer, 0, bytesRead1).ShouldBe("ab");

        var bytesRead2 = await stream.ReadAsync(buffer, 2, 4);
        bytesRead2.ShouldBe(4);
        Encoding.ASCII.GetString(buffer, 2, bytesRead2).ShouldBe("cdef");
    }

    [Fact]
    public void Read_SplitsBetweenPrefixAndInner()
    {
        var prefix = Encoding.ASCII.GetBytes("abc");
        var inner = new MemoryStream(Encoding.ASCII.GetBytes("def"));
        var stream = new PrependStream(prefix, inner);

        var buffer = new byte[4];
        var bytesRead = stream.Read(buffer, 0, 4);

        bytesRead.ShouldBe(4);
        Encoding.ASCII.GetString(buffer).ShouldBe("abcd"); // 3 from prefix, 1 from inner
    }


    private class MemoryStreamWithFlush : MemoryStream
    {
        private readonly Action _onFlush;
        public MemoryStreamWithFlush(Action onFlush) => _onFlush = onFlush;
        public override void Flush() => _onFlush();
    }

    [Fact]
    public void PrependStream_ShouldDisposeInnerStream_WhenDisposed()
    {
        var disposed = false;
        var inner = new DelegatingStream { OnDispose = () => disposed = true };

        using (var stream = new PrependStream(Array.Empty<byte>(), inner)) { }

        disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task PrependStream_ShouldDisposeInnerStreamAsync_WhenDisposedAsync()
    {
        var disposedAsync = false;
        var inner = new DelegatingStream { OnDisposeAsync = () => { disposedAsync = true; return ValueTask.CompletedTask; } };

        await using (var stream = new PrependStream(Array.Empty<byte>(), inner)) { }

        disposedAsync.ShouldBeTrue();
    }
}
