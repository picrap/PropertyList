using System.IO;

namespace PropertyList;

internal static class StreamUtility
{
    public static Stream ToSeekableStream(this Stream stream)
    {
        if (stream.CanSeek)
            return stream;
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }

    public static int ReadAll(this Stream stream, byte[] buffer) => ReadAll(stream, buffer, 0, buffer.Length);

    public static int ReadAll(this Stream stream, byte[] buffer, int offset, int count)
    {
        var totalRead = 0;
        while (count > 0)
        {
            var bytesRead = stream.Read(buffer, offset, count);
            if (bytesRead == 0)
                break;
            totalRead += bytesRead;
            count -= bytesRead;
            offset += bytesRead;
        }
        return totalRead;
    }

    public static byte[] ReadBytes(this Stream stream, int size)
    {
        var bytes = new byte[size];
        stream.ReadAll(bytes);
        return bytes;
    }

    public static long ReadBigEndianInt(this Stream stream, int size) => stream.ReadBytes(size).GetBigEndianInt();

    public static float ReadBigEndianFloat(this Stream stream) => stream.ReadBytes(4).GetBigEndianFloat();

    public static double ReadBigEndianDouble(this Stream stream) => stream.ReadBytes(8).GetBigEndianDouble();
}
