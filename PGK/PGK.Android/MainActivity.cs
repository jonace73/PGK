using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using PGK.Views;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace PGK.Droid
{
    [Activity(Label = "PGK", Icon = "@drawable/LogoPGK", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected async override void OnCreate(Bundle savedInstanceState)
        {
            DebugPage.AppendLine("MainActivity.OnCreate App.isFirstCreation:" + App.isFirstCreation);

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            // Call OnCreate common to Android and iOS
            await App.CommonOnCreate();
        }
        protected override void OnResume()
        {
            DebugPage.AppendLine("MainActivity.OnResume App.isFirstResume:" + App.isFirstResume);
            base.OnResume();

            // Call OnResume common to Android and iOS
            App.CommonOnResume();
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}