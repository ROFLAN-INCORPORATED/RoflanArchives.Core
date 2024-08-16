using System;
using System.Collections.Generic;

namespace RoflanArchives.Core;

public sealed class RoflanArchiveSourceDirectoryInfo
{
    public string Path { get; }
    public IEnumerable<RoflanArchiveSourceFileInfo>? Files { get; }
    public IEnumerable<string>? BlacklistPaths { get; }
    public int MaxDepth { get; }



    public RoflanArchiveSourceDirectoryInfo(
        string path,
        IEnumerable<RoflanArchiveSourceFileInfo>? files = null,
        IEnumerable<string>? blacklistPaths = null,
        int maxDepth = -1)
    {
        Path = path;
        Files = files;
        BlacklistPaths = blacklistPaths;
        MaxDepth = maxDepth;
    }
}
