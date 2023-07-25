using PGK.Data;
using PGK.Views;
using PGK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;
using Xamarin.Forms;
using static PGK.Models.Node;

namespace PGK.Services
{
    public class SpanCodeConverter : IValueConverter
    {
        public enum Action
        {
            Prompt, Weblink, NodeLink, Text // Text MUST be last
        }

        public static string[] startingDelimiters = { MarkerCodes.promptMarker[0], MarkerCodes.weblink[0], MarkerCodes.linkOutMarker[0] };
        public static string[] midDelimiters = { MarkerCodes.promptMarker[1], MarkerCodes.weblink[1], MarkerCodes.linkOutMarker[1] };
        public static string[] endingDelimiters = { MarkerCodes.promptMarker[2], MarkerCodes.weblink[2], MarkerCodes.linkOutMarker[2] };

        public static Action[] actionTypes = { Action.Prompt, Action.Weblink, Action.NodeLink };
        //              actionTypes MUST exactly correspond to the delimiters
        public SpanCodeConverter() { }

        public object Convert(object value, Type targetType, object sourceNode, CultureInfo culture)
        {
            var formatted = new FormattedString();

            // In CreateSpan, a span is added for each breaking component which if a link will have gesture recognizer
            foreach (var item in ProcessString(value as string, sourceNode as string))
                formatted.Spans.Add(CreateSpan(item));

            return formatted;
        }
        public object CodesToSpans(object value, string sourceNode)
        {
            try
            {
                return Convert(value, null, sourceNode, null);
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("SpanCodeConverter.CodesToSpans Error: " + ex.Message);
            }
            return null;
        }
        private Span CreateSpan(StringSection section)
        {
            var span = new Span();

            // If PROMPT
            if (section.action == Action.Prompt)
            {
                span.GestureRecognizers.Add(new TapGestureRecognizer()
                {
                    Command = promptCommand,
                    CommandParameter = section.Link
                });

                span.Text = section.Text;
                span.TextColor = Color.Red;
                return span;
            }

            // If nodeLink
            if (section.action == Action.NodeLink)
            {
                span.GestureRecognizers.Add(new TapGestureRecognizer()
                {
                    Command = nodeLinkCommand,
                    CommandParameter = section.Link
                });

                span.Text = section.Text;
                span.TextColor = Color.Black;// Jump to same page might be resolved LATER
                return span;
            }

            // If LINK
            if (section.action == Action.Weblink)
            {
                span.GestureRecognizers.Add(new TapGestureRecognizer()
                {
                    Command = webLinkCommand,
                    CommandParameter = section.Link
                });
                span.Text = section.Text;
                span.TextColor = Color.Blue;
                return span;
            }//*/

            // If TEXT
            span.Text = section.Text;
            span.TextColor = Color.Black;
            return span;
        }

        public IList<StringSection> ProcessString(string rawText, string sourceNode)
        {
            // Create a list of sections
            var sections = new List<StringSection>();

            // List of delimiters 
            int numberDelimeters = startingDelimiters.Length;

            // Starting point to search for the delimiters
            int searchBeginingIndex = 0;

            //DebugPage.AppendLine("Node.CreateSpan startingTextIndex: "+ startingTextIndex + " numberDelimeters: " + numberDelimeters + " rawText: " + rawText);

            while (true)
            {
                // Loop to find the nearest index
                int startingIndex = int.MaxValue;
                int ithDelimiter = -1;
                for (int ii = 0; ii < numberDelimeters; ii++)
                {
                    int index = rawText.IndexOf(startingDelimiters[ii], searchBeginingIndex);
                    if (index < 0) continue;
                    if (index < startingIndex)
                    {
                        startingIndex = index;
                        ithDelimiter = ii;
                    }
                }

                // IF all text, i.e., no delimiter
                if (ithDelimiter < 0)
                {
                    sections.Add(new StringSection(rawText.Substring(searchBeginingIndex), actionTypes.Length, sourceNode));// Last action type if Text
                    return sections;
                }

                // If not starting with a delimiter
                if (startingIndex > 0)
                {
                    if (startingIndex > searchBeginingIndex) // Don't process in case of startingIndex == searchBeginingIndex
                        sections.Add(new StringSection(rawText.Substring(searchBeginingIndex, startingIndex - searchBeginingIndex), actionTypes.Length, sourceNode));// Last action type if Text
                }

                //DebugPage.AppendLine("Node.CreateSpan ithDelimiter: " + ithDelimiter + " startingIndex: " + startingIndex);

                // ELSE find the corresponding section
                int indexBeginContent = startingIndex + startingDelimiters[ithDelimiter].Length;
                int endingIndex = rawText.IndexOf(endingDelimiters[ithDelimiter], indexBeginContent);
                string midSection = rawText.Substring(indexBeginContent, endingIndex - indexBeginContent);
                sections.Add(new StringSection(midSection, ithDelimiter, sourceNode));

                //DebugPage.AppendLine("Node.CreateSpan indexBeginContent: " + indexBeginContent + " endingIndex: " + endingIndex + " midSection: " + midSection);

                // If the new starting point is equal to rawText.Length THEN return
                searchBeginingIndex = endingIndex + endingDelimiters[ithDelimiter].Length;
                if (searchBeginingIndex >= rawText.Length) return sections;

                //DebugPage.AppendLine("Node.CreateSpan startingTextIndex: " + searchBeginingIndex);

                // ELSE continue searching
            }

            //return sections;
        }

