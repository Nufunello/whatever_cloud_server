using storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace search
{
    public delegate void FilesUpdated();
    public delegate void FileRemoved(object sender, IEnumerable<string> path);
    public interface IContext
    {
        string GetFilePath(int index);
        int GetSize();
        event FilesUpdated FilesUpdated;
        event FileRemoved FileRemoved;
        void UpdateFiles(IEnumerable<string> files);
        void RemoveFile(IEnumerable<int> index);
    }
    public class Context
        : IContext
    {
        IEnumerable<string> files;
        public Context(IEnumerable<string> files)
        {
            this.files = files;
        }

        public event FilesUpdated FilesUpdated;
        public event FileRemoved FileRemoved;

        public string GetFilePath(int index)
        {
            return files.ElementAt(index);
        }

        public void UpdateFiles(IEnumerable<string> files)
        {
            this.files = files;
            FilesUpdated?.Invoke();
        }

        public int GetSize()
        {
            return files.Count();
        }

        public void RemoveFile(IEnumerable<int> indexes)
        {
            FileRemoved?.Invoke(this, indexes
                .Select(index => files.ElementAt(index))
                .ToArray()
            );
        }
    }
    public class Pattern
    {
        public string Path { get; set; }
        public string Name { get; set; } = "*";
        public bool LookInsideDirectories { get; set; } = false;
    }
    public class ContextFactory
    {
        private readonly ContentProvider contentProvider;
        public ContextFactory(ContentProvider contentProvider)
        {
            this.contentProvider = contentProvider;
        }
        private readonly Dictionary<string, IContext> contexts = new Dictionary<string, IContext>();
        private IContext CreateContext(Pattern pattern)
        {
            var searchOption = pattern.LookInsideDirectories ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;
            var context = new Context(contentProvider.EnumerateFileSystemEntries(pattern.Path, pattern.Name, searchOption));
            contentProvider.SubscribeTo(path: pattern.Path, (sender, updatedFiles) =>
            {
                context.UpdateFiles(contentProvider.EnumerateFileSystemEntries(pattern.Path, pattern.Name, searchOption));
            });
            context.FileRemoved += (sender, pathes) =>
            {
                foreach (var path in pathes.Skip(1))
                {
                    contentProvider.DeleteContent(path, false);
                }
                if (pathes.Count() > 0)
                {
                    contentProvider.DeleteContent(pathes.First(), true);
                }
            };
            return context;
        }
        public void AddContext(string name, Pattern pattern)
        {
            contexts.Add(name, CreateContext(pattern));
        }
        public IContext GetContext(string name)
        {
            return contexts[name];
        }
        public IContext this[string name] => contexts[name];
    }
}
