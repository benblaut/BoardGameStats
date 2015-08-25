using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Google.GData.Client;
using Google.GData.Spreadsheets;
using MahApps.Metro.Controls;

namespace BoardGameStats
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : MetroWindow
    {
        public string myKey { get; set; }

        public string CLIENT_ID = "838728461242-agec3ihahnkq5gt8qtjqjelcgocvicab.apps.googleusercontent.com";
        public string CLIENT_SECRET = "PeMyEnxsMt1myjEyOzCoA6Or";
        public string SCOPE = "https://spreadsheets.google.com/feeds";
        public string REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";

        public OAuth2Parameters parameters = new OAuth2Parameters();

        public SpreadsheetsService service = new SpreadsheetsService("BoardGameStats");

        public LoginWindow()
        {
            InitializeComponent();

            parameters.ClientId = CLIENT_ID;
            parameters.ClientSecret = CLIENT_SECRET;
            parameters.RedirectUri = REDIRECT_URI;
            parameters.Scope = SCOPE;

            string authorizationUrl = OAuthUtil.CreateOAuth2AuthorizationUrl(parameters);
            LoginBrowser.Source = new Uri(authorizationUrl, UriKind.Absolute);
        }

        private void GetAccessCode(object sender, NavigationEventArgs e)
        {
            mshtml.IHTMLDocument2 doc = LoginBrowser.Document as mshtml.IHTMLDocument2;
            string accessCode = doc.title;
            if (accessCode.Substring(0, 7) == "Success")
            {
                accessCode = accessCode.Remove(0, 13);
                parameters.AccessCode = accessCode;// Key.Text;
                OAuthUtil.GetAccessToken(parameters);
                string accessToken = parameters.AccessToken;

                GOAuth2RequestFactory requestFactory = new GOAuth2RequestFactory(null, "BoardGameStats", parameters);
                service.RequestFactory = requestFactory;

                if (service != null)
                {
                    DialogResult = true;
                }
                else
                {
                    DialogResult = false;
                }
            }
        }
    }
}
