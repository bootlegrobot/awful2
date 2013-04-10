using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

#if WP8

using System.Threading.Tasks;

#endif

namespace Awful
{
    public class LoginPageBackgroundPicker : Common.BindableBase
    {
        public enum Resolutions { WVGA, WXGA, HD720p };

#if WP8

        private const string WVGA_BACKGROUND = "SplashScreenImage.screen-WVGA.jpg";
        private const string WXGA_BACKGROUND = "SplashScreenImage.screen-WXGA.jpg";
        private const string HD720P_BACKGROUND = "SplashScreenImage.screen-720p.jpg";

#endif

#if WP7

        private const string WP7_BACKGROUND = "SplashScreenImage.jpg";

#endif

        public Uri Source
        {
            get
            {

#if WP7
                return new Uri(WP7_BACKGROUND, UriKind.Relative);
#endif

#if WP8
                switch (CurrentResolution)
                {
                    case Resolutions.WXGA:
                        return new Uri(WXGA_BACKGROUND, UriKind.Relative);

                    case Resolutions.WVGA:
                        return new Uri(WVGA_BACKGROUND, UriKind.Relative);

                    case Resolutions.HD720p:
                        return new Uri(HD720P_BACKGROUND, UriKind.Relative);

                    default:
                        throw new InvalidOperationException("Unknown resolution");
                }

#endif
            }
        }

#if WP8

        private bool IsWvga { get { return App.Current.Host.Content.ScaleFactor == 100; } }
        private bool IsWxga { get { return App.Current.Host.Content.ScaleFactor == 160; } }
        private bool Is720p { get { return App.Current.Host.Content.ScaleFactor == 150; } }

        private Resolutions CurrentResolution
        {
            get
            {
                if (IsWvga) return Resolutions.WVGA;
                else if (IsWxga) return Resolutions.WXGA;
                else if (Is720p) return Resolutions.HD720p;
                else throw new InvalidOperationException("Unknown resolution");
            }
        }

#endif

    }
}
