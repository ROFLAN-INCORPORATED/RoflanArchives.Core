using System;

namespace RoflanArchives.Core;

public class RoflanArchiveFile : IRoflanArchiveFile
{
    uint IRoflanArchiveFileDefinition.Id { get; set; }
    string IRoflanArchiveFileDefinition.RelativePath { get; set; }
    string IRoflanArchiveFileDefinition.Name { get; set; }
    string IRoflanArchiveFileDefinition.Extension { get; set; }
    ReadOnlyMemory<byte> IRoflanArchiveFileDefinition.ContentHash { get; set; }
    ulong IRoflanArchiveFileDefinition.OriginalContentSize { get; set; }
    ulong IRoflanArchiveFileDefinition.ContentSize { get; set; }
    ulong IRoflanArchiveFileDefinition.ContentOffset { get; set; }
    ulong IRoflanArchiveFileDefinition.EndOffset { get; set; }

    RoflanArchiveFileType IRoflanArchiveFileContent.Type { get; set; }
    ReadOnlyMemory<byte> IRoflanArchiveFileContent.Data { get; set; }



    public uint Id
    {
        get
        {
            return ((IRoflanArchiveFile)this).Id;
        }
    }
    public string RelativePath
    {
        get
        {
            return ((IRoflanArchiveFile)this).RelativePath;
        }
    }
    public string Name
    {
        get
        {
            return ((IRoflanArchiveFile)this).Name;
        }
    }
    public string Extension
    {
        get
        {
            return ((IRoflanArchiveFile)this).Extension;
        }
    }
    public ReadOnlyMemory<byte> Data
    {
        get
        {
            return ((IRoflanArchiveFile)this).Data;
        }
    }



#pragma warning disable CS8618

    internal RoflanArchiveFile(
        uint id,
        string relativePath,
        ReadOnlyMemory<byte> data = default,
        RoflanArchiveFileType type = RoflanArchiveFileType.RawBytes,
        ulong size = 0,
        ulong offset = 0)
    {
        var definition = (IRoflanArchiveFileDefinition)this;

        definition.Id = id;
        definition.RelativePath = relativePath;
        definition.Name = System.IO.Path.GetFileNameWithoutExtension(RelativePath);
        definition.Extension = System.IO.Path.GetExtension(RelativePath);
        definition.ContentHash = new byte[8];
        definition.ContentSize = size;
        definition.ContentOffset = offset;

        var content = (IRoflanArchiveFileContent)this;

        content.Type = type;
        content.Data = data;
    }

#pragma warning restore CS8618
}
