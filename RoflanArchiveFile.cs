using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using RoflanArchive.Core.Extensions;
using RoflanArchive.Core.Internal;

namespace RoflanArchive.Core;

public class RoflanArchiveFile : IRoflanHeader
{
    public const string Extension = ".roflarc";



    uint IRoflanHeader.FilesCount { get; set; }
    ulong IRoflanHeader.StartDefinitionsOffset { get; set; }
    ulong IRoflanHeader.StartContentsOffset { get; set; }



    private readonly ObservableDictionary<uint, RoflanFile> _files;



    public string Path { get; }
    public string Name { get; private set; }
    public ReadOnlyDictionary<uint, RoflanFile> Files
    {
        get
        {
            return new ReadOnlyDictionary<uint, RoflanFile>(
                _files);
        }
    }



    private RoflanArchiveFile(
        string filePath,
        string name = "")
    {
        _files = new ObservableDictionary<uint, RoflanFile>(
            new Dictionary<uint, RoflanFile>());

        _files.CollectionChanged += Files_OnCollectionChanged;

        Path = filePath;
        Name = name;
    }



    private RoflanArchiveFile Load()
    {
        _files.CollectionChanged -= Files_OnCollectionChanged;

        using var stream = File.Open(
            Path, FileMode.Open);
        using var reader = new BinaryReader(
            stream);

        Name = reader.ReadString();

        var header = (IRoflanHeader)this;
        header.FilesCount = reader.ReadUInt32();
        header.StartDefinitionsOffset = reader.ReadUInt64();
        header.StartContentsOffset = reader.ReadUInt64();

        for (uint i = 0; i < header.FilesCount; ++i)
        {
            var id = reader.ReadUInt32();
            var relativePath = reader.ReadString();

            var file = new RoflanFile(
                id,
                relativePath,
                relativePath);

            var definition = (IRoflanFileDefinition)file;
            definition.ContentSize = reader.ReadUInt64();
            definition.ContentOffset = reader.ReadUInt64();

            _files.Add(id, file);
        }

        foreach (var (_, file) in Files)
        {
            var definition = (IRoflanFileDefinition)file;

            var offset =
                header.StartContentsOffset + definition.ContentOffset;

            reader.BaseStream.Position = (long)offset;

            var content = (IRoflanFileContent)file;
            content.Type = (RoflanFileType)reader.ReadByte();

            file.Data = reader.ReadBytes((int)definition.ContentSize);
        }

        _files.CollectionChanged += Files_OnCollectionChanged;

        return this;
    }

    private RoflanArchiveFile Save()
    {
        using var stream = File.Open(
            Path, FileMode.OpenOrCreate);
        using var writer = new BinaryWriter(
            stream);

        var header = (IRoflanHeader)this;

        writer.Write(header.Name);

        header.StartDefinitionsOffset =
            (ulong)(writer.BaseStream.Position
                    + sizeof(uint)
                    + sizeof(ulong)
                    + sizeof(ulong));

        writer.Write(header.FilesCount);
        writer.Write(header.StartDefinitionsOffset);
        writer.Write(header.StartContentsOffset);

        var contentOffset = 0UL;

        foreach (var (_, file) in Files)
        {
            file.Data = File.ReadAllBytes(file.Path);

            var content = (IRoflanFileContent)file;
            content.Type = RoflanFileType.RawBytes;

            var definition = (IRoflanFileDefinition)file;
            definition.ContentSize = (ulong)content.Data.Length;
            definition.ContentOffset = contentOffset;

            contentOffset += sizeof(RoflanFileType) + definition.ContentSize;

            writer.Write(definition.Id);
            writer.Write(definition.RelativePath);
            writer.Write(definition.ContentSize);
            writer.Write(definition.ContentOffset);
        }

        header.StartContentsOffset = (ulong)writer.BaseStream.Position;

        writer.BaseStream.Position = (long)(header.StartDefinitionsOffset - sizeof(ulong));
        writer.Write(header.StartContentsOffset);
        writer.BaseStream.Position = (long)header.StartContentsOffset;

        foreach (var (_, file) in Files)
        {
            var content = (IRoflanFileContent)file;

            writer.Write((byte)content.Type);
            writer.Write(content.Data.Span);
        }

        return this;
    }


