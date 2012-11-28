using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Threading;
using Awful.ViewModels;
using Awful.Data;
using Awful.Common;
using Telerik.Windows.Controls;

namespace Awful.ViewModels
{
    public class ForumNavViewModel : Common.BindableBase
    {
        public ForumNavViewModel()
        {
            this.Items = new ObservableCollection<ForumDataModel>();
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<ForumDataModel> Items { get; private set; }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get { return this._isRunning; }
            private set
            {
                this._isRunning = value;
                this.OnPropertyChanged("IsRunning");
            }
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData()
        {
            LoadDataAsync(ProcessData);
        }

        private delegate IEnumerable<ForumMetadata> LoadDataDelegate(string username, string password);

        private IEnumerable<ForumDataModel> LoadDataWork()
        {
            // retrieve the forums from storage first if possible.
            //var forums = AppDataModel.LoadForums();
            //var items = CreateItems(forums);
            //return items;
            return null;
        }

        private IEnumerable<ForumDataModel> CreateItems(IEnumerable<ForumMetadata> forums)
        {
            if (forums == null)
                throw new ArgumentNullException("Cannot create model items without metadata.");

            List<ForumDataModel> result = new List<ForumDataModel>(50);
            var enumerator = forums.GetEnumerator();
            OrganizeItems(enumerator, result, null, null, 10);
            CollapseSubforums(result);
            return result;
        }

        private void CollapseSubforums(List<ForumDataModel> result)
        {
            foreach (var forum in result)
            {
                var children = forum.Subforums.SelectMany(f => f.Subforums)
                    .Concat(forum.Subforums);

                var subforums = children.ToList();
                forum.Subforums.Clear();
                forum.Subforums.AddRange(subforums);
            }
        }

        private ForumDataModel OrganizeItems(IEnumerator<ForumMetadata> enumerator,
            IList<ForumDataModel> list,
            ForumDataModel parent,
            int? level,
            int weightStep)
        {
            ForumDataModel ancestor = null;

            while (enumerator.MoveNext())
            {
                var currentLevel = enumerator.Current.LevelCount;
                var item = new ForumDataModel(enumerator.Current);
                level = level.GetValueOrDefault(currentLevel);
                
                if (parent == null)
                    parent = item;

                // set the weight of the item here. this will be used for sorting later.
                item.Weight = parent.Weight + weightStep;

                // if the current item is a sibling, add to the list.
                if (level.Value == currentLevel)
                {
                    list.Add(item);
                    parent = item;
                }

                // if the item is a descendant of the previous item.
                else if (currentLevel > level)
                {
                    // add the item as a child of the parent.
                    parent.Subforums.Add(item);
                    //parent.AddAsSubforum(item);

                    // set child's group to that of the parent.
                    item.Data.ForumGroup = parent.Data.ForumGroup;
                    
                    // add future children to this item until we reach a sibling.
                    // the weight of this items should fall within an order of 1.
                    var sibling = OrganizeItems(enumerator, parent.Subforums, item, currentLevel, 1);
                    list.Add(sibling);
                    parent = sibling;
                }

                // if the item is an ancestor
                else 
                { 
                    ancestor = item;
                    break;
                }
            }

            return ancestor;
        }

        private void LoadDataAsync(Action<IEnumerable<ForumDataModel>> completed)
        {
            this.IsRunning = true;
            ThreadPool.QueueUserWorkItem((state) =>
            {
                var forums = LoadDataWork();
                Deployment.Current.Dispatcher.BeginInvoke(() => { completed(forums); });

            }, null);
        }

        private void ProcessData(IEnumerable<ForumDataModel> forums)
        {
            foreach (var item in forums)
                this.Items.Add(item);

            this.IsRunning = false;
            this.IsDataLoaded = true;
        }
    }
}