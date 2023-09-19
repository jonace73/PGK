using Foundation;
using UIKit;

namespace PGK.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }        
        public override async void OnActivated(UIApplication uiApplication)
        {
            // Called when the application is launched and every time the app returns to the foreground. 
            base.OnActivated(uiApplication);
            
            // Call OnCreate common to Android and iOS
            // The next line will just be entered then exited right away after the first creation.
            await App.CommonOnCreate();

            // The next line will not be executed at the first creation, but after.
            App.CommonOnResume();
        }
    } // END CLASS
}
