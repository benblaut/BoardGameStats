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
            URL.Text = authorizationUrl;
        }

        private void GetAccessToken(object sender, RoutedEventArgs e)
        {
            if (Key.Text.Length > 0)
            {
                parameters.AccessCode = Key.Text;
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
            else
            {
                MessageBox.Show("Please enter the url into your browser, then copy the authentication code.");
            }
        }
    }
}
