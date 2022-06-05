using System;
using K4os.Compression.LZ4;

namespace RoflanArchive.Core;

internal interface IRoflanHeader
{
    Version Version { get; }
    string Name { get; }
    LZ4Level CompressionLevel { get; internal set; }
    uint FilesCount { get; internal set; }
    ulong StartDefinitionsOffset { get; internal set; }
    ulong StartContentsOffset { get; internal set; }
}

internal interface IRoflanFileDefinition
{
    uint Id { get; }
    string Path { get; }
    string RelativePath { get; }
    string Name { get; }
    string Extension { get; }
    ulong OriginalContentSize { get; internal set; }
    ulong ContentSize { get; internal set; }
    ulong ContentOffset { get; internal set; }



    ulong EndOffset { get; internal set; }
}

internal interface IRoflanFileContent
{
    RoflanFileType Type { get; internal set; }
    ReadOnlyMemory<byte> Data { get; }
}

internal interface IRoflanFile : IRoflanFileDefinition, IRoflanFileContent
{

}
