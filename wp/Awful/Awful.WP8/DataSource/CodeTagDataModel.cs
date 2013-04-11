﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.Data
{
    public class CodeTagDataModel : CommonDataItem
    {
        public CodeTagDataModel() : base() { }

        private string _code;
        public string Code
        {
            get { return _code; }
            set { SetProperty(ref _code, value, "Code"); }
        }
		
		private string _tag;
		public string Tag
		{
			get { return _tag; }
			set { SetProperty(ref _tag, value, "Tag"); }
		}
    }

    public class SmilieyDataModel : CodeTagDataModel, Common.IUpdateable<SmilieyDataModel>
    {
        static SmilieyDataModel() { ImageTools.IO.Decoders.AddDecoder<ImageTools.IO.Gif.GifDecoder>(); }
        public SmilieyDataModel() : base() { }

        private TagMetadata _data;

        public SmilieyDataModel(TagMetadata data)
            : this()
        {
            SetMetadata(data);
        }

        private void SetMetadata(TagMetadata data)
        {
            this._data = data;
            this.Subtitle = data.Title;
            this.ImagePath = data.TagUri;
            this.Title = data.Value;
            this.Code = data.Value;
        }

        public Uri ImageUri
        {
            get { return new Uri(ImagePath, UriKind.Absolute); }
        }

        public void Update(SmilieyDataModel updated)
        {
            SetMetadata(updated._data);
        }
    }

    public class CodeTagCollection : CommonDataGroup
    {
        public CodeTagCollection() : base() { }
    }
}