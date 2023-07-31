using System;
using System.Collections.ObjectModel;
using K4os.Compression.LZ4;

namespace RoflanArchives.Core;

internal interface IRoflanArchiveHeader
{
    Version Version { get; internal set; }
    string Name { get; internal set; }
    LZ4Level CompressionLevel { get; internal set; }
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
    string Name { get; internal set; }
    string Extension { get; internal set; }
    ulong OriginalContentSize { get; internal set; }
    ulong ContentSize { get; internal set; }
    ulong ContentOffset { get; internal set; }



    ulong EndOffset { get; internal set; }
}

internal interface IRoflanArchiveFileContent
{
    RoflanArchiveFileType Type { get; internal set; }
    ReadOnlyMemory<byte> Data { get; internal set; }
}

internal interface IRoflanArchiveFile : IRoflanArchiveFileDefinition, IRoflanArchiveFileContent
{

}
