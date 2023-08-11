using PGK.Services;
using PGK.Views;
using System.Collections.Generic;
using Xamarin.Forms;

namespace PGK
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public Dictionary<string, ShellContent> rootPathToShellDictionary;
        public AppShell()
        {
            InitializeComponent();
            // Use the keys as the ROOT of paths
            rootPathToShellDictionary = new Dictionary<string, ShellContent>(){
    {"Home", HomeShell}, {"Beliefs", BeliefsShell}, {"Divine", DivineShell}, {"Social", SocialIssuesShell}, {"Authority", StructureShell}, {"Update", UpdateShell } };//*/
            
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
        }
        public void ShowUpdatePage()
        {
            DebugPage.AppendLine("AppShell.ShowUpdatePage");

            UpdateShell.IsVisible = true;
            HomeShell.IsVisible = false;
            DivineShell.IsVisible = false;
            StructureShell.IsVisible = false;
            BeliefsShell.IsVisible = false;
            SocialIssuesShell.IsVisible = false;

            // Set debug tab based on debug status
            DebugShell.IsVisible = DebugPage.isInDebug;

            // Show updateShell
            Current.CurrentItem = rootPathToShellDictionary["Update"];

            OnAppearing();

        }
        public void LeaveUpdatePage()
        {
            UpdateShell.IsVisible = false;
            HomeShell.IsVisible = true;
            DivineShell.IsVisible = true;
            StructureShell.IsVisible = true;
            BeliefsShell.IsVisible = true;
            SocialIssuesShell.IsVisible = true;//*/

            // Set debug tab based on debug status
            DebugShell.IsVisible = DebugPage.isInDebug;

            // Set the ShellContent to be displayed first 
            DebugPage.AppendLine("AppShell.LeaveUpdate pathRoot: " + ViewProcessor.ExtractPathRoot(App.globalPath));
            Current.CurrentItem = rootPathToShellDictionary[ViewProcessor.ExtractPathRoot(App.globalPath)];
            
            OnAppearing();//*/
        }
        public void DisplayPage(string rootPath)
        {
            // Set the ShellContent to be displayed first 
            DebugPage.AppendLine("AppShell.DisplayPage rootPath: " + rootPath);
            Current.CurrentItem = rootPathToShellDictionary[rootPath];

            OnAppearing();//*/
        }
    }
}
