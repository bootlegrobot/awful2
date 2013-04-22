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

namespace Awful.ViewModels
{
    public class AboutViewModel
    {
        public string Quote
        {
            get;
            set;
        }

        public ICommand RateThisAppCommand
        {
            get;
            private set;
        }

        public ICommand SendAnEmailCommand
        {
            get;
            private set;
        }

        public string AppName
        {
            get;
            set; 
        }

        public string AppDescription
        {
            get;
            set;
        }

        public string AppEmail
        {
            get;
            set;
        }

        public string Copyright
        {
            get;
            set;
        }

        public string MiscText1
        {
            get;
            set;
        }

        public string MiscText2
        {
            get;
            set;
        }

        public ImageSource AppLogoUri
        {
            get;
            set;
        }

        public string AppVersion
        {
            get;
            set;
        }

        public AboutViewModel()
        {
            AppName = string.Empty;
            AppVersion = string.Empty;
            AppDescription = string.Empty;
            MiscText1 = string.Empty;
            MiscText2 = string.Empty;
            Copyright = string.Empty;

            RateThisAppCommand = new RateThisAppCommand();
            SendAnEmailCommand = new Commands.SendAnEmailCommand(){ Subject = "Awful QCS" };
        }
    }
}
