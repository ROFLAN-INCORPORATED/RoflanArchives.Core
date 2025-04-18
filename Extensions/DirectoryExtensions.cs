﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RoflanArchives.Core.Extensions;

internal static class DirectoryExtensions
{
    public static List<string> GetAllFiles(
        string directoryPath,
        string[]? blacklistPaths = null,
        int? maxDepth = -1)
    {
        return EnumerateAllFiles(
                directoryPath,
                blacklistPaths,
                maxDepth)
            .ToList();
    }


    public static IEnumerable<string> EnumerateAllFiles(
        string directoryPath,
        string[]? blacklistPaths = null,
        int? maxDepth = -1)
    {
        directoryPath = directoryPath
            .TrimEnd(Path.DirectorySeparatorChar)
            .TrimEnd(Path.AltDirectorySeparatorChar);

        directoryPath = !Path.IsPathRooted(directoryPath)
            ? Path.GetFullPath(directoryPath)
            : directoryPath;

        if (!Directory.Exists(directoryPath))
            throw new FileNotFoundException($"Directory at path '{directoryPath}' not found");

        blacklistPaths ??= Array.Empty<string>();

        for (var i = 0; i < blacklistPaths.Length; ++i)
        {
            ref var blacklistPath = ref blacklistPaths[i];

            if (string.IsNullOrWhiteSpace(blacklistPath))
                continue;

            blacklistPath = Path.Combine(
                directoryPath, blacklistPath);
        }

        return EnumerateAllFilesInternal(
            directoryPath,
            blacklistPaths,
            maxDepth);
    }
    private static IEnumerable<string> EnumerateAllFilesInternal(
        string directoryPath,
        string[] blacklistPaths,
        int? maxDepth = -1)
    {
        maxDepth ??= -1;

        if (!Directory.Exists(directoryPath))
            return Array.Empty<string>();

        foreach (var blacklistPath in blacklistPaths)
        {
            if (directoryPath == blacklistPath)
                return Array.Empty<string>();
        }

        var list = new List<string>(50);

        if (maxDepth is < 0 or not 0)
        {
            foreach (var directory in Directory.EnumerateDirectories(directoryPath))
            {
                list.AddRange(
                    EnumerateAllFilesInternal(
                        directory,
                        blacklistPaths,
                        maxDepth is < 0
                            ? maxDepth
                            : maxDepth - 1));
            }
        }

        foreach (var file in Directory.EnumerateFiles(directoryPath))
        {
            var isBlacklistPath = false;

            foreach (var blacklistPath in blacklistPaths)
            {
                if (file != blacklistPath)
                    continue;

                isBlacklistPath = true;

                break;
            }

            if (isBlacklistPath)
                continue;

            list.Add(file);
        }

        return list;
    }
}
