using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using K4os.Compression.LZ4;
using RoflanArchives.Core.Api;
using RoflanArchives.Core.Extensions;

namespace RoflanArchives.Core;

public class RoflanArchive : IRoflanArchive, IEnumerable<RoflanArchiveFile>
{
    public const string Extension = ".roflarc";


    string IRoflanArchive.Path { get; set; }
    ReadOnlyObservableCollection<IRoflanArchiveFile> IRoflanArchive.Files
    {
        get
        {
            return new ReadOnlyObservableCollection<IRoflanArchiveFile>(
                new ObservableCollection<IRoflanArchiveFile>(
                    Files.Select(file => (IRoflanArchiveFile)file)));
        }
    }
    IRoflanArchiveFile IRoflanArchive.this[uint id]
    {
        get
        {
            return this[id];
        }
    }
    IRoflanArchiveFile IRoflanArchive.this[string relativePath]
    {
        get
        {
            return this[relativePath];
        }
    }

    Version IRoflanArchiveHeader.Version { get; set; }
    string IRoflanArchiveHeader.Name { get; set; }
    LZ4Level IRoflanArchiveHeader.CompressionLevel { get; set; }
    uint IRoflanArchiveHeader.FilesCount { get; set; }
    ulong IRoflanArchiveHeader.StartDefinitionsOffset { get; set; }
    ulong IRoflanArchiveHeader.StartContentsOffset { get; set; }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }



    private readonly IRoflanArchiveApi _api;



    // ReSharper disable InconsistentNaming

    internal readonly ObservableCollection<RoflanArchiveFile> _files;
    internal readonly Dictionary<uint, RoflanArchiveFile> _filesById;
    internal readonly Dictionary<string, RoflanArchiveFile> _filesByRelativePath;

    // ReSharper restore InconsistentNaming



    public string Path
    {
        get
        {
            return ((IRoflanArchive)this).Path;
        }
    }

    public Version Version
    {
        get
        {
            return ((IRoflanArchiveHeader)this).Version;
        }
    }
    public string Name
    {
        get
        {
            return ((IRoflanArchiveHeader)this).Name;
        }
    }

    public ReadOnlyObservableCollection<RoflanArchiveFile> Files { get; internal set; }



    public RoflanArchiveFile this[uint id]
    {
        get
        {
            return _filesById[id];
        }
    }
    public RoflanArchiveFile this[string relativePath]
    {
        get
        {
            return _filesByRelativePath[relativePath];
        }
    }



#pragma warning disable CS8618

    private RoflanArchive(
        string filePath,
        string name = "",
        Version? version = null)
    {
        var archive = (IRoflanArchive)this;

        archive.Path = filePath;

        var header = (IRoflanArchiveHeader)this;

        header.Version = version
                         ?? typeof(RoflanArchive).Assembly.GetName().Version
                         ?? new Version(0, 0, 0, 0);
        header.Name = name;

        _api = ApiManager.GetApi(this);

        _files =
            new ObservableCollection<RoflanArchiveFile>(
                new List<RoflanArchiveFile>());
        _filesById =
            new Dictionary<uint, RoflanArchiveFile>();
        _filesByRelativePath =
            new Dictionary<string, RoflanArchiveFile>();

        _files.CollectionChanged += Files_OnCollectionChanged;

        Files = new ReadOnlyObservableCollection<RoflanArchiveFile>(
            _files);
    }
    private RoflanArchive(
        string filePath,
        LZ4Level compressionLevel,
        string name,
        Version? version)
        : this(filePath, name, version)
    {
        var header = (IRoflanArchiveHeader)this;

        header.CompressionLevel = compressionLevel;
    }

