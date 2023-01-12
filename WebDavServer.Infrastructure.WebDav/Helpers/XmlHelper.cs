using System.Net;
using System.Xml.Linq;
using Microsoft.AspNetCore.WebUtilities;

namespace WebDavServer.Infrastructure.WebDav.Helpers
{
    /// <summary>
    /// Xml helper class for generate XElements
    /// </summary>
    public static class XmlHelper
    {
        /// <summary>
        /// Get root xml element with include namespaces
        /// </summary>
        /// <param name="ns">Namespace</param>
        /// <param name="name">Name element</param>
        /// <param name="namespaces">Include namespaces</param>
        /// <returns></returns>
        public static XElement GetRoot(
            XNamespace ns, 
            string name,
            Dictionary<string, XNamespace> namespaces
            )
        {
            var nsName = ns == null ? name : ns + name;

            XElement root = new XElement(nsName);

            foreach (var n in namespaces)
            {
                var nString = n.Value.ToString();
                var xAttribute = new XAttribute(XNamespace.Xmlns + n.Key, nString);

                root.Add(xAttribute);
            }

            return root;
        }
        /// <summary>
        /// Get xml element with custom object
        /// </summary>
        /// <param name="ns">Namespace</param>
        /// <param name="name">Name element</param>
        /// <param name="value">Value element</param>
        /// <returns></returns>
        public static XElement GetElement(XNamespace? ns, string name, object? value = null)
        {
            var xName = ns == null ? name : ns + name;
            return value == null ? new XElement(xName) : new XElement(xName, value);
        }
        /// <summary>
        /// Get xml element with section "prop"
        /// </summary>
        /// <param name="ns">Namespace</param>
        /// <param name="items">Properties</param>
        /// <returns></returns>
        public static XElement GetProps(XNamespace ns, Dictionary<string, object> items)
        {
            var result = GetElement(ns, "prop");

            foreach(var item in items)
            {
                var xElement = GetElement(ns, item.Key, item.Value);

                result.Add(xElement);
            }

            return result;
        }
        /// <summary>
        /// Get Xml element with status description
        /// </summary>
        /// <param name="ns">Namespace</param>
        /// <param name="statusCode">Status code</param>
        /// <returns></returns>
        public static XElement GetStatus(XNamespace ns, HttpStatusCode statusCode)
        {
            var reasonPhrases = ReasonPhrases.GetReasonPhrase((int)statusCode);

            return GetElement(ns, "status", $"HTTP/1.1 {(int)statusCode} {reasonPhrases}");
        }
    }
}
