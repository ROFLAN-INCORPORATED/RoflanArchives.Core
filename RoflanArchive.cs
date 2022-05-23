using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RoflanArchive.Core
{
    struct FileInfo
    {
        public int offset;
        public int size;
        public string name;
    }

    struct Header
    {
        public char[] title;
        public int fileCount;
    }

    struct FileData
    {
        public string name;
        public byte[] data;
    }

    class RoflanArchive
    {
        public List<FileData> Files
        {
            get { return _files; }
        }

        FileInfo[] _archiveInfo;
        private List<FileData> _files;
        private Header _fileHeader;

        public RoflanArchive(string path)
        {
            _files = new List<FileData>();

            BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));
            Header header;

            _fileHeader.title = reader.ReadChars(4);
            _fileHeader.fileCount = reader.ReadInt32();

            _archiveInfo = new FileInfo[_fileHeader.fileCount];

            for (int i = 0; i < _fileHeader.fileCount; i++)
            {
                _archiveInfo[i].offset = reader.ReadInt32();
                _archiveInfo[i].size = reader.ReadInt32();
            }

            foreach(FileInfo info in _archiveInfo)
            {
                FileData data;

                data.name = "Test";

                data.data = new byte[info.size];

                reader.BaseStream.Position = info.offset;
                data.data = reader.ReadBytes(info.size);

                _files.Add(data);
            }

        }

        public RoflanArchive()
        {

        }

        public void AddFile(string path)
        {
            _fileHeader.fileCount++;
            
        }

        public void Save()
        {

        }
    }
}
