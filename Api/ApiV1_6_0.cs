using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using RoflanArchives.Core.Cryptography;
using RoflanArchives.Core.Extensions;

namespace RoflanArchives.Core.Api;

// ReSharper disable once UnusedType.Global
// ReSharper disable once InconsistentNaming
internal sealed class ApiV1_6_0 : IRoflanArchiveApi
{
    internal static class CompressionUtils
    {
        private static readonly Dictionary<RoflanArchiveCompressionType, Type?> CompressionTypeToLevelMap;



        public static readonly RoflanArchiveCompressionType DefaultCompressionType = RoflanArchiveCompressionType.LZ4Stream;
        public static readonly RoflanArchiveCompressionType DefaultCompressionLevel = (byte)LZ4Level.L00_FAST;



        static CompressionUtils()
        {
            CompressionTypeToLevelMap = CreateCompressionTypeToLevelMap();
        }



        private static Dictionary<RoflanArchiveCompressionType, Type?> CreateCompressionTypeToLevelMap()
        {
            return new Dictionary<RoflanArchiveCompressionType, Type?>
            {
                { RoflanArchiveCompressionType.Inherited, null },
                { RoflanArchiveCompressionType.NoCompression, null },
                { RoflanArchiveCompressionType.LZ4Block, typeof(LZ4Level) },
                { RoflanArchiveCompressionType.LZ4Stream, typeof(LZ4Level) }
            };
        }



        public static (RoflanArchiveCompressionType Type, Enum? MappedLevel) GetCompressionInfo(
            IRoflanArchiveHeader header,
            IRoflanArchiveFile file)
        {
            if (file.CompressionType == RoflanArchiveCompressionType.Inherited)
            {
                if (header.CompressionType == RoflanArchiveCompressionType.Inherited)
                    return (DefaultCompressionType, DefaultCompressionLevel);
                if (header.CompressionType == RoflanArchiveCompressionType.NoCompression)
                    return (header.CompressionType, null);

                var headerLevel = MapToCompressionLevelByTypeInternal(
                    header.CompressionType, header.CompressionLevel);

                if (headerLevel != null)
                    return (header.CompressionType, headerLevel);

                return (DefaultCompressionType, DefaultCompressionLevel);
            }
            if (file.CompressionType == RoflanArchiveCompressionType.NoCompression)
                return (file.CompressionType, null);

            var level = MapToCompressionLevelByTypeInternal(
                file.CompressionType, file.CompressionLevel);

            if (level != null)
                return (file.CompressionType, level);

            return (DefaultCompressionType, DefaultCompressionLevel);
        }
        public static Enum? MapToCompressionLevelByTypeInternal(
            RoflanArchiveCompressionType compressionType,
            byte? compressionLevel)
        {
            if (compressionType == RoflanArchiveCompressionType.NoCompression)
                return null;
            if (compressionLevel == null)
                return null;
            if (!CompressionTypeToLevelMap.ContainsKey(compressionType))
                return null;

            var compressionLevelUnderlyingEnumType =
                CompressionTypeToLevelMap[compressionType];

            if (compressionLevelUnderlyingEnumType == null)
                return null;

            var compressionLevelUnderlyingEnumValues = Enum
                .GetValuesAsUnderlyingType(
                    compressionLevelUnderlyingEnumType)
                .Cast<int>()
                .ToArray();

            if (compressionLevelUnderlyingEnumValues.Length == 0)
                return null;

            var compressionLevelEnumValue = Math.Clamp(
                compressionLevel.Value,
                (byte)compressionLevelUnderlyingEnumValues[0],
                (byte)compressionLevelUnderlyingEnumValues[^1]);

            return (Enum)Enum.ToObject(
                compressionLevelUnderlyingEnumType,
                compressionLevelEnumValue);
        }


