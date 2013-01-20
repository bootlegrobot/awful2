using System;
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

    public class CodeTagCollection : CommonDataGroup
    {
        public CodeTagCollection() : base() { }
    }
}
