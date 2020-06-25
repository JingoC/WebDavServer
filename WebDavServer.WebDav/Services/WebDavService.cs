using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using WebDavServer.Core.Providers;
using WebDavServer.FileStorage.Enums;
using WebDavServer.FileStorage.Models;
using WebDavServer.FileStorage.Services;
using WebDavServer.WebDav.Helpers;
using WebDavServer.WebDav.Models;

namespace WebDavServer.WebDav.Services
{
    public interface IWebDavService
    {
        Task<byte[]> GetAsync(string drive, string path);
        void MkCol(string drive, string path);
        Task<string> PropfindAsync(PropfindRequest r);
        void Delete(string drive, string path);
        void Move(MoveRequest r);
        void Copy(CopyRequest r);
        Task PutAsync(string drive, string path, byte[] data);
        LockResponse Lock(LockRequest r);
        void Unlock(string drive, string path);
    }

    public class WebDavService : IWebDavService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ICacheProvider _cacheProvider;
        public WebDavService(
            IFileStorageService fileStorageService,
            ICacheProvider cacheProvider
            )
        {
            _fileStorageService = fileStorageService;
            _cacheProvider = cacheProvider;
        }

        public async Task<byte[]> GetAsync(string drive, string path)
        {
            return await _fileStorageService.GetContentAsync(drive, path);
        }

        public void MkCol(string drive, string path)
        {
            _fileStorageService.CreateDirectory(drive, path);
        }

        public async Task<string> PropfindAsync(PropfindRequest r)
        {
            bool withDirectoryContent = r.Depth == DepthType.One;

            var propertiesList = _fileStorageService.GetProperties(r.Drive, r.Path, withDirectoryContent);

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

        public void Delete(string drive, string path)
        {
            _fileStorageService.Delete(drive, path);
        }
        public void Move(MoveRequest r)
        {
            _fileStorageService.Move(r);
        }
        public void Copy(CopyRequest r)
        {
            _fileStorageService.Copy(r);
        }
        public async Task PutAsync(string drive, string path, byte[] data)
        {
            await _fileStorageService.CreateFileAsync(drive, path, data);
        }

        public LockResponse Lock(LockRequest r)
        {
            var min = r.TimeoutSecond / 60;
            min = min == 0 ? 1 : min;

            var lockToken = _fileStorageService.LockItemAsync(r.Drive, r.Path, min);

            var lockInfo = ConvertXmlToLockInfo(r.Xml);
            var xResponse = GetLockXmlResponse(r.Url, r.TimeoutSecond.ToString(), lockToken, lockInfo);

            return new LockResponse()
            {
                LockToken = lockToken,
                Xml = xResponse.ToString()
            };  
        }
        public void Unlock(string drive, string path)
        {
            _fileStorageService.UnlockItem(drive, path);
        }

        #region private_methods

        XElement GetPropfindXmlResponse(XNamespace ns, List<string> requestProperties, ItemInfo properties, string url)
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

        #endregion
    }
}
