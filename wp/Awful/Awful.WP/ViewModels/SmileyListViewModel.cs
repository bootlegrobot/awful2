using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Awful.ViewModels
{
    public class SmileyListViewModel : PagedListViewModel<Data.SmilieyDataModel>
    {
        private readonly List<TagMetadata> _smilies = new List<TagMetadata>();
        private List<Data.SmilieyDataModel> _allSmilies;
        private int _maxPages;

        private const int SMILIES_PER_PAGE = 40;

        public List<Data.SmilieyDataModel> Suggestions
        {
            get { return _allSmilies; }
            set { SetProperty(ref _allSmilies, value, "Suggestions"); }
        }

        public SmileyListViewModel()
            : base()
        {

        }

        protected override IEnumerable<Data.SmilieyDataModel> LoadPageInBackground(int index)
        {
            var list = new List<Data.SmilieyDataModel>();

            if (System.ComponentModel.DesignerProperties.IsInDesignTool)
                return list;

            // load smilies from the web, or from cache
            else if (this._smilies.Count == 0)
            {
                IEnumerable<TagMetadata> cache = CoreExtensions.LoadFromFile<List<TagMetadata>>("smilies.xml");

                if (cache == null)
                    cache = ForumTasks.FetchAllSmilies();   
                    
                this._smilies.AddRange(cache);

                if (this._smilies.Count != 0)
                    this._smilies.SaveToFile("smilies.xml");

                this._allSmilies = new List<Data.SmilieyDataModel>(
                    this._smilies.Select(item => new Data.SmilieyDataModel(item)));

                double maxPages = this._smilies.Count / SMILIES_PER_PAGE;

                this._maxPages = (int)Math.Ceiling(maxPages);
            }

            // create a sub set of smilies at a time
            if (index <= _maxPages)
            {
                var page = this._allSmilies.Page(index, SMILIES_PER_PAGE);
                list.AddRange(page);
            }

            return list;
        }

        protected override void OnError(Exception exception)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, exception);
            Notification.ShowError("An error occurred while loading smilies.", "");
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess()
        {
            // do nothing //
            OnPropertyChanged("Suggestions");
        }
    }
}