#pragma warning restore CS8618



    private RoflanArchive Load()
    {
        _files.CollectionChanged -= Files_OnCollectionChanged;

        var archive = _api.Load(this);

        _files.CollectionChanged += Files_OnCollectionChanged;

        return archive;
    }

    private RoflanArchive Save()
    {
        return _api.Save(this);
    }


    private IRoflanArchiveFile LoadFile(
        uint targetId)
    {
        return _api.LoadFile(this, targetId);
    }
    private IRoflanArchiveFile LoadFile(
        string targetRelativePath)
    {
        return _api.LoadFile(this, targetRelativePath);
    }



    public IEnumerator<RoflanArchiveFile> GetEnumerator()
    {
        return _files.GetEnumerator();
    }



    private void Files_OnCollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        var header = (IRoflanArchiveHeader)this;

        header.FilesCount = (uint)_files.Count;

        Files = new ReadOnlyObservableCollection<RoflanArchiveFile>(
            _files);
    }



    public static RoflanArchive Open(
        string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File at path '{filePath}' was not found");

        return new RoflanArchive(
                filePath)
            .Load();
    }


    public static RoflanArchive Pack(
        string directoryPath,
        string fileName,
        string sourceDirectoryPath,
        LZ4Level compressionLevel = LZ4Level.L00_FAST,
        string? archiveName = null,
        Version? version = null,
        IEnumerable<string>? blacklistPaths = null,
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

        var archive = new RoflanArchive(
            System.IO.Path.Combine(directoryPath, $"{fileName}{Extension}"),
            compressionLevel,
            archiveName ?? fileName,
            version);

        var id = uint.MinValue;

        foreach (var filePath in DirectoryExtensions.EnumerateAllFiles(
                     sourceDirectoryPath, blacklistPaths?.ToArray(), maxNestingLevel))
        {
            var fileRelativePath = System.IO.Path.GetRelativePath(
                sourceDirectoryPath, filePath);

            var fileData = File.ReadAllBytes(
                filePath);

            var file = new RoflanArchiveFile(
                id,
                fileRelativePath,
                fileData);

            archive._files.Add(
                file);
            archive._filesById.Add(
                id, file);
            archive._filesByRelativePath.Add(
                fileRelativePath, file);

            ++id;
        }

        return archive.Save();
    }
    public static RoflanArchive Pack(
        string directoryPath,
        string fileName,
        IEnumerable<(string DirectoryPath, string[]? FilePaths)> sourcePaths,
        LZ4Level compressionLevel = LZ4Level.L00_FAST,
        string? archiveName = null,
        Version? version = null)
    {
        return Pack(
            directoryPath,
            fileName,
            sourcePaths.Select(element =>
                (element.DirectoryPath, element.FilePaths?.AsEnumerable())),
            compressionLevel,
            archiveName,
            version);
    }
    public static RoflanArchive Pack(
        string directoryPath,
        string fileName,
        IEnumerable<(string DirectoryPath, List<string>? FilePaths)> sourcePaths,
        LZ4Level compressionLevel = LZ4Level.L00_FAST,
        string? archiveName = null,
        Version? version = null)
    {
        return Pack(
            directoryPath,
            fileName,
            sourcePaths.Select(element =>
                (element.DirectoryPath, element.FilePaths?.AsEnumerable())),
            compressionLevel,
            archiveName,
            version);
    }
    public static RoflanArchive Pack(
        string directoryPath,
        string fileName,
        IEnumerable<(string DirectoryPath, IEnumerable<string>? FilePaths)> sourcePaths,
        LZ4Level compressionLevel = LZ4Level.L00_FAST,
        string? archiveName = null,
        Version? version = null)
    {
        directoryPath = directoryPath
            .TrimEnd(System.IO.Path.DirectorySeparatorChar)
            .TrimEnd(System.IO.Path.AltDirectorySeparatorChar);
        directoryPath = !System.IO.Path.IsPathRooted(directoryPath)
            ? System.IO.Path.GetFullPath(directoryPath)
            : directoryPath;

        if (!Directory.Exists(directoryPath))
            throw new FileNotFoundException($"Directory at path '{directoryPath}' was not found");

        var archive = new RoflanArchive(
            System.IO.Path.Combine(directoryPath, $"{fileName}{Extension}"),
            compressionLevel,
            archiveName ?? fileName,
            version);

        var id = uint.MinValue;

        foreach (var sourcePath in sourcePaths)
        {
            var sourceDirectoryPath = sourcePath.DirectoryPath;

            sourceDirectoryPath = sourceDirectoryPath
                .TrimEnd(System.IO.Path.DirectorySeparatorChar)
                .TrimEnd(System.IO.Path.AltDirectorySeparatorChar);
            sourceDirectoryPath = !System.IO.Path.IsPathRooted(sourceDirectoryPath)
                ? System.IO.Path.GetFullPath(sourceDirectoryPath)
                : sourceDirectoryPath;

            if (!Directory.Exists(sourceDirectoryPath))
                throw new FileNotFoundException($"Directory at path '{sourceDirectoryPath}' was not found");

            // Small hack for getting the name of the current directory instead of the parent
            var sourceDirectoryName = System.IO.Path.GetFileName(
                sourceDirectoryPath)!;

            var sourceFilePaths = sourcePath.FilePaths;

            // ReSharper disable PossibleMultipleEnumeration

            sourceFilePaths = sourceFilePaths == null || !sourceFilePaths.Any()
                ? DirectoryExtensions.EnumerateAllFiles(
                    sourceDirectoryPath)
                : sourceFilePaths;

            // ReSharper restore PossibleMultipleEnumeration

            foreach (var sourceFilePath in sourceFilePaths)
            {
                string filePath;
                string fileRelativePath;

                if (!System.IO.Path.IsPathRooted(sourceFilePath))
                {
                    filePath = System.IO.Path.Combine(
                        sourceDirectoryPath,
                        sourceFilePath);

                    fileRelativePath = System.IO.Path.Combine(
                        sourceDirectoryName,
                        sourceFilePath);
                }
                else
                {
                    filePath = sourceFilePath;

                    fileRelativePath = System.IO.Path.GetRelativePath(
                        sourceDirectoryPath,
                        filePath);
                    fileRelativePath = !System.IO.Path.IsPathRooted(fileRelativePath)
                        ? System.IO.Path.Combine(
                            sourceDirectoryName,
                            fileRelativePath)
                        : fileRelativePath[(System.IO.Path.GetPathRoot(fileRelativePath)?.Length ?? 0)..];
                }

                var fileData = File.ReadAllBytes(
                    filePath);

                var file = new RoflanArchiveFile(
                    id,
                    fileRelativePath,
                    fileData);

                archive._files.Add(
                    file);
                archive._filesById.Add(
                    id, file);
                archive._filesByRelativePath.Add(
                    fileRelativePath, file);

                ++id;
            }
        }

        return archive.Save();
    }
    public static RoflanArchive Pack(
        string directoryPath,
        string fileName,
        IEnumerable<(string DirectoryPath, (uint Id, string Path)[]? FileInfos)> sourcePaths,
        LZ4Level compressionLevel = LZ4Level.L00_FAST,
        string? archiveName = null,
        Version? version = null)
    {
        return Pack(
            directoryPath,
            fileName,
            sourcePaths.Select(element =>
                (element.DirectoryPath, element.FileInfos?.AsEnumerable())),
            compressionLevel,
            archiveName,
            version);
    }
    public static RoflanArchive Pack(
        string directoryPath,
        string fileName,
        IEnumerable<(string DirectoryPath, List<(uint Id, string Path)>? FileInfos)> sourcePaths,
        LZ4Level compressionLevel = LZ4Level.L00_FAST,
        string? archiveName = null,
        Version? version = null)
    {
        return Pack(
            directoryPath,
            fileName,
            sourcePaths.Select(element =>
                (element.DirectoryPath, element.FileInfos?.AsEnumerable())),
            compressionLevel,
            archiveName,
            version);
    }
    public static RoflanArchive Pack(
        string directoryPath,
        string fileName,
        IEnumerable<(string DirectoryPath, IEnumerable<(uint Id, string Path)>? FileInfos)> sourcePaths,
        LZ4Level compressionLevel = LZ4Level.L00_FAST,
        string? archiveName = null,
        Version? version = null)
    {
        directoryPath = directoryPath
            .TrimEnd(System.IO.Path.DirectorySeparatorChar)
            .TrimEnd(System.IO.Path.AltDirectorySeparatorChar);
        directoryPath = !System.IO.Path.IsPathRooted(directoryPath)
            ? System.IO.Path.GetFullPath(directoryPath)
            : directoryPath;

        if (!Directory.Exists(directoryPath))
            throw new FileNotFoundException($"Directory at path '{directoryPath}' was not found");

        var archive = new RoflanArchive(
            System.IO.Path.Combine(directoryPath, $"{fileName}{Extension}"),
            compressionLevel,
            archiveName ?? fileName,
            version);

        var id = uint.MaxValue;

        foreach (var sourcePath in sourcePaths)
        {
            var sourceDirectoryPath = sourcePath.DirectoryPath;

            sourceDirectoryPath = sourceDirectoryPath
                .TrimEnd(System.IO.Path.DirectorySeparatorChar)
                .TrimEnd(System.IO.Path.AltDirectorySeparatorChar);
            sourceDirectoryPath = !System.IO.Path.IsPathRooted(sourceDirectoryPath)
                ? System.IO.Path.GetFullPath(sourceDirectoryPath)
                : sourceDirectoryPath;

            if (!Directory.Exists(sourceDirectoryPath))
                throw new FileNotFoundException($"Directory at path '{sourceDirectoryPath}' was not found");

            // Small hack for getting the name of the current directory instead of the parent
            var sourceDirectoryName = System.IO.Path.GetFileName(
                sourceDirectoryPath)!;

            var sourceFileInfos = sourcePath.FileInfos;

            // ReSharper disable PossibleMultipleEnumeration

            sourceFileInfos = sourceFileInfos == null || !sourceFileInfos.Any()
                ? DirectoryExtensions
                    .EnumerateAllFiles(
                        sourceDirectoryPath)
                    .Select(
                        path => (--id, path))
                : sourceFileInfos;

            // ReSharper restore PossibleMultipleEnumeration

            foreach (var (fileId, sourceFilePath) in sourceFileInfos)
            {
                string filePath;
                string fileRelativePath;

                if (!System.IO.Path.IsPathRooted(sourceFilePath))
                {
                    filePath = System.IO.Path.Combine(
                        sourceDirectoryPath,
                        sourceFilePath);

                    fileRelativePath = System.IO.Path.Combine(
                        sourceDirectoryName,
                        sourceFilePath);
                }
                else
                {
                    filePath = sourceFilePath;

                    fileRelativePath = System.IO.Path.GetRelativePath(
                        sourceDirectoryPath,
                        filePath);
                    fileRelativePath = !System.IO.Path.IsPathRooted(fileRelativePath)
                        ? System.IO.Path.Combine(
                            sourceDirectoryName,
                            fileRelativePath)
                        : fileRelativePath[(System.IO.Path.GetPathRoot(fileRelativePath)?.Length ?? 0)..];
                }

                var fileData = File.ReadAllBytes(
                    filePath);

                var file = new RoflanArchiveFile(
                    fileId,
                    fileRelativePath,
                    fileData);

                archive._files.Add(
                    file);
                archive._filesById.Add(
                    fileId, file);
                archive._filesByRelativePath.Add(
                    fileRelativePath, file);
            }
        }

        return archive.Save();
    }

    public static void Unpack(
        string filePath,
        string targetDirectoryPath)
    {
        var archive = new RoflanArchive(
                filePath)
            .Load();

        Unpack(
            archive,
            targetDirectoryPath);
    }
    public static void Unpack(
        RoflanArchive file,
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


    public static RoflanArchiveFile GetFile(
        string filePath,
        uint id)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File at path '{filePath}' was not found");

        return GetFile(
            new RoflanArchive(
                filePath),
            id);
    }
    public static RoflanArchiveFile GetFile(
        RoflanArchive file,
        uint id)
    {
        return (RoflanArchiveFile)file
            .LoadFile(id);
    }
    public static RoflanArchiveFile GetFile(
        string filePath,
        string relativePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File at path '{filePath}' was not found");

        return GetFile(
            new RoflanArchive(
                filePath),
            relativePath);
    }
    public static RoflanArchiveFile GetFile(
        RoflanArchive file,
        string relativePath)
    {
        return (RoflanArchiveFile)file
            .LoadFile(relativePath);
    }
}
