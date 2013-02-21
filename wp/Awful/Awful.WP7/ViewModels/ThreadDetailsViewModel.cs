using Awful.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Awful.ViewModels
{
    public class ThreadDetailsViewModel : Common.BindableBase, IThreadNavContext
    {
        private IApplicationBar _threadViewBar;
        public IApplicationBar ThreadViewBar
        {
            get { return _threadViewBar; }
            set { SetProperty(ref _threadViewBar, value, "ThreadViewBar"); }
        }

        private IApplicationBar _replyViewBar;
        public IApplicationBar ReplyViewBar
        {
            get { return _replyViewBar; }
            set { SetProperty(ref _replyViewBar, value, "ReplyViewBar"); }
        }

        private ICommand _ratingCommand;
        public ICommand RatingCommand
        {
            get { return _ratingCommand; }
            set { SetProperty(ref _ratingCommand, value, "RatingCommand"); }
        }

        private ICommand _firstPage;
        public ICommand FirstPageCommand
        {
            get
            {
                return _firstPage;
            }
            set
            {
                SetProperty(ref _firstPage, value, "FirstPageCommand"); 
            }
        }

        private ICommand _lastPage;
        public ICommand LastPageCommand
        {
            get
            {
                return _lastPage;
            }
            set
            {
                SetProperty(ref _lastPage, value, "LastPageCommand");
            }
        }

        private ICommand _customPage;
        public ICommand CustomPageCommand
        {
            get
            {
                return _customPage;
            }
            set
            {
                SetProperty(ref _customPage, value, "CustomPageCommand");
            }
        }
    }
}
