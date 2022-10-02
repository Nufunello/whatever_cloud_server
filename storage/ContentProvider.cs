using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Media.Imaging;

namespace storage
{
    public class Content
    {
        public string MimeType { get; set; }
        public NonDisposableStream Stream { get; set; }
        public Size Size { get; set; }
    }
    public enum FileStatus
    {
        Added,
        Modified,
        Deleted
    };
    public class UpdatedFile
    {
        public FileStatus Status { get; set; }
        public string Path { get; set; }
    };
    public delegate void ContentUpdatedHandler(object sender, IEnumerable<UpdatedFile> files);
    public class ContentProvider
    {
        readonly string root;
        readonly Func<string, string> typeProvider;
        readonly Dictionary<string, Content> content = new Dictionary<string, Content>();
        readonly static Dictionary<string, Func<string, Size>> size = new Dictionary<string, Func<string, Size>>
        {
            {
                "video", (fullpath) =>
                {
                    using (var shellFile = ShellFile.FromFilePath(fullpath))
                    {
                        var properties = shellFile.Properties.System.Video;
                        return new Size(
                            unchecked((int)properties.FrameWidth.Value),
                            unchecked((int)properties.FrameHeight.Value));
                    }
                }
            },
            {   
                "image", (fullpath) =>
                {
                    using (var image = Image.FromFile(fullpath))
                    { 
                        return image.Size;
                    } 
                }
            }
        };
        readonly Dictionary<string, Func<Content, string, Content>> thumbnail;
        public ContentProvider(string root, Func<string, string> typeProvider)
        {
            this.root = root;
            this.typeProvider = typeProvider;

            this.thumbnail = new Dictionary<string, Func<Content, string, Content>>
            {
                {"image", (orig, path) => orig },
                {"video", (orig, path) =>
                    {
                        using(var shellFile = ShellFile.FromFilePath(path))
                        {
                            var stream = new MemoryStream();
                            shellFile.Thumbnail.Bitmap.Save(stream, ImageFormat.Png);
                            return new Content { MimeType = this.typeProvider(".png"), Size = orig.Size, Stream = new NonDisposableStream(stream) };
                        }
                    }
                }
            };
        }
        public Content GetContent(string path)
        {
            if (content.ContainsKey(path))
            {
                return content[path];
            }
            else
            {
                var fullpath = Path.Combine(root, path);

                var type = typeProvider(path);
                var shortType = type.Substring(0, type.IndexOf('/'));
                Size size;
                try
                {
                    size = ContentProvider.size[shortType](fullpath);
                }
                catch (KeyNotFoundException) 
                {
                   size = new Size();
                }
                var content = new Content
                {
                    MimeType = type,
                    Stream = new NonDisposableStream(File.OpenRead(fullpath)),
                    Size = size
                };
                this.content.Add(path, content);
                return content;
            }
        }
        public Content GetThumbnail(string path)
        {
            var original = GetContent(path);
            var type = original.MimeType;
            var shortType = type.Substring(0, type.IndexOf('/'));

            return thumbnail[shortType](original, Path.Combine(root, path));
        }
        private readonly Dictionary<string, List<ContentUpdatedHandler>> OnSearchRequest = new Dictionary<string, List<ContentUpdatedHandler>>();
        public void SubscribeTo(string path, ContentUpdatedHandler handler)
        {
            if (OnSearchRequest.ContainsKey(path))
            {
                OnSearchRequest[path].Add(handler);
            }
            else
            {
                OnSearchRequest.Add(path, new List<ContentUpdatedHandler> { handler });
            }
        }

        private void Notify(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!OnSearchRequest.ContainsKey(dir))
            {
                return;
            }

            var updatedFiles = new UpdatedFile[] { new UpdatedFile { Status = FileStatus.Added, Path = path } };
            var subscribers = OnSearchRequest[dir];
            foreach (var subscriber in subscribers)
            {
                subscriber.Invoke(this, updatedFiles);
            }
        }

        public bool DeleteContent(string path, bool notify = true)
        {
            if (this.content.ContainsKey(path))
            {
                var content = this.content[path];
                content.Stream.RealClose();
                this.content.Remove(path);
            }
            try
            {
                File.Delete(Path.Combine(root, path));
                if (notify)
                {
                    Notify(path);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SaveContent(string path, Stream content, bool notify = true)
        {
            var output = Path.Combine(root, path);
            using (var stream = File.Create(output))
            {
                content.CopyTo(stream);
            }
            if (!File.Exists(output))
            {
                return false;
            }
            if (notify)
            {
                Notify(path);
            }
            return true;
        }

        public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        {
            string pathToReplace = root + "\\";
            return Directory.EnumerateFileSystemEntries(Path.Combine(root, path), searchPattern, searchOption).Select(x => x.Replace(pathToReplace, ""));
        }
    }
}
