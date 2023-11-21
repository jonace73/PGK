using PGK.Data;
using PGK.Models;
using PGK.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using static PGK.Models.Node;

namespace PGK.Services
{
    public class ViewProcessor
    {
        public static string pathToJump = string.Empty;
        public static double scrollHeight = 0;
        public static double heigthPerLine = 1;
        public ViewProcessor() { }

        //============================= TEXT PROCESSING =========================================
        public static Node LineFromAssetToNode(string line)
        {
            // Replace apostrophy
            line = line.Replace(MarkerCodes.apostrophe[0], MarkerCodes.apostrophe[1]);

            string[] fields = line.Split(new string[] { MarkerCodes.dbDelimiter }, StringSplitOptions.None);

            Node node = new Node();
            node.LeafTag = fields.ElementAt(0);
            node.Keyword = DecodeQuotes(ExtractKeyword(node.LeafTag));
            node.Header = DecodeQuotes(fields.ElementAt(1));
            node.Answer = DecodeQuotes(fields.ElementAt(2));
            node.nodeType = ExtractNodeType(fields.ElementAt(3).Trim());
            node.IsDeleted = fields.ElementAt(4).Trim().Equals("1");

            // The date in NodesDB.txt has the format dd/month/year hour:minutes.
            // It will be reformatted to be similar to that of the phpMyAdmin which is year/month/dd hour:minutes:seconds
            string dateUpdated = fields.ElementAt(5);

            // INPUT: dateUpdated 23/07/31 7:37:12
            string[] elements = dateUpdated.Split(' ');
            string[] days = elements[0].Split('/');
            string[] times = elements[1].Split(':');
            node.DateUpdated = days[2] + "-" + days[1] + "-" + days[0] + " " + times[0] + ":" + times[1] + ":0";
            // OUTPUT: 2023-07-31 7:37:0

            //DebugPage.AppendLine("Node date: " + node.DateUpdated);

            node.IsAnswerShown = false;
            node.LargeFramePadding = 0;

            return node;
        }
        public static Node LineFromServerToNode(string line)
        {
            //DebugPage.AppendLine("Node.StringToNode");

            Node node = new Node();

            string[] fields = line.Split(MarkerCodes.serverDBdelimiter);
            node.LeafTag = fields.ElementAt(0);
            node.Keyword = DecodeQuotes(ExtractKeyword(node.LeafTag));
            node.Header = DecodeQuotes(fields.ElementAt(1));
            node.Answer = DecodeQuotes(fields.ElementAt(2));
            node.nodeType = ExtractNodeType(fields.ElementAt(3).Trim());
            node.IsDeleted = fields.ElementAt(4).Trim().Equals("1");
            node.DateUpdated = fields.ElementAt(5);

            node.IsAnswerShown = false;
            node.LargeFramePadding = 0;

            //node.Print();

            return node;
        }
        public static string PutLineBreaks(string answer)
        {
            string finalAnswer = answer.Replace(MarkerCodes.doubleNewLine, Environment.NewLine + Environment.NewLine);
            finalAnswer = finalAnswer.Replace(MarkerCodes.singleNewLine, Environment.NewLine);
            return finalAnswer;
        }
        public static string DecodeQuotes(string text)
        {
            text = text.Replace(MarkerCodes.doubleQuote[0], MarkerCodes.doubleQuote[1]);
            text = text.Replace(MarkerCodes.singleQuote[0], MarkerCodes.singleQuote[1]);
            return text.Replace(MarkerCodes.apostrophe[0], MarkerCodes.apostrophe[1]);
        }
        public static bool IsExcluded(string text)
        {
            //DebugPage.AppendLine("Node.IsExcluded text: " + text);
            if (string.IsNullOrEmpty(text)) return true;

            //DebugPage.AppendLine("Node.IsExcluded preChecking.");
            string[] excludedPrepositions = { "since", "for", "than", "from", "through", "among", "in", "to", "as", "into", "at", "like", "of", "on", "onto", "via", "with", "by" };
            string[] excludedArticles = { "a", "an", "the", "is", "was", "are", "were", "has", "had", "been", "have", "some", "all", "that", "did", "do", "does" };
            string[] excludedPronouns = { "he", "she", "it", "they", "i", "we", "you", "me", "us", "her", "him", "them", "hers", "his", "theirs", "my", "our", "your", "their" };
            string[] excludedMorePronouns = { "there", "not", "should", "must", "any", "both", "each", "either", "such", "these", "this", "those", "what", "which", "who", "whom", "whose" };

            if (IsExcludedFrom(excludedPrepositions, text)) return true;
            if (IsExcludedFrom(excludedArticles, text)) return true;
            if (IsExcludedFrom(excludedPronouns, text)) return true;
            if (IsExcludedFrom(excludedMorePronouns, text)) return true;

            // Check for special character
            char[] letters = text.ToCharArray();
            foreach (char letter in letters)
            {
                if (!char.IsLetterOrDigit(letter)) return true;
            }

            //DebugPage.AppendLine("Node.IsExcluded pass.");
            return false;
        }
        private static bool IsExcludedFrom(string[] excludedWords, string text)
        {
            foreach (string word in excludedWords)
            {
                if (text.Equals(word)) return true;
            }

            //DebugPage.AppendLine("Node.IsExcluded pass.");
            return false;
        }
        public static string StripWord(string word)
        {
            string retWord = word.Replace("?", "");
            retWord = retWord.Replace(".", "");
            retWord = retWord.Replace("'", "");
            retWord = retWord.Replace("!", "");
            retWord = retWord.Replace(",", "");
            retWord = retWord.ToLower();
            DebugPage.AppendLine("StripWord: " + retWord);
            return retWord;
        }

