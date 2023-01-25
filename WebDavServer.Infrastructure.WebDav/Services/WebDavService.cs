using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WebDavServer.Application.Contracts.FileStorage;
using WebDavServer.Application.Contracts.FileStorage.Enums;
using WebDavServer.Application.Contracts.FileStorage.Models.Request;
using WebDavServer.Application.Contracts.FileStorage.Models.Response;
using WebDavServer.Application.Contracts.WebDav;
using WebDavServer.Application.Contracts.WebDav.Enums;
using WebDavServer.Application.Contracts.WebDav.Models;
using WebDavServer.Application.Contracts.WebDav.Models.Request;
using WebDavServer.Infrastructure.WebDav.Helpers;

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

        public async Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[WEBDAV] Get, {path}");

            var response = await _fileStorageService.ReadAsync(new ReadRequest()
            {
                Path = path
            }, cancellationToken);

            return response.ReadStream;
        }
        
        public async Task<ErrorType> MkColAsync(string path, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[WEBDAV] MkCol, {path}");

            var response = await _fileStorageService.CreateAsync(new CreateRequest()
            {
                ItemType = ItemType.Directory,
                Path = path
            }, cancellationToken);

            return response.ErrorType;
        }

        public async Task<string> PropfindAsync(PropfindRequest r, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[WEBDAV] Propfind, {r.Url}, {r.Path}");
            
            var response = await _fileStorageService.GetPropertiesAsync(new GetPropertiesRequest()
            {
                Path = r.Path,
                WithDirectoryContent = r.Depth == DepthType.One
            }, cancellationToken);

            var propertiesList = response.Items;

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

            return xMultiStatus.ToString();
        }
        
        public async Task<ErrorType> DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[WEBDAV] DeleteAsync, {path}");

            var response = await _fileStorageService.DeleteAsync(new DeleteRequest {Path = path}, cancellationToken);

            return response.ErrorType;
        }
        public async Task<ErrorType> MoveAsync(Application.Contracts.WebDav.Models.Request.MoveRequest r, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[WEBDAV] MoveAsync, {r.SrcPath} -> {r.DstPath}");

            var response = await _fileStorageService.MoveAsync(new()
            {
                SrcPath = r.SrcPath,
                DstPath = r.DstPath,
                IsForce = r.IsForce
            }, cancellationToken);

            return response.ErrorType;
        }
        public async Task<ErrorType> CopyAsync(Application.Contracts.WebDav.Models.Request.CopyRequest r, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[WEBDAV] CopyAsync, {r.SrcPath} -> {r.DstPath}");

            var response = await _fileStorageService.CopyAsync(new()
            {
                SrcPath = r.SrcPath,
                DstPath = r.DstPath,
                IsForce = r.IsForce
            }, cancellationToken);

            return response.ErrorType;
        }
        public async Task PutAsync(string path, Stream stream, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[WEBDAV] Put, {path}");

            await _fileStorageService.CreateAsync(new  CreateRequest()
            {
                ItemType = ItemType.File,
                Path = path,
                Stream = stream
            }, cancellationToken);
        }

        public async Task<Application.Contracts.WebDav.Models.Response.LockResponse> 
            LockAsync(Application.Contracts.WebDav.Models.Request.LockRequest r, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[WEBDAV] LockAsync, {r.Url}, {r.Path}");

            var min = r.TimeoutSecond / 60;
            min = min == 0 ? 1 : min;

            var response = await _fileStorageService.LockAsync(new()
            {
                Path = r.Path,
                TimeoutMin = r.TimeoutSecond * 60
            }, cancellationToken);

            var lockToken = response.Token;

            var lockInfo = ConvertXmlToLockInfo(r.Xml);
            var xResponse = GetLockXmlResponse(r.Url, r.TimeoutSecond.ToString(), lockToken, lockInfo);

            return new ()
            {
                LockToken = lockToken,
                Xml = xResponse.ToString()
            };  
        }
        public async Task UnlockAsync(string path, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[WEBDAV] UnlockAsync, {path}");

            await _fileStorageService.UnlockAsync(new UnlockRequest() {Path = path}, cancellationToken);
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
