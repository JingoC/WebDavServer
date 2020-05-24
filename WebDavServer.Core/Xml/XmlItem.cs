using System;
using System.Collections.Generic;
using System.Linq;

namespace WebDavServer.Core.Xml
{
    public class XmlItem
    {
        public string Label { get; set; } = null;
        public string Tag { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public List<XmlAttribute> Attributes = new List<XmlAttribute>();
        public List<XmlItem> Items { get; set; } = new List<XmlItem>();

        public override string ToString()
        {
            var attributes = string.Join(' ', Attributes);
            var tag = Label == null ? $"{Tag}" : $"{Label}:{Tag}";

            string items = string.Join(Environment.NewLine, Items.Select(x => x.ToString()));

            return $"<{tag} {attributes}>{Value}{items}</{tag}>";
        }
    }
}
