using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebDavServer.WebDav;

namespace WebDavServer.WebApi.Helpers
{
    public static class HeaderHelper
    {
        public static DepthType GetDepth(IHeaderDictionary headers)
        {
            if (headers.TryGetValue("Depth", out StringValues v))
            {
                switch (v)
                {
                    case "0": return DepthType.Zero;
                    case "1": return DepthType.One;
                    case "Infinity": return DepthType.Infinity;
                    default: return DepthType.None;
                }
            }

            return DepthType.None;
        }

        public static bool GetIfLastModify(IHeaderDictionary headers)
        {
            if (headers.TryGetValue("If-Modified-Since", out var v))
            {
                return true;
            }

            return false;
        }

        public static string GetDestination(IHeaderDictionary headers)
        {
            if (headers.TryGetValue("Destination", out var v))
            {
                return v;
            }

            throw new Exception("Destination header no found");
        }

        public static int GetTimeoutSecond(IHeaderDictionary headers)
        {
            if (headers.TryGetValue("Timeout", out var v))
            {
                var timeoutSecondString = v.ToString().Split('-').Last();

                if (int.TryParse(timeoutSecondString, out var iv))
                {
                    return iv;
                }
            }

            return 600;
        }
    }
}