        //============================= PATH PROCESSING =========================================
        public static bool IsRoot(string path)
        {
            // segmentSeparator >
            string[] segments = path.Split(MarkerCodes.segmentSeparator);
            //DebugPage.AppendLine("Node.IsRoot length: " + segments.Length);
            return segments.Length == 2;
        }
        public static string ExtractPathRoot(string path)
        {
            // Example: A>B>C-> yields A

            // Remove '-'
            int charPos = path.IndexOf(MarkerCodes.leafKeySeparator);
            path = path.Remove(charPos, 1);

            // Split WRT '>'
            string[] segments = path.Split(MarkerCodes.segmentSeparator);

            // Return the root
            return segments[0];
        }
        public static string ExtractPathSeed(string path)
        {
            // Remove end of a path. Example, A>B->C to A>B->
            string[] splitInTwo = path.Split(MarkerCodes.leafKeySeparator);// separate WRT '-'
            return splitInTwo[0] + MarkerCodes.leafSeparator; // add "->"
        }
        public static string DecrementPathSeed(string pathSeed)
        {
            // RemoveLastSegment of a path. Example, A>B>C-> to A>B->
            string[] splitInTwo = pathSeed.Split(MarkerCodes.leafKeySeparator);
            string[] lastSegments = splitInTwo[0].Split(MarkerCodes.segmentSeparator);
            int nSegmentsLessTwo = lastSegments.Length - 2;

            string retVal = "";
            for (int ii = 0; ii < nSegmentsLessTwo; ii++)
            {
                retVal += lastSegments[ii] + MarkerCodes.segmentSeparator;
            }
            retVal += lastSegments[nSegmentsLessTwo];
            return retVal + MarkerCodes.leafSeparator;
        }
        public static int CountSegments(string pathSeed)
        {
            string[] lastSegments = pathSeed.Split(MarkerCodes.segmentSeparator);
            return lastSegments.Length;
        }
        public static string LeafTagToSeedPath(string LeafTag)
        {
            // Extract leafTag excluding Keyword. Remove '-', put endTag
            // Example, A>B->C to A>B>C->
            string leafTag;
            int charPos = LeafTag.IndexOf(MarkerCodes.leafKeySeparator);
            leafTag = LeafTag.Remove(charPos, 1);
            leafTag += MarkerCodes.leafSeparator;
            return leafTag;
        }
        public static void SavePaths(string pathSeed)
        {
            // Convert leafTag to path
            DebugPage.AppendLine("Node.SavePaths pathSeed: " + pathSeed);

            // Save path to global
            App.globalPath = pathSeed;

            string rootPath = ExtractPathRoot(pathSeed);
            if (rootPath.Equals(HomePage.rootName))
            {
                HomePage.pathSeed = pathSeed; return;
            }
            if (rootPath.Equals(BeliefsPage.rootName))
            {
                BeliefsPage.pathSeed = pathSeed; return;
            }
            if (rootPath.Equals(DivinePage.rootName))
            {
                DivinePage.pathSeed = pathSeed; return;
            }
            if (rootPath.Equals(SocialIssuesPage.rootName))
            {
                SocialIssuesPage.pathSeed = pathSeed; return;
            }
            if (rootPath.Equals(StructurePage.rootName))
            {
                StructurePage.pathSeed = pathSeed; return;
            }
        }

