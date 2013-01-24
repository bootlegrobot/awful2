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
using Microsoft.Phone.Tasks;
using System.Text.RegularExpressions;

namespace Awful.Commands
{
    public class SendAnEmailCommand : ICommand
    {
        private static readonly string EmailPattern = string.Empty;

        private string subject = string.Empty;
        private string body = string.Empty;

        public string Subject
        {
            get { return subject; }
            set { subject = value; }
        }

        public string Body
        {
            get { return body; }
            set { body = value; }
        }

        static SendAnEmailCommand()
        {
            EmailPattern = GenerateEmailPattern();
        }

        static string GenerateEmailPattern()
        {
            // Example 2 - Email address formats

            string theEmailPattern = @"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*"
                                   + "@"
                                   + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$";

            // The string pattern from here doesn't not work in all instances.
            // http://www.cambiaresearch.com/c4/bf974b23-484b-41c3-b331-0bd8121d5177/Parsing-Email-Addresses-with-Regular-Expressions.aspx
            //String theEmailPattern = @"^(([^<>()[\]\\.,;:\s@\""]+(\.[^<>()[\]\\.,;:\s@\""]+)*)|(\"".+\""))"
            //                       + "@"
            //                       + @"((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])"
            //                       + "|"
            //                       + @"(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$";

            return theEmailPattern;
        }

        private bool Log(string email, string pattern)
        {
            bool inValue = !string.IsNullOrEmpty(email) && Regex.Match(email, pattern).Success;

            if (inValue)
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Valid email.");
            }
            else
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Invalid email.");
            }

            return inValue;
        }

        public bool CanExecute(object parameter)
        {
            string email = parameter as string;
            return Log(email, EmailPattern);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            EmailComposeTask emailTask = new EmailComposeTask();
            emailTask.To = parameter as string;
            emailTask.Body = this.Body;
            emailTask.Subject = this.Subject;

            try { emailTask.Show(); }
            catch (Exception) { }
        }
    }
}
