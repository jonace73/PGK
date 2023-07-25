using Newtonsoft.Json;
using PGK.Data;
using PGK.Models;
using PGK.Services;
using System;
using System.Collections.Generic;
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
        public static bool transmissionError = false;
        public static string retransmitKeyword = "Retransmit";
        public static string appID;
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
        public static DateTime appLastIPchangeTime = new DateTime(2023, 01, 01, 00, 00, 00);

        public UpdatePage()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            DebugPage.AppendLine("UpdatePage.OnAppearing isDownloadFromServer: " + isDownloadFromServer);
            RotateImage();
            if (isDownloadFromServer) DownloadFromServer();
        }
        public async Task<int> RotateImage()
        {
            // Rotate image until node download is finished
            // Leave UpdatePage and go to ContentPage

            DebugPage.AppendLine("UpdatePage.RotateImage.");

            int ii = 0;
            while (isUpdateOnGoing && !transmissionError)
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
            AppShellInstance.LeaveUpdate();

            return 0;
        }
        public async Task<int> DownloadFromServer()
        {
            // Send message
            DebugPage.AppendLine("UpdatePage.DownloadFromServer.");
            string msg = JsonMsgToServer("phoneRequestUpdate", DateTimeToStringOneBased(appLastUpdateTime));
            await DependencyService.Get<ICommWithServer>().SendByDependency(msg, "ACKphoneRequestUpdate");

            return 0;
        }
        public static string JsonMsgToServer(string type, string timeStamp)
        {
            // One-off Username
            return JsonConvert.SerializeObject(new { Type = type, TimeStamp = timeStamp, Username = AppID });
        }
        public static async Task<int> ExtractNodes(string serverMsg)
        {
            // ALGO: Extract nodes then stop rotating image
            //DebugPage.AppendLine("UpdatePage.ExtractNodes");

            string[] result = serverMsg.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<Node> nodes = new List<Node>();
            if (result.Length == 1) return 0;

            // If there are nodes
            int count = 0;
            foreach (string str in result)
            {
                // Skip header
                if (count > 0)
                {/*/ Result 24/05/2023 11:59: 51 AM
                  * This matches with the RESULTs of MainActivity.ExtractOrigDBsetUpdateTime()
                    Node node = Node.ServerStringToNode(str);
                    DateTime date = UpdatePage.StringToDateTime(node.DateUpdated);
                    DebugPage.AppendLine("ExtractNodes date: " + date);//*/

                    nodes.Add(ViewProcessor.LineFromServerToNode(str));
                }
                count++;
            }
            DebugPage.AppendLine("UpdatePage.ExtractNodes numberNodes: " + (count - 1));

            // Save nodes to DB
            await NodeDatabase.DBnodes.SaveNodeSAsync(nodes);

            // Reduce the count to account the Ackowledgement
            return --count;
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
        public static DateTime FileStringToDateTime(string dateTime)
        {
            //DebugPage.AppendLine("UpdatePage.StringToDateTime: " + dateTime);
            string[] elements = dateTime.Split(' ');
            string[] days = elements[0].Split('/');
            string[] times = elements[1].Split(':');

            return new DateTime(Int32.Parse(days[0]), Int32.Parse(days[1]), Int32.Parse(days[2]), Int32.Parse(times[0]), Int32.Parse(times[1]), Int32.Parse(times[2]));
        }
        public static string[] ExtractHeader(string serverMsg)
        {
            //DebugPage.AppendLine("ExtractHeader");

            string[] result = serverMsg.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            return result[0].Split('$');
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