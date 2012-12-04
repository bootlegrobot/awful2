using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Awful.Data
{
    public class ForumDataModel : CommonDataItem, ICommand
    {
        private const string DEFAULT_GROUP = "DEFAULT";

        public ForumDataModel(ForumMetadata data)
        {
            this._data = data;
            this.UniqueId = data.ForumID;
            this.Title = data.ForumName;
            this.Subtitle = data.ForumName;
        }

        private static string AbbreviateName(string forumName)
        {
            var tokens = forumName.Split(' ');
            var stringbuilder = new StringBuilder();
            foreach (var token in tokens)
                stringbuilder.Append(token.ToUpper());

            return stringbuilder.ToString();
        }

        private readonly ForumMetadata _data;
        public ForumMetadata Data
        {
            get { return this._data; }
        }

        private List<ForumDataModel> _subforums;
        public List<ForumDataModel> Subforums
        {
            get
            {
                if (this._subforums == null)
                    this._subforums = new List<ForumDataModel>();

                return this._subforums;
            }
        }

        public List<ForumDataModel> Items { get { return this.Subforums; } }

        private bool _showItems;
        public bool ShowItems
        {
            get { return _showItems; }
            set { SetProperty(ref _showItems, value, "ShowItems"); }
        }

        public ForumDataModel Parent { get; set; }
        public bool IsRoot { get { return this.Data.LevelCount < 3; } }
        public bool HasSubforums { get { return !this._subforums.IsNullOrEmpty(); } }
        public string ItemsDescription
        {
            get
            {
                return string.Format("{0} {1}",
                  Subforums.Count,
                  Subforums.Count == 1 ? "subforum" : "subforums");
            }
        }
        public bool HasNoItems { get { return !HasSubforums; } }
        public bool IsPinned { get; set; }
        public double Weight { get; set; }
        public ICommand Command { get { return this; } }
        public bool Handled { get; set; }

        /// <summary>
        /// Adds the specified forum to this instance as a subforum.
        /// </summary>
        /// <param name="model">The specified forum.</param>
        public void AddAsSubforum(ForumDataModel model)
        {
            /*
             * NOTE: This method is essentially a helper method in achieving
             * a forum list with a subforum depth no greater than 1.
             * Subforums that also have subforums will have their subforums
             * added to the root parent's subforum list.
             */

            if (!model.IsRoot)
                model.Parent = this;

            // if parent is null, then this model is a root forum
            if (this.Parent == null)
            {
                this.Subforums.Add(model);
            }
            else
                // bubble up model until we reach a root model to add to its subforum list.
                this.Parent.AddAsSubforum(model);
        }

        public void NavigateToForum(NavigationService service)
        {
            service.Navigate(new Uri("/ForumViewPage.xaml?" + ForumViewPage.FORUMID_QUERY + "=" + this.Data.ForumID, UriKind.RelativeOrAbsolute));
        }

        public bool CanExecute(object parameter)
        {
            return this.HasSubforums;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                this.ShowItems = !this.ShowItems;
                this.Handled = true;
            }
        }
    }
}