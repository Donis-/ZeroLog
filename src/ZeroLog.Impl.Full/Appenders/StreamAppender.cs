using System;
using System.IO;
using System.Text;
using ZeroLog.Formatting;

namespace ZeroLog.Appenders;

public abstract class StreamAppender : Appender
{
    private readonly char[] _charBuffer = GC.AllocateUninitializedArray<char>(LogManager.OutputBufferSize);
    private readonly byte[] _byteBuffer = GC.AllocateUninitializedArray<byte>(4 * LogManager.OutputBufferSize);

    private PrefixWriter? _prefixWriter;
    private Encoding _encoding = Encoding.UTF8;
    private byte[] _newLineBytes = Array.Empty<byte>();

    protected Stream? Stream { get; set; }

    protected internal Encoding Encoding
    {
        get => _encoding;
        set
        {
            _encoding = value;
            UpdateNewLineBytes();
        }
    }

    public string PrefixPattern
    {
        get => _prefixWriter?.Pattern ?? string.Empty;
        set => _prefixWriter = !string.IsNullOrEmpty(value) ? new PrefixWriter(value) : null;
    }

    protected StreamAppender()
    {
        UpdateNewLineBytes();
    }

    public override void Dispose()
    {
        Stream?.Dispose();
        Stream = null;

        base.Dispose();
    }

    public override void WriteMessage(FormattedLogMessage message)
    {
        if (Stream is null)
            return;

        if (_prefixWriter != null)
        {
            var prefixLength = _prefixWriter.WritePrefix(message, _charBuffer);
            Write(_charBuffer.AsSpan(0, prefixLength));
        }

        WriteLine(message.GetMessage());

        if (message.Exception != null)
        {
            // This allocates, but there's no better way to get the details.
            WriteLine(message.Exception.ToString());
        }
    }

    private void Write(ReadOnlySpan<char> value)
    {
        if (Stream is null)
            return;

        var byteCount = _encoding.GetBytes(value, _byteBuffer);
        Stream.Write(_byteBuffer, 0, byteCount);
    }

    private void WriteLine(ReadOnlySpan<char> value)
    {
        if (Stream is null)
            return;

        var byteCount = _encoding.GetBytes(value, _byteBuffer);

        if (byteCount + _newLineBytes.Length <= _byteBuffer.Length)
        {
            _newLineBytes.CopyTo(_byteBuffer.AsSpan(byteCount));
            Stream.Write(_byteBuffer, 0, byteCount + _newLineBytes.Length);
        }
        else
        {
            Stream.Write(_byteBuffer, 0, byteCount);
            Stream.Write(_newLineBytes, 0, _newLineBytes.Length);
        }
    }

    public override void Flush()
    {
        Stream?.Flush();
        base.Flush();
    }

    private void UpdateNewLineBytes()
        => _newLineBytes = _encoding.GetBytes(Environment.NewLine);
}
