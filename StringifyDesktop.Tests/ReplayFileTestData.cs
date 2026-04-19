namespace StringifyDesktop.Tests;

internal static class ReplayFileTestData
{
    internal const uint LocalFileMagic = 0x1ca2e27f;
    internal const uint NetworkDemoMagic = 0x2cf5a13d;

    public static byte[] CreateReplayBytes(
        uint? localMagic = null,
        int fileVersion = 2,
        bool compressed = false,
        uint? headerMagic = null)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(localMagic ?? LocalFileMagic);
        writer.Write(fileVersion);
        writer.Write(1_234);
        writer.Write(1);
        writer.Write(1);
        writer.Write(0);
        writer.Write(0);

        if (fileVersion >= 3)
        {
            writer.Write(0L);
        }

        if (fileVersion >= 2)
        {
            writer.Write(compressed ? 1 : 0);
        }

        if (fileVersion >= 6)
        {
            writer.Write(0);
            writer.Write(0);
        }

        if (headerMagic is uint magic)
        {
            writer.Write(0u);
            writer.Write(4);
            writer.Write(magic);
        }

        writer.Flush();
        return stream.ToArray();
    }
}
