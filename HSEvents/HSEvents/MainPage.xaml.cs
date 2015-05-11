using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Info;
using Microsoft.Phone.Notification;
using Microsoft.Phone.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using HSEvents.Resources;
using HSEvents.ServiceReference1;
using Newtonsoft.Json.Linq;

namespace HSEvents
{
    public partial class MainPage : PhoneApplicationPage
    {
        public class EventItem
        {
            public string eventID { get; set; }
            public string userID { get; set; }
            public string day { get; set; }
            public string month { get; set; }
            public string year { get; set; }
            public string eventName { get; set; }
        }
        
        
        private string filePath = "userID";

        private MobileServiceUser user;

        string userID;

        // string filter;

        //Create Push Notification Channel - MPNS - **DOES NOT WORK**
        // private HttpNotificationChannel channel;

        //private const string ChannelName = "EventsChannel";

        public MainPage()
        {
            InitializeComponent();
        }

        //--------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------
        //Above is the functions to check authorisation token, and if not available choose which SSO to use


        //Login to system
        private void login_Click(object sender, RoutedEventArgs e)
        {
            while (user == null)
            {
                string message;

                try
                {
                    //Retrieve userID from isolated storage
                    byte[] ProtectedIDByte = this.ReadIDFromFile();
                    byte[] IDByte;


                    if (ProtectedIDByte != null) //Error handling
                    {
                        //Decrypt ID using unprotect method
                        IDByte = ProtectedData.Unprotect(ProtectedIDByte, null);
                        //Display ID
                        userID = Encoding.UTF8.GetString(IDByte, 0, IDByte.Length);
                        message = ("You are now logged in - {0}" + userID);
                        MessageBox.Show(message);
                        visibilities();
                        break;
                    }
                    else
                    {
                        loginSelect.IsOpen = true;
                        visibilities();
                        break;
                    }
                }

                catch (InvalidOperationException ex)
                {
                    MessageBox.Show("Problem Logging in!\n This may be somewhat useful" + ex);
                }
            }

        }
        
        //Choose to login with Twitter
        private void twitterLogin_Click(object sender, RoutedEventArgs e)
        {
            int twitterLogin = 0;

            AsyncLogin(twitterLogin);
        }

        //Choose to login with Facebook
        private void facebookLogin_Click(object sender, RoutedEventArgs e)
        {
            int facebookLogin = 1;

            AsyncLogin(facebookLogin);

        }

        //Choose to login with Microsoft **DOES NOT WORK**
      /*  private void microsoftLogin_Click(object sender, RoutedEventArgs e)
       * {
       *     int microsoftLogin = 2;
       *
       *     AsyncLogin(microsoftLogin);
       * }
      */
  
        //Choose to login with Google
        private void googleLogin_Click(object sender, RoutedEventArgs e)
        {
            int googleLogin = 3;

            AsyncLogin(googleLogin);
        }

        //Visbilities once signed in
        private void visibilities()
        {
            login.Visibility = Visibility.Collapsed;
            findData.Visibility = Visibility.Visible;
            addData.Visibility = Visibility.Visible;
         //   pushNotifications.Visibility = Visibility.Visible;
        }


        //--------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------
        //Below is Functions for logining in, and storing authorisation token


        //Single Sign On function
        async void AsyncLogin(int type)
        {
            if (type == 0)
            {
                user = await App.MobileService.LoginAsync(MobileServiceAuthenticationProvider.Twitter);
            }
            else if (type == 1)
            {
                user = await App.MobileService.LoginAsync(MobileServiceAuthenticationProvider.Facebook);
            }
            else if (type == 2)
            {
                user = await App.MobileService.LoginAsync(MobileServiceAuthenticationProvider.MicrosoftAccount);
            }
            else if (type == 3)
            {
                user = await App.MobileService.LoginAsync(MobileServiceAuthenticationProvider.Google);
            }

            userID = user.UserId;
            string message = string.Format("You are now logged in - {0}", user.UserId);

            // isolated storage/protectedclass to save userid/token


            //convert to byte
            byte[] UserIDByte = Encoding.UTF8.GetBytes(userID);

            //Encrypt the ID using Protect method
            byte[] ProtectedUserIDByte = ProtectedData.Protect(UserIDByte, null);

            //Store Encrypted ID in storage
            this.WriteIDToFile(ProtectedUserIDByte);
            //NavigationService.Navigate(new Uri("/facebookLogin.xaml", UriKind.Relative));



            MessageBox.Show(message);
        }

