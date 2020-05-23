using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using WebDavServer.FileStorage.Entities;

namespace WebDavServer.WebDav.Helpers
{
    public static class XmlHelper
    {
        public static string GetPropfindResponse(List<Item> items)
        {
            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");

            xml.AppendLine("<D:multistatus xmlns:D=\"DAV:\">");


            return xml.ToString();
        }
    }
}
