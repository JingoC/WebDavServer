using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using WebDavServer.Infrastructure.WebDav.Helpers;
using WebDavService.Application.Contracts.FileStorage;
using WebDavService.Application.Contracts.FileStorage.Enums;
using WebDavService.Application.Contracts.FileStorage.Models;
using WebDavService.Application.Contracts.WebDav;
using WebDavService.Application.Contracts.WebDav.Enums;
using WebDavService.Application.Contracts.WebDav.Models;

namespace WebDavServer.Infrastructure.WebDav.Services
{


    public class WebDavService : IWebDavService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<WebDavService> _logger;

        public WebDavService(
            IFileStorageService fileStorageService, ILogger<WebDavService> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<byte[]> GetAsync(string path, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Get, {path}");

            return await _fileStorageService.GetContentAsync(path, cancellationToken);
        }

        public void MkCol(string path)
        {
            _logger.LogInformation($"MkCol, {path}");

            _fileStorageService.CreateDirectory(path);
        }

        public Task<string> PropfindAsync(PropfindRequest r, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Propfind, {r.Url}, {r.Path}");
            
            bool withDirectoryContent = r.Depth == DepthType.One;

            var propertiesList = _fileStorageService.GetProperties(r.Path, withDirectoryContent);

            XNamespace ns = "DAV:";

            var dictNamespaces = new Dictionary<string, XNamespace>();
            dictNamespaces.Add("D", ns);

            var xMultiStatus = XmlHelper.GetRoot(ns, "multistatus", dictNamespaces);

            var xResponse = GetPropfindXmlResponse(ns, new List<string>(), propertiesList.First(), r.Url);
            xMultiStatus.Add(xResponse);

            foreach (var properties in propertiesList.Skip(1))
            {
                var url = r.Url + properties.Name;

                if (properties.Type == ItemType.Directory)
                    url += "/";

                xResponse = GetPropfindXmlResponse(ns, new List<string>(), properties, url);

                xMultiStatus.Add(xResponse);
            }

            return Task.FromResult(xMultiStatus.ToString());
        }

        public void Delete(string path)
        {
            _logger.LogInformation($"Delete, {path}");

            _fileStorageService.Delete(path);
        }
        public void Move(MoveRequest r)
        {
            _logger.LogInformation($"Move, {r.SrcPath} -> {r.DstPath}");

            _fileStorageService.Move(r);
        }
        public void Copy(CopyRequest r)
        {
            _logger.LogInformation($"Copy, {r.SrcPath} -> {r.DstPath}");

            _fileStorageService.Copy(r);
        }
        public async Task PutAsync(string path, byte[] data, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Put, {path}");

            await _fileStorageService.CreateFileAsync(path, data, cancellationToken);
        }

        public LockResponse Lock(LockRequest r)
        {
            _logger.LogInformation($"Lock, {r.Url}, {r.Path}");

            var min = r.TimeoutSecond / 60;
            min = min == 0 ? 1 : min;

            var lockToken = _fileStorageService.LockItemAsync(r.Path, min);

            var lockInfo = ConvertXmlToLockInfo(r.Xml);
            var xResponse = GetLockXmlResponse(r.Url, r.TimeoutSecond.ToString(), lockToken, lockInfo);

            return new LockResponse()
            {
                LockToken = lockToken,
                Xml = xResponse.ToString()
            };  
        }
        public void Unlock(string path)
        {
            _logger.LogInformation($"Unlock, {path}");

            _fileStorageService.UnlockItem(path);
        }
        
        XElement GetPropfindXmlResponse(XNamespace ns, List<string> requestProperties, ItemInfo properties, string url)
        {
            var xResponse = XmlHelper.GetElement(ns, "response");
            var xHref = XmlHelper.GetElement(ns, "href", url);
            var xPropstat = XmlHelper.GetElement(ns, "propstat");

            xResponse.Add(xHref);
            xResponse.Add(xPropstat);

            var xProp = GetXmlProperties(ns, requestProperties, properties);

            xPropstat.Add(xProp);

            var status = GetStatus(properties);
            _logger.LogInformation($"Status: {status}");
            var xStatus = XmlHelper.GetStatus(ns, status);

            xPropstat.Add(xStatus);

            return xResponse;
        }

        HttpStatusCode GetStatus(ItemInfo itemInfo)
        {
            if (itemInfo.IsForbidden)
                return HttpStatusCode.Forbidden;

            if (!itemInfo.IsExists)
                return HttpStatusCode.NotFound;

            return HttpStatusCode.OK;
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
                    //v = "Thu, 21 May 2020 10:06:25 GMT";
                    //v = properties.ModifyDate;
                    break;
                case "resourcetype":
                    if (properties.Type == ItemType.Directory)
                        return XmlHelper.GetElement(ns, "collection", string.Empty);
                    break;
                case "lockdiscovery":
                    
                    break;
                default: return string.Empty;
            }

            return v;
        }

        XElement GetLockXmlResponse(string url, string timeoutSecond, string lockToken, LockInfo lockInfo)
        {
            XNamespace ns = "DAV:";

            var dictNamespaces = new Dictionary<string, XNamespace>();
            dictNamespaces.Add("D", ns);

            var xProp = XmlHelper.GetRoot(ns, "prop", dictNamespaces);

            var xLockDiscovery = XmlHelper.GetElement(ns, "lockdiscovery");
            var xActiveLock = XmlHelper.GetElement(ns, "activelock");

            xProp.Add(xLockDiscovery);
            xLockDiscovery.Add(xActiveLock);

            var xLockType = XmlHelper.GetElement(ns, "locktype", XmlHelper.GetElement(ns, lockInfo.LockType));
            var xLockScope = XmlHelper.GetElement(ns, "lockscope", XmlHelper.GetElement(ns, lockInfo.LockScope));
            var xOwner = XmlHelper.GetElement(ns, "owner", XmlHelper.GetElement(ns, "href", lockInfo.Owner));
            var xTimeout = XmlHelper.GetElement(ns, "timeout", $"Second-{timeoutSecond}");
            var xLockToken = XmlHelper.GetElement(ns, "locktoken", XmlHelper.GetElement(ns, "href", $"urn:uuid:{lockToken}"));
            var xLockRoot = XmlHelper.GetElement(ns, "lockroot", XmlHelper.GetElement(ns, "href", url));

            xActiveLock.Add(xLockType);
            xActiveLock.Add(xLockScope);
            xActiveLock.Add(xOwner);
            xActiveLock.Add(xTimeout);
            xActiveLock.Add(xLockToken);
            xActiveLock.Add(xLockRoot);

            return xProp;
        }

        LockInfo ConvertXmlToLockInfo(string xml)
        {
            var result = new LockInfo();

            var m = Regex.Match(xml, "lockscope[^>]?>[^:]+:([^ />]+)/");
            if (m.Groups.Count > 1)
            {
                result.LockScope = m.Groups[1].Value;
            }

            m = Regex.Match(xml, "locktype[^>]?>[^:]+:([^ />]+)");
            if (m.Groups.Count > 1)
            {
                result.LockType = m.Groups[1].Value;
            }

            m = Regex.Match(xml, "owner[^>]?>[^:]+:([^ />]+)>([^<]+)<");
            if (m.Groups.Count > 2)
            {
                result.Owner = m.Groups[2].Value;
            }

            return result;
        }
    }
}
