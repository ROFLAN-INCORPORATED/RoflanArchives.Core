using System;

namespace RoflanArchives.Core;

public class RoflanArchiveSourceFileInfo
{
    public string Path { get; }
    public uint? Id { get; }
    public RoflanArchiveCompressionType CompressionType { get; }
    public byte? CompressionLevel { get; }



    public RoflanArchiveSourceFileInfo(
        string path,
        uint? id = null,
        RoflanArchiveCompressionType compressionType = RoflanArchiveCompressionType.Inherited,
        byte? compressionLevel = null)
    {
        Path = path;
        Id = id;
        CompressionType = compressionType;
        CompressionLevel = compressionLevel;
    }
}
