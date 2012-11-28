using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Telerik.Windows.Controls;
using System.Collections.ObjectModel;
using Awful.Data;

namespace Awful.ViewModels
{
    public class ThreadPageNavViewModel : Data.SampleDataCommon
    {
        public ThreadPageNavViewModel() { }

        public ThreadPageNavViewModel(ThreadMetadata metadata)
        {
            this._metadata = metadata;
            InitializeViewmodel(metadata);
        }

        void OnPagesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged("Items");
        }

        private ThreadMetadata _metadata;
        public ThreadMetadata Metadata
        {
            get { return this._metadata; }
            set
            {
                if (this._metadata == null ||
                    !this._metadata.Equals(value))
                {
                    this._metadata = value;
                    InitializeViewmodel(value);
                }
            }
        }

        private void InitializeViewmodel(ThreadMetadata value)
        {
            this.Title = value.Title;

            var pages = new ObservableCollection<ThreadPageDataModel>();

            for (int i = 0; i < value.PageCount; i++)
            {
                var page = new ThreadPageDataModel(value, i);
                page.ThreadPageUpdated += new EventHandler(OnThreadPageUpdated);
                pages.Add(page);
            }

            this._pages = pages;
            this._pages.CollectionChanged +=
                new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnPagesCollectionChanged);

            this.OnPropertyChanged("Items");
        }

        private ThreadPageDataModel _selectedItem;
        public ThreadPageDataModel SelectedItem
        {
            get { return this._selectedItem; }
            set { this.SetProperty(ref _selectedItem, value, "SelectedItem"); }
        }

        private ObservableCollection<ThreadPageDataModel> _pages;
        public ObservableCollection<ThreadPageDataModel> Items
        {
            get { return this._pages; }
        }

        private void OnThreadPageUpdated(object sender, EventArgs e)
        {
            var page = sender as ThreadPageDataModel;
            this.Title = page.Metadata.ThreadTitle;

            if (page.Metadata.LastPage > this.Metadata.PageCount)
                this.UpdatePages(page.Metadata.LastPage, this.Metadata.PageCount);
        }

        private void UpdatePages(int start, int max)
        {
            for (int i = start; i < max; i++)
            {
                var page = new ThreadPageDataModel(this.Metadata, i);
                page.ThreadPageUpdated += new EventHandler(OnThreadPageUpdated);
                this.Items.Add(page);
            }
        }
    }
}
