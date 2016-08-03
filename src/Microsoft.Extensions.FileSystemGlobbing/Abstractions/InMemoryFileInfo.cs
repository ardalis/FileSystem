using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.FileSystemGlobbing.Abstractions
{
    public class InMemoryFileInfo : FileInfoBase
    {
        private InMemoryDirectoryInfo _parent;
        public InMemoryFileInfo(string file, InMemoryDirectoryInfo parent)
        {
            FullName = file;
            Name = Path.GetFileName(file);
            _parent = parent;
        }
        public override string FullName { get; }

        public override string Name { get; }

        public override DirectoryInfoBase ParentDirectory
        {
            get
            {
                return _parent;
            }
        }
    }
}
