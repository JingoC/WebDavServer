using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WebDavServer.FileStorage.Enums;
using WebDavServer.FileStorage.Models;
using WebDavServer.FileStorage.Services;
using WebDavServer.WebDav.Helpers;
using WebDavServer.WebDav.Models;

namespace WebDavServer.WebDav.Services
{
    public interface IWebDavService
    {
        Task<string> Propfind(PropfindRequest r);
    }

    public class WebDavService : IWebDavService
    {
        private readonly IFileStorageService _fileStorageService;
        public WebDavService(
            IFileStorageService fileStorageService
            )
        {
            _fileStorageService = fileStorageService;
        }

        public async Task<string> Propfind(PropfindRequest r)
        {
            bool withDirectoryContent = r.Depth == DepthType.One;

            var propertiesList = _fileStorageService.GetProperties(r.Drive, r.Path, withDirectoryContent);

            XNamespace ns = "DAV:";

            var dictNamespaces = new Dictionary<string, XNamespace>();
            dictNamespaces.Add("D", ns);

            var xMultiStatus = XmlHelper.GetRoot(ns, "multistatus", dictNamespaces);

            //result.AppendLine("<D:multistatus xmlns:D=\"DAV:\">");

            var xResponse = GetXmlResponse(ns, new List<string>(), propertiesList.First(), r.Url);
            xMultiStatus.Add(xResponse);

            foreach (var properties in propertiesList.Skip(1))
            {
                var url = r.Url + properties.Name;

                if (properties.Type == ItemType.Directory)
                    url += "/";

                xResponse = GetXmlResponse(ns, new List<string>(), properties, url);

                xMultiStatus.Add(xResponse);
            }

            return xMultiStatus.ToString();
        }

        XElement GetXmlResponse(XNamespace ns, List<string> requestProperties, ItemInfo properties, string url)
        {
            var xResponse = XmlHelper.GetElement(ns, "response");
            var xHref = XmlHelper.GetElement(ns, "href", url);
            var xPropstat = XmlHelper.GetElement(ns, "propstat");

            xResponse.Add(xHref);
            xResponse.Add(xPropstat);

            var xProp = GetXmlProperties(ns, requestProperties, properties);

            xPropstat.Add(xProp);
            
            var xStatus = XmlHelper.GetStatus(ns, HttpStatusCode.OK);

            xPropstat.Add(xStatus);

            return xResponse;
        }

        XElement GetXmlProperties(XNamespace ns, List<string> requestProperties, ItemInfo properties)
        {
            var propDictionary = new Dictionary<string, object>();

            if (requestProperties.Count == 0)
            {
                propDictionary.Add("creationdate", GetProperty(ns, "creationdate", properties));
                propDictionary.Add("getcontentlength", GetProperty(ns, "getcontentlength", properties));
                propDictionary.Add("getcontenttype", GetProperty(ns, "getcontenttype", properties));
                propDictionary.Add("getetag", GetProperty(ns, "getetag", properties));
                propDictionary.Add("getlastmodified", GetProperty(ns, "getlastmodified", properties));
                propDictionary.Add("resourcetype", GetProperty(ns, "resourcetype", properties));
                propDictionary.Add("lockdiscovery", GetProperty(ns, "lockdiscovery", properties));
            }
            else
            {
                
            }

            return XmlHelper.GetProps(ns, propDictionary);
        }

        object GetProperty(XNamespace ns, string key, ItemInfo properties)
        {
            object v = string.Empty;

            switch(key)
            {
                case "owner": break;
                case "group": break;
                case "current-user-privilege-set": break;
                case "creationdate":
                    v = "2020-05-21T10:04:35Z";
                    //v = properties.CreatedDate;
                    break;
                case "getcontentlength": 
                    if (properties.Type == ItemType.File)
                        v = properties.Size.ToString();
                    break;
                case "getcontenttype": 
                    if (properties.Type == ItemType.File)
                        v = properties.ContentType;
                    break;
                case "getetag":
                    if (properties.Type == ItemType.File)
                        v = "acc5643b3a8c4653bb9630b9013a72a6";
                    break;
                case "getlastmodified":
                    v = "Thu, 21 May 2020 10:06:25 GMT";
                    //v = properties.ModifyDate;
                    break;
                case "resourcetype":
                    return XmlHelper.GetElement(ns, "collection", string.Empty);
                case "lockdiscovery":
                    
                    break;
                default: return string.Empty;
            }

            return v;
        }
    }
}
