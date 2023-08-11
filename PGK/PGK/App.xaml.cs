
using Xamarin.Forms;
using PGK.Services;
using PGK.Views;

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
    }
}