    private RoflanFile LoadFile(
        uint targetId)
    {
        using var stream = File.Open(
            Path, FileMode.Open);
        using var reader = new BinaryReader(
            stream);

        Name = reader.ReadString();

        var header = (IRoflanHeader)this;
        header.FilesCount = reader.ReadUInt32();
        header.StartDefinitionsOffset = reader.ReadUInt64();
        header.StartContentsOffset = reader.ReadUInt64();

        RoflanFile? file = null;
        IRoflanFileDefinition? definition = null;
        IRoflanFileContent? content = null;

        for (uint i = 0; i < header.FilesCount; ++i)
        {
            var id = reader.ReadUInt32();

            if (id != targetId)
            {
                var relativePathLength = reader.Read7BitEncodedInt();

                reader.BaseStream.Position +=
                    relativePathLength
                    + sizeof(ulong)
                    + sizeof(ulong);

                continue;
            }

            var relativePath = reader.ReadString();

            file = new RoflanFile(
                id,
                relativePath,
                relativePath);
            definition = file;
            content = file;

            definition.ContentSize = reader.ReadUInt64();
            definition.ContentOffset = reader.ReadUInt64();

            break;
        }

        if (file is null || definition is null || content is null)
            throw new FileNotFoundException($"File with provided id[{targetId}] was not found.");

        var offset =
            header.StartContentsOffset + definition.ContentOffset;

        reader.BaseStream.Position = (long)offset;

        content.Type = (RoflanFileType)reader.ReadByte();

        file.Data = reader.ReadBytes((int)definition.ContentSize);

        return file;
    }
    private RoflanFile LoadFile(
        string targetRelativePath)
    {
        using var stream = File.Open(
            Path, FileMode.Open);
        using var reader = new BinaryReader(
            stream);

        Name = reader.ReadString();

        var header = (IRoflanHeader)this;
        header.FilesCount = reader.ReadUInt32();
        header.StartDefinitionsOffset = reader.ReadUInt64();
        header.StartContentsOffset = reader.ReadUInt64();

        RoflanFile? file = null;
        IRoflanFileDefinition? definition = null;
        IRoflanFileContent? content = null;

        for (uint i = 0; i < header.FilesCount; ++i)
        {
            var id = reader.ReadUInt32();
            var relativePath = reader.ReadString();

            if (relativePath != targetRelativePath)
            {
                reader.BaseStream.Position +=
                    sizeof(ulong)
                    + sizeof(ulong);

                continue;
            }

            file = new RoflanFile(
                id,
                relativePath,
                relativePath);
            definition = file;
            content = file;

            definition.ContentSize = reader.ReadUInt64();
            definition.ContentOffset = reader.ReadUInt64();

            break;
        }

        if (file is null || definition is null || content is null)
            throw new FileNotFoundException($"File with provided relative path[{targetRelativePath}] was not found.");

        var offset =
            header.StartContentsOffset + definition.ContentOffset;

        reader.BaseStream.Position = (long)offset;

        content.Type = (RoflanFileType)reader.ReadByte();

        file.Data = reader.ReadBytes((int)definition.ContentSize);

        return file;
    }



