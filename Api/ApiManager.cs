using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RoflanArchives.Core.Api
{
    internal static class ApiManager
    {
        private static ReadOnlyDictionary<Version, IRoflanArchiveApi> Apis { get; }



        static ApiManager()
        {
            Apis = new ReadOnlyDictionary<Version, IRoflanArchiveApi>(
                GetApis());
        }



        private static IDictionary<Version, IRoflanArchiveApi> GetApis()
        {
            var types = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsClass &&
                               typeof(IRoflanArchiveApi)
                                   .IsAssignableFrom(type))
                .ToArray();
            var apis = new SortedDictionary<Version, IRoflanArchiveApi>();

            foreach (var type in types)
            {
                var api = Activator.CreateInstance(
                    type, true) as IRoflanArchiveApi;

                if (api == null)
                    continue;

                apis.Add(api.Version, api);
            }

            return apis;
        }



        private static Version ReadVersion(
            IRoflanArchive archive)
        {
            var header = (IRoflanArchiveHeader)archive;

            if (!File.Exists(archive.Path))
                return header.Version;

            using var reader = new BinaryReader(
                File.Open(
                    archive.Path,
                    FileMode.Open));

            var major = reader.ReadInt32();
            var minor = reader.ReadInt32();
            var build = reader.ReadInt32();
            var revision = reader.ReadInt32();

            header.Version = new Version(
                major, minor,
                build, revision);

            return header.Version;
        }



        public static IRoflanArchiveApi GetLastApi()
        {
            return Apis.Values.Last();
        }


        public static IRoflanArchiveApi GetApi(
            IRoflanArchive archive)
        {
            var archiveVersion = ReadVersion(
                archive);

            IRoflanArchiveApi? targetApi = null;

            foreach (var (version, api) in Apis)
            {
                if (archiveVersion >= version)
                    continue;

                targetApi = api;

                break;
            }

            targetApi ??= GetLastApi();

            if (archiveVersion < targetApi.Version)
                throw new VersionNotFoundException($"Api for the archive with version[{archiveVersion}] was not found.");

            return targetApi;
        }
    }
}
