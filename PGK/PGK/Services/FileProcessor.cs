using PGK.Data;
using PGK.Models;
using PGK.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace PGK.Services
{
    public class FileProcessor
    {
        public static void ExtractMarkerCodes()
        {
            DebugPage.AppendLine("FileProcessor.ExtractMarkerCodes");

            var assembly = typeof(FileProcessor).GetTypeInfo().Assembly;
            //                                                  Namespace.Folder.Filename.Extension MUST be an embedded resource
            Stream stream = assembly.GetManifestResourceStream("PGK.Assets.Commands.tex");
            StreamReader reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                MarkerCodes.ExtractCodes(line);
            }
        }
        public static async Task<bool> ExtractAssetDBandSetUpdateTime()
        {
            DebugPage.AppendLine("FileProcessor.ExtractAssetDBandSetUpdateTime");

            DateTime refDate = UpdatePage.appLastUpdateTime;
            try
            {
                var assembly = typeof(FileProcessor).GetTypeInfo().Assembly;
                //                                                  Namespace.Folder.Filename.Extension MUST be an embedded resource
                Stream stream = assembly.GetManifestResourceStream("PGK.Assets.NodesDB.txt");
                StreamReader reader = new StreamReader(stream);
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    Node node = ViewProcessor.LineFromAssetToNode(line);//

                    // Set appLastUpdateTime
                    DateTime date = UpdatePage.StringToDateTime(node.DateUpdated);
                    // RESULT: 24/05/2023 8:39:00. This matches with that of UpdatePage.ExtractNodes()
                    TimeSpan timeDiff = date - refDate;
                    if (timeDiff.Minutes >= 1)
                    {
                        refDate = date;
                    }
                    int numberInserted = await NodeDatabase.DBnodes.InsertNodeAsync(node);
                    if (numberInserted < 0) break;
                }
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("FileProcessor.ExtractAssetDBandSetUpdateTime ERROR: " + ex.Message);
            }

            // Save latest update date
            UpdatePage.appLastUpdateTime = refDate;
            DebugPage.AppendLine("FileProcessor.ExtractAssetDBandSetUpdateTime appLastUpdateTime: " + UpdatePage.appLastUpdateTime);
            return true;
        }
        public static string CreateRandomAppID()
        {
            int randValue;
            char letter;
            string str = "";
            Random rand = new Random();
            for (int i = 0; i < 8; i++)
            {

                // Generating a random number.
                randValue = rand.Next(0, 26);

                // Generating random character by converting
                // the random number into character.
                letter = Convert.ToChar(randValue + 65);

                // Appending the letter to string.
                str = str + letter;
            }
            return str;
        }
    } // END OF CLASS
}
