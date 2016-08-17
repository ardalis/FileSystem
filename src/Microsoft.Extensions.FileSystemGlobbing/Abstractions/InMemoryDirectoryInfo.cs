// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        private readonly IEnumerable<string> _files;

        public InMemoryDirectoryInfo(string rootDir, IEnumerable<string> files)
            : this(rootDir, files, false)
        {
        }

        private InMemoryDirectoryInfo(string rootDir, IEnumerable<string> files, bool normalized)
        {
            if (files == null)
            {
                files = new List<string>();
            }

            Name = Path.GetFileName(rootDir);
            if (normalized)
            {
                _files = files;
                FullName = rootDir;
            }
            else
            {
                var fileList = new List<string>(files.Count());

                // normalize
                foreach (var file in files)
                {
                    fileList.Add(Path.GetFullPath(file.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)));
                }

                _files = fileList;

                FullName = Path.GetFullPath(rootDir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            }
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
                        dict[name] = new List<string> { file };
                    }
                    else
                    {
                        list.Add(file);
                    }
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
