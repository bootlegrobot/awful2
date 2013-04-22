using Awful.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace Awful.Data
{
    public class IconTagDataModel : CommonDataItem
    {
        public static readonly IconTagDataModel NoIcon;

        static IconTagDataModel()
        {
            NoIcon = new IconTagDataModel()
            {
                Tag = "no icon",
                Value = "0"
            };
        }

        private string _value;
        public string Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value, "Value"); }
        }

        private string _tag;
        public string Tag
        {
            get { return _tag; }
            set { SetProperty(ref _tag, value, "Tag"); }
        }

        private bool _showImage = false;
        public bool ShowImage
        {
            get { return _showImage; }
            set { SetProperty(ref _showImage, value, "ShowImage"); }
        }

        private TagMetadata _data;
        public TagMetadata Metadata
        {
            get { return _data; }
            set
            {
                if (SetProperty(ref _data, value, "Metadata"))
                    SetMetadata(value);
            }
        }

        public void SetMetadata(TagMetadata data)
        {
            this._data = data;
            this.Title = data.Title;
            FormatImage(data.TagUri);
        }

        private string GetThreadTagFilename(string iconUri)
        {
            // strip the '/'
            var tokens = iconUri.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string filename = tokens[tokens.Length - 1];
            tokens = filename.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            filename = tokens[0];
            return filename;
        }

        private void FormatImage(string iconUri)
        {
            if (iconUri != null)
            {
                string filename = GetThreadTagFilename(iconUri);
                this.Tag = filename;
                this.CheckCacheForImage(filename);
            }
        }

        private void CheckCacheForImage(string filename)
        {
            var localFilePath = string.Format("/ThreadTags/{0}.jpg", filename);
            var remoteFilePath = string.Format("{0}/{1}.png", Constants.THREAD_TAG_REMOTE_SOURCE, filename);
            var storage = IsolatedStorageFile.GetUserStoreForApplication();

            if (!storage.FileExists(localFilePath))
                SetImage(remoteFilePath);
            else
                SetImage(localFilePath);
        }

        protected override void OnImageOpened(BitmapImage bitmap)
        {
            WriteableBitmap wb = new WriteableBitmap(bitmap);
            var path = string.Format("/ThreadTags/{0}.jpg", this.Tag);
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            using (var file = storage.CreateFile(path))
                wb.SaveJpeg(file, wb.PixelWidth, wb.PixelHeight, 0, 85);

            this.ShowImage = true;
        }

        protected override void OnImageFailed(BitmapImage image)
        {
            var path = string.Format("/ThreadTags/{0}.jpg", this.Tag);
            image = new BitmapImage();

            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    using (var file = storage.OpenFile(path, FileMode.Open))
                        image.SetSource(file);

                    this.Image = image;
                    this.ShowImage = true;
                }
                catch (Exception) { storage.DeleteFile(path); }
            }

        }
    }
}
