using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.FileSystemGlobbing.Abstractions
{
    public class InMemoryFileInfo : FileInfoBase
    {
        private FileInfo _fileInfo;
        private InMemoryDirectoryInfo _parent;
        public InMemoryFileInfo(FileInfo file, InMemoryDirectoryInfo parent)
        {
            _fileInfo = file;
            _parent = parent;
        }
        public override string FullName
        {
            get
            {
                return _fileInfo.FullName;
            }
        }

        public override string Name
        {
            get
            {
                return _fileInfo.Name;
            }
        }

        public override DirectoryInfoBase ParentDirectory
        {
            get
            {
                return _parent;
            }
        }
    }
}
