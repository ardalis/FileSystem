using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.FileSystemGlobbing.Abstractions
{
    public class InMemoryDirectoryInfo : DirectoryInfoBase
    {
        private static readonly char[] DirectorySeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private readonly DirectoryInfo _directoryInfo;
        private readonly List<string> _files;

        public InMemoryDirectoryInfo(DirectoryInfo rootDir, List<string> files)
        {
            if (files == null)
            {
                _files = new List<string>();
            }
            else
            {
                _files = files;
                for (int i = 0; i < _files.Count; ++i)
                {
                    _files[i] = Path.GetFullPath(_files[i].Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
                }
            }

            _directoryInfo = rootDir;
        }

        public override string FullName
        {
            get
            {
                return _directoryInfo.FullName;
            }
        }

        public override string Name
        {
            get
            {
                return _directoryInfo.Name;
            }
        }

        public override DirectoryInfoBase ParentDirectory
        {
            get
            {
                return new InMemoryDirectoryInfo(_directoryInfo.Parent, _files);
            }
        }

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            var dict = new Dictionary<string, List<string>>();
            foreach (var file in _files)
            {
                var fullPath = Path.GetFullPath(file);
                if (!IsRootDirectory(FullName, fullPath))
                {
                    continue;
                }
                var endPath = fullPath.Length;

                var beginPath = FullName.Length;
                var beginSegment = beginPath + 1;
                var endSegment = NextIndex(fullPath, DirectorySeparators, beginSegment, fullPath.Length);

                if (endPath == endSegment)
                {
                    yield return new InMemoryFileInfo(new FileInfo(fullPath), this);
                }
                else
                {
                    var name = fullPath.Substring(0, endSegment);
                    List<string> list;
                    if (!dict.TryGetValue(name, out list))
                    {
                        dict[name] = new List<string>();
                    }
                    dict[name].Add(fullPath);
                }
            }

            foreach (var item in dict)
            {
                yield return new InMemoryDirectoryInfo(new DirectoryInfo(item.Key), item.Value);
            }
        }

        private int NextIndex(string pattern, char[] anyOf, int startIndex, int endIndex)
        {
            var index = pattern.IndexOfAny(anyOf, startIndex, endIndex - startIndex);
            return index == -1 ? endIndex : index;
        }

        private bool IsRootDirectory(string rootDir, string filePath)
        {
            if (!filePath.StartsWith(rootDir)
                || filePath.IndexOfAny(DirectorySeparators, rootDir.Length) != rootDir.Length)
            {
                return false;
            }

            return true;
        }

        public override DirectoryInfoBase GetDirectory(string path)
        {
            if (string.Equals(path, "..", StringComparison.Ordinal))
            {
                var parentFiles = new List<string>();
                foreach (var file in _files)
                {
                    parentFiles.Add(Path.Combine(_directoryInfo.Parent.FullName, file));
                }
                return new InMemoryDirectoryInfo(new DirectoryInfo(Path.Combine(_directoryInfo.FullName, path)), parentFiles);
            }
            else
            {
                var list = new List<string>();
                var fullDir = Path.GetFullPath(path);
                foreach (var file in _files)
                {
                    var fullPath = Path.GetFullPath(file);
                    if (!IsRootDirectory(fullDir, fullPath))
                    {
                        continue;
                    }

                    list.Add(file.Substring(fullDir.Length - 1));
                }
                return new InMemoryDirectoryInfo(new DirectoryInfo(path), list);
            }
        }

        public override FileInfoBase GetFile(string path)
        {
            throw new NotImplementedException();
        }
    }
}
