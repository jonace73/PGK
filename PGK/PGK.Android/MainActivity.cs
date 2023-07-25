using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using PGK.Droid.Services;
using PGK.Data;
using System.IO;
using PGK.Models;
using PGK.Views;
using System;
using PGK.Services;
using Xamarin.Forms;

namespace PGK.Droid
{
    [Activity(Label = "PGK", Icon = "@drawable/LogoPGK", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        
        public static AndroidCommWithServer commWithInternetServer = new AndroidCommWithServer();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            DebugPage.AppendLine("MainActivity.OnCreate");

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            DebugPage.AppendLine("MainActivity.OnCreate App.extractFromAsset:" + App.extractFromAsset);

            // Create marker codes from Command.tex
            MarkerCodes markerCodes = new MarkerCodes(); 
            
            if (App.extractFromAsset)
            {
                App.extractFromAsset = false;
                UpdatePage.appLastCheckTime = DateTime.UtcNow;
                UpdatePage.isUpdateOnGoing = true;
                UpdatePage.isDownloadFromServer = false;
                (Xamarin.Forms.Shell.Current as AppShell).ShowUpdatePage();
                ExtractAssetDBsetUpdateTime();
            } else
            {
                ContactServer();
            }
        }
        private async void ContactServer()
        {
            //DebugPage.AppendLine("MainActivity.ContactServer UtcNow: " + DateTime.UtcNow + " appLastCheckTime: " + UpdatePage.appLastCheckTime);

            // ASSUMPTION: DB update happens at least once day
            TimeSpan timeDiff = DateTime.UtcNow - UpdatePage.appLastCheckTime;
            var AppShellInstance = Xamarin.Forms.Shell.Current as AppShell;
            if (timeDiff.Days >= 1) // TESTED ALREADY .Days Minutes Seconds
            {
                DebugPage.AppendLine("MainActivity.ContactServer pass time limit");
                UpdatePage.appLastCheckTime = DateTime.UtcNow;
                UpdatePage.isUpdateOnGoing = true;
                UpdatePage.transmissionError = false;
                UpdatePage.isDownloadFromServer= true;
                AppShellInstance.ShowUpdatePage();
            }//
            else
            {
                DebugPage.AppendLine("MainActivity.ContactServer NOT pass time limit");
                AppShellInstance.LeaveUpdate();
                
                // Send signal to update IP 
                TimeSpan timeIPDiff = DateTime.UtcNow - UpdatePage.appLastIPchangeTime;
                if (timeIPDiff.Days >= 1) // TESTED ALREADY .Days Minutes Seconds
                {
                    DebugPage.AppendLine("MainActivity.ContactServer IPchange.");
                    string msg = UpdatePage.JsonMsgToServer("phoneRequestIPchange", UpdatePage.DateTimeToStringOneBased(UpdatePage.appLastIPchangeTime));
                    await DependencyService.Get<ICommWithServer>().SendByDependency(msg, "ACKphoneRequestIPchange");
                }

            } //*/

        }
        private async void ExtractAssetDBsetUpdateTime()
        {
            DebugPage.AppendLine("MainActivity.ExtractOrigDBsetUpdateTime");

            DateTime refDate = UpdatePage.appLastUpdateTime;
            try
            {
                StreamReader sr = new StreamReader(Android.App.Application.Context.Assets.Open("NodesDB.txt"));
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    Node node = ViewProcessor.LineFromAssetToNode(line);//

                    // Set appLastUpdateTime
                    DateTime date = UpdatePage.StringToDateTime(node.DateUpdated);
                    // RESULT: 24/05/2023 8:39:00. This matches with that of UpdatePage.ExtractNodes()
                    //DebugPage.AppendLine("MainActivity date: " + date); 
                    TimeSpan timeDiff = date -refDate;
                    if (timeDiff.Minutes >= 1)
                    {
                        refDate = date;
                    }
                    int numberInserted =  await NodeDatabase.DBnodes.InsertNodeAsync(node);
                    if (numberInserted < 0) break;
                }
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("MainActivity.ExtractOrigDBsetUpdateTime assets: " + ex.Message);
            }

            // Save latest update date
            UpdatePage.appLastUpdateTime = refDate;
            DebugPage.AppendLine("MainActivity.ExtractOrigDBsetUpdateTime appLastUpdateTime: " + UpdatePage.appLastUpdateTime);

            // Terminate logo rotation and leave update page
            UpdatePage.isUpdateOnGoing = false;
            
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}