using System.Threading.Tasks;
using System.Threading;
using Xamarin.Essentials;

using PGK.Services;
using NUnit.Framework;
using Newtonsoft.Json;
using Websockets;
using PGK.Views;
using PGK.Droid.Services;
using Xamarin.Forms;
using System.Linq;

[assembly: Dependency(typeof(AndroidCommWithServer))]
namespace PGK.Droid.Services
{
    public class AndroidCommWithServer : ICommWithServer
    {
        public static string WSECHOD_URL = "wss://70.32.30.125:8443";
        private IWebSocketConnection connection;
        private CancellationTokenSource token;
        private bool Failed;
        private bool Echo;
        private string SendInfoType;

        // My properties
        private bool isConnectedToServer = false;
        private bool isConnectedToInternet = false;

        public AndroidCommWithServer()
        {

        }
        public async Task<bool> ConnectToInternet()
        {
            isConnectedToInternet = Connectivity.NetworkAccess == NetworkAccess.Internet;
            if (!isConnectedToInternet)
            {
                UpdatePage.transmissionError = true;
                await DebugPage.Prompt("No Internet connection:", "Go to phone settings, set data usage on, then open app again", "OK");
            }
            return isConnectedToInternet;
        }
        public async Task<bool> ConnectToServer()
        {
            if (!isConnectedToInternet)
            {
                return false;
            }

            // 1) Set up
            Setup();

            // 2) Call factory from your PCL code.
            // This is the same as new   Websockets.Droid.WebsocketConnection();
            // Except that the Factory is in a PCL and accessible anywhere
            connection = Websockets.WebSocketFactory.Create();

            connection.SetIsAllTrusted();
            connection.OnLog += Connection_OnLog;
            connection.OnError += Connection_OnError;
            connection.OnMessage += Connection_OnMessage;
            connection.OnOpened += Connection_OnOpened;

            //Timeout / Setup
            Echo = Failed = false;
            token = new CancellationTokenSource();
            Timeout(token.Token);

            connection.Open(WSECHOD_URL);
            while (!connection.IsOpen && !Failed)
            {
                await Task.Delay(10);
            }

            if (!connection.IsOpen)
            {
                token.Cancel();
                isConnectedToServer = false;
                UpdatePage.transmissionError = true;
                await DebugPage.Prompt("Connection:", "Sorry, PGK server is down. Retry after some time.", "OK");

                return false;
            }

            //DebugPage.AppendLine("AndroidCommWithServer.ConnectToServer: Connected to server");
            isConnectedToServer = true;
            return true;
        }
        private async Task<bool> CheckInternetServerConnectioins()
        {
            //DebugPage.AppendLine("AndroidCommWithServer.CheckInternetServerConnectioins");

            // If not connected make a connection then connect to server
            if (!isConnectedToInternet)
            {
                await ConnectToInternet();
                if (isConnectedToInternet)
                {
                    await ConnectToServer();
                    if (!isConnectedToServer) { return false; }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (!isConnectedToServer)
                {
                    await ConnectToServer();
                    if (!isConnectedToServer) { return false; }
                }
            }

            return true;
        }
        public async Task<bool> SendMessage(string msg)
        {
            //DebugPage.AppendLine("AndroidCommWithServer.SendMessage");

            // If connections fail, return
            bool connectionStatus = await CheckInternetServerConnectioins();

            //DebugPage.AppendLine("AndroidCommWithServer.SendMessage connectionStatus: " + connectionStatus);
            if (!connectionStatus) { return false; }
            connection.Send(msg);
            int ii = 0;
            while (!Echo && !Failed)
            {
                await Task.Delay(10);
            }

            if (!Echo)
            {
                UpdatePage.transmissionError = true;
                DebugPage.AppendLine("AndroidCommWithServer.SendMessage transmissionError: " + UpdatePage.transmissionError);
                return false;
            }

            token.Cancel();
            Assert.True(true);

            UpdatePage.transmissionError = false;
            DebugPage.AppendLine("AndroidCommWithServer.SendMessage transmissionError: " + UpdatePage.transmissionError);

            return true;

        }
        public async Task<bool> SendByDependency(string msg, string givenType)
        {
            //DebugPage.AppendLine("AndroidCommWithServer.SendByDependency");
            MainActivity.commWithInternetServer.SetReferenceType(givenType);
            return await MainActivity.commWithInternetServer.SendMessage(msg);
        }
        public async Task<bool> CloseConnectionsByDependency()
        {
            await MainActivity.commWithInternetServer.CloseConnections();
            return true;
        }
        public async Task<bool> CloseConnections()
        {
            // Send message to SERVER to close connection.
            await SendCloseMessage();

            // Set CLIENT parameters to close
            isConnectedToServer = false;
            isConnectedToInternet = false;

            return true;
        }
        private async Task<bool> SendCloseMessage()
        {
            string type = "closeConnection";
            string message = JsonConvert.SerializeObject(new { Type = type });
            return await SendMessage(message);

        }
        public void SetReferenceType(string givenType)
        {

            //DebugPage.AppendLine("Inside SetReferenceType");
            SendInfoType = givenType;
        }
        private async void Connection_OnMessage(string serverMsg)
        {
            // Extract elements of the header
            string[] header = UpdatePage.ExtractHeader(serverMsg);
            DebugPage.AppendLine("AndroidCommWithServer.Connection_OnMessage ACK: " + header.ElementAt(0).Trim());
            if (header.ElementAt(0).Trim().Equals("ACKphoneRequestUpdate"))
            {
                string NodeStatus = header.ElementAt(2).Trim();
                DebugPage.AppendLine("AndroidCommWithServer.Connection_OnMessage UpdateStatus: " + NodeStatus);
                if (NodeStatus.Equals("WithUpdates"))
                {
                    // Set appLastUpdateTime
                    UpdatePage.appLastUpdateTime = UpdatePage.StringToDateTime(header.ElementAt(1));

                    // Extract new nodes 
                    await UpdatePage.ExtractNodes(serverMsg);
                }

                // Stop the wait for update
                UpdatePage.isUpdateOnGoing = false;
            }

            // Process IP. header in this case is just one line and has terms separated by '$'
            if (header.ElementAt(0).Trim().Equals("ACKphoneRequestIPchange"))
            {

                string NodeStatus = header.ElementAt(2).Trim();
                DebugPage.AppendLine("AndroidCommWithServer.Connection_OnMessage UpdateStatus: " + NodeStatus);
                if (NodeStatus.Equals("IPchange"))
                {
                    // Change IP
                    WSECHOD_URL = header.ElementAt(3).Trim();

                    // Change IP update time. TESTED OK
                    UpdatePage.appLastIPchangeTime = UpdatePage.StringToDateTime(header.ElementAt(1));
                    DebugPage.AppendLine("AndroidCommWithServer.Connection_OnMessage appLastIPchangeTime: " + UpdatePage.appLastIPchangeTime +" IP: " + WSECHOD_URL);
                }
            }

            // Close connection
            await CloseConnections();
            Echo = true;
        }

        //============================ Codes below are from GitHub ============================//

        public void Setup()
        {
            // 1) Link in your main activity
            Websockets.Droid.WebsocketConnection.Link();
        }
        private void Connection_OnOpened()
        {
            //DebugPage.AppendLine("Connection_OnOpened: Opened !");
        }
        async void Timeout(CancellationToken token)
        {
            try
            {
                var t = Task.Delay(30000, token);
                await t;
                if (!t.IsCanceled)
                {
                    DebugPage.AppendLine("Timeout");
                    Failed = true;
                }
            }
            catch (TaskCanceledException) { }
        }
        private void Connection_OnError(System.Exception ex)
        {
            DebugPage.AppendLine("Connection_OnError: " + ex.ToString());
            UpdatePage.transmissionError = true;
            Failed = true;
            // USING THIS WILL CRASH THE APP ===> await DebugPage.Prompt("Connection error:", "Sorry, retry later.", "OK");
        }
        private void Connection_OnLog(string obj)
        {
            DebugPage.AppendLine(obj);
        }
        public void Tear()
        {
            if (connection != null)
            {
                connection.Dispose();
            }
        }
    }
}