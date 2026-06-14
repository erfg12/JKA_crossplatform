using System.Text;

namespace JkaProtocolProxy.OpenJk;

/// <summary>
/// Quake III / OpenJK userinfo strings: backslash-separated key/value pairs.
/// Ported from OpenJK codemp/qcommon/q_shared.c (Info_ValueForKey, Info_SetValueForKey).
/// </summary>
internal static class QuakeInfoString
{
    private const int MaxInfoString = 1024;
    private static readonly char[] Blacklist = ['\\', ';', '"'];

    public static string? ValueForKey(string info, string key)
    {
        if (string.IsNullOrEmpty(info))
        {
            return null;
        }

        ReadOnlySpan<char> s = info.AsSpan();
        if (s.Length > 0 && s[0] == '\\')
        {
            s = s[1..];
        }

        while (!s.IsEmpty)
        {
            int keyEnd = s.IndexOf('\\');
            ReadOnlySpan<char> currentKey = keyEnd < 0 ? s : s[..keyEnd];
            if (keyEnd < 0)
            {
                return null;
            }

            s = s[(keyEnd + 1)..];
            int valueEnd = s.IndexOf('\\');
            ReadOnlySpan<char> value = valueEnd < 0 ? s : s[..valueEnd];

            if (currentKey.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return value.ToString();
            }

            if (valueEnd < 0)
            {
                break;
            }

            s = s[(valueEnd + 1)..];
        }

        return null;
    }

    public static string SetValueForKey(string info, string key, string value)
    {
        if (ContainsBlacklisted(key) || ContainsBlacklisted(value))
        {
            throw new ArgumentException("Info string keys/values cannot contain \\ ; or \"");
        }

        info = RemoveKey(info, key);
        if (string.IsNullOrEmpty(value))
        {
            return info;
        }

        var insertion = $"\\{key}\\{value}";
        var combined = insertion + info;
        if (combined.Length >= MaxInfoString)
        {
            throw new InvalidOperationException("Info string length exceeded.");
        }

        return combined;
    }

    private static string RemoveKey(string info, string key)
    {
        if (string.IsNullOrEmpty(info))
        {
            return string.Empty;
        }

        var result = new StringBuilder(info.Length);
        ReadOnlySpan<char> s = info.AsSpan();

        while (!s.IsEmpty)
        {
            ReadOnlySpan<char> start = s;
            if (s[0] == '\\')
            {
                s = s[1..];
            }

            int keyEnd = s.IndexOf('\\');
            if (keyEnd < 0)
            {
                result.Append(start);
                break;
            }

            ReadOnlySpan<char> currentKey = s[..keyEnd];
            s = s[(keyEnd + 1)..];
            int valueEnd = s.IndexOf('\\');
            ReadOnlySpan<char> currentValue = valueEnd < 0 ? s : s[..valueEnd];

            if (!currentKey.Equals(key, StringComparison.Ordinal))
            {
                result.Append('\\');
                result.Append(currentKey);
                result.Append('\\');
                result.Append(currentValue);
            }

            if (valueEnd < 0)
            {
                break;
            }

            s = s[(valueEnd + 1)..];
        }

        return result.ToString();
    }

    private static bool ContainsBlacklisted(string text)
    {
        return text.IndexOfAny(Blacklist) >= 0;
    }
}
