using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using RoflanArchive.Core.Extensions;

namespace RoflanArchive.Core;

public class RoflanArchiveFile : IRoflanHeader
{
    public const string Extension = ".roflarc";



    uint IRoflanHeader.FilesCount
    {
        get
        {
            return (uint)Files.Count;
        }
    }
    ulong IRoflanHeader.StartDefinitionsOffset { get; set; }
    ulong IRoflanHeader.StartContentsOffset { get; set; }



    public string Path { get; }
    public string Name { get; }
    public ReadOnlyDictionary<uint, RoflanFile> Files { get; private set; }



    private RoflanArchiveFile(
        string filePath,
        string name = "")
    {
        Path = filePath;
        Name = name;
        Files = new ReadOnlyDictionary<uint, RoflanFile>(
            new Dictionary<uint, RoflanFile>());
    }



    private RoflanArchiveFile Load()
    {
        using var stream = File.Open(
            Path, FileMode.Open);
        using var reader = new BinaryReader(
            stream);



        return this;
    }

    private RoflanArchiveFile Save()
    {
        using var stream = File.Open(
            Path, FileMode.OpenOrCreate);
        using var writer = new BinaryWriter(
            stream);

        var header = (IRoflanHeader)this;
        header.StartDefinitionsOffset = (ulong)(header.Name.AsSpan().Length * sizeof(char) + sizeof(uint) + sizeof(ulong) + sizeof(ulong));

        writer.Write(header.Name);
        writer.Write(header.FilesCount);
        writer.Write(header.StartDefinitionsOffset);

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

        header.StartContentsOffset = (ulong)(writer.BaseStream.Position + sizeof(ulong));

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



    public static RoflanArchiveFile Open(
        string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File at path '{filePath}' not found");

        return new RoflanArchiveFile(
                filePath)
            .Load();
    }

    public static RoflanArchiveFile Create(
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

        if (!Directory.Exists(directoryPath))
            throw new FileNotFoundException($"Directory at path '{directoryPath}' not found");

        directoryPath += System.IO.Path.DirectorySeparatorChar;

        var archive = new RoflanArchiveFile(
            System.IO.Path.Combine(directoryPath, $"{fileName}{Extension}"),
            System.IO.Path.GetDirectoryName(directoryPath) ?? fileName);

        var id = 0U;
        var files = new Dictionary<uint, RoflanFile>();

        foreach (var sourceFilePath in DirectoryExtensions.EnumerateAllFiles(
                     sourceDirectoryPath, blacklistPaths, maxNestingLevel))
        {
            var sourceFileRelativePath = System.IO.Path.GetRelativePath(
                sourceDirectoryPath, sourceFilePath);

            files.Add(
                id,
                new RoflanFile(
                    id,
                    sourceFilePath,
                    sourceFileRelativePath));

            ++id;
        }

        archive.Files = new ReadOnlyDictionary<uint, RoflanFile>(
            files);

        return archive.Save();
    }
}