    private void Files_OnCollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        var header = (IRoflanHeader)this;
        header.FilesCount = (uint)_files.Count;
    }



    public static RoflanArchiveFile Open(
        string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File at path '{filePath}' was not found");

        return new RoflanArchiveFile(
                filePath)
            .Load();
    }

    public static RoflanArchiveFile Pack(
        string directoryPath,
        string fileName,
        string sourceDirectoryPath,
        string[]? blacklistPaths = null,
        int maxNestingLevel = -1)
    {
        directoryPath = directoryPath
            .TrimEnd(System.IO.Path.DirectorySeparatorChar)
            .TrimEnd(System.IO.Path.AltDirectorySeparatorChar);
        directoryPath = !System.IO.Path.IsPathRooted(directoryPath)
            ? System.IO.Path.GetFullPath(directoryPath)
            : directoryPath;

        sourceDirectoryPath = sourceDirectoryPath
            .TrimEnd(System.IO.Path.DirectorySeparatorChar)
            .TrimEnd(System.IO.Path.AltDirectorySeparatorChar);
        sourceDirectoryPath = !System.IO.Path.IsPathRooted(sourceDirectoryPath)
            ? System.IO.Path.GetFullPath(sourceDirectoryPath)
            : sourceDirectoryPath;

        if (!Directory.Exists(directoryPath))
            throw new FileNotFoundException($"Directory at path '{directoryPath}' was not found");

        var archive = new RoflanArchiveFile(
            System.IO.Path.Combine(directoryPath, $"{fileName}{Extension}"),
            directoryPath[(directoryPath.LastIndexOfAny(new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }) + 1)..]);

        var id = 0U;

        foreach (var sourceFilePath in DirectoryExtensions.EnumerateAllFiles(
                     sourceDirectoryPath, blacklistPaths, maxNestingLevel))
        {
            var sourceFileRelativePath = System.IO.Path.GetRelativePath(
                sourceDirectoryPath, sourceFilePath);

            archive._files.Add(
                id,
                new RoflanFile(
                    id,
                    sourceFilePath,
                    sourceFileRelativePath));

            ++id;
        }

        return archive.Save();
    }

    public static void Unpack(
        string filePath,
        string targetDirectoryPath)
    {
        var archive = new RoflanArchiveFile(
                filePath)
            .Load();

        Unpack(
            archive,
            targetDirectoryPath);
    }
    public static void Unpack(
        RoflanArchiveFile file,
        string targetDirectoryPath)
    {
        targetDirectoryPath = targetDirectoryPath
            .TrimEnd(System.IO.Path.DirectorySeparatorChar)
            .TrimEnd(System.IO.Path.AltDirectorySeparatorChar);
        targetDirectoryPath = !System.IO.Path.IsPathRooted(targetDirectoryPath)
            ? System.IO.Path.GetFullPath(targetDirectoryPath)
            : targetDirectoryPath;

        if (!Directory.Exists(targetDirectoryPath))
            Directory.CreateDirectory(targetDirectoryPath);

        foreach (var (_, targetFile) in file._files)
        {
            var targetFilePath = System.IO.Path.Combine(
                targetDirectoryPath,
                targetFile.RelativePath);
            var targetFileDirectoryPath = System.IO.Path.GetDirectoryName(
                targetFilePath);

            if (targetFileDirectoryPath != null && !Directory.Exists(targetFileDirectoryPath))
                Directory.CreateDirectory(targetFileDirectoryPath);

            File.WriteAllBytes(
                targetFilePath,
                targetFile.Data.ToArray());
        }
    }


    public static RoflanFile GetFile(
        string filePath, uint id)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File at path '{filePath}' was not found");

        return GetFile(
            new RoflanArchiveFile(
                filePath),
            id);
    }
    public static RoflanFile GetFile(
        RoflanArchiveFile file, uint id)
    {
        return file
            .LoadFile(id);
    }
    public static RoflanFile GetFile(
        string filePath, string relativePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File at path '{filePath}' was not found");

        return GetFile(
            new RoflanArchiveFile(
                filePath),
            relativePath);
    }
    public static RoflanFile GetFile(
        RoflanArchiveFile file, string relativePath)
    {
        return file
            .LoadFile(relativePath);
    }
}
