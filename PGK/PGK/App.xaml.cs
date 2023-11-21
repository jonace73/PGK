
using Xamarin.Forms;
using PGK.Services;
using PGK.Views;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using PGK.Data;

namespace PGK
{
    public partial class App : Application
    {
        // These two MUST be in App and NOT in MainActivity
        public static bool isFirstCreation = true;
        public static bool isFirstResume = true;

        static string sGlobalPath; // DON'T DELETE
        public static string globalPath
        {
            // This is used for OnRestore from OnSleep
            // Has a sample form A>B>C->
            get
            {
                if (sGlobalPath == null)
                {
                    sGlobalPath = "Home" + MarkerCodes.leafSeparator;
                }
                return sGlobalPath;
            }
            set { sGlobalPath = value; }
        }

        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }
        protected override void OnStart()
        {
        }
        protected override void OnSleep()
        {
        }
        protected override void OnResume()
        {
        }
        public static void CommonOnResume()
        {
            DebugPage.AppendLine("App.CommonOnResume App.isFirstResume:" + isFirstResume);

            // If passing onCreate DON'T contact server
            if (isFirstResume)
            {
                isFirstResume = false;
                return;
            }
            // App.isFirstResume is MADE false inside ExtractAssetDBsetUpdateTime() AFTER the DB is formed from the asset
            // Thus, even if the app is suspended when the DB is formed then resumed,
            // the server will not be contacted until the formation is done.

            // If not time to update return
            TimeSpan timeDiff = DateTime.UtcNow - UpdatePage.appLastCheckTime;
            if (timeDiff.Seconds < 1) // Days Seconds
            {
                (Xamarin.Forms.Shell.Current as AppShell).LeaveUpdatePage();
                return;
            }

            // Otherwise
            UpdatePage.UpdateFromServer();

            // Upload location to server. NOTE: this method is NOT waited. If it is, phone hangs up.
            UpdatePage.uploadUserLocationToServer();

        }
        public static async Task<bool> CommonOnCreate()
        {
            DebugPage.AppendLine("App.CommonOnCreate isFirstCreation: "+ isFirstCreation);

            // Extract marker codes from Command.tex AND create nodes DB ONLY at the first creation
            if (!App.isFirstCreation) return false;

            // Set flag not to return again
            App.isFirstCreation = false;

            // Extract from Asset
            UpdatePage.appLastCheckTime = DateTime.UtcNow;
            UpdatePage.isUpdateOnGoing = true;
            UpdatePage.isTransmissionInError = false;
            UpdatePage.isDownloadFromServer = false;
            (Xamarin.Forms.Shell.Current as AppShell).ShowUpdatePage();

            // *** Extract markers. This *** MUST *** be done after ShowUpdatePage()
            // AND *** MUST *** be prior to ExtractAssetDBandSetUpdateTime()
            FileProcessor.ExtractMarkerCodes();

            // Set app as not being from crash *** THIS MUST BE PRIOR TO ExtractAssetDBandSetUpdateTime ***
            NodeDatabase.isFromCrash = false;

            // Extract nodes from asset, SET last update time, AND set 
            await FileProcessor.ExtractAssetDBandSetUpdateTime();
            // This contains the code: NodeDatabase.DBnodes.InsertNodeAsync() which can EVENTUALLY alter *** NodeDatabase.isFromCrash ***
            // If from CRASH, the DB is destroyed then regenerated from Asset, i.e., last update time is based on Asset

            // Check for crash
            if (NodeDatabase.isFromCrash)
            {
                DebugPage.AppendLine("App.CommonOnCreate: From crash");
            }

            // If app had crashed, download from server 
            await UpdatePage.DownloadFromServer();

            // Terminate logo rotation and leave update page
            UpdatePage.isUpdateOnGoing = false;

            // CREATE then add appID to DB to be used during OnResumeCommon() for update of DB            
            await UpdatePage.AddAppIDtoDB();

            // Make VS happy
            return true;
        }
    } // END OF CLASS
}
