using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.Common
{
    public delegate void RedirectEventHandler(object sender, Uri redirect);

    public class RedirectionListener
    {
        public static event RedirectEventHandler Redirect;
        private static RedirectionListener Instance { get; set; }

        static RedirectionListener() { Instance = new RedirectionListener(); }

        private RedirectionListener() { }

        public static void Notify(Uri uri)
        {
            if (Redirect != null)
                Redirect(Instance, uri);
        }
    }
}
