using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using K4os.Compression.LZ4;
using RoflanArchive.Core.Extensions;

namespace RoflanArchive.Core;

public class RoflanArchiveFile : IRoflanHeader, IEnumerable<RoflanFile>
{
    public const string Extension = ".roflarc";



    LZ4Level IRoflanHeader.CompressionLevel { get; set; }
    uint IRoflanHeader.FilesCount { get; set; }
    ulong IRoflanHeader.StartDefinitionsOffset { get; set; }
    ulong IRoflanHeader.StartContentsOffset { get; set; }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }



    private readonly ObservableCollection<RoflanFile> _files;
    private readonly Dictionary<uint, RoflanFile> _filesById;
    private readonly Dictionary<string, RoflanFile> _filesByRelativePath;



    public string Path { get; }
    public Version Version { get; private set; }
    public string Name { get; private set; }

    public ReadOnlyObservableCollection<RoflanFile> Files { get; private set; }



    public RoflanFile this[uint id]
    {
        get
        {
            return _filesById[id];
        }
    }
    public RoflanFile this[string relativePath]
    {
        get
        {
            return _filesByRelativePath[relativePath];
        }
    }



    private RoflanArchiveFile(
        string filePath,
        string name = "")
    {
        _files =
            new ObservableCollection<RoflanFile>(
                new List<RoflanFile>());
        _filesById =
            new Dictionary<uint, RoflanFile>();
        _filesByRelativePath =
            new Dictionary<string, RoflanFile>();

        _files.CollectionChanged += Files_OnCollectionChanged;

        Path = filePath;
        Version = typeof(RoflanArchiveFile).Assembly.GetName().Version
                  ?? new Version(0, 0, 0, 0);
        Name = name;

        Files = new ReadOnlyObservableCollection<RoflanFile>(
            _files);
    }



    private RoflanArchiveFile Load()
    {
        _files.CollectionChanged -= Files_OnCollectionChanged;

        using var stream = File.Open(
            Path, FileMode.Open);
        using var reader = new BinaryReader(
            stream);

        var major = reader.ReadInt32();
        var minor = reader.ReadInt32();
        var build = reader.ReadInt32();
        var revision = reader.ReadInt32();

        Version = new Version(
            major, minor,
            build, revision);
        Name = reader.ReadString();

        var header = (IRoflanHeader)this;
        header.CompressionLevel = (LZ4Level)reader.ReadInt32();
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
            definition.OriginalContentSize = reader.ReadUInt64();
            definition.ContentSize = reader.ReadUInt64();
            definition.ContentOffset = reader.ReadUInt64();

            _files.Add(
                file);
            _filesById.Add(
                id, file);
            _filesByRelativePath.Add(
                relativePath, file);
        }

        foreach (var file in _files)
        {
            var definition = (IRoflanFileDefinition)file;

            var offset =
                header.StartContentsOffset + definition.ContentOffset;

            reader.BaseStream.Position = (long)offset;

            var content = (IRoflanFileContent)file;
            content.Type = (RoflanFileType)reader.ReadByte();

            var dataCompressed = reader.ReadBytes(
                (int)definition.ContentSize);
            var data = new byte[definition.OriginalContentSize];

            LZ4Codec.Decode(
                dataCompressed,
                data);

            file.Data = data;
        }

        Files = new ReadOnlyObservableCollection<RoflanFile>(
            _files);

        _files.CollectionChanged += Files_OnCollectionChanged;

        return this;
    }

    private RoflanArchiveFile Save(
        LZ4Level compressionLevel)
    {
        using var stream = File.Open(
            Path, FileMode.OpenOrCreate);
        using var writer = new BinaryWriter(
            stream);

        var header = (IRoflanHeader)this;

        writer.Write(header.Version.Major);
        writer.Write(header.Version.Minor);
        writer.Write(header.Version.Build);
        writer.Write(header.Version.Revision);
        writer.Write(header.Name);

        header.CompressionLevel = compressionLevel;
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

        var index = 0U;
        var contentOffset = 0UL;
        var endDefinitionOffsets = new long[header.FilesCount];

        foreach (var file in _files)
        {
            file.Data = File.ReadAllBytes(
                file.Path);

            var content = (IRoflanFileContent)file;
            content.Type = RoflanFileType.RawBytes;

            var definition = (IRoflanFileDefinition)file;
            definition.OriginalContentSize = (ulong)content.Data.Length;
            definition.ContentOffset = contentOffset;

            contentOffset += sizeof(RoflanFileType) + definition.ContentSize;

            writer.Write(definition.Id);
            writer.Write(definition.RelativePath);
            writer.Write(definition.OriginalContentSize);
            writer.Write(definition.ContentSize);
            writer.Write(definition.ContentOffset);

            endDefinitionOffsets[index] = writer.BaseStream.Position;

            ++index;
        }

        header.StartContentsOffset = (ulong)writer.BaseStream.Position;

        writer.BaseStream.Position = (long)(header.StartDefinitionsOffset - sizeof(ulong));
        writer.Write(header.StartContentsOffset);
        writer.BaseStream.Position = (long)header.StartContentsOffset;

        index = 0U;

        foreach (var file in _files)
        {
            var definition = (IRoflanFileDefinition)file;
            var content = (IRoflanFileContent)file;

            var dataCompressed = new byte[LZ4Codec.MaximumOutputSize(content.Data.Length)];
            var dataCompressedLength =
                LZ4Codec.Encode(
                    content.Data.Span,
                    dataCompressed,
                    header.CompressionLevel);

            Array.Resize(
                ref dataCompressed,
                dataCompressedLength);

            definition.ContentSize = (ulong)dataCompressedLength;

            var position = writer.BaseStream.Position;

            writer.BaseStream.Position = endDefinitionOffsets[index]
                                         - sizeof(ulong)
                                         - sizeof(ulong);
            writer.Write(definition.ContentSize);
            writer.BaseStream.Position = position;

            file.Data = dataCompressed;

            writer.Write((byte)content.Type);
            writer.Write(content.Data.Span);

            ++index;
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

        var major = reader.ReadInt32();
        var minor = reader.ReadInt32();
        var build = reader.ReadInt32();
        var revision = reader.ReadInt32();

        Version = new Version(
            major, minor,
            build, revision);
        Name = reader.ReadString();

        var header = (IRoflanHeader)this;
        header.CompressionLevel = (LZ4Level)reader.ReadInt32();
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

            definition.OriginalContentSize = reader.ReadUInt64();
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

        var dataCompressed = reader.ReadBytes(
            (int)definition.ContentSize);
        var data = new byte[definition.OriginalContentSize];

        LZ4Codec.Decode(
            dataCompressed,
            data);

        file.Data = data;

        return file;
    }
    private RoflanFile LoadFile(
        string targetRelativePath)
    {
        using var stream = File.Open(
            Path, FileMode.Open);
        using var reader = new BinaryReader(
            stream);

        var major = reader.ReadInt32();
        var minor = reader.ReadInt32();
        var build = reader.ReadInt32();
        var revision = reader.ReadInt32();

        Version = new Version(
            major, minor,
            build, revision);
        Name = reader.ReadString();

        var header = (IRoflanHeader)this;
        header.CompressionLevel = (LZ4Level)reader.ReadInt32();
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
                    + sizeof(ulong)
                    + sizeof(ulong);

                continue;
            }

            file = new RoflanFile(
                id,
                relativePath,
                relativePath);
            definition = file;
            content = file;

            definition.OriginalContentSize = reader.ReadUInt64();
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

        var dataCompressed = reader.ReadBytes(
            (int)definition.ContentSize);
        var data = new byte[definition.OriginalContentSize];

        LZ4Codec.Decode(
            dataCompressed,
            data);

        file.Data = data;

        return file;
    }



    public IEnumerator<RoflanFile> GetEnumerator()
    {
        return _files.GetEnumerator();
    }



    private void Files_OnCollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        var header = (IRoflanHeader)this;
        header.FilesCount = (uint)_files.Count;

        Files = new ReadOnlyObservableCollection<RoflanFile>(
            _files);
    }



    public static RoflanArchiveFile Open(
        string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File at path '{filePath}' was not found");

        return new RoflanArchiveFile(
                filePath,
                string.Empty)
            .Load();
    }

    public static RoflanArchiveFile Pack(
        string directoryPath,
        string fileName,
        string sourceDirectoryPath,
        LZ4Level compressionLevel = LZ4Level.L00_FAST,
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
        if (!Directory.Exists(sourceDirectoryPath))
            throw new FileNotFoundException($"Directory at path '{sourceDirectoryPath}' was not found");

        var archive = new RoflanArchiveFile(
            System.IO.Path.Combine(directoryPath, $"{fileName}{Extension}"),
            sourceDirectoryPath[(sourceDirectoryPath.LastIndexOfAny(new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }) + 1)..]);

        var id = 0U;

        foreach (var sourceFilePath in DirectoryExtensions.EnumerateAllFiles(
                     sourceDirectoryPath, blacklistPaths, maxNestingLevel))
        {
            var sourceFileRelativePath = System.IO.Path.GetRelativePath(
                sourceDirectoryPath, sourceFilePath);

            var file = new RoflanFile(
                id,
                sourceFilePath,
                sourceFileRelativePath);

            archive._files.Add(
                file);
            archive._filesById.Add(
                id, file);
            archive._filesByRelativePath.Add(
                sourceFileRelativePath, file);

            ++id;
        }

        return archive.Save(
            compressionLevel);
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

        foreach (var targetFile in file._files)
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