        //Write userID to file
        private void WriteIDToFile(byte[] userIDData)
        {
            //Create file in apps isolated storage
            IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication();
            IsolatedStorageFileStream writestream = new IsolatedStorageFileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, file);

            Stream write = new StreamWriter(writestream).BaseStream;
            write.Write(userIDData, 0, userIDData.Length);
            write.Close();
            writestream.Close();
        }

        //Read userID to file
        private byte[] ReadIDFromFile()
        {

            try
            {
                //Access fill in apps isolated storage.
                IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication();
                IsolatedStorageFileStream readstream = new IsolatedStorageFileStream(filePath, System.IO.FileMode.Open, FileAccess.Read, file);


                //Read ID from file.
                Stream reader = new StreamReader(readstream).BaseStream;
                byte[] IDArray = new byte[reader.Length];

                reader.Read(IDArray, 0, IDArray.Length);
                reader.Close();
                readstream.Close();

                return IDArray;
            }

            catch (IsolatedStorageException)
            {
                MessageBox.Show("No Login Credentials Found!\n Please Use Single Sign-On to Login");
                return null;
            }
        }

        //--------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------
        //Below is Functions for getting data


        private void findData_Click(object sender, RoutedEventArgs e)
        {
            WebClient events = new WebClient();
            events.DownloadStringCompleted += events_DownloadStringCompleted;
            // **** ENSURE THE BELOW URL POINTS TO YOUR LOCALLY RUNNING SERVICE OR SERVICE IN THE CLOUD! *****
            events.DownloadStringAsync(new Uri("http://hsevent.cloudapp.net/Service1.svc/viewusers?format=json&userID=" + userID));
            icon.Visibility = Visibility.Collapsed;
            data.Visibility = Visibility.Visible;
        }

        void events_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            // use try/catch to handle potential network errors
            try
            {
                List<EventItem> contentList = new List<EventItem>();
                JArray Even = JArray.Parse(e.Result);
                int count = 0;

                while (count < Even.Count)
                {
                    EventItem events = new EventItem();
                    events.day = Even[count]["day"].ToString();
                    events.eventID = Even[count]["eventID"].ToString();
                    events.eventName = Even[count]["eventName"].ToString();
                    events.month = Even[count]["month"].ToString();
                    events.userID = Even[count]["userID"].ToString();
                    events.year = Even[count]["year"].ToString();
                    contentList.Add(events);
                    count++;
                }

                data.ItemsSource = contentList.ToList();
                
            }
            // display message in case of network error
            catch (Exception error)
            {
                MessageBox.Show("A network error has occured, please try again!");
                Console.WriteLine("An error occured:" + error);
            }
        }

        //--------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------
        //Below is Functions for push notifications      
