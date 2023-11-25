using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;
using PGK.Services;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace PGK.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DebugPage : ContentPage
    {
        public static bool isInDebug = true;//true false
        public static bool isEmailTesting = true;//true false MAKE THIS FALSE AFTER TESTING APP ON MANY PHONES
        static int nthDebugLine = 0;
        static string debugText;
        public static string DebugText
        {
            get
            {
                if (debugText == null)
                {
                    debugText = "";
                }
                return debugText;
            }
            set
            {
                debugText = value;
            }
        }
        public DebugPage()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            DebuggingOutputs.Text = DebugText;
        }
        public static void AppendLine(string textToAppend)
        {
            if (isInDebug)
            {
                string newText = (nthDebugLine++) + ".) " + textToAppend;
                DebugText = DebugText + newText + Environment.NewLine;
            }
        }
        public async static Task<bool> Prompt(string header, string body, string OK)
        {
            if (isInDebug)
                await App.Current.MainPage.DisplayAlert(header, body, OK);

            return true;
        }
        void OnEraseMsgsClicked(object sender, EventArgs e)
        {
            DebugText = "";
            OnAppearing();
        }
        async void OnCopyMsgsClicked(object sender, EventArgs e)
        {
            try
            {
                await Clipboard.SetTextAsync(debugText);
            }
            catch (Exception err)
            {
                AppendLine("DebugPage.OnSendMsgsClicked ERR: " + err.Message);
            }

        }
        protected override bool OnBackButtonPressed()
        {
            // ASSUME: Do nothing when hardware back button is pressed
            return true;
        }
        async void OnSaveHeightPerLine(object sender, EventArgs e)
        {
            if (int.TryParse(HeightPerLine.Text, out int value))
            {
                ViewProcessor.heigthPerLine = value;
                AppendLine("DebugPage.OnSaveHeightPerLine heigthPerLine: " + ViewProcessor.heigthPerLine);
            }
            else
            {
                await DisplayAlert("Error:", "HeigthPerLine is not an integer.", "OK");
            }
        }
        async void OnDeliberateCrash(object sender, EventArgs e)
        {
            bool isEnabled = await Analytics.IsEnabledAsync();
            DebugPage.AppendLine("OnDeliberateCrash crash analytics isEnabled: " + isEnabled);
            if (isEnabled)
            {
                DebugPage.AppendLine("DebugPage.OnDeliberateCrash");
                DependencyService.Get<ICrash>().MakeToast("Called from OnDeliberateCrash");
                //Crashes.GenerateTestCrash();
            }
        }
        async void OnReportPreviousCrash(object sender, EventArgs e)
        {
            bool isEnabled = await Analytics.IsEnabledAsync();
            DebugPage.AppendLine("DebugPage.OnReportPreviousCrash crash analytics isEnabled: " + isEnabled);// Analytics.TrackEvent("OnAlarmsToServer clicked");
            if (!isEnabled) return;

            bool didAppCrash = await Crashes.HasCrashedInLastSessionAsync();// This works
            if (didAppCrash)
            {
                ErrorReport crashReport = await Crashes.GetLastSessionCrashReportAsync();// This DOESN'T work
                // No report even in Diagnostics > Issues > Crashes
                DebugPage.AppendLine("DebugPage.Last CrashReport: " + crashReport.StackTrace);
                // Send this same report to WhizKod server
            }
        }
    } // END CLASS
}