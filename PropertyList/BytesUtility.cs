
using System;
using System.Linq;

namespace PropertyList;

internal static class BytesUtility
{
    public static long GetBigEndianInt(this byte[] bytes) => bytes.GetBigEndianInt(bytes.Length, 0);
    public static long GetBigEndianInt(this byte[] bytes, int size, int offset = 0)
    {
        long result = 0;
        while (size-- > 0)
            result = (result << 8) + bytes[offset++];
        return result;
    }

    public static float GetBigEndianFloat(this byte[] bytes)
    {
        if (BitConverter.IsLittleEndian)
            return BitConverter.ToSingle(bytes.Reverse().ToArray());
        return BitConverter.ToSingle(bytes);
    }

    public static double GetBigEndianDouble(this byte[] bytes)
    {
        if (BitConverter.IsLittleEndian)
            return BitConverter.ToDouble(bytes.Reverse().ToArray());
        return BitConverter.ToDouble(bytes);
    }
}