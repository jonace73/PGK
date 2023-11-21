using PGK.Data;
using PGK.Views;
using System.Threading;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace PGK.Services
{
    public class Post
    {
        public string signalType { get; set; }
        public int numberNodes { get; set; }
        public string lastClientUpdateDate { get; set; }
        public string Nodes { get; set; }
        public string LeafTag { get; set; }// appIDforDB + MarkerCodes.leafSeparator + RandomAppID
        public double latitude { get; set; }
        public double longitude { get; set; }
        public Post()
        {
            signalType = "Update";
            lastClientUpdateDate = UpdatePage.DateTimeToStringOneBased(UpdatePage.appLastUpdateTime);
            LeafTag = "";
            latitude = 0.0;
            longitude = 0.0;
        }
        public override string ToString()
        {
            return string.Format("Post signalType: {0}\n numberNodes: {1}\n lastClientUpdateDate: {2}\n Nodes: {3}", signalType, numberNodes, lastClientUpdateDate, Nodes);
        }
    }
}