        public static void CompressContent(
            IRoflanArchiveHeader header,
            IRoflanArchiveFile file,
            Stream compressedDataStream)
        {
            var content = (IRoflanArchiveFileContent)file;

            var position = 0L;

            var compressionInfo =
                GetCompressionInfo(header, file);

            switch (compressionInfo.Type)
            {
                case RoflanArchiveCompressionType.LZ4Block:
                {
                    var dataLength = (int)content.DataStream.Length;
                    var data = new byte[dataLength];

                    // save position
                    position = content.DataStream.Position;

                    // set position 0
                    content.DataStream.Position = 0;

                    _ = content.DataStream.Read(
                        data, 0, dataLength);

                    // reset position
                    content.DataStream.Position = position;

                    var compressedData = new byte[LZ4Codec.MaximumOutputSize(dataLength)];
                    var compressedDataLength =
                        LZ4Codec.Encode(
                            data,
                            compressedData,
                            (LZ4Level)(compressionInfo.MappedLevel ?? LZ4Level.L00_FAST));

                    Array.Resize(
                        ref compressedData,
                        compressedDataLength);

                    compressedDataStream.Position = 0;

                    compressedDataStream.Write(
                        compressedData, 0, compressedDataLength);

                    compressedDataStream.Position = 0;

                    return;
                }
                case RoflanArchiveCompressionType.LZ4Stream:
                {
                    var compressSettings = new LZ4EncoderSettings
                    {
                        CompressionLevel = (LZ4Level)(compressionInfo.MappedLevel ?? LZ4Level.L00_FAST),
                        BlockChecksum = false,
                        BlockSize = 65536,
                        ChainBlocks = true,
                        ContentChecksum = false,
                        ContentLength = null,
                        ExtraMemory = 0
                    };

                    using (var compressStream = LZ4Stream.Encode(
                               compressedDataStream, compressSettings, true))
                    {
                        // save position
                        position = content.DataStream.Position;

                        // set position 0
                        content.DataStream.Position = 0;

                        content.DataStream.CopyTo(
                            compressStream);

                        // reset position
                        content.DataStream.Position = position;
                    }

                    compressedDataStream.Position = 0;

                    return;
                }
                case RoflanArchiveCompressionType.NoCompression:
                case RoflanArchiveCompressionType.Default:
                default:
                    // save position
                    position = content.DataStream.Position;

                    // set position 0
                    content.DataStream.Position = 0;

                    content.DataStream.CopyTo(
                        compressedDataStream);

                    // reset position
                    content.DataStream.Position = position;

                    compressedDataStream.Position = 0;

                    return;
            }
        }

        public static void DecompressContent(
            IRoflanArchiveHeader header,
            IRoflanArchiveFile file,
            Stream compressedDataStream)
        {
            var content = (IRoflanArchiveFileContent)file;

            var compressionInfo =
                GetCompressionInfo(header, file);

            switch (compressionInfo.Type)
            {
                case RoflanArchiveCompressionType.LZ4Block:
                {
                    var compressedDataLength = (int)compressedDataStream.Length;
                    var compressedData = new byte[compressedDataLength];

                    compressedDataStream.Position = 0;

                    _ = compressedDataStream.Read(
                        compressedData, 0, compressedDataLength);

                    var dataLength = (int)file.OriginalContentSize;
                    var data = new byte[dataLength];

                    LZ4Codec.Decode(
                        compressedData,
                        data);

                    content.DataStream.Position = 0;

                    content.DataStream.Write(
                        data, 0, dataLength);

                    content.DataStream.Position = 0;

                    return;
                }
                case RoflanArchiveCompressionType.LZ4Stream:
                {
                    var decompressSettings = new LZ4DecoderSettings
                    {
                        ExtraMemory = 0
                    };

                    compressedDataStream.Position = 0;

                    using (var decompressStream = LZ4Stream.Decode(
                               compressedDataStream, decompressSettings, true))
                    {
                        content.DataStream.Position = 0;

                        decompressStream.CopyTo(
                            content.DataStream);
                    }

                    content.DataStream.Position = 0;

                    return;
                }
                case RoflanArchiveCompressionType.Default:
                case RoflanArchiveCompressionType.NoCompression:
                default:
                    compressedDataStream.Position = 0;

                    content.DataStream.Position = 0;

                    compressedDataStream.CopyTo(
                        content.DataStream);

                    content.DataStream.Position = 0;

                    return;
            }
        }
    }



    // ReSharper disable ConvertToConstant.Global
    public static readonly ulong DummyULong = ulong.MaxValue;
    public static readonly byte[] DummyXXHash3 = Enumerable.Repeat<byte>(255, XxHash3.SizeInBytes).ToArray();
    // ReSharper restore ConvertToConstant.Global



    public Version Version { get; }
    public XxHash3 HashAlgorithm { get; }



    private ApiV1_6_0()
    {
        Version = new Version(
            1, 6, 0, 0);
        HashAlgorithm = new XxHash3();
    }



    private BinaryReader CreateReader(
        IRoflanArchive archive)
    {
        return new BinaryReader(
            File.Open(
                archive.Path,
                FileMode.Open));
    }

