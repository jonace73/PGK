using Newtonsoft.Json;
using PGK.Data;
using PGK.Models;
using PGK.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PGK.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UpdatePage : ContentPage
    {
        public static bool isDownloadFromServer = true;
        public static bool isUpdateOnGoing = true;
        public static bool isTransmissionInError = false;
        public static string retransmitKeyword = "Retransmit";
        public static string appID;
        public static string URI = "https://whizkod.com/PGK/HttpPHP/RxPost.php";
        public static string AppID
        {
            get
            {
                if (appID == null)
                {
                    appID = CreateRandomAppID();
                }
                return appID;
            }
        }
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
        public async Task<int> DownloadFromServer()
        {
            // Send message
            DebugPage.AppendLine("UpdatePage.DownloadFromServer appLastUpdateTime: " + DateTimeToStringOneBased(appLastUpdateTime));
            using (var client = new HttpClient())
            {
                // Create a new post
                var novoPost = new Post
                {
                    signalType = "Update",
                    lastClientUpdateDate = DateTimeToStringOneBased(appLastUpdateTime)
                };

                // create the request content and define Json  
                var json = JsonConvert.SerializeObject(novoPost);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                //  send a POST request
                var uri = URI;
                var result = await client.PostAsync(uri, content);

                // on error throw a exception  
                result.EnsureSuccessStatusCode();

                // handling the answer  
                var resultString = await result.Content.ReadAsStringAsync();
                Post post = JsonConvert.DeserializeObject<Post>(resultString);

                // Process post
                await ProcessReceivedPost(post);
            }

            return 0;
        }
        private async Task<bool> ProcessReceivedPost(Post post)
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
                if (node.Keyword.Equals("URIchange"))
                {
                    ChangeURI(node.Header);
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
        public static void ChangeURI(string URInew)
        {
            DebugPage.AppendLine("UpdatePage.ChangeURI URInew: " + URInew);
            URI = URInew;
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
        public static string CreateRandomAppID()
        {
            int randValue;
            char letter;
            string str = "";
            Random rand = new Random();
            for (int i = 0; i < 8; i++)
            {

                // Generating a random number.
                randValue = rand.Next(0, 26);

                // Generating random character by converting
                // the random number into character.
                letter = Convert.ToChar(randValue + 65);

                // Appending the letter to string.
                str = str + letter;
            }
            return str;
        }
    } // END class
}