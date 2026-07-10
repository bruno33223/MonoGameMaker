using System.Collections.Generic;

namespace MonoGameMaker.IDE.Core
{
    public class FileTreeNode
    {
        public string Name { get; }
        public string FullPath { get; }
        public bool IsDirectory { get; }
        public IReadOnlyList<FileTreeNode> Children { get; }

        public FileTreeNode(string name, string fullPath, bool isDirectory, IReadOnlyList<FileTreeNode>? children)
        {
            Name = name;
            FullPath = fullPath;
            IsDirectory = isDirectory;
            Children = children ?? new List<FileTreeNode>().AsReadOnly();
        }
    }
}
