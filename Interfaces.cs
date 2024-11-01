﻿using System;
using System.Collections.ObjectModel;
using System.IO;

namespace RoflanArchives.Core;

internal interface IRoflanArchiveHeader
{
    Version Version { get; internal set; }
    string Name { get; internal set; }
    RoflanArchiveCompressionType CompressionType { get; internal set; }
    byte CompressionLevel { get; internal set; }
    uint FilesCount { get; internal set; }
    ulong StartDefinitionsOffset { get; internal set; }
    ulong StartContentsOffset { get; internal set; }
}

internal interface IRoflanArchive : IRoflanArchiveHeader
{
    string Path { get; internal set; }

    ReadOnlyObservableCollection<IRoflanArchiveFile> Files { get; }



    IRoflanArchiveFile this[uint id] { get; }
    IRoflanArchiveFile this[string relativePath] { get; }
}

internal interface IRoflanArchiveFileDefinition
{
    uint Id { get; internal set; }
    string RelativePath { get; internal set; }
    RoflanArchiveCompressionType CompressionType { get; internal set; }
    byte CompressionLevel { get; internal set; }
    ReadOnlyMemory<byte> ContentHash { get; internal set; }
    ulong OriginalContentSize { get; internal set; }
    ulong ContentSize { get; internal set; }
    ulong ContentOffset { get; internal set; }



    string Name { get; internal set; }
    string Extension { get; internal set; }
    string DirectoryPath { get; internal set; }
    ulong EndOffset { get; internal set; }



    FileStream GetReadStream();

    FileStream GetWriteStream();
}

internal interface IRoflanArchiveFileContent
{
    [Obsolete($"For compatibility with API versions lower than 1.5.0.0. Use {nameof(DataStream)} property instead")]
    ReadOnlyMemory<byte> Data { get; internal set; }
    Stream DataStream { get; internal set; }
}

internal interface IRoflanArchiveFile : IRoflanArchiveFileDefinition, IRoflanArchiveFileContent
{

}
