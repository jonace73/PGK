using PGK.Data;
using PGK.Services;
using PGK.Views;
using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using Color = Xamarin.Forms.Color;

namespace PGK.Models
{
    public class Node
    {
        public enum NodeType
        {
            Branch = 0b_0000_0000,  // 0
            Answer = 0b_0000_0001,  // 1
            SearchBar = 0b_0000_0010,  // 2
            SearchResult = 0b_0000_0100  // 4
        }

        [PrimaryKey]
        public string LeafTag { get; set; }
        public string Keyword { get; set; }
        public string Header { get; set; }
        public string Answer { get; set; }
        public string DateUpdated { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsAnswerShown { get; set; }
        public int LargeFramePadding { get; set; }
        public NodeType nodeType { get; set; }
        public static int topMargin = 5;

        //============================= CREATION =========================================
        public Node()
        {
            LeafTag = string.Empty;
            Keyword = string.Empty;
            Header = string.Empty;
            Answer = string.Empty;
            IsDeleted = false;
            DateUpdated = string.Empty;
            nodeType = NodeType.Branch;

            IsAnswerShown = false;//
            LargeFramePadding = 0;
        }
        public Frame CreateNodeFrame()
        {

            // Create search bar
            if (isGivenType(nodeType, NodeType.SearchBar))
            {
                return CreateSearchBarFrame();
            }

            // Create retransmit and other nodeFrame
            Span keywordSpan;
            if (Keyword.Equals(UpdatePage.retransmitKeyword))
            {
                keywordSpan = new Span { Text = Keyword + MarkerCodes.keyDelimiter, TextColor = Color.Red, FontAttributes = FontAttributes.Bold };
            }
            else
            {
                keywordSpan = new Span { Text = Keyword, TextColor = Color.LightYellow, FontAttributes = FontAttributes.Bold };
            }
            return CreateAllTypeNodeFrame(keywordSpan);
        }
        public Frame CreateAllTypeNodeFrame(Span keywordSpan)
        {
            //DebugPage.AppendLine("Node.CreateAllTypeNodeFrame");

            // Answer label is invisible.
            // This is used for QUICK search of the tapped header to activate its answer,
            // AND for QUICK sorting nodes to display
            Label keywordLabel = new Label
            {
                Text = Keyword,
                IsVisible = false
            };

            // Create the header texts with different formats Node.keywordAdditionalDelimeter
            var formattedString = new FormattedString();
            formattedString.Spans.Add(keywordSpan);
            formattedString.Spans.Add(new Span { Text = Header, TextColor = Color.White });

            // Header label
            Label headerLabel = new Label
            {
                FormattedText = formattedString
            };

            // Invisible leafTag
            Label invisibleLeafTagLabel = new Label
            {
                Text = LeafTag,
                IsVisible = false
            };

            // Create Header stackLayout
            StackLayout headderLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal
            };
            headderLayout.Children.Add(keywordLabel);
            headderLayout.Children.Add(headerLabel);
            headderLayout.Children.Add(invisibleLeafTagLabel);

            // Create header frame that contains headderLayout
            bool nodeColor = isGivenType(nodeType, NodeType.Answer) || isGivenType(nodeType, NodeType.SearchBar);
            Frame headerFrame = new Frame
            {
                BorderColor = Color.Gray,
                BackgroundColor = nodeColor ? Color.FromHex("03989e") : Color.DarkKhaki,
                HasShadow = true,
                CornerRadius = 10,
                Padding = 10,
                Content = headderLayout
            };

            // Add gesture recognizer to nodeFrame
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += async (object sender, EventArgs args) =>
            {
                Frame tappedHeaderFrame = (Frame)sender;
                if (ViewProcessor.IsRetransmitFrame(tappedHeaderFrame))
                {
                    TapResponseRetransmit();
                    return;
                }

                if (isGivenType(nodeType, NodeType.SearchResult))
                {
                    await this.TapResponseSearchResult();// 
                    return;
                }

                // SearBar response is in CreateSearchBarFrame -> SearchBarResponse()
                // Thus, the TapResponseCommon() is just for Answer and Branch
                await TapResponseCommon(tappedHeaderFrame);
            };
            headerFrame.GestureRecognizers.Add(tapGestureRecognizer);//*/

            // Answer label
            Label answer = CreateAnswer();

            // Create stackLayout to enclose header and answer
            StackLayout innerLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical
            };
            innerLayout.Children.Add(headerFrame);
            innerLayout.Children.Add(answer);

