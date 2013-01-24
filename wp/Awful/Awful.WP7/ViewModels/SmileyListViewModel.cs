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
        private int _maxPages;

        private const int SMILIES_PER_PAGE = 40;

        public SmileyListViewModel()
            : base()
        {

        }

        protected override IEnumerable<Data.SmilieyDataModel> LoadPageInBackground(int index)
        {
            var list = new List<Data.SmilieyDataModel>();

            if (System.ComponentModel.DesignerProperties.IsInDesignTool)
                return list;

            // load smilies from the web
            else if (this._smilies.Count == 0)
            {
                this._smilies.AddRange(ForumTasks.FetchAllSmilies());
                this._maxPages = (int)Math.Ceiling(this._smilies.Count / SMILIES_PER_PAGE);
            }

            // create a sub set of smilies at a time
            if (index <= _maxPages)
            {
                var page = this._smilies.Page(index, SMILIES_PER_PAGE);
                list.AddRange(page.Select(item => new Data.SmilieyDataModel(item)));
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
        }
    }
}
