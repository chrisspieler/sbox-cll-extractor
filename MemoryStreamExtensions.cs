using System.Text;

namespace SboxCllExtractor;

public static class MemoryStreamExtensions
{
    public static bool TryReadInt32( this MemoryStream ms, out int value )
    {
        value = 0;

        if ( ms.Position > ms.Length - sizeof(int) )
            return false;

        var buffer = new byte[sizeof(int)];
        try
        {
            ms.ReadExactly( buffer );
        }
        catch
        {
            return false;
        }
        value = BitConverter.ToInt32( buffer, 0 );
        return true;
    }

    public static bool TryReadLengthPrependedAsciiString( this MemoryStream ms, out string value )
    {
        value = string.Empty;

        if ( !ms.TryReadInt32(out var length) || length <= 0 )
            return false;

        var buffer = new byte[length];
        try
        {
            ms.ReadExactly( buffer );
        }
        catch
        {
            return false;
        }
        value = Encoding.ASCII.GetString( buffer );
        return true;
    }
}
