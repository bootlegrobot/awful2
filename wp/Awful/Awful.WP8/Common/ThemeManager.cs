using System;
using System.Net;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using Awful;

#if WINDOWS_STORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;
#endif

#if WINDOWS_PHONE
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Controls;
#endif

namespace Awful.Common
{
    public class ThemeManager : Common.BindableBase
    {
        private readonly static DispatcherTimer _ThemeUpdateTimer;

        static ThemeManager()
        {
            _ThemeUpdateTimer = new DispatcherTimer();
            _ThemeUpdateTimer.Interval = TimeSpan.FromSeconds(0.25);
            _ThemeUpdateTimer.Tick += UpdateTheme;
            _ThemeUpdateTimer.Start();
        }

        private static void UpdateTheme(object sender, EventArgs args)
        {
            _ThemeUpdateTimer.Tick -= UpdateTheme;
            _ThemeUpdateTimer.Stop();
            Instance.CurrentTheme.Initialize();
            Instance.OnPropertyChanged("CurrentTheme");
        }

        public static ThemeManager Instance
        {
            get { return App.Current.Resources["AwfulThemeManager"] as ThemeManager; }
        }
		
		private List<ForumLayout> _forumLayouts = new List<ForumLayout>();
        public List<ForumLayout> ForumLayouts
		{
			get { return _forumLayouts; }
			set { _forumLayouts = value; }
		}
		
		private List<ApplicationTheme> _themes = new  List<ApplicationTheme>();
        public List<ApplicationTheme> Themes 
		{
			get { return _themes; }
			set { _themes = value; }
		}

        private ApplicationTheme _currentTheme;
        public ApplicationTheme CurrentTheme
        {
            get 
            {
                if (_currentTheme == null && !Themes.IsNullOrEmpty())
                    _currentTheme = Themes[0];

                return _currentTheme;
            }

            private set
            {
                this.SetProperty(ref _currentTheme, value, "CurrentTheme");
            }
        }

        public void SetCurrentTheme(string title)
        {
            if (!Themes.IsNullOrEmpty())
            {
                var selected = Themes.Where(theme => theme.Title.Equals(title)).SingleOrDefault();
                this.CurrentTheme = selected;
            }
        }

        public ForumLayout GetForumLayoutById(string forumId)
        {
            ForumLayout layout = null;
            if (!ForumLayouts.IsNullOrEmpty())
                layout = ForumLayouts.Where(forum => forum.ForumId.Equals(forumId)).SingleOrDefault();

            return layout;
        }
    }

    public class ForumLayout
    {
        private string _forumId;
        public string ForumId 
        {
            get { return _forumId; }
            set { _forumId = value; }
        }

        private string _nickname;
        public string Nickname 
        {
            get { return _nickname; }
            set { _nickname = value; }
        }

        private Brush _primary;
        public Brush PrimaryColor {
            get { return _primary; }
            set { _primary = value; }
        }

        private string _imageUri;
        public string ImageUri
        {
            get { return _imageUri; }
            set { _imageUri = value; }
        }

        private BitmapImage _image;
        public ImageSource Image
        {
            get
            {
                if (this._image == null && this._imageUri != null)
                {
                    this._image = new BitmapImage(new Uri(this._imageUri, UriKind.RelativeOrAbsolute));
                }
                return this._image;
            }
        }

    }

    public class BookmarkColorMapping
    {
        public BookmarkColorCategory Category { get; set; }
        public Brush CategoryBrush { get; set; }
    }

    public class ApplicationTheme
    {
        public string Title { get; set; }
      
        public virtual Brush HomePageBackgroundBrush { get; set; }
        public virtual Brush ForegroundBrush { get; set; }
        public virtual Brush HeaderForegroundBrush { get; set; }
        public virtual Brush BackgroundBrush { get; set; }
        public virtual Brush AccentBrush { get; set; }

        public virtual Color AccentColor { get; set; }
        public virtual Color ThreadPageForegroundColor { get; set; }
        public virtual Color ThreadPageBackgroundColor { get; set; }
        public virtual Color ThreadPageOldPostColor { get; set; }
        public virtual Color ModeratorColor { get; set; }
        public virtual Color AdministratorColor { get; set; }

        public virtual List<BookmarkColorMapping> BookmarkColors { get; set; }

        public ApplicationTheme()
        {
            Initialize();
        }

        public virtual void Initialize() 
        {
            if (BookmarkColors == null)
                BookmarkColors = new List<BookmarkColorMapping>();
        }

        public override string ToString()
        {
            return Title;
        }

      
    }
}
