using PGK.Services;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PGK.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StructurePage : ContentPage
    {
        static string rootName = "Authority";
        public static bool useScratchDB = false;
        public static List<Frame> scratchNodeFrameDB = new List<Frame>();
        public static bool showBackArrow = false;
        static string sPathSeed;
        public static string pathSeed
        {
            get
            {
                if (sPathSeed == null)
                {
                    sPathSeed = rootName + MarkerCodes.leafSeparator;
                }
                return sPathSeed;
            }
            set { sPathSeed = value; }
        }
        public StructurePage()
        {
            InitializeComponent();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();

            // In RootPage.OnAppearing, the reviseD NodesToScrollView is called that uses the SCRATCH based on the FLAG which will be set to FALSE
            if (useScratchDB)
            {
                DebugPage.AppendLine(rootName + ".OnAppearing useScratchDB");
                Content = ViewProcessor.ScratchFramesToScrollView(scratchNodeFrameDB);//

                ScrollView scrollViewScratch = Content as ScrollView;
                // Set scrollView
                await (new ViewProcessor()).ScrollToPoint(0, ViewProcessor.scrollHeight, true, scrollViewScratch, rootName + " useScratchDB scrollHeight: " + ViewProcessor.scrollHeight);
                ViewProcessor.scrollHeight = 0;
                // Reset use of scratch DB
                useScratchDB = false;
                return;
            }

            // Alter back arrow
            backArrow.IsVisible = showBackArrow;

            // Save pathSeed to globalPath
            DebugPage.AppendLine(rootName + ".OnAppearing pathSeed: " + pathSeed);
            App.globalPath = pathSeed;

            // Create the contents of this page
            ScrollView scrollView = await ViewProcessor.CreateContent(pathSeed);

            if (scrollView != null)
            {
                Content = scrollView;

                // Scroll to an intended point
                await (new ViewProcessor()).ScrollToPoint(0, ViewProcessor.scrollHeight, true, scrollView, rootName + ".OnAppearing scrollHeight: " + ViewProcessor.scrollHeight);
                ViewProcessor.scrollHeight = 0;
            }
        }
        public void DisplayPage()
        {
            OnAppearing();
        }
        public async void SetContentPage(StackLayout pageLayout)
        {
            DebugPage.AppendLine("Authority.SetContentPage");

            ScrollView scrollView = new ScrollView()
            {
                Content = pageLayout
            };

            this.Content = scrollView;

            // Scroll to an intended point
            await(new ViewProcessor()).ScrollToPoint(0, ViewProcessor.scrollHeight, true, scrollView, rootName + ".OnAppearing scrollHeight: " + ViewProcessor.scrollHeight);
            ViewProcessor.scrollHeight = 0;
        }
        void OnTapBackArrow(object sender, EventArgs args)
        {
            DebugPage.AppendLine(rootName + ".OnTapHomeBackArrow");
            ArrowResponse();

            // Show the last page.
            OnAppearing();
        }
        public void ArrowResponse()
        {

            // RemoveLastSegment to create a NEW path. Example, A>B>C-> to A>B->
            pathSeed = ViewProcessor.DecrementPathSeed(pathSeed);
            //DebugPage.AppendLine(rootName + ".ArrowResponse path: " + pathSeed);

            // During the tap, path has a count of at least TWO as there is no arrow at count of ONE.
            // If at the last removal rootPage is reached then don't show back arrow

            //DebugPage.AppendLine(rootName + ".CountSegments: " + ViewProcessor.CountSegments(pathSeed));
            if (ViewProcessor.CountSegments(pathSeed) == 2) // Left is "Home" right is nothing
            {
                showBackArrow = false;
                //DebugPage.AppendLine(rootName + ".CountSegments: inside if CountSegments");
            }
        }
        protected override bool OnBackButtonPressed()
        {
            // ASSUME: Do nothing when hardware back button is pressed
            return true;
        }
    } // END CLASS
}