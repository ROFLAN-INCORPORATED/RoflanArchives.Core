using System;

namespace RoflanArchive.Core;

public class RoflanFile : IRoflanFile
{
    ulong IRoflanFileDefinition.OriginalContentSize { get; set; }
    ulong IRoflanFileDefinition.ContentSize { get; set; }
    ulong IRoflanFileDefinition.ContentOffset { get; set; }
    ulong IRoflanFileDefinition.EndOffset { get; set; }

    RoflanFileType IRoflanFileContent.Type { get; set; }



    public uint Id { get; }
    public string RelativePath { get; }
    public string Name { get; }
    public string Extension { get; }
    public ReadOnlyMemory<byte> Data { get; internal set; }



    internal RoflanFile(
        uint id,
        string relativePath,
        ReadOnlyMemory<byte> data = default,
        RoflanFileType type = RoflanFileType.RawBytes,
        ulong size = 0,
        ulong offset = 0)
    {
        var definition = (IRoflanFileDefinition)this;
        definition.ContentSize = size;
        definition.ContentOffset = offset;

        var content = (IRoflanFileContent)this;
        content.Type = type;

        Id = id;
        RelativePath = relativePath;
        Name = System.IO.Path.GetFileNameWithoutExtension(RelativePath);
        Extension = System.IO.Path.GetExtension(RelativePath);
        Data = data;
    }
}
