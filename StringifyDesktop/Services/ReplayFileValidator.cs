using System.Buffers.Binary;
using StringifyDesktop.Models;

namespace StringifyDesktop.Services;

public sealed class ReplayFileValidator
{
    private const uint LocalFileMagic = 0x1ca2e27f;
    private const uint NetworkDemoMagic = 0x2cf5a13d;
    private const int MinHeaderSize = 20;
    private const int HeaderReadSize = 64 * 1024;

    public async Task<ReplayValidationResult> ValidateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var buffer = GC.AllocateUninitializedArray<byte>(HeaderReadSize);

        try
        {
            await using var stream = File.OpenRead(filePath);
            var bytesRead = await ReadUpToAsync(stream, buffer, cancellationToken);
            return ValidateHeader(buffer.AsSpan(0, bytesRead));
        }
        catch (Exception error)
        {
            return new ReplayValidationResult(false, $"Could not read replay header: {error.Message}");
        }
    }

    private static ReplayValidationResult ValidateHeader(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < MinHeaderSize)
        {
            return Invalid($"File too small ({buffer.Length} bytes)");
        }

        var magic = ReadUInt32LE(buffer, 0);
        if (magic != LocalFileMagic)
        {
            return Invalid($"Invalid replay file (magic 0x{magic:x8})");
        }

        var fileVersion = ReadUInt32LE(buffer, 4);
        var pos = 20;

        if (pos + 4 > buffer.Length)
        {
            return ReplayValidationResult.Valid;
        }

        pos = SkipFString(buffer, pos);

        if (pos + 4 <= buffer.Length)
        {
            pos += 4;
        }

        if (fileVersion >= 3 && pos + 8 <= buffer.Length)
        {
            pos += 8;
        }

        var isCompressed = false;
        if (fileVersion >= 2 && pos + 4 <= buffer.Length)
        {
            isCompressed = ReadUInt32LE(buffer, pos) != 0;
            pos += 4;
        }

        if (fileVersion >= 6 && pos + 4 <= buffer.Length)
        {
            pos += 4;
            if (pos + 4 <= buffer.Length)
            {
                var keyLen = ReadInt32LE(buffer, pos);
                pos += 4;
                if (keyLen > 0 && keyLen <= 256)
                {
                    pos += keyLen;
                }
            }
        }

        while (pos + 8 <= buffer.Length)
        {
            var chunkType = ReadUInt32LE(buffer, pos);
            pos += 4;

            var chunkSize = ReadInt32LE(buffer, pos);
            pos += 4;

            var chunkDataStart = pos;
            if (chunkSize < 0 || (long)chunkDataStart + chunkSize > buffer.Length)
            {
                break;
            }

            if (chunkType == 0)
            {
                if (chunkSize >= 4)
                {
                    var headerMagic = ReadUInt32LE(buffer, chunkDataStart);
                    if (headerMagic == NetworkDemoMagic)
                    {
                        break;
                    }

                    if (!isCompressed)
                    {
                        return Invalid($"Invalid header chunk magic (0x{headerMagic:x8})");
                    }
                }

                break;
            }

            pos = chunkDataStart + chunkSize;
        }

        return ReplayValidationResult.Valid;
    }

    private static int ReadInt32LE(ReadOnlySpan<byte> buffer, int offset)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset, sizeof(int)));
    }

    private static uint ReadUInt32LE(ReadOnlySpan<byte> buffer, int offset)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, sizeof(uint)));
    }

    private static int SkipFString(ReadOnlySpan<byte> buffer, int offset)
    {
        var length = ReadInt32LE(buffer, offset);
        if (length == 0)
        {
            return offset + 4;
        }

        var byteLength = length > 0 ? length : -(long)length * 2;
        var nextOffset = offset + 4L + byteLength;
        return nextOffset > int.MaxValue ? buffer.Length + 1 : (int)nextOffset;
    }

    private static async Task<int> ReadUpToAsync(FileStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }

    private static ReplayValidationResult Invalid(string error)
    {
        return new(false, error);
    }
}
