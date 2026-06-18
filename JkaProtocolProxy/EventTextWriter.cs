using System.Text;

namespace JkaProtocolProxy;

public class EventTextWriter : TextWriter
{
    public event Action<string>? OnWrite;
    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        if (value != null) OnWrite?.Invoke(value);
    }

    public override void Write(string? value)
    {
        if (value != null) OnWrite?.Invoke(value);
    }
}