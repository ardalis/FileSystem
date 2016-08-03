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
        private readonly List<string> _files;

        public InMemoryDirectoryInfo(string rootDir, List<string> files)
        {
            if (files == null)
            {
                _files = new List<string>();
            }
            else
            {
                _files = files;

                // normalize
                for (int i = 0; i < _files.Count; ++i)
                {
                    _files[i] = Path.GetFullPath(_files[i].Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
                }
            }

            FullName = Path.GetFullPath(rootDir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            Name = Path.GetFileName(rootDir);
        }

        // This is to avoid the overhead of Path.GetFullPath from the public constructor
        private InMemoryDirectoryInfo(string rootDir, List<string> files, bool normalized)
        {
            _files = files;
            FullName = rootDir;
            Name = Path.GetFileName(rootDir);
        }

        public override string FullName { get; }

        public override string Name { get; }

        public override DirectoryInfoBase ParentDirectory
        {
            get
            {
                return new InMemoryDirectoryInfo(Path.GetDirectoryName(FullName), _files, true);
            }
        }

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            var dict = new Dictionary<string, List<string>>();
            foreach (var file in _files)
            {
                if (!IsRootDirectory(FullName, file))
                {
                    continue;
                }

                var endPath = file.Length;
                var beginSegment = FullName.Length + 1;
                var endSegment = NextIndex(file, DirectorySeparators, beginSegment, file.Length);

                if (endPath == endSegment)
                {
                    yield return new InMemoryFileInfo(file, this);
                }
                else
                {
                    var name = file.Substring(0, endSegment);
                    List<string> list;
                    if (!dict.TryGetValue(name, out list))
                    {
                        dict[name] = new List<string>();
                    }
                    dict[name].Add(file);
                }
            }

            foreach (var item in dict)
            {
                yield return new InMemoryDirectoryInfo(item.Key, item.Value, true);
            }
        }

        private int NextIndex(string pattern, char[] anyOf, int startIndex, int endIndex)
        {
            var index = pattern.IndexOfAny(anyOf, startIndex, endIndex - startIndex);
            return index == -1 ? endIndex : index;
        }

        private bool IsRootDirectory(string rootDir, string filePath)
        {
            if (!filePath.StartsWith(rootDir, StringComparison.Ordinal)
                || filePath.IndexOf(Path.DirectorySeparatorChar, rootDir.Length) != rootDir.Length)
            {
                return false;
            }

            return true;
        }

        public override DirectoryInfoBase GetDirectory(string path)
        {
            if (string.Equals(path, "..", StringComparison.Ordinal))
            {
                return new InMemoryDirectoryInfo(Path.Combine(FullName, path), _files, true);
            }
            else
            {
                return new InMemoryDirectoryInfo(path, _files, true);
            }
        }

        public override FileInfoBase GetFile(string path)
        {
            var normPath = Path.GetFullPath(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            foreach (var file in _files)
            {
                if (string.Equals(file, normPath))
                {
                    return new InMemoryFileInfo(file, this);
                }
            }

            return null;
        }
    }
}
