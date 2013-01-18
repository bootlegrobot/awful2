using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Awful.Controls
{
    public interface IThreadNavContext
    {
        ICommand FirstPageCommand { get; set; }
        ICommand LastPageCommand { get; set; }
        ICommand CustomPageCommand { get; set; }
    }

	public partial class ThreadNavControl : UserControl
	{
		public ThreadNavControl()
		{
			// Required to initialize variables
			InitializeComponent();
			DataContext = this;
		}
		
		public static readonly DependencyProperty FirstPageCommandProperty = DependencyProperty.Register(
			"FirstPageCommand", typeof(ICommand), typeof(ThreadNavControl), new PropertyMetadata(null));
		
		public ICommand FirstPageCommand
		{
			get { return GetValue(FirstPageCommandProperty) as ICommand; }
			set { SetValue(FirstPageCommandProperty, value); }
		}
		
			public static readonly DependencyProperty LastPageCommandProperty = DependencyProperty.Register(
			"LastPageCommand", typeof(ICommand), typeof(ThreadNavControl), new PropertyMetadata(null));
		
		public ICommand LastPageCommand
		{
			get { return GetValue(LastPageCommandProperty) as ICommand; }
			set { SetValue(LastPageCommandProperty, value); }
		}
		
		public static readonly DependencyProperty CustomPageCommandProperty = DependencyProperty.Register(
			"CustomPageCommand", typeof(ICommand), typeof(ThreadNavControl), new PropertyMetadata(null));
		
		public ICommand CustomPageCommand
		{
			get { return GetValue(CustomPageCommandProperty) as ICommand; }
			set { SetValue(CustomPageCommandProperty, value); }
		}
		
		public static readonly DependencyProperty ThreadSourceProperty = DependencyProperty.Register(
			"ThreadSource", typeof(Data.ThreadDataSource), typeof(ThreadNavControl), new PropertyMetadata(null, (o, e) =>
            {
                (o as ThreadNavControl).UpdateDataContext((Data.ThreadDataSource)e.NewValue);   
            }));

        private void UpdateDataContext(Data.ThreadDataSource threadDataSource)
        {
            this.DataContext = this;
        }
		
		public Data.ThreadDataSource ThreadSource
		{
			get { return GetValue(ThreadSourceProperty) as Data.ThreadDataSource; }
			set { SetValue(ThreadSourceProperty, value); }
		}

		private void InitializeCustomPageButtonLabel(object sender, System.Windows.RoutedEventArgs e)
		{
			// TODO: Add event handler implementation here.
			UpdateButtonLabel(this.CustomPageInput.Text);
		}
		
		private void UpdateButtonLabel(string text)
		{
            try
            {
                double value = -1;
                if (!double.TryParse(text, out value))
                    value = 1;

                value = Math.Max(value, 1);
                value = Math.Min(value, ThreadSource.PageCount);

                int rounded = Convert.ToInt32(Math.Round(value));
                if (this.CustomPageButton != null)
                    this.CustomPageButton.Content = string.Format("Goto Page {0}", rounded);
            }
            catch (Exception ex)
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
            }
		}

        private void UpdateCustomPageButtonLabel(object sender, TextChangedEventArgs e)
        {
            if (this.CustomPageInput.Text != string.Empty)
                UpdateButtonLabel(this.CustomPageInput.Text);
        }
			
	}
}