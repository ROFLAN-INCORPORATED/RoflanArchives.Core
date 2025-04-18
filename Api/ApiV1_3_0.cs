﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using K4os.Compression.LZ4;

namespace RoflanArchives.Core.Api;

// ReSharper disable once UnusedType.Global
// ReSharper disable once InconsistentNaming
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces |
                            DynamicallyAccessedMemberTypes.PublicConstructors |
                            DynamicallyAccessedMemberTypes.NonPublicConstructors |
                            DynamicallyAccessedMemberTypes.PublicProperties |
                            DynamicallyAccessedMemberTypes.PublicMethods)]
[Obsolete]
internal sealed class ApiV1_3_0 : IRoflanArchiveApi
{
    public Version Version { get; }



    private ApiV1_3_0()
    {
        Version = new Version(
            1, 3, 0, 0);
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

        header.CompressionLevel = (byte)(LZ4Level)reader.ReadInt32();
        header.FilesCount = reader.ReadUInt32();
        header.StartDefinitionsOffset = reader.ReadUInt64();
        header.StartContentsOffset = reader.ReadUInt64();

        return header;
    }

    private IRoflanArchiveHeader WriteHeader(
        BinaryWriter writer,
        IRoflanArchiveHeader header)
    {
        writer.Write(header.Version.Major);
        writer.Write(header.Version.Minor);
        writer.Write(header.Version.Build);
        writer.Write(header.Version.Revision);
        writer.Write(header.Name);

        header.StartDefinitionsOffset =
            (ulong)(writer.BaseStream.Position
                    + sizeof(int)
                    + sizeof(uint)
                    + sizeof(ulong)
                    + sizeof(ulong));

        writer.Write((int)header.CompressionLevel);
        writer.Write(header.FilesCount);
        writer.Write(header.StartDefinitionsOffset);
        writer.Write(header.StartContentsOffset);

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
                sizeof(ulong)
                + sizeof(ulong)
                + sizeof(ulong);

            return null;
        }

        return ReadFileDefinitionInternal(
            reader, id, relativePath);
    }

    private IRoflanArchiveFileDefinition WriteFileDefinition(
        BinaryWriter writer,
        IRoflanArchiveFile file)
    {
        var content = (IRoflanArchiveFileContent)file;
        var definition = (IRoflanArchiveFileDefinition)file;

        definition.OriginalContentSize = (ulong)content.Data.Length;

        writer.Write(definition.Id);
        writer.Write(definition.RelativePath);
        writer.Write(definition.OriginalContentSize);
        writer.Write(definition.ContentSize);
        writer.Write(definition.ContentOffset);

        definition.EndOffset = (ulong)writer.BaseStream.Position;

        return definition;
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

        reader.BaseStream.Position = (long)(header.StartContentsOffset
                                            + definition.ContentOffset);

        _ = reader.ReadByte(); // RoflanArchiveFileType IRoflanArchiveFileContent.Type (always equals 0 - RawBytes)

        var dataCompressed = reader.ReadBytes(
            (int)definition.ContentSize);
        var data = new byte[definition.OriginalContentSize];

        LZ4Codec.Decode(
            dataCompressed,
            data);

        file.Data = data;

        return content;
    }

    private IRoflanArchiveFileContent WriteFileContent(
        BinaryWriter writer,
        IRoflanArchiveHeader header,
        IRoflanArchiveFile file)
    {
        var definition = (IRoflanArchiveFileDefinition)file;
        var content = (IRoflanArchiveFileContent)file;

        var dataCompressed = new byte[LZ4Codec.MaximumOutputSize(content.Data.Length)];
        var dataCompressedLength =
            LZ4Codec.Encode(
                content.Data.Span,
                dataCompressed,
                (LZ4Level)header.CompressionLevel);

        Array.Resize(
            ref dataCompressed,
            dataCompressedLength);

        WriteContentSize(
            writer, definition,
            (ulong)dataCompressedLength);

        WriteContentOffset(
            writer, definition,
            (ulong)writer.BaseStream.Position - header.StartContentsOffset);

        writer.Write((byte)0); // RoflanArchiveFileType IRoflanArchiveFileContent.Type (always equals 0 - RawBytes)
        writer.Write(dataCompressed.AsSpan());

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
