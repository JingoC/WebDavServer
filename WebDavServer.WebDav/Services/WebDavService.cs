using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebDavServer.FileStorage.Enums;
using WebDavServer.FileStorage.Models;
using WebDavServer.FileStorage.Services;
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

            var result = new StringBuilder();

            //result.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            result.AppendLine("<D:multistatus xmlns:D=\"DAV:\">");

            var rootXmlResponse = GetXmlResponse(new List<string>(), propertiesList.First(), r.Url);
            result.AppendLine(rootXmlResponse);

            foreach (var properties in propertiesList.Skip(1))
            {
                var url = r.Url + properties.Name + "/";
                var xmlResponse = GetXmlResponse(new List<string>(), properties, url);

                result.AppendLine(xmlResponse);
            }

            result.AppendLine("</D:multistatus>");

            return result.ToString();
        }

        string GetXmlResponse(List<string> requestProperties, ItemInfo properties, string url)
        {
            var result = new StringBuilder();

            result.AppendLine("<D:response>");
            result.AppendLine($"<D:href>{url}</D:href>");
            result.AppendLine("<D:propstat>");

            result.AppendLine("<D:prop>");
            var props = GetXmlProperties(requestProperties, properties);
            result.AppendLine(props.TrimEnd());
            result.AppendLine("</D:prop>");

            result.AppendLine("<D:status>HTTP/1.1 200 OK</D:status>");

            result.AppendLine("</D:propstat>");
            result.AppendLine("</D:response>");

            return result.ToString().Trim();
        }

        string GetXmlProperties(List<string> requestProperties, ItemInfo properties)
        {
            if (requestProperties.Count == 0)
            {
                //var owner = GetProperty("owner", properties);
                //var group = GetProperty("group", properties);
                //var currentUserPrivilegeSet = GetProperty("current-user-privilege-set", properties);

                var creationdate = GetProperty("creationdate", properties);
                var getcontentlength = GetProperty("getcontentlength", properties);
                var getcontenttype = GetProperty("getcontenttype", properties);
                var getetag = GetProperty("getetag", properties);
                var getlastmodified = GetProperty("getlastmodified", properties);
                var resourcetype = GetProperty("resourcetype", properties);

                return //owner + group + currentUserPrivilegeSet +
                    creationdate + getcontentlength + getcontenttype +
                    getetag + getlastmodified + resourcetype;
            }
            else
            {
                return string.Empty;
            }
        }

        string GetProperty(string key, ItemInfo properties)
        {
            string v = string.Empty;

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
                    //v = "Thu, 21 May 2020 10:06:25 GMT";
                    //v = properties.ModifyDate;
                    break;
                case "resourcetype":
                    //v = "<D:collection></D:collection>";
                    break;

                default: return string.Empty;
            }

            return $"<D:{key}>{v}</D:{key}>{Environment.NewLine}";
        }
    }
}
