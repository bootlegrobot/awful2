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

namespace Awful.Helpers
{
    public class ThreadIconViewer : Common.BindableBase
    {
        private AppDataModel _model;

        public ThreadIconViewer()
        {
            _model = App.Model;

            if (_model != null)
            {
                _model.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnModelPropertyChanged);
            }
        }

        private void OnModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("HideThreadIcons"))
                OnPropertyChanged("IconView");
        }

        public bool ShowIcons
        {
            get
            {
                if (this._model == null)
                    return true;

                return this._model.HideThreadIcons ? false : true;
            }
        }

        public Visibility IconView
        {
            get { return ShowIcons ? Visibility.Visible : Visibility.Collapsed; }
        }
    }
}