/*      **PUSH NOTIFICATION FUNCTIONS. DID NOT WORK**
 * 
 *       private void pushNotifications_Click(object sender, RoutedEventArgs e)
 *       {
 *           pushSelect.IsOpen = true;
 *       }
 *       
 *       private void yesPush_Click(object sender, RoutedEventArgs e)
 *       {
 *           yesPush.IsEnabled = false;
 *           noPush.IsEnabled = true;
 *           pushSelect.IsOpen = false;
 *
 *           SetupNotificationsChannel();
 *
 *           MessageBox.Show("Push Notifications are ON!");
 *       }
 *
 *       //setup push notifications
 *       private void SetupNotificationsChannel()
 *       {
 *           //Check for channel
 *           channel = HttpNotificationChannel.Find(ChannelName);
 *
 *           //Setup Channel
 *           if (channel == null)
 *           {
 *
 *               //create channel
 *               channel = new HttpNotificationChannel(ChannelName);
 *
 *               //get channel uri, and call updated method
 *               channel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(channel_ChannelUriUpdated);
 *
 *               //open channel
 *               channel.Open();
 *           }
 *
 *           else
 *           {
 *               // Register for notifications
 *               RegisterForNotifications();
 *
 *          }
 *
 *       }
 *
 *       //fires when we get a new channel or it changes
 *       void channel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
 *       {
 *           //get & set currently open channel
 *           channel = HttpNotificationChannel.Find(ChannelName);
 *
 *           //Bind channel to Tile
 *
 *           channel.BindToShellTile();
 *
 *           //Register uri in storage
 *
 *           RegisterForNotifications();
 *
 *       }
 *
 *       //call cloud service to register deviceid and channel uri
 *       private void RegisterForNotifications()
 *       {
 *           //get device id
 *           object DeviceUniqueID;
 *
 *           string DeviceID = null;
 *
 *           byte[] DeviceIDbyte = null;
 *
 *          //DeviceExtendedProperties.TryGetValue("DeviceUniqueID", out DeviceUniqueID);
 *           if (DeviceExtendedProperties.TryGetValue("DeviceUniqueID", out DeviceUniqueID))
 *           {
 *               DeviceID = DeviceUniqueID as string;
 *           }
 *           MessageBox.Show(DeviceID);
 *           // string DeviceID = Convert.ToBase64String(DeviceIDbyte);
 *
 *           // call our service with device id and channel uri
 *
 *           ServiceReference1.Service1Client svc = new ServiceReference1.Service1Client();
 *
 *           svc.SubscribeAsync(DeviceID, channel.ChannelUri.ToString());
 *
 *
 *
 *         }
 *
 *
 *       private void noPush_Click(object sender, RoutedEventArgs e)
 *       {
 *           yesPush.IsEnabled = true;
 *           noPush.IsEnabled = false;
 *           pushSelect.IsOpen = false;
 *           MessageBox.Show("Push Notifications are OFF!");
 *       }
*/


        //--------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------
        //Events Methods

        private void addData_Click(object sender, RoutedEventArgs e)
        {
            addFields.IsOpen = true;
        }

        private void year_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            year.Text = "";
        }

        private void day_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            day.Text = "";
        }

        private void eventName_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            eventName.Text = "";
        }
        
        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            addFields.IsOpen = false;
            eventName.Text = "Event Name";
            day.Text = "Day";
            month.Content = "Month";
            year.Text = "Year";
            
        }

        private void addEvent_Click(object sender, RoutedEventArgs e)
        {
            if (eventName.Text != null && day.Text != null && year.Text != null && month.Content.ToString() != "Month")
            {
                if (eventName.Text != "Event Name" && day.Text != "Day" && year.Text != "Year")
                {
                    string userIDToSend = userID;
                    string dayToSend = day.Text;
                    string monthToSend = month.Content.ToString();
                    string yearToSend = year.Text;
                    string eventNameToSend = eventName.Text;

                    WebClient addProfile = new WebClient();
                    addProfile.UploadStringAsync(new Uri("http://hsevent.cloudapp.net/Service1.svc/users?userID=" + userIDToSend + "&day=" + dayToSend + "&month=" + monthToSend + "&year=" + yearToSend + "&eventName=" + eventNameToSend), "POST");
                    addProfile.UploadStringCompleted += addProfile_UploadStringCompleted;
                    
                    addFields.IsOpen = false;
                    eventName.Text = "Event Name";
                    day.Text = "Day";
                    month.Content = "Month";
                    year.Text = "Year";
                }
                else
                {
                    MessageBox.Show("Please Fill in All Fields");
                }

            }
            else
            {
                MessageBox.Show("Please Fill in All Fields");
            }

            
        }

        void addProfile_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                MessageBox.Show("Event added!", "Success", MessageBoxButton.OK);
            }
            else
            {
                MessageBox.Show("Problem adding event", "Unsuccessful", MessageBoxButton.OK);
                Console.WriteLine("An error occured:" + e.Error);
            }
        }

        private void month_Click(object sender, RoutedEventArgs e)
        {
            addMonth.IsOpen = true;
        }


        //--------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------
        //Months Functions

        private void jan_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "January";
            addMonth.IsOpen = false;
        }

        private void feb_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "February";
            addMonth.IsOpen = false;
        }

        private void mar_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "March";
            addMonth.IsOpen = false;
        }

        private void apr_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "April";
            addMonth.IsOpen = false;
        }

        private void may_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "May";
            addMonth.IsOpen = false;
        }

        private void jun_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "June";
            addMonth.IsOpen = false;
        }

        private void jul_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "July";
            addMonth.IsOpen = false;
        }

        private void aug_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "August";
            addMonth.IsOpen = false;
        }

        private void sep_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "September";
            addMonth.IsOpen = false;
        }

        private void oct_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "October";
            addMonth.IsOpen = false;
        }

        private void nov_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "November";
            addMonth.IsOpen = false;
        }

        private void dec_Click(object sender, RoutedEventArgs e)
        {
            month.Content = "December";
            addMonth.IsOpen = false;
        }


        //--------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------
        //Other

        private void help_Click(object sender, RoutedEventArgs e)
        {
            helpPopup.IsOpen = true;
        }

        private void closeHelp_Click(object sender, RoutedEventArgs e)
        {
            helpPopup.IsOpen = false;
        }

        private void contact_Click(object sender, RoutedEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("https://twitter.com/WOPR_Josh");
            task.Show();
        }



    }
}