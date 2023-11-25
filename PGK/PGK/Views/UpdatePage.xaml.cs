using Newtonsoft.Json;
using PGK.Data;
using PGK.Models;
using PGK.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PGK.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UpdatePage : ContentPage
    {
        public static string rootName = "Update";
        public static bool isDownloadFromServer = true;
        public static bool isUpdateOnGoing = true;
        public static bool isTransmissionInError = false;
        public static string retransmitKeyword = "Retransmit";

        // NOTE: No need to change anything when migrating to anothe server host
        public static string URI = "https://whizkod.com/PGK/HttpPHP/RxAppRequest.php";

        // Any changes in    appIDforDB   must be reflected to Web\HttpPHP\Functions.php: isForappID() and extractAppID()
        public static string appIDforDB = "appID";

        // Attach this info to email
        public static string appIDforTesting = null;
        public static double longitudeForTesting = 0.0;
        public static double latitudeForTesting = 0.0;

        // Month is ONE-BASED
        public static DateTime appLastUpdateTime = new DateTime(2023, 01, 01, 00, 00, 00);
        public static DateTime appLastCheckTime = new DateTime(2023, 01, 01, 00, 00, 00);

        public UpdatePage()
        {
            InitializeComponent();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();

            DebugPage.AppendLine("UpdatePage.OnAppearing isDownloadFromServer: " + isDownloadFromServer);

            // Rotate image. DON'T WAIT
            RotateImage();

            // If not downloading from server return
            if (!isDownloadFromServer) return;

            // Download from server 
            try
            {
                await DownloadFromServer();
                isUpdateOnGoing = false;
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("UpdatePage.OnAppearing ERROR: " + ex.Message);
                isTransmissionInError = true;
            }
        }
        public async Task<int> RotateImage()
        {
            // Rotate image until node download is finished
            // Leave UpdatePage and go to ContentPage

            DebugPage.AppendLine("UpdatePage.RotateImage.");

            int ii = 0;
            while (isUpdateOnGoing && !isTransmissionInError)
            {
                // 10/T = 360/N => T = 1000N/36 msec. For N = 1 sec T = 27.7 ~ 30 msec
                await BlessedTrinityImage.RotateTo(10 * (ii++), 30);
                ii = ii % 36;
            }

            // Reset isUpdateOnGoing for the next server download
            isUpdateOnGoing = true;

            // This is verified
            DebugPage.AppendLine("UpdatePage.RotateImage loop is cut appLastUpdateTime: " + appLastUpdateTime);

            // Leave UpdatePage, i.e., show Home
            var AppShellInstance = Xamarin.Forms.Shell.Current as AppShell;
            AppShellInstance.LeaveUpdatePage();

            return 0;
        }
        public static async Task<int> DownloadFromServer()
        {
            // Send message
            DebugPage.AppendLine("UpdatePage.DownloadFromServer appLastUpdateTime: " + DateTimeToStringOneBased(appLastUpdateTime));
            try
            {
                using (var client = new HttpClient())
                {
                    // Create a new post
                    var novoPost = new Post();

                    // create the request content and define Json  
                    var json = JsonConvert.SerializeObject(novoPost);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    //  send a POST request
                    var result = await client.PostAsync(URI, content);

                    // on error throw a exception  
                    result.EnsureSuccessStatusCode();

                    // handling the answer  
                    var resultString = await result.Content.ReadAsStringAsync();
                    Post post = JsonConvert.DeserializeObject<Post>(resultString);

                    // Process post
                    await ProcessReceivedPost(post);//*/
                }
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("UpdatePage.DownloadFromServer: " + ex.Message);
            }

            return 0;
        }
        public static async Task<int> uploadUserLocationToServer()
        {
            // Send message
            DebugPage.AppendLine("UpdatePage.uploadUserToServer");
            try
            {
                using (var client = new HttpClient())
                {
                    // Create a new post
                    var novoPost = new Post();

                    // Add appID and location
                    string appID = await ExtractAppIDFromDB();
                    novoPost.LeafTag = appIDforDB + MarkerCodes.leafSeparator + appID;
                    Location location = await GetCurrentLocation();
                    novoPost.latitude = location.Latitude;
                    novoPost.longitude = location.Longitude;

                    // Attach this info to email
                    if (DebugPage.isEmailTesting)
                    {
                        UpdatePage.appIDforTesting = appID;
                        UpdatePage.latitudeForTesting = location.Latitude;
                        UpdatePage.longitudeForTesting = location.Longitude;
                    }

                    // create the request content and define Json  
                    var json = JsonConvert.SerializeObject(novoPost);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    //  send a POST request
                    var result = await client.PostAsync(URI, content);

                    // on error throw a exception  
                    result.EnsureSuccessStatusCode();

                    // handling the answer  
                    var resultString = await result.Content.ReadAsStringAsync();
                    Post post = JsonConvert.DeserializeObject<Post>(resultString);

                    // Display result numberNodes
                    DebugPage.AppendLine("UpdatePage.uploadUserToServer is user encoded: " +(post.numberNodes == 1));
                }
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("UpdatePage.uploadUserToServer: " + ex.Message);
            }

            return 0;
        }
        public static void UpdateFromServer()
        {
            DebugPage.AppendLine("UpdatePage.UpdateFromServer");

            appLastCheckTime = DateTime.UtcNow;
            isUpdateOnGoing = true;
            isTransmissionInError = false;
            isDownloadFromServer = true;
            var AppShellInstance = Xamarin.Forms.Shell.Current as AppShell;
            AppShellInstance.ShowUpdatePage();
        }
        private static async Task<bool> ProcessReceivedPost(Post post)
        {
            DebugPage.AppendLine("UpdatePage.ProcessReceivedPost numberNodes: " + post.numberNodes);
            if (post.numberNodes == 0) return true;

            // Set appLastUpdateTime
            appLastUpdateTime = StringToDateTime(post.lastClientUpdateDate);

            // Extract new nodes 
            await ExtractNodes(post.Nodes);

            return true;
        }
        public static async Task<int> ExtractNodes(string serverMsg)
        {
            string[] result = serverMsg.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<Node> nodes = new List<Node>();

            int count = 0;
            foreach (string str in result)
            {
                Node node = ViewProcessor.LineFromServerToNode(str);
                if (ViewProcessor.ExtractPathRoot(node.LeafTag).Equals("Parameters"))
                {
                    ProcessParameters(node);
                }
                else
                {
                    nodes.Add(node);
                    count++;
                }
            }
            DebugPage.AppendLine("UpdatePage.ExtractNodes numberNodes: " + count);

            // Save nodes to DB
            if (count > 0)
            {
                await NodeDatabase.DBnodes.SaveNodeSAsync(nodes);
            }

            return count;
        }
        public static void ProcessParameters(Node node)
        {
            DebugPage.AppendLine("UpdatePage.ProcessParameters");
            switch (node.Keyword)
            {
                case "Testing": UpdateParameters(node); break;
            }

        }
        static void UpdateParameters(Node node)
        {
            DebugPage.AppendLine("UpdatePage.Test");
            // LATER
        }
        public static string DateTimeToStringOneBased(DateTime dateTime)
        {
            string retValue = dateTime.Year + "-" + dateTime.Month + "-" + dateTime.Day + " " + dateTime.Hour + ":" + dateTime.Minute + ":" + dateTime.Second;
            //DebugPage.AppendLine("UpdatePage.DateTimeToStringOneBased retValue: " + retValue);
            return retValue;
        }
        public static DateTime StringToDateTime(string dateTime)
        {
            //DebugPage.AppendLine("UpdatePage.StringToDateTime: " + dateTime);
            string[] elements = dateTime.Split(' ');
            string[] days = elements[0].Split('-');
            string[] times = elements[1].Split(':');

            return new DateTime(Int32.Parse(days[0]), Int32.Parse(days[1]), Int32.Parse(days[2]), Int32.Parse(times[0]), Int32.Parse(times[1]), Int32.Parse(times[2]));
        }
        public async static Task<Location> GetCurrentLocation()
        {
            try
            {
                CancellationTokenSource cts;
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                cts = new CancellationTokenSource();
                return await Geolocation.GetLocationAsync(request, cts.Token);
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                DebugPage.AppendLine("Handle not supported on device exception: " + fnsEx.Message);
            }
            catch (FeatureNotEnabledException fneEx)
            {
                DebugPage.AppendLine("Handle not enabled on device exception: " + fneEx.Message);
            }
            catch (PermissionException pEx)
            {
                DebugPage.AppendLine("Handle permission exception: " + pEx.Message);
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("Unable to get location: " + ex.Message);
            }
            return null;
        }
        public static async Task<string> AddAppIDtoDB()
        {
            string appID = FileProcessor.CreateRandomAppID();
            Node node = new Node();
            node.LeafTag = appIDforDB + MarkerCodes.leafSeparator + appID;
            await NodeDatabase.DBnodes.InsertNodeAsync(node);
            return appID;
        }
        public static async Task<string> ExtractAppIDFromDB()
        {
            string appID = string.Empty;
            List<Node> allNodes = await NodeDatabase.DBnodes.SearchAppIDAsync();
            foreach (Node node in allNodes)
            {
                appID = ViewProcessor.ExtractKeyword(node.LeafTag);
            }
            return appID;
        }
        public static string AttachPhoneInfo()
        {
            return "?appID="+UpdatePage.appIDforTesting +"&Longitude=" + UpdatePage.longitudeForTesting + "&Latitude=" + UpdatePage.latitudeForTesting;
        }
    } // END class
}