            // Create the outer frame with innerLayout inside
            Frame nodeFrame = new Frame
            {
                Margin = new Thickness(5, topMargin, 5, 0),
                BorderColor = Color.Gray,
                BackgroundColor = Color.LightYellow,
                HasShadow = true,
                CornerRadius = 10,
                Padding = LargeFramePadding,
                Content = innerLayout
            };

            return nodeFrame;
        }
        public Frame CreateSearchBarFrame()
        {
            SearchBar searchBar = new SearchBar
            {
                Placeholder = HomePage.placeholder,
                PlaceholderColor = Color.Orange,
                TextColor = Color.Orange,
                HorizontalTextAlignment = TextAlignment.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(SearchBar))
            };

            // Create header frame that contains stackLayout
            Frame nodeFrame = new Frame
            {
                Margin = new Thickness(5, 5, 5, 0),
                BorderColor = Color.Gray,
                BackgroundColor = Color.LightYellow,
                HasShadow = true,
                CornerRadius = 10,
                Padding = 0,
                Content = searchBar
            };

            // Add response method
            searchBar.SearchButtonPressed += async (object sender, EventArgs args) =>
            {
                await TapResponseSearchBar(sender, args);
            };

            return nodeFrame;
        }
        private Label CreateAnswer()
        {
            // Replace dash code by actual dash
            this.Answer = Answer.Replace(MarkerCodes.Dash[0], MarkerCodes.Dash[1]);
            Label answer;
            if (this.nodeType == NodeType.Branch || nodeType == NodeType.SearchBar)
            {
                answer = new Label
                {
                    Text = "", //Answer,
                    IsVisible = IsAnswerShown
                };

                return answer;
            }

            //DebugPage.AppendLine("Node.CreateAnswer nodeType: " + nodeType);

            SpanCodeConverter spanCodeConverter = new SpanCodeConverter();
            FormattedString formatted = spanCodeConverter.CodesToSpans(ViewProcessor.PutLineBreaks(Answer), this.LeafTag) as FormattedString;
            if (formatted == null)
            {
                answer = new Label
                {
                    Text = ViewProcessor.PutLineBreaks(Answer), //Answer,
                    IsVisible = IsAnswerShown
                };
                return answer;
            }

            answer = new Label
            {
                Text = ViewProcessor.PutLineBreaks(Answer), //Answer,
                IsVisible = IsAnswerShown,
                FormattedText = formatted
            };

            return answer;
        }
        private string removeHeaderVerse()
        {
            int indexBeginSearch = 0;
            string header = string.Empty;
            while (indexBeginSearch < Header.Length)
            {
                // Find starting indes
                int indexStart = Header.IndexOf(SpanCodeConverter.startingDelimiters[0], indexBeginSearch);
                //DebugPage.AppendLine("Node.removeHeaderVerse indexStart: "+ indexStart);

                // No verse found
                if (indexStart < 0)
                {
                    //DebugPage.AppendLine("Node.removeHeaderVerse indexStart: " + indexBeginSearch);
                    // Copy heading text
                    header += Header.Substring(indexBeginSearch);
                    return header;
                }

                // Copy trailing text
                if (indexStart > indexBeginSearch)
                {
                    header += Header.Substring(indexBeginSearch, indexStart - indexBeginSearch);
                }

                // Mid index
                int indexStartVerse = indexStart + SpanCodeConverter.startingDelimiters[0].Length;
                int indexMid = Header.IndexOf(SpanCodeConverter.midDelimiters[0], indexStartVerse);

                // Copy mid text
                header += Header.Substring(indexStartVerse, indexMid - indexStartVerse);
                //DebugPage.AppendLine("Node.removeHeaderVerse indexMid: " + indexMid +" Verse: " + Header.Substring(indexStartVerse, indexMid - indexStartVerse));

                // End index
                int indexEnd = Header.IndexOf(SpanCodeConverter.endingDelimiters[0], indexMid + SpanCodeConverter.midDelimiters[0].Length);
                indexBeginSearch = indexEnd + SpanCodeConverter.endingDelimiters[0].Length;
                //DebugPage.AppendLine("Node.removeHeaderVerse NEW indexBeginSearch: " + indexBeginSearch);
            }

            return header;
        }

        //============================= MISC =========================================
        public static void SortNodes(ref List<Node> nodes)
        {
            nodes.Sort(new Comparison<Node>((x, y) => (x.Keyword.CompareTo(y.Keyword))));
        }
        public async Task<bool> ToggleAnswer(Frame tappedHeaderFrame)
        {
            // Toggle Frame padding
            LargeFramePadding = IsAnswerShown ? 0 : 5;
            IsAnswerShown = !IsAnswerShown;

            // Save the toggle
            int numberUpdated = await NodeDatabase.DBnodes.UpdateNodeAsync(this);
            if (numberUpdated < 0) return false;

            // Set padding on node frame
            StackLayout innerLayout = (StackLayout)tappedHeaderFrame.Parent;
            Frame nodeFrame = (Frame)innerLayout.Parent;
            nodeFrame.Padding = LargeFramePadding;

            // Show answer Children[1]
            Label answer = innerLayout.Children[1] as Label;
            answer.IsVisible = IsAnswerShown;

            return true;
        }
        public static bool isGivenType(NodeType toTestType, NodeType referenceType)
        {
            return (toTestType & referenceType) == referenceType;
        }
        public void Print()
        {
            DebugPage.AppendLine("Node.Print LeafTag:" + LeafTag + " Header: " + Header + " Answer: " + Answer + " nodeType " + nodeType + " IsDeleted " + IsDeleted + " DateUpdated " + DateUpdated);
        }
        List<Node> RearrangeNodes(List<Node> allNodes, string wordToUse)
        {
            wordToUse = wordToUse.ToLower();
            //DebugPage.AppendLine("wordToUse: " + wordToUse);
            List<Node> keyNodes= new List<Node>();
            List<Node> headerNodes = new List<Node>();
            foreach (Node node in allNodes)
            {
                if (node.Keyword.ToLower().Contains(wordToUse))
                {
                    //DebugPage.AppendLine("Keyword: " + node.Keyword);
                    keyNodes.Add(node);
                }
                else
                {
                    headerNodes.Add(node);
                }
            }

            // Join the nodes
            keyNodes.AddRange(headerNodes);
            return keyNodes;
        }

        //============================= RESPONSES =========================================
        public void TapResponseRetransmit()
        {
            var AppShellInstance = Xamarin.Forms.Shell.Current as AppShell;
            UpdatePage.appLastCheckTime = DateTime.UtcNow;
            UpdatePage.isUpdateOnGoing = true;

            // Used by UpdatePage.RotateImage() to continue rotating until UpdatePage.isUpdateOnGoing becomes false
            UpdatePage.transmissionError = false;
            UpdatePage.isDownloadFromServer = true;
            AppShellInstance.ShowUpdatePage();
        }
        public async Task<bool> TapResponseCommon(Frame tappedHeaderFrame)
        {
            // Extract the leafTag of the node, tappedNode, from the INVISIBLE label's text
            // THEN retrieve (await) the node object from the DB
            StackLayout headderLayout = (StackLayout)tappedHeaderFrame.Content;

            Label invisibleLeafTagLabel = (Label)headderLayout.Children[2];
            string leafTag = invisibleLeafTagLabel.Text;//
            Node tappedNode = await NodeDatabase.DBnodes.GetChildAsync(leafTag);
            if (tappedNode == null) return false;//*/

            DebugPage.AppendLine("Node.TapResponseCommon leafTag: " + leafTag);

            // Toggle Answer (and outermost frame) and save to DB; tappedNode.IsAnswerNode
            if (isGivenType(tappedNode.nodeType, NodeType.Answer))
            {
                await tappedNode.ToggleAnswer(tappedHeaderFrame);

                // Set path of node to jump to,
                ViewProcessor.pathToJump = tappedNode.LeafTag;

                // Refresh the scrollView where the node is
                ViewProcessor.RefreshContentPage(tappedHeaderFrame, ViewProcessor.ExtractPathRoot(leafTag));
            }
            else // Branch
            {
                // Go to the next branch
                ViewProcessor.ShowBranches(tappedHeaderFrame, LeafTag);
            }

            return true;

        }
        public async Task<bool> TapResponseSearchResult()
        {
            // 
            DebugPage.AppendLine("Node.TapResponseSearchResult");

            // Set this node as common 
            string rootPath = ViewProcessor.ExtractPathRoot(this.LeafTag);
            string pathSeed;
            if (isGivenType(nodeType, NodeType.Answer))
            {
                // Toggle IsAnswerShown
                IsAnswerShown = isGivenType(nodeType, NodeType.Answer);
                this.LargeFramePadding = IsAnswerShown ? 5 : 0;

                // Save this node to rootPath's DB to REGISTER the change in LargeFramePadding
                // NOT registering will cause an error as the answer have no large frame
                int numberSaved = await NodeDatabase.DBnodes.SaveNodeAsync(this);
                if (numberSaved < 0) return false;
                pathSeed = ViewProcessor.ExtractPathSeed(this.LeafTag);
            }
            else
            {
                pathSeed = ViewProcessor.LeafTagToSeedPath(LeafTag);
            }

            // Set back arrow if NOT root            
            ViewProcessor.SetBackArrow(rootPath, !ViewProcessor.IsRoot(pathSeed));

            // Save new path to pathSeed and App.globalPath
            ViewProcessor.SavePaths(pathSeed);

            // useScratchDB to false
            ViewProcessor.SetUseScratchDB(rootPath, false);

            // Set path to jump to
            ViewProcessor.pathToJump = LeafTag;
            DebugPage.AppendLine("Node.TapResponseSearchResult LeafTag: " + LeafTag);

            // As we are moving away from HomePage, call AppShell.LeaveUpdatePage
            // where nodes are read from DB, translated to scrollView elements
            // AND their corresponding frames are copied to scratchNodeFrameDB
            var AppShellInstance = Xamarin.Forms.Shell.Current as AppShell;
            AppShellInstance.DisplayPage(rootPath);//*/

            return true;
        }
        async Task<int> TapResponseSearchBar(object sender, EventArgs e)
        {

            // Split words in the search bar
            SearchBar searchBar = (SearchBar)sender;
            string origText = searchBar.Text;
            string[] words = origText.Split(' ');

            // Find the first word which is not articles nor prepositions
            string wordToUse = string.Empty;
            foreach (string word in words)
            {
                // Set all characters to small
                string wordExam = ViewProcessor.StripWord(word.Trim());
                if (!ViewProcessor.IsExcluded(wordExam))
                {
                    wordToUse = wordExam;
                    break;
                }
            }

            DebugPage.AppendLine("Node.TapResponseSearchBar Height: " + searchBar.Height);

            // If typed characters are not allowed, just return
            if (string.IsNullOrEmpty(wordToUse))
            {
                searchBar.Text = string.Empty;// Make search bar empty for placeholder to appear
                searchBar.Placeholder = "\"" + origText + "\"" + " not allowed.";
                return 0;
            }

            // Search DB for text in keyword (e.g., ->father) and header
            List<Node> allNodes = await NodeDatabase.DBnodes.SearchKeywordAsync(wordToUse);

            if (allNodes == null)
            {
                searchBar.Text = string.Empty;// Make search bar empty for placeholder to appear
                searchBar.Placeholder = "\"" + origText + "\"" + " not found.";
                return 0;
            }
            DebugPage.AppendLine("Node.SearchNow text: " + wordToUse + "*" + " count: " + allNodes.Count);

            // Set parameters for HomePage
            HomePage.placeholder = wordToUse; // Save searched text
            HomePage.isDisplayingSearchResults = true; // This causes the scratch to be displayed

            // Reaarange, keyword simialar highest priority
            allNodes = RearrangeNodes(allNodes, wordToUse);

            // Copy to scratchDB HomePage.scratchNodeFrameDB.Add(frame);
            HomePage.scratchNodeFrameDB = new List<Frame>();
            foreach (Node node in allNodes)
            {
                // Exclude Home elements 
                if (ViewProcessor.ExtractPathRoot(node.LeafTag).Equals("Home")) continue;

                // Set attributes
                node.IsAnswerShown = false;//
                node.nodeType = node.nodeType | NodeType.SearchResult; // Label as search result
                node.LargeFramePadding = node.IsAnswerShown ? 5 : 0;
                Span keywordSpan = new Span { Text = node.Keyword, TextColor = Color.LightYellow, FontAttributes = FontAttributes.Bold };
                Frame frame = node.CreateAllTypeNodeFrame(keywordSpan);
                HomePage.scratchNodeFrameDB.Add(frame);
            }

            // Set parameters
            HomePage.pathSeed = "Home->";
            App.globalPath = "Home->";
            HomePage.useScratchDB = true;
            HomePage.showBackArrow = true;

            Frame nodeFrame = searchBar.Parent as Frame;
            StackLayout pageLayout = nodeFrame.Parent as StackLayout;
            ScrollView scrollView = pageLayout.Parent as ScrollView;
            (scrollView.Parent as HomePage).DisplayPage();

            return allNodes.Count;
        }
        
    }// END CLASS
}
