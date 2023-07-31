using System;

namespace RoflanArchives.Core.Api;

internal interface IRoflanArchiveApi
{
    Version Version { get; }



    RoflanArchive Load(
        RoflanArchive archive);

    RoflanArchive Save(
        RoflanArchive archive);


    IRoflanArchiveFile LoadFile(
        RoflanArchive archive,
        uint targetId);
    IRoflanArchiveFile LoadFile(
        RoflanArchive archive,
        string targetRelativePath);
}
