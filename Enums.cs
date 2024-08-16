using System;

namespace RoflanArchives.Core;


public enum RoflanArchiveCompressionType : byte
{
    Inherited = 0,
    NoCompression = 1,
    LZ4Block = 2,
    LZ4Stream = 3,
    Default = Inherited
}
