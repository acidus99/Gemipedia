using System;
using System.IO;
using System.Text;

namespace Gemipedia.Renderer;

public class CountingTextWriter : TextWriter
{
    private readonly TextWriter _innerWriter;
    private int _characterCount;
    private long _byteCount;

    public CountingTextWriter(TextWriter innerWriter)
    {
        _innerWriter = innerWriter ?? throw new ArgumentNullException(nameof(innerWriter));
        _characterCount = 0;
        _byteCount = 0;
    }

    public override Encoding Encoding => _innerWriter.Encoding;

    public int CharacterCount => _characterCount;

    public long ByteCount => _byteCount;

    public override void Write(char value)
    {
        _innerWriter.Write(value);
        _characterCount++;
        _byteCount += Encoding.GetByteCount(new[] { value });
    }

    public override void Write(char[] buffer, int index, int count)
    {
        _innerWriter.Write(buffer, index, count);
        _characterCount += count;
        _byteCount += Encoding.GetByteCount(buffer, index, count);
    }

    public override void Write(string value)
    {
        if (value != null)
        {
            _innerWriter.Write(value);
            _characterCount += value.Length;
            _byteCount += Encoding.GetByteCount(value);
        }
    }

    public override void WriteLine()
    {
        _innerWriter.WriteLine();
        _characterCount += Environment.NewLine.Length;
        _byteCount += Encoding.GetByteCount(Environment.NewLine);
    }

    public override void WriteLine(string value)
    {
        if (value != null)
        {
            _innerWriter.WriteLine(value);
            _characterCount += value.Length + Environment.NewLine.Length;
            _byteCount += Encoding.GetByteCount(value + Environment.NewLine);
        }
        else
        {
            WriteLine();
        }
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        _innerWriter.WriteLine(buffer, index, count);
        _characterCount += count + Environment.NewLine.Length;
        _byteCount += Encoding.GetByteCount(new string(buffer, index, count) + Environment.NewLine);
    }

    public override void WriteLine(char value)
    {
        Write(value);
        WriteLine();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerWriter?.Dispose();
        }
        base.Dispose(disposing);
    }
}
