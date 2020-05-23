using System;
using System.Collections.Generic;
using System.Text;

namespace WebDavServer.WebDav.Models
{
    public class PropfindRequest
    {
        public string Drive { get; set; }
        public string Path { get; set; }
        public string Url { get; set; }
        public DepthType Depth { get; set; }
        public string Xml { get; set; }
    }
}