        //============================= SCRATCH PROCESSING =========================================
        public static bool IsRetransmitFrame(Frame tappedHeaderFrame)
        {
            // Extract the leafTag of the node, tappedNode, from the INVISIBLE label's text
            // THEN retrieve (await) the node object from the DB
            StackLayout headderLayout = (StackLayout)tappedHeaderFrame.Content;

            // If keyword is Retransmit fire part of App.OnStart(). Then return
            Label Keyword = (Label)headderLayout.Children[0];
            return Keyword.Text.Equals(UpdatePage.retransmitKeyword);
        }
        public static void SetUseScratchDB(string rootPath, bool setOrNot)
        {
            if (rootPath.Equals(HomePage.rootName))
            {
                HomePage.useScratchDB = setOrNot; return;
            }
            if (rootPath.Equals(BeliefsPage.rootName))
            {
                BeliefsPage.useScratchDB = setOrNot; return;
            }
            if (rootPath.Equals(DivinePage.rootName))
            {
                DivinePage.useScratchDB = setOrNot; return;
            }
            if (rootPath.Equals(SocialIssuesPage.rootName))
            {
                SocialIssuesPage.useScratchDB = setOrNot; return;
            }
            if (rootPath.Equals(StructurePage.rootName))
            {
                StructurePage.useScratchDB = setOrNot; return;
            }
        }
        private static void ScratchAddFrame(Frame frame, string rootPath)
        {
            if (rootPath.Equals(HomePage.rootName))
            {
                HomePage.scratchNodeFrameDB.Add(frame); return;
            }
            if (rootPath.Equals(BeliefsPage.rootName))
            {
                BeliefsPage.scratchNodeFrameDB.Add(frame); return;
            }
            if (rootPath.Equals(DivinePage.rootName))
            {
                DivinePage.scratchNodeFrameDB.Add(frame); return;
            }
            if (rootPath.Equals(SocialIssuesPage.rootName))
            {
                SocialIssuesPage.scratchNodeFrameDB.Add(frame); return;
            }
            if (rootPath.Equals(StructurePage.rootName))
            {
                StructurePage.scratchNodeFrameDB.Add(frame); return;
            }
        }
        private static void ScratchNodeFramesReset(string rootPath)
        {
            if (rootPath.Equals(HomePage.rootName))
            {
                HomePage.scratchNodeFrameDB = new List<Frame>(); return;
            }
            if (rootPath.Equals(BeliefsPage.rootName))
            {
                BeliefsPage.scratchNodeFrameDB = new List<Frame>(); return;
            }
            if (rootPath.Equals(DivinePage.rootName))
            {
                DivinePage.scratchNodeFrameDB = new List<Frame>(); return;
            }
            if (rootPath.Equals(SocialIssuesPage.rootName))
            {
                SocialIssuesPage.scratchNodeFrameDB = new List<Frame>(); return;
            }
            if (rootPath.Equals(StructurePage.rootName))
            {
                StructurePage.scratchNodeFrameDB = new List<Frame>(); return;
            }
        }
        public static ScrollView ScratchFramesToScrollView(List<Frame> scratchFrames)
        {
            // Create the page's stack layout. DON'T PUT start, center or end IN pageLayout SO AS NOT TO RESTRICT THE VIEW
            StackLayout pageLayout = new StackLayout();

            // Add its children/nodes
            scrollHeight = 0;
            bool addHeight = true;
            foreach (Frame frame in scratchFrames)
            {
                // add Frames to pageLayout
                pageLayout.Children.Add(frame);

                // Add frame height
                if (pathToJump.Length > 0)
                {
                    if (addHeight)
                    {
                        scrollHeight += AddFrameHeight(frame, ref addHeight);
                    }
                }
            }
            scrollHeight *= heigthPerLine;
            DebugPage.AppendLine("ScratchFramesToScrollView scrollHeight: " + scrollHeight);

            // Scroll to view all nodes
            ScrollView scrollView = new ScrollView()
            {
                Content = pageLayout
            };

            return scrollView;
        }
        private static double AddFrameHeight(Frame nodeFrame, ref bool willAddHeight)
        {
            // Consider DIFFERENT TYPES of frames inside a stackLayout
            // If the frame is a search bar
            if (nodeFrame.Content is SearchBar)
            {
                DebugPage.AppendLine("ViewProcessor.AddFrameHeight is SearchBar");
                return nodeFrame.Height;
            }

            // ELSE 
            double heightToAdd;
            StackLayout innerLayout = nodeFrame.Content as StackLayout;
            Frame tappedHeaderFrame = innerLayout.Children[0] as Frame;
            StackLayout headderLayout = tappedHeaderFrame.Content as StackLayout;
            Label invisibleLeafTagLabel = headderLayout.Children[2] as Label;
            string pathFrom = invisibleLeafTagLabel.Text;
            if (pathToJump.Equals(pathFrom))
            {
                heightToAdd = 0;
                willAddHeight = false;
            }
            else
            {
                heightToAdd = nodeFrame.Height + Node.topMargin;
            }
            return heightToAdd;
        }