        public class StringSection
        {
            public string Text { get; set; }
            public string Link { get; set; }
            public string sourceNode { get; set; }
            public Action action { get; set; }
            public StringSection(string itemValue, int indexAction, string sourceNode)
            {
                //DebugPage.AppendLine("SpanCodeConverter.StringSection indexAction: " + indexAction);

                // Text action
                if (indexAction >= actionTypes.Length)
                {
                    this.action = Action.Text;
                    Text = itemValue;
                    return;
                }

                // Input the action
                this.action = actionTypes[indexAction];
                if (action == Action.Prompt)
                {
                    Text = itemValue.Substring(0, itemValue.IndexOf(midDelimiters[0]));// verse number only
                    Link = itemValue; // Contains verse number, midDelimiter, and verse
                    return;
                }
                if (action == Action.NodeLink)
                {
                    string[] linkSections = itemValue.Split(new string[] { MarkerCodes.linkOutMarker[1] }, StringSplitOptions.None);
                    Link = linkSections[0];
                    Text = linkSections[1];
                    this.sourceNode = sourceNode;
                    return;
                }
                if (action == Action.Weblink)
                {
                    string[] linkSections = itemValue.Split('\"');
                    Link = linkSections[1]; // Extract in-between double quotes. This is the URL
                    int indexMid = itemValue.IndexOf(midDelimiters[indexAction]);
                    int indexAfterMid = indexMid + midDelimiters[indexAction].Length;
                    Text = itemValue.Substring(indexAfterMid);// The one to be tapped
                    return;
                }
            }
        }

        private ICommand promptCommand = new Command<string>(async (linkText) =>
        {
            string[] verse = linkText.Split(new string[] { midDelimiters[0] }, StringSplitOptions.None);
            await App.Current.MainPage.DisplayAlert(verse[0], verse[1], "OK");
        });

        private ICommand webLinkCommand = new Command<string>((url) =>
        {
            Device.OpenUri(new Uri(url));
        });

        private ICommand nodeLinkCommand = new Command<string>(async (leafTag) =>
        {
            DebugPage.AppendLine("SpanCodeConverter.nodelinkCommand linkSource: " + leafTag);
            try
            {
                // Read node from DB
                string rootPath = ViewProcessor.ExtractPathRoot(leafTag);
                Node tappedNode = await NodeDatabase.DBnodes.GetChildAsync(leafTag);

                // Set node as shown
                string pathSeed;
                if (isGivenType(tappedNode.nodeType, NodeType.Answer))
                {
                    // Toggle IsAnswerShown
                    tappedNode.IsAnswerShown = true;
                    tappedNode.LargeFramePadding = 5;

                    // Save this node to rootPath's DB to REGISTER the change in LargeFramePadding
                    // NOT registering will cause an ERROR as the answer have no large frame
                    int numberSaved = await NodeDatabase.DBnodes.SaveNodeAsync(tappedNode);
                    DebugPage.AppendLine("SpanCodeConverter.nodelinkCommand numberSaved: " + numberSaved);
                    pathSeed = ViewProcessor.ExtractPathSeed(leafTag);
                }
                else
                {
                    pathSeed = ViewProcessor.LeafTagToSeedPath(leafTag);
                }

                // Set back arrow if NOT root            
                ViewProcessor.SetBackArrow(rootPath, !ViewProcessor.IsRoot(pathSeed));

                // Save new path to pathSeed and App.globalPath
                ViewProcessor.SavePaths(pathSeed);

                // useScratchDB to false
                ViewProcessor.SetUseScratchDB(rootPath, false);

                // As we are moving away from HomePage, call AppShell.LeaveUpdatePage
                // where nodes are read from DB, translated to scrollView elements
                // AND their corresponding frames are copied to scratchNodeFrameDB
                var AppShellInstance = Xamarin.Forms.Shell.Current as AppShell;
                AppShellInstance.DisplayPage(rootPath);//*/

            }
            catch(Exception ex)
            {
                DebugPage.AppendLine("SpanCodeConverter.nodelinkCommand ERROR: " + ex.Message);
            }
        });
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
