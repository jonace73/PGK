using Android.Widget;
using Xamarin.Forms;
using PGK.Droid.Services;
using PGK.Services;
using PGK.Views;

[assembly: Dependency(typeof(AndroidCrash))]
namespace PGK.Droid.Services
{
    internal class AndroidCrash : ICrash
    {
        /**
         * This will deliberately crash the app
         */
        public void MakeToast(string txt)
        {
            DebugPage.AppendLine("AndroidCrash.MakeToast: " + txt);
            Toast.MakeText(null, "AndroidCrash: " + txt, ToastLength.Short).Show();// This works
        }
    }
}