        public static async Task<ScrollView> CreatePageContent(string pathSeed)
        {

            // Retrieve then display nodes. DON'T put await inside NodesToScrollView. OTHERWISE, some nodes will not appear
            List<Node> allNodes = await NodeDatabase.DBnodes.GetBranchesAsync(pathSeed);
            if (allNodes == null) return null;
            Node.SortNodes(ref allNodes);

            // Reset NodeType to only be either Answer or Branch
            ResetNodeType(ref allNodes);

            // Translate nodes to scrollView elements
            // AND copy them to scratchNodeFrameDB
            return NodesToScrollView(ExtractPathRoot(pathSeed), allNodes);
        }

        //============================= ELEMENT PROCESSING =========================================
        public static string ExtractKeyword(string leafTag)
        {
            return leafTag.Split(MarkerCodes.segmentSeparator).Last();
        }
        public static string GetKeywordNode(Frame nodeFrame)
        {
            StackLayout innerLayout = nodeFrame.Content as StackLayout;
            Frame tappedHeaderFrame = innerLayout.Children[0] as Frame;
            StackLayout headderLayout = tappedHeaderFrame.Content as StackLayout;
            return (headderLayout.Children[0] as Label).Text;
        }
        private static void ResetNodeType(ref List<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                node.nodeType = node.nodeType & Node.NodeType.AnswerAsText;
            }
        }
        public static void SetBackArrow(string rootPath, bool showBackArrow)
        {
            DebugPage.AppendLine("Node.SetBackArrow rootPath: " + rootPath);
            if (rootPath.Equals(HomePage.rootName))
            {
                HomePage.showBackArrow = showBackArrow; return;
            }
            if (rootPath.Equals(BeliefsPage.rootName))
            {
                BeliefsPage.showBackArrow = showBackArrow; return;
            }
            if (rootPath.Equals(DivinePage.rootName))
            {
                DivinePage.showBackArrow = showBackArrow; return;
            }
            if (rootPath.Equals(SocialIssuesPage.rootName))
            {
                SocialIssuesPage.showBackArrow = showBackArrow; return;
            }
            if (rootPath.Equals(StructurePage.rootName))
            {
                StructurePage.showBackArrow = showBackArrow; return;
            }
        }
        public static ScrollView NodesToScrollView(string rootPath, List<Node> nodes)
        {

            // Create the page's stack layout. DON'T PUT start, center or end IN pageLayout SO AS NOT TO RESTRICT THE VIEW
            StackLayout pageLayout = new StackLayout();

            // Reset ResetScratchNodeFrames, i.e., make NEW list of frames
            ScratchNodeFramesReset(rootPath);

            // Add its children/nodes to pageLayout and SCRATCH
            scrollHeight = 0;
            bool addHeight = true;
            foreach (Node node in nodes)
            {
                // Save Frames to SCRATCH list
                Frame nodeFrame = node.CreateNodeFrame();
                if (nodeFrame == null) break;

                // If is an imageAnswer type node increase frame height by image height
                if (node.nodeType == NodeType.AnswerAsImage)
                {
                    string[] imageAnswer = node.Answer.Split(new string[] { MarkerCodes.imageSeparator }, StringSplitOptions.None);

                    StackLayout innerLayout = (StackLayout)nodeFrame.Children[0];
                    // This will PREPARE the frame height is image (innerLayout.Children[1]) should be presented
                    innerLayout.Children[1].HeightRequest = node.IsAnswerShown ? Int32.Parse(imageAnswer[0]) : 0;
                }

                if (pathToJump.Length > 0)
                {
                    if (addHeight)
                    {
                        scrollHeight += AddFrameHeight(nodeFrame, ref addHeight);
                    }
                }
                ScratchAddFrame(nodeFrame, rootPath);
                pageLayout.Children.Add(nodeFrame);
            }

            // Scale scroll height
            scrollHeight *= heigthPerLine;

            // Scroll to view all nodes
            ScrollView scrollView = new ScrollView()
            {
                Content = pageLayout
            };

            DebugPage.AppendLine("ViewProcessor.NodesToScrollView scrollHeight: " + scrollHeight);

            return scrollView;
        }
        private static void DisplayPage(Frame tappedHeaderFrame, string rootPath)
        {
            DebugPage.AppendLine("Node.DisplayPage rootPath: " + rootPath);

            // Find the StackLayout that contains all the node frame
            StackLayout innerLayout = tappedHeaderFrame.Parent as StackLayout;
            Frame nodeFrame = innerLayout.Parent as Frame;
            StackLayout pageLayout = nodeFrame.Parent as StackLayout;
            ScrollView scrollView = pageLayout.Parent as ScrollView;

            // Find the page that contains pageLayout and call its OnAppearing through  DisplayPage()
            if (rootPath.Equals(HomePage.rootName))
            {
                (scrollView.Parent as HomePage).DisplayPage(); return;
            }
            if (rootPath.Equals(BeliefsPage.rootName))
            {
                (scrollView.Parent as BeliefsPage).DisplayPage(); return;
            }
            if (rootPath.Equals(DivinePage.rootName))
            {
                (scrollView.Parent as DivinePage).DisplayPage(); return;
            }
            if (rootPath.Equals(SocialIssuesPage.rootName))
            {
                (scrollView.Parent as SocialIssuesPage).DisplayPage(); return;
            }
            if (rootPath.Equals(StructurePage.rootName))
            {
                (scrollView.Parent as StructurePage).DisplayPage(); return;
            }
        }
        public static void RefreshContentPage(Frame tappedHeaderFrame, string rootPath)
        {
            DebugPage.AppendLine("ViewProcessor.RefreshContentPage rootPath: " + rootPath);

            // Find the StackLayout that contains all the node frame
            StackLayout innerLayout = tappedHeaderFrame.Parent as StackLayout;
            Frame nodeFrame = innerLayout.Parent as Frame;
            StackLayout pageLayout = nodeFrame.Parent as StackLayout;
            ScrollView scrollView = pageLayout.Parent as ScrollView;

            // Set scroll height
            NodeHeight(pageLayout);

            // Find the page that contains pageLayout and call its OnAppearing through  DisplayPage()
            if (rootPath.Equals(HomePage.rootName))
            {
                (scrollView.Parent as HomePage).SetContentPage(pageLayout); return;
            }
            if (rootPath.Equals(BeliefsPage.rootName))
            {
                (scrollView.Parent as BeliefsPage).SetContentPage(pageLayout); return;
            }
            if (rootPath.Equals(DivinePage.rootName))
            {
                (scrollView.Parent as DivinePage).SetContentPage(pageLayout); return;
            }
            if (rootPath.Equals(SocialIssuesPage.rootName))
            {
                (scrollView.Parent as SocialIssuesPage).SetContentPage(pageLayout); return;
            }
            if (rootPath.Equals(StructurePage.rootName))
            {
                (scrollView.Parent as StructurePage).SetContentPage(pageLayout); return;
            }
        }
        public static bool ShowBranches(Frame tappedHeaderFrame, string LeafTag)
        {

            // Set back arrow
            string rootPath = ViewProcessor.ExtractPathRoot(LeafTag);
            ViewProcessor.SetBackArrow(rootPath, true);

            // Save new path to pathSeed and App.globalPath
            string nextPath = ViewProcessor.LeafTagToSeedPath(LeafTag);
            DebugPage.AppendLine("Node.ShowNextBranch nextPath: " + nextPath);
            ViewProcessor.SavePaths(nextPath);

            // Show the shell and hence the branches 
            DisplayPage(tappedHeaderFrame, rootPath);

            return true;
        }
        public async Task<bool> ScrollToPoint(double X, double Y, bool animate, ScrollView scrollViewScratch, string comment)
        {

            // Set scrollView
            if (pathToJump.Length > 0)
            {
                await scrollViewScratch.ScrollToAsync(X, Y, animate);

                // Reset path to jump to
                pathToJump = string.Empty;
                scrollHeight = 0;
                DebugPage.AppendLine("ViewProcessor.ScrollToPoint " + comment);
            }

            return true;
        }
        public static void ArrowResponse(ref string pathSeed, ref bool showBackArrow)
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
        public static void NodeHeight(StackLayout pageLayout)
        {
            if (pathToJump.Length <= 0) return;

            scrollHeight = 0;
            bool willAddHeight = true;
            foreach (Frame nodeFrame in pageLayout.Children)
            {
                if (!willAddHeight) continue;

                scrollHeight += AddFrameHeight(nodeFrame, ref willAddHeight);
            }

            // Scale scroll height
            scrollHeight *= heigthPerLine;
            DebugPage.AppendLine("ViewProcessor.NodeHeight scrollHeight: " + scrollHeight);
        }

    } // END CLASS
}
