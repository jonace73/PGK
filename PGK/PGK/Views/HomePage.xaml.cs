
using PGK.Models;
using PGK.Data;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.Generic;
using PGK.Services;

namespace PGK.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : ContentPage
    {
        static string rootName = "Home";
        public static bool useScratchDB = false;
        public static List<Frame> scratchNodeFrameDB = new List<Frame>();
        public static bool showBackArrow = false;
        static string sPathSeed;
        public static string placeholder = "Type keyword to search here.";
        public static bool isDisplayingSearchResults = false;
        public static string pathSeed
        {// Has a sample form A>B>C->
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
        public HomePage()
        {
            InitializeComponent();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();

            DebugPage.AppendLine("HomePage.OnAppearing");

            // Alter back arrow
            backArrow.IsVisible = showBackArrow;

            // In RootPage.OnAppearing, ScratchFramesToScrollView (the reviseD NodesToScrollView) is called that uses the SCRATCH based on the FLAG
            // which will be set to FALSE.
            // This is used for tapping answer nodes ONLY
            if (useScratchDB || isDisplayingSearchResults)
            {
                DebugPage.AppendLine("HomePage.OnAppearing useScratchDB || isDisplayingSearchResults");
                Content = ViewProcessor.ScratchFramesToScrollView(scratchNodeFrameDB);

                ScrollView scrollViewScratch = Content as ScrollView;
                // Set scrollView
                await (new ViewProcessor()).ScrollToPoint(0, ViewProcessor.scrollHeight, true, scrollViewScratch, rootName + " useScratchDB scrollHeight: " + ViewProcessor.scrollHeight);
                ViewProcessor.scrollHeight = 0;
                useScratchDB = false;
                return;
            }

            // Init all nodes
            List<Node> allNodes = new List<Node>();

            // Add nonDBnodes
            AddNonDBnodes(ref allNodes);

            // Get new pathSeed
            DebugPage.AppendLine(rootName + ".OnAppearing pathSeed: " + pathSeed);

            // Save pathSeed to globalPath
            App.globalPath = pathSeed;

            // Retrieve then display DB nodes. The next line might be called before the creation of the DB and will then cause an error.
            List<Node> DBnodes = await NodeDatabase.DBnodes.GetBranchesAsync(pathSeed);
            if (DBnodes == null) return;
            Node.SortNodes(ref DBnodes);

            // Add DBnodes
            allNodes.AddRange(DBnodes);//*/

            // Translate nodes to scrollView elements
            // AND copy them to scratchNodeFrameDB
            Content = ViewProcessor.NodesToScrollView(ViewProcessor.ExtractPathRoot(pathSeed), allNodes);

            ScrollView scrollView = Content as ScrollView;
            await (new ViewProcessor()).ScrollToPoint(0, ViewProcessor.scrollHeight, true, scrollView, rootName + ".OnAppearing scrollHeight: " + ViewProcessor.scrollHeight);
            ViewProcessor.scrollHeight = 0;
        }
        public void SetBackArrow(bool showBackArrow)
        {
            backArrow.IsVisible = showBackArrow;
        }
        void AddNonDBnodes(ref List<Node> allNodes)
        {

            // If network error: add retransmit node on top.
            // If not in error: retransmit node will not be added and hence no need to remove
            DebugPage.AppendLine(rootName + ".OnAppearing transmissionError: " + UpdatePage.isTransmissionInError);
            if (UpdatePage.isTransmissionInError)
            {
                //allNodes.Add(Node.CreateRetransmitNode());
            }
            // COPY TO OTHER PAGES LATER

            // Create search node
            allNodes.Add(Node.CreateSearchBarNode());
        }
        public void DisplayPage()
        {
            OnAppearing();
        }
        public async void SetContentPage(StackLayout pageLayout)
        {
            ScrollView scrollView = new ScrollView()
            {
                Content = pageLayout
            };

            Content = scrollView;

            // Scroll to the tapped node 
            await(new ViewProcessor()).ScrollToPoint(0, ViewProcessor.scrollHeight, true, scrollView, rootName + ".SetContentPage scrollHeight: " + ViewProcessor.scrollHeight);
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
            // In case of HomePage, there is only one page after the root.
            // Thus, if the back arrow is present, clicking it must return to root page
            // Hence set arrow to invisible
            showBackArrow = false;
            isDisplayingSearchResults = false;
        }
        protected override bool OnBackButtonPressed()
        {
            // ASSUME: Do nothing when hardware back button is pressed
            return true;
        }
    } // END CLASS
}