using System;

namespace RoflanArchive.Core;

internal interface IRoflanHeader
{
    string Name { get; }
    uint FilesCount { get; }
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
    ulong ContentSize { get; internal set; }
    ulong ContentOffset { get; internal set; }
}

internal interface IRoflanFileContent
{
    RoflanFileType Type { get; internal set; }
    ReadOnlyMemory<byte> Data { get; }
}

internal interface IRoflanFile : IRoflanFileDefinition, IRoflanFileContent
{

}
