using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using PGK.Data;
using System.IO;
using PGK.Models;
using PGK.Views;
using System;
using PGK.Services;
using System.Threading.Tasks;

namespace PGK.Droid
{
    [Activity(Label = "PGK", Icon = "@drawable/LogoPGK", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public static bool isToUpdateDB = true;
        public static bool isToUpdateIP = false;
        protected async override void OnCreate(Bundle savedInstanceState)
        {
            DebugPage.AppendLine("MainActivity.OnCreate App.isFirstCreation:" + App.isFirstCreation);

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            // Extract marker codes from Command.tex AND create nodes DB ONLY at the first creation
            if (!App.isFirstCreation) return;

            // Set flag not to return again
            App.isFirstCreation = false;

            // Download from Asset
            UpdatePage.appLastCheckTime = DateTime.UtcNow;
            UpdatePage.isUpdateOnGoing = true;
            UpdatePage.isTransmissionInError = false;
            UpdatePage.isDownloadFromServer = false;
            (Xamarin.Forms.Shell.Current as AppShell).ShowUpdatePage();

            // *** Extract markers. This *** MUST *** be done after ShowUpdatePage()
            // AND *** MUST *** be prior to ExtractAssetDBandSetUpdateTime()
            ExtractMarkerCodes();

            // Extract nodes from asset and SET last update time
            await ExtractAssetDBandSetUpdateTime();

            // Terminate logo rotation and leave update page
            UpdatePage.isUpdateOnGoing = false;
        }
        protected override void OnResume()
        {
            DebugPage.AppendLine("MainActivity.OnResume App.isFirstResume:" + App.isFirstResume);
            base.OnResume();

            // If passing onCreate DON'T contact server
            if (App.isFirstResume)
            {
                App.isFirstResume = false;
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
            UpdateFromServer();
        }
        private void UpdateFromServer()
        {
            DebugPage.AppendLine("MainActivity.UpdateFromServer");

            UpdatePage.appLastCheckTime = DateTime.UtcNow;
            UpdatePage.isUpdateOnGoing = true;
            UpdatePage.isTransmissionInError = false;
            UpdatePage.isDownloadFromServer = true;
            var AppShellInstance = Xamarin.Forms.Shell.Current as AppShell;
            AppShellInstance.ShowUpdatePage();
        }
        private void ExtractMarkerCodes()
        {
            DebugPage.AppendLine("MainActivity.ExtractMarkerCodes");
            StreamReader sr = new StreamReader(Android.App.Application.Context.Assets.Open("Commands.tex"));
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                MarkerCodes.ExtractCodes(line);
            }
        }
        private async Task<bool> ExtractAssetDBandSetUpdateTime()
        {
            DebugPage.AppendLine("MainActivity.ExtractAssetDBandSetUpdateTime");

            DateTime refDate = UpdatePage.appLastUpdateTime;
            try
            {
                StreamReader sr = new StreamReader(Application.Context.Assets.Open("NodesDB.txt"));
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    Node node = ViewProcessor.LineFromAssetToNode(line);//

                    // Set appLastUpdateTime
                    DateTime date = UpdatePage.StringToDateTime(node.DateUpdated);
                    // RESULT: 24/05/2023 8:39:00. This matches with that of UpdatePage.ExtractNodes()
                    TimeSpan timeDiff = date - refDate;
                    if (timeDiff.Minutes >= 1)
                    {
                        refDate = date;
                    }
                    int numberInserted = await NodeDatabase.DBnodes.InsertNodeAsync(node);
                    if (numberInserted < 0) break;
                }
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("MainActivity.ExtractAssetDBandSetUpdateTime ERROR: " + ex.Message);
            }

            // Save latest update date
            UpdatePage.appLastUpdateTime = refDate;
            DebugPage.AppendLine("MainActivity.ExtractAssetDBandSetUpdateTime appLastUpdateTime: " + UpdatePage.appLastUpdateTime);
            return true;
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}