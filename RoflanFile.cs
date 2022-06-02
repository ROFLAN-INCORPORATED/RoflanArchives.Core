using System;

namespace RoflanArchive.Core;

public class RoflanFile : IRoflanFile
{
    ulong IRoflanFileDefinition.OriginalContentSize { get; set; }
    ulong IRoflanFileDefinition.ContentSize { get; set; }
    ulong IRoflanFileDefinition.ContentOffset { get; set; }

    RoflanFileType IRoflanFileContent.Type { get; set; }



    public uint Id { get; }
    public string Path { get; }
    public string RelativePath { get; }
    public string Name { get; }
    public string Extension { get; }
    public ReadOnlyMemory<byte> Data { get; internal set; }



    internal RoflanFile(
        uint id,
        string path,
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
        Path = path;
        RelativePath = relativePath;
        Name = System.IO.Path.GetFileNameWithoutExtension(Path);
        Extension = System.IO.Path.GetExtension(Path);
        Data = data;
    }
}