    private BinaryWriter CreateWriter(
        IRoflanArchive archive)
    {
        return new BinaryWriter(
            File.Open(
                archive.Path,
                FileMode.OpenOrCreate));
    }



    private IRoflanArchiveHeader ReadHeader(
        BinaryReader reader,
        IRoflanArchiveHeader header)
    {
        var major = reader.ReadInt32();
        var minor = reader.ReadInt32();
        var build = reader.ReadInt32();
        var revision = reader.ReadInt32();

        header.Version = new Version(
            major, minor,
            build, revision);
        header.Name = reader.ReadString();

        header.CompressionLevel = reader.ReadByte();
        header.FilesCount = reader.ReadUInt32();
        header.StartDefinitionsOffset = reader.ReadUInt64();
        header.StartContentsOffset = reader.ReadUInt64();

        return header;
    }

    /// <summary>
    /// The actual value of the <i>
    /// <see cref="IRoflanArchiveHeader.StartContentsOffset"/>
    /// </i> property will be written in the <b>
    /// <see cref="Save(RoflanArchive)"/>
    /// </b>method
    /// </summary>
    private IRoflanArchiveHeader WriteHeader(
        BinaryWriter writer,
        IRoflanArchiveHeader header)
    {
        writer.Write(header.Version.Major);
        writer.Write(header.Version.Minor);
        writer.Write(header.Version.Build);
        writer.Write(header.Version.Revision);
        writer.Write(header.Name);
        writer.Write(header.CompressionLevel);
        writer.Write(header.FilesCount);

        header.StartDefinitionsOffset =
            (ulong)(writer.BaseStream.Position
                    + sizeof(ulong)
                    + sizeof(ulong));

        writer.Write(header.StartDefinitionsOffset);
        writer.Write(DummyULong); // IRoflanArchiveHeader.StartContentsOffset

        return header;
    }


    private void WriteStartContentsOffset(
        BinaryWriter writer,
        IRoflanArchiveHeader header,
        ulong startContentsOffset)
    {
        header.StartContentsOffset = startContentsOffset;

        writer.BaseStream.Position = (long)(header.StartDefinitionsOffset - sizeof(ulong));
        writer.Write(header.StartContentsOffset);
        writer.BaseStream.Position = (long)header.StartContentsOffset;
    }


    private IRoflanArchiveFileDefinition ReadFileDefinitionInternal(
        BinaryReader reader,
        uint id,
        string relativePath)
    {
        var file = new RoflanArchiveFile(
            id,
            relativePath);

        var definition = (IRoflanArchiveFileDefinition)file;

        definition.CompressionType = (RoflanArchiveCompressionType)reader.ReadByte();
        definition.CompressionLevel = reader.ReadByte();
        definition.ContentHash = reader.ReadBytes(XxHash3.SizeInBytes);
        definition.OriginalContentSize = reader.ReadUInt64();
        definition.ContentSize = reader.ReadUInt64();
        definition.ContentOffset = reader.ReadUInt64();

        definition.EndOffset = (ulong)reader.BaseStream.Position;

        return definition;
    }
    private IRoflanArchiveFileDefinition ReadFileDefinition(
        BinaryReader reader)
    {
        var id = reader.ReadUInt32();
        var relativePath = reader.ReadString();

        return ReadFileDefinitionInternal(
            reader, id, relativePath);
    }
    private IRoflanArchiveFileDefinition? ReadFileDefinition(
        BinaryReader reader,
        uint targetId)
    {
        var id = reader.ReadUInt32();

        if (id != targetId)
        {
            var relativePathLength = reader.Read7BitEncodedInt();

            reader.BaseStream.Position +=
                relativePathLength
                + sizeof(byte)
                + sizeof(byte)
                + XxHash3.SizeInBytes
                + sizeof(ulong)
                + sizeof(ulong)
                + sizeof(ulong);

            return null;
        }

        var relativePath = reader.ReadString();

        return ReadFileDefinitionInternal(
            reader, id, relativePath);
    }
    private IRoflanArchiveFileDefinition? ReadFileDefinition(
        BinaryReader reader,
        string targetRelativePath)
    {
        var id = reader.ReadUInt32();
        var relativePath = reader.ReadString();

        if (relativePath != targetRelativePath)
        {
            reader.BaseStream.Position +=
                sizeof(byte)
                + sizeof(byte)
                + XxHash3.SizeInBytes
                + sizeof(ulong)
                + sizeof(ulong)
                + sizeof(ulong);

            return null;
        }

        return ReadFileDefinitionInternal(
            reader, id, relativePath);
    }

