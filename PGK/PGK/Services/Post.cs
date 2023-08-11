using System;
using System.Collections.Generic;
using System.Text;

namespace PGK.Services
{
    public class Post
    {
        public string signalType { get; set; }
        public int numberNodes { get; set; }
        public string lastClientUpdateDate { get; set; }
        public string Nodes { get; set; }

        public override string ToString()
        {
            return string.Format("Post signalType: {0}\n numberNodes: {1}\n lastClientUpdateDate: {2}\n Nodes: {3}", signalType, numberNodes, lastClientUpdateDate, Nodes);
        }
    }
}
