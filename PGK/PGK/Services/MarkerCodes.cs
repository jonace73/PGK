using System.IO;

namespace PGK.Services
{
    public class MarkerCodes
    {

        public static string termSeparator, singleNewLine, doubleNewLine, EndPar, leafSeparator, keyDelimiter, dbDelimiter;
        public static char segmentSeparator, serverDBdelimiter, leafKeySeparator;

        public static string[] verseMarker, headerMarker, answerMarker, targetMarker, chapterMarker, sectionMarker, subSectionMarker;
        public static string[] biblioMarker, promptMarker, weblink, apostrophe;

        // NOTE: linkMarker marks \LinkTo. However, after disassembling the link the output MUST be delimited by linkOutMarker
        public static string[] linkMarker, linkOutMarker; 

        public static string[] ff, fl, fi, singleQuote, doubleQuote, Dash;
        public MarkerCodes() { }

        public static void ExtractCodes(string line)
        {
            string preCode = "%\\";
            if (!line.Contains(preCode)) return;
            int indexStart = preCode.Length;
            string[] codePair = line.Substring(indexStart).Split('=');
            switch (codePair[0])
            {//
                case "serverDBdelimiter": serverDBdelimiter = char.Parse(codePair[1]); break;
                case "leafKeySeparator": leafKeySeparator = char.Parse(codePair[1]); break;
                case "dbDelimiter": dbDelimiter = codePair[1]; break;
                case "termSeparator": termSeparator = codePair[1]; break;
                case "singleNewLine": singleNewLine = codePair[1]; break;
                case "doubleNewLine": doubleNewLine = codePair[1]; break; 
                case "EndPar": EndPar = codePair[1]; break;                    
                case "segmentSeparator": segmentSeparator = codePair[1][0]; break;
                case "leafSeparator": leafSeparator = codePair[1]; break;
                case "keyDelimiter": keyDelimiter = codePair[1]; break;                   
                case "Dash": Dash = new string[] { codePair[1], "--" }; break;
                case "verseMarker": verseMarker = translateAcode(codePair[1]); break;
                case "headerMarker": headerMarker = translateAcode(codePair[1]); break;
                case "answerMarker": answerMarker = translateAcode(codePair[1]); break;
                case "targetMarker": targetMarker = translateAcode(codePair[1]); break;
                case "linkMarker": linkMarker = translateAcode(codePair[1]); break;
                case "linkOutMarker": linkOutMarker = translateAcode(codePair[1]); break;
                case "chapterMarker": chapterMarker = translateAcode(codePair[1]); break;
                case "sectionMarker": sectionMarker = translateAcode(codePair[1]); break;
                case "subSectionMarker": subSectionMarker = translateAcode(codePair[1]); break;
                case "biblioMarker": biblioMarker = translateAcode(codePair[1]); break;
                case "promptMarker": promptMarker = translateAcode(codePair[1]); break;
                case "weblink": weblink = translateAcode(codePair[1]); break;                    
                case "ff": ff = new string[]{codePair[1], "ff"}; break;
                case "fl": fl = new string[] { codePair[1], "fl" }; break;
                case "fi": fi = new string[] { codePair[1], "fi" }; break;
                // MUST NOT BE USED IN THIS PROGRAM TO REPLACE CODE WITH APOSTROPHE
                case "singleQuote": singleQuote = new string[] { codePair[1], "'" }; break;
                case "doubleQuote": doubleQuote = new string[] { codePair[1], "\"" }; break;
                case "apostrophe": apostrophe = new string[] { codePair[1], "'" }; break;
            }            
        }
        private static string[] translateAcode(string line)
        {
            return line.Split(',');
        }
    } // CLASS END
}
