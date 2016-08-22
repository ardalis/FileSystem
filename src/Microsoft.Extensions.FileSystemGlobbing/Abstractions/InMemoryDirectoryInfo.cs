// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Extensions.FileSystemGlobbing.Abstractions
{
    /// <summary>
    /// Avoids using disk for uses like Pattern Matching.
    /// </summary>
    public class InMemoryDirectoryInfo : DirectoryInfoBase
    {
        private static readonly char[] DirectorySeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private readonly IEnumerable<string> _files;

        /// <summary>
        /// Creates a new InMemoryDirectoryInfo with the root directory and files given.
        /// </summary>
        /// <param name="rootDir">The root directory that this FileSystem will use.</param>
        /// <param name="files">Collection of file names.</param>
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

        /// <summary>
        /// The full path for the root directory.
        /// </summary>
        public override string FullName { get; }

        /// <summary>
        /// The directory name for the root directory.
        /// </summary>
        public override string Name { get; }

        /// <summary>
        /// Gets an InMemoryDirectoryInfo that represents the
        /// parent directory of the current InMemoryDirectoryInfo.
        /// </summary>
        public override DirectoryInfoBase ParentDirectory
        {
            get
            {
                return new InMemoryDirectoryInfo(Path.GetDirectoryName(FullName), _files, true);
            }
        }

        /// <summary>
        /// Enumerates the files and directories in the root directory.
        /// </summary>
        /// <returns>The list of directories and files in the root directory.</returns>
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
                var endSegment = file.IndexOfAny(DirectorySeparators, beginSegment, endPath - beginSegment);

                if (endSegment == -1)
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

        private bool IsRootDirectory(string rootDir, string filePath)
        {
            if (!filePath.StartsWith(rootDir, StringComparison.Ordinal)
                || filePath.IndexOf(Path.DirectorySeparatorChar, rootDir.Length) != rootDir.Length)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a new InMemoryDirectoryInfo using the <paramref name="path"/> for the root directory.
        /// </summary>
        /// <param name="path">The root directory that the new InMemoryDirectoryInfo will use.</param>
        /// <returns>InMemoryDirectoryInfo with <paramref name="path"/> used for root directory.</returns>
        public override DirectoryInfoBase GetDirectory(string path)
        {
            if (string.Equals(path, "..", StringComparison.Ordinal))
            {
                return new InMemoryDirectoryInfo(Path.Combine(FullName, path), _files, true);
            }
            else
            {
                var normPath = Path.GetFullPath(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
                return new InMemoryDirectoryInfo(normPath, _files, true);
            }
        }

        /// <summary>
        /// Gets an InMemoryFileInfo that matches the <paramref name="path"/> given.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>InMemoryFileInfo for the <paramref name="path"/>.</returns>
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