    /// <summary>
    /// The actual values of the <i>
    /// <see cref="IRoflanArchiveFileDefinition.ContentHash"/>,
    /// <see cref="IRoflanArchiveFileDefinition.OriginalContentSize"/>,
    /// <see cref="IRoflanArchiveFileDefinition.ContentSize"/> and
    /// <see cref="IRoflanArchiveFileDefinition.ContentOffset"/>
    /// </i> properties will be written in the <b>
    /// <see cref="WriteFileContent(BinaryWriter, IRoflanArchiveHeader, IRoflanArchiveFile)"/>
    /// </b>method
    /// </summary>
    private IRoflanArchiveFileDefinition WriteFileDefinition(
        BinaryWriter writer,
        IRoflanArchiveFileDefinition definition)
    {
        writer.Write(definition.Id);
        writer.Write(definition.RelativePath);
        writer.Write((byte)definition.CompressionType);
        writer.Write(definition.CompressionLevel);
        writer.Write(DummyXXHash3); // IRoflanArchiveFileDefinition.ContentHash
        writer.Write(DummyULong); // IRoflanArchiveFileDefinition.OriginalContentSize
        writer.Write(DummyULong); // IRoflanArchiveFileDefinition.ContentSize
        writer.Write(DummyULong); // IRoflanArchiveFileDefinition.ContentOffset

        definition.EndOffset = (ulong)writer.BaseStream.Position;

        return definition;
    }


    private void WriteContentHash(
        BinaryWriter writer,
        IRoflanArchiveFileDefinition definition,
        ReadOnlyMemory<byte> contentHash)
    {
        definition.ContentHash = contentHash;

        var position = writer.BaseStream.Position;

        writer.BaseStream.Position = (long)(definition.EndOffset
                                            - XxHash3.SizeInBytes
                                            - sizeof(ulong)
                                            - sizeof(ulong)
                                            - sizeof(ulong));

        writer.Write(definition.ContentHash.Span);

        writer.BaseStream.Position = position;
    }


    private void WriteOriginalContentSize(
        BinaryWriter writer,
        IRoflanArchiveFileDefinition definition,
        ulong originalContentSize)
    {
        definition.OriginalContentSize = originalContentSize;

        var position = writer.BaseStream.Position;

        writer.BaseStream.Position = (long)(definition.EndOffset
                                            - sizeof(ulong)
                                            - sizeof(ulong)
                                            - sizeof(ulong));

        writer.Write(definition.OriginalContentSize);

        writer.BaseStream.Position = position;
    }


    private void WriteContentSize(
        BinaryWriter writer,
        IRoflanArchiveFileDefinition definition,
        ulong contentSize)
    {
        definition.ContentSize = contentSize;

        var position = writer.BaseStream.Position;

        writer.BaseStream.Position = (long)(definition.EndOffset
                                            - sizeof(ulong)
                                            - sizeof(ulong));

        writer.Write(definition.ContentSize);

        writer.BaseStream.Position = position;
    }


    private void WriteContentOffset(
        BinaryWriter writer,
        IRoflanArchiveFileDefinition definition,
        ulong contentOffset)
    {
        definition.ContentOffset = contentOffset;

        var position = writer.BaseStream.Position;

        writer.BaseStream.Position = (long)(definition.EndOffset
                                            - sizeof(ulong));

        writer.Write(definition.ContentOffset);

        writer.BaseStream.Position = position;
    }


    private IRoflanArchiveFileContent ReadFileContent(
        BinaryReader reader,
        IRoflanArchiveHeader header,
        IRoflanArchiveFile file)
    {
        var definition = (IRoflanArchiveFileDefinition)file;
        var content = (IRoflanArchiveFileContent)file;

        var position = 0L;

        // save position
        position = reader.BaseStream.Position;

        // set position
        reader.BaseStream.Position = (long)(header.StartContentsOffset
                                            + definition.ContentOffset);

        using var compressedDataStream = new MemoryStream();

        reader.BaseStream.CopyBytesTo(
            compressedDataStream,
            (long)definition.ContentSize);

        CompressionUtils.DecompressContent(
            header, file, compressedDataStream);

        var dataDecompressedLength = content.DataStream.Length;

        // reset position
        reader.BaseStream.Position = position;

        if ((ulong)dataDecompressedLength != definition.OriginalContentSize)
            throw new InvalidDataException($"File[Id={definition.Id}, RelativePath={definition.RelativePath}] was corrupted (saved original length is not equal to length obtained after decompression).");

        // save position
        position = content.DataStream.Position;

        // set position 0
        content.DataStream.Position = 0;

        var dataCorrupted = !HashAlgorithm.VerifyHash(
            content.DataStream,
            definition.ContentHash.Span);

        // reset position
        content.DataStream.Position = position;

        if (dataCorrupted)
            throw new InvalidDataException($"File[Id={definition.Id}, RelativePath={definition.RelativePath}] was corrupted (hash mismatch).");

        return content;
    }

