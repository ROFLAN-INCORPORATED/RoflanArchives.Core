﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using K4os.Compression.LZ4;
using RoflanArchives.Core.Api;
using RoflanArchives.Core.Cryptography;
using RoflanArchives.Core.Definitions.Json;
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
    RoflanArchiveCompressionType IRoflanArchiveHeader.CompressionType { get; set; }
    byte IRoflanArchiveHeader.CompressionLevel { get; set; }
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
        RoflanArchiveCompressionType compressionType = RoflanArchiveCompressionType.Inherited,
        byte? compressionLevel = null,
        string name = "",
        Version? version = null)
    {
        var archive = (IRoflanArchive)this;

        archive.Path = filePath;

        var header = (IRoflanArchiveHeader)this;

        header.CompressionType =
            compressionType == RoflanArchiveCompressionType.Inherited
                ? RoflanArchiveCompressionType.LZ4Stream
                : compressionType;
        header.CompressionLevel =
            compressionLevel ?? (byte)LZ4Level.L00_FAST;

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
        IEnumerable<RoflanArchiveSourceDirectoryInfo> sources,
        RoflanArchiveCompressionType compressionType = RoflanArchiveCompressionType.LZ4Stream,
        byte compressionLevel = (byte)LZ4Level.L00_FAST,
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

        var fileNameWithoutExtension =
            System.IO.Path.GetExtension(fileName) == Extension
                ? System.IO.Path.GetFileNameWithoutExtension(fileName)
                : fileName;
        fileName = fileNameWithoutExtension + Extension;

        var archive = new RoflanArchive(
            System.IO.Path.Combine(directoryPath, fileName),
            compressionType,
            compressionLevel,
            archiveName ?? fileNameWithoutExtension,
            version);

        foreach (var source in sources)
        {
            var sourcePath = source.Path;

            sourcePath = sourcePath
                .TrimEnd(System.IO.Path.DirectorySeparatorChar)
                .TrimEnd(System.IO.Path.AltDirectorySeparatorChar);
            sourcePath = !System.IO.Path.IsPathRooted(sourcePath)
                ? System.IO.Path.GetFullPath(sourcePath)
                : sourcePath;

            if (!Directory.Exists(sourcePath))
                throw new FileNotFoundException($"Source at path '{sourcePath}' was not found");

            // Small hack for getting the name of the current directory instead of the parent
            var sourceDirectoryName =
                System.IO.Path.GetFileName(
                    sourcePath);

            var sourceFiles = source.Files;

            // ReSharper disable PossibleMultipleEnumeration

            sourceFiles = sourceFiles == null || !sourceFiles.Any()
                ? DirectoryExtensions
                    .EnumerateAllFiles(
                        sourcePath,
                        source.BlacklistPaths?.ToArray(),
                        source.MaxDepth)
                    .Select(
                        path => new RoflanArchiveSourceFileInfo(path))
                : sourceFiles;

            // ReSharper restore PossibleMultipleEnumeration

            foreach (var sourceFile in sourceFiles)
            {
                string filePath;
                string fileRelativePath;

                if (!System.IO.Path.IsPathRooted(sourceFile.Path))
                {
                    filePath = System.IO.Path.Combine(
                        sourcePath,
                        sourceFile.Path);

                    fileRelativePath = System.IO.Path.Combine(
                        sourceDirectoryName,
                        sourceFile.Path);
                }
                else
                {
                    filePath = sourceFile.Path;

                    fileRelativePath = System.IO.Path.GetRelativePath(
                        sourcePath,
                        filePath);
                    fileRelativePath = !System.IO.Path.IsPathRooted(fileRelativePath)
                        ? System.IO.Path.Combine(
                            sourceDirectoryName,
                            fileRelativePath)
                        : fileRelativePath[(System.IO.Path.GetPathRoot(fileRelativePath)?.Length ?? 0)..];
                }

                if (OperatingSystem.IsWindows())
                    fileRelativePath = fileRelativePath.Replace('\\', '/');

                var file = new RoflanArchiveFile(
                    sourceFile.Id ?? RoflanArchiveUtils.GenerateId(fileRelativePath),
                    fileRelativePath,
                    sourceFile.CompressionType,
                    sourceFile.CompressionLevel);

                // Small hack to get the parent directory
                ((IRoflanArchiveFile)file).DirectoryPath = System.IO.Path.Combine(sourcePath, "..");

                if (archive._api.Version < new Version(1, 5, 0, 0))
                {
                    var fileData = File.ReadAllBytes(
                        filePath);

#pragma warning disable CS0618 // Тип или член устарел
                    ((IRoflanArchiveFile)file).Data = fileData;
#pragma warning restore CS0618 // Тип или член устарел
                }
                else
                {
                    using var fileDataStream =
                        file.GetReadStream();

                    file.DataStream.Position = 0;

                    fileDataStream.CopyTo(
                        file.DataStream);

                    file.DataStream.Position = 0;
                }

                archive._files.Add(
                    file);
                archive._filesById.Add(
                    file.Id, file);
                archive._filesByRelativePath.Add(
                    file.RelativePath, file);
            }
        }

        return archive.Save();
    }
    public static IEnumerable<RoflanArchive> Pack(
        string definitionFilePath)
    {
        if (!File.Exists(definitionFilePath))
            throw new FileNotFoundException($"Definition file at path '{definitionFilePath}' was not found");

        JsonDefinition? definition = null;

        try
        {
            using (var definitionFile = File.OpenRead(definitionFilePath))
            {
                definition = new JsonDefinition(definitionFile);
            }
        }
        catch (Exception e)
        {
            throw new SerializationException($"Error when trying to read json definition from file at path '{definitionFilePath}'", e);
        }

        if (definition == null)
            throw new SerializationException($"Error when trying to read json definition from file at path '{definitionFilePath}'");

        var archives = new List<RoflanArchive>(
            definition.Schema.Archives.Count);

        foreach (var archiveSchema in definition.Schema.Archives)
        {
            var directoryPath = System.IO.Path.GetDirectoryName(archiveSchema.FilePath);
            var fileName = System.IO.Path.GetFileName(archiveSchema.FilePath);
            var archiveName = archiveSchema.Name;
            var compressionType = archiveSchema.CompressionType;
            var compressionLevel = (byte?)archiveSchema.CompressionLevel;
            var version = archiveSchema.Version != null
                ? new Version(archiveSchema.Version)
                : null;
            var sources = archiveSchema.SourceDirectories;

            if (directoryPath == null)
                directoryPath = AppDomain.CurrentDomain.BaseDirectory;

            directoryPath = directoryPath
                .TrimEnd(System.IO.Path.DirectorySeparatorChar)
                .TrimEnd(System.IO.Path.AltDirectorySeparatorChar);
            directoryPath = !System.IO.Path.IsPathRooted(directoryPath)
                ? System.IO.Path.GetFullPath(directoryPath)
                : directoryPath;

            if (!Directory.Exists(directoryPath))
                throw new FileNotFoundException($"Directory at path '{directoryPath}' was not found");

            var fileNameWithoutExtension =
                System.IO.Path.GetExtension(fileName) == Extension
                    ? System.IO.Path.GetFileNameWithoutExtension(fileName)
                    : fileName;
            fileName = fileNameWithoutExtension + Extension;

            var archive = new RoflanArchive(
                System.IO.Path.Combine(directoryPath, fileName),
                compressionType,
                compressionLevel,
                archiveName ?? fileNameWithoutExtension,
                version);

            foreach (var source in sources)
            {
                var sourcePath = source.Path;

                sourcePath = sourcePath
                    .TrimEnd(System.IO.Path.DirectorySeparatorChar)
                    .TrimEnd(System.IO.Path.AltDirectorySeparatorChar);
                sourcePath = !System.IO.Path.IsPathRooted(sourcePath)
                    ? System.IO.Path.GetFullPath(sourcePath)
                    : sourcePath;

                if (!Directory.Exists(sourcePath))
                    throw new FileNotFoundException($"Source at path '{sourcePath}' was not found");

                // Small hack for getting the name of the current directory instead of the parent
                var sourceDirectoryName =
                    System.IO.Path.GetFileName(
                        sourcePath);

                var sourceFiles = source.Files;

                // ReSharper disable PossibleMultipleEnumeration

                sourceFiles = sourceFiles == null || !sourceFiles.Any()
                    ? DirectoryExtensions
                        .EnumerateAllFiles(
                            sourcePath,
                            source.BlacklistPaths?.ToArray(),
                            source.MaxDepth)
                        .Select(
                            path => new FileSchema { Path = path })
                        .ToObservableCollection()
                    : sourceFiles;

                // ReSharper restore PossibleMultipleEnumeration

                foreach (var sourceFile in sourceFiles)
                {
                    string filePath;
                    string fileRelativePath;

                    if (!System.IO.Path.IsPathRooted(sourceFile.Path))
                    {
                        filePath = System.IO.Path.Combine(
                            sourcePath,
                            sourceFile.Path);

                        fileRelativePath = System.IO.Path.Combine(
                            sourceDirectoryName,
                            sourceFile.Path);
                    }
                    else
                    {
                        filePath = sourceFile.Path;

                        fileRelativePath = System.IO.Path.GetRelativePath(
                            sourcePath,
                            filePath);
                        fileRelativePath = !System.IO.Path.IsPathRooted(fileRelativePath)
                            ? System.IO.Path.Combine(
                                sourceDirectoryName,
                                fileRelativePath)
                            : fileRelativePath[(System.IO.Path.GetPathRoot(fileRelativePath)?.Length ?? 0)..];
                    }

                    if (OperatingSystem.IsWindows())
                        fileRelativePath = fileRelativePath.Replace('\\', '/');

                    var file = new RoflanArchiveFile(
                        sourceFile.Id ?? RoflanArchiveUtils.GenerateId(fileRelativePath),
                        fileRelativePath,
                        sourceFile.CompressionType,
                        (byte?)sourceFile.CompressionLevel);

                    // Small hack to get the parent directory
                    ((IRoflanArchiveFile)file).DirectoryPath = System.IO.Path.Combine(sourcePath, "..");

                    if (archive._api.Version < new Version(1, 5, 0, 0))
                    {
                        var fileData = File.ReadAllBytes(
                            filePath);

#pragma warning disable CS0618 // Тип или член устарел
                        ((IRoflanArchiveFile)file).Data = fileData;
#pragma warning restore CS0618 // Тип или член устарел
                    }
                    else
                    {
                        using var fileDataStream =
                            file.GetReadStream();

                        file.DataStream.Position = 0;

                        fileDataStream.CopyTo(
                            file.DataStream);

                        file.DataStream.Position = 0;
                    }

                    archive._files.Add(
                        file);
                    archive._filesById.Add(
                        file.Id, file);
                    archive._filesByRelativePath.Add(
                        file.RelativePath, file);
                }
            }

            archives.Add(archive.Save());
        }

        return archives;
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
            var targetFileRelativePath = targetFile.RelativePath;

            if (OperatingSystem.IsWindows())
                targetFileRelativePath = targetFileRelativePath.Replace('/', '\\');

            var targetFilePath = System.IO.Path.Combine(
                targetDirectoryPath,
                targetFileRelativePath);
            var targetFileDirectoryPath = System.IO.Path.GetDirectoryName(
                targetFilePath);

            if (targetFileDirectoryPath != null && !Directory.Exists(targetFileDirectoryPath))
                Directory.CreateDirectory(targetFileDirectoryPath);

            ((IRoflanArchiveFile)targetFile).DirectoryPath = targetDirectoryPath;

            if (file._api.Version < new Version(1, 5, 0, 0))
            {
#pragma warning disable CS0618 // Тип или член устарел
                File.WriteAllBytes(
                    targetFilePath,
                    targetFile.Data.ToArray());
#pragma warning restore CS0618 // Тип или член устарел
            }
            else
            {
                using var fileDataStream =
                    targetFile.GetWriteStream();

                // save position
                var position = targetFile.DataStream.Position;

                // set position 0
                targetFile.DataStream.Position = 0;

                targetFile.DataStream.CopyTo(
                    fileDataStream);

                // reset position
                targetFile.DataStream.Position = position;
            }
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
        if (OperatingSystem.IsWindows())
            relativePath = relativePath.Replace('\\', '/');

        return (RoflanArchiveFile)file
            .LoadFile(relativePath);
    }



    public static void CreateSignKeys(
        string keyName,
        string publicKeysDirectoryPath,
        string privateKeysDirectoryPath)
    {
        SignUtils.CreateKeysFiles(
            keyName,
            publicKeysDirectoryPath,
            privateKeysDirectoryPath);
    }


    public static void CreateSign(
        RoflanArchive archive,
        string privateKeyPath,
        string signDirectoryPath = "")
    {
        CreateSign(
            archive.Path,
            privateKeyPath,
            signDirectoryPath);
    }
    public static void CreateSign(
        string archivePath,
        string privateKeyPath,
        string signDirectoryPath = "")
    {
        SignUtils.CreateSignFile(
            privateKeyPath,
            archivePath,
            signDirectoryPath);
    }

    public static void VerifySigns(
        RoflanArchive archive,
        string publicKeysDirectoryPath,
        string signsDirectoryPath = "")
    {
        VerifySigns(
            archive.Path,
            publicKeysDirectoryPath,
            signsDirectoryPath);
    }
    public static void VerifySigns(
        string archivePath,
        string publicKeysDirectoryPath,
        string signsDirectoryPath = "")
    {
        var verifySuccess = TryVerifySigns(
            out var failedVerifySignPath,
            archivePath,
            publicKeysDirectoryPath,
            signsDirectoryPath);

        if (!verifySuccess)
            throw new Exception($"Verify file['{archivePath}'] by sign file['{failedVerifySignPath}'] was failed");
    }

    public static bool TryVerifySigns(
        out string failedVerifySignPath,
        RoflanArchive archive,
        string publicKeysDirectoryPath,
        string signsDirectoryPath = "")
    {
        return TryVerifySigns(
            out failedVerifySignPath,
            archive.Path,
            publicKeysDirectoryPath,
            signsDirectoryPath);
    }
    public static bool TryVerifySigns(
        out string failedVerifySignPath,
        string archivePath,
        string publicKeysDirectoryPath,
        string signsDirectoryPath = "")
    {
        failedVerifySignPath = string.Empty;

        var archiveFileName =
            System.IO.Path.GetFileName(
                archivePath);

        foreach (var signFilePath in Directory.EnumerateFiles(
                     signsDirectoryPath, $"{archiveFileName}.*.roflsign"))
        {
            var signFileName =
                System.IO.Path.GetFileNameWithoutExtension(
                    signFilePath);
            var publicKeyName =
                signFileName[(archiveFileName.Length + 1)..];
            var publicKeyPath = System.IO.Path.Combine(
                publicKeysDirectoryPath, $"{publicKeyName}.roflkey");

            var verifySuccess = SignUtils.VerifySignFile(
                publicKeyPath,
                archivePath,
                signFilePath);

            if (verifySuccess)
                continue;

            failedVerifySignPath = signFilePath;

            return false;
        }

        return true;
    }
}
