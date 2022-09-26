using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace storage
{
    public class IconProvider
    {
        readonly ContentProvider content;
        readonly Dictionary<string, List<Content>> icons = new Dictionary<string, List<Content>>();
        public IconProvider(ContentProvider content)
        {
            this.content = content;
        }
        private Content CreateContent(string path)
        {
            var content = this.content.GetThumbnail(path);
            icons.Add(path, new List<Content> { content });
            return content;
        }
        private Content Resize(Content content, System.Drawing.Size size)
        {
            var image = System.Drawing.Image.FromStream(content.Stream, true, true);
            var resizedImage = image.GetThumbnailImage(size.Width, size.Height, null, IntPtr.Zero);
            var stream = new MemoryStream();
            resizedImage.Save(stream, image.RawFormat); 
            stream.Position = 0;
            return new Content { Stream = new NonDisposableStream(stream), MimeType = content.MimeType, Size = size };
        }
        public Content GetIcon(string path, int width, int height)
        {
            var size = new System.Drawing.Size(width: width, height: height);
            if (icons.ContainsKey(path))
            {
                try
                {
                    return icons[path].First(x => x.Size == size);
                }
                catch { }
            }
            else
            {
                var content = CreateContent(path);
                if (content.Size == size)
                {
                    return content;
                }
            }
            {
                var list = icons[path];
                var content = list
                    .OrderBy(x => x.Size.Width).OrderBy(x => x.Size.Height)
                    .First((x) => x.Size.Width >= size.Width && x.Size.Height >= size.Height);
                var resized = Resize(content, size);
                list.Add(resized);
                return resized;
            }

        }
    }
}