    private IRoflanArchiveFileContent WriteFileContent(
        BinaryWriter writer,
        IRoflanArchiveHeader header,
        IRoflanArchiveFile file)
    {
        var definition = (IRoflanArchiveFileDefinition)file;
        var content = (IRoflanArchiveFileContent)file;

        var position = 0L;

        using var compressedDataStream = new MemoryStream();

        CompressionUtils.CompressContent(
            header, file, compressedDataStream);

        var dataOriginalLength = content.DataStream.Length;
        var dataCompressedLength = compressedDataStream.Length;

        // save position
        position = content.DataStream.Position;

        // set position 0
        content.DataStream.Position = 0;

        WriteContentHash(
            writer, definition,
            HashAlgorithm.GetHash(
                content.DataStream));

        // reset position
        content.DataStream.Position = position;

        WriteOriginalContentSize(
            writer, definition,
            (ulong)dataOriginalLength);

        WriteContentSize(
            writer, definition,
            (ulong)dataCompressedLength);

        WriteContentOffset(
            writer, definition,
            (ulong)writer.BaseStream.Position - header.StartContentsOffset);

        writer.Flush();

        compressedDataStream.Position = 0;

        compressedDataStream.CopyTo(
            writer.BaseStream);

        writer.Flush();

        return content;
    }



    public RoflanArchive Load(
        RoflanArchive archive)
    {
        using var reader = CreateReader(
            archive);

        var header = ReadHeader(
            reader, archive);

        for (uint i = 0; i < header.FilesCount; ++i)
        {
            var file = (RoflanArchiveFile)ReadFileDefinition(
                reader);

            archive._files.Add(
                file);
            archive._filesById.Add(
                file.Id, file);
            archive._filesByRelativePath.Add(
                file.RelativePath, file);
        }

        foreach (var file in archive._files)
        {
            ReadFileContent(
                reader, archive, file);
        }

        archive.Files = new ReadOnlyObservableCollection<RoflanArchiveFile>(
            archive._files);

        return archive;
    }

    public RoflanArchive Save(
        RoflanArchive archive)
    {
        using var writer = CreateWriter(
            archive);

        WriteHeader(
            writer, archive);

        foreach (var file in archive._files)
        {
            WriteFileDefinition(
                writer, file);
        }

        WriteStartContentsOffset(
            writer, archive,
            (ulong)writer.BaseStream.Position);

        foreach (var file in archive._files)
        {
            WriteFileContent(
                writer, archive, file);
        }

        return archive;
    }


    public IRoflanArchiveFile LoadFile(
        RoflanArchive archive,
        uint targetId)
    {
        using var reader = CreateReader(
            archive);

        var header = ReadHeader(
            reader, archive);

        RoflanArchiveFile? file = null;

        for (uint i = 0; i < header.FilesCount; ++i)
        {
            file = ReadFileDefinition(
                reader, targetId) as RoflanArchiveFile;

            if (file is not null)
                break;
        }

        if (file is null)
            throw new FileNotFoundException($"File with provided id[{targetId}] was not found.");

        ReadFileContent(
            reader, archive, file);

        return file;
    }
    public IRoflanArchiveFile LoadFile(
        RoflanArchive archive,
        string targetRelativePath)
    {
        using var reader = CreateReader(
            archive);

        var header = ReadHeader(
            reader, archive);

        RoflanArchiveFile? file = null;

        for (uint i = 0; i < header.FilesCount; ++i)
        {
            file = ReadFileDefinition(
                reader, targetRelativePath) as RoflanArchiveFile;

            if (file is not null)
                break;
        }

        if (file is null)
            throw new FileNotFoundException($"File with provided relative path[{targetRelativePath}] was not found.");

        ReadFileContent(
            reader, archive, file);

        return file;
    }
}
