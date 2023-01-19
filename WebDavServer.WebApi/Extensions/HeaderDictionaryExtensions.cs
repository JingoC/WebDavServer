using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;
using WebDavServer.Application.Contracts.WebDav.Enums;

namespace WebDavServer.WebApi.Extensions
{
    internal static class HeaderDictionaryExtensions
    {
        /// <summary>
        /// Get parameter 'Depth' from Header
        /// </summary>
        /// <param name="headers">Headers</param>
        /// <returns>Parameter 'Depth'</returns>
        public static DepthType GetDepth(this IHeaderDictionary headers)
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

        /// <summary>
        /// Get parameter 'If-Modified-Since' from Header
        /// </summary>
        /// <param name="headers">Headers</param>
        /// <returns></returns>
        public static bool IsIfLastModify(this IHeaderDictionary headers)
            => headers.ContainsKey("If-Modified-Since");

        /// <summary>
        /// Get parameter 'Destination' from headers
        /// </summary>
        /// <param name="headers">Headers</param>
        /// <returns>Parameter 'Destination'</returns>
        public static string GetDestination(this IHeaderDictionary headers)
            => headers.TryGetValue("Destination", out var v) 
                ? v.ToString()
                : throw new KeyNotFoundException("Destination header no found");

        /// <summary>
        /// Get parameter 'Timeout' from headers
        /// </summary>
        /// <param name="headers">Headers</param>
        /// <returns>Parameter Timeout</returns>
        public static int GetTimeoutSecond(this IHeaderDictionary headers)
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

        /// <summary>
        /// Перезапись файлов
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static bool IsOverwriteForse(this IHeaderDictionary headers)
        {
            if (headers.TryGetValue("Overwrite", out var v))
            {
                return v.Equals("F");
            }
            
            return false;
        }
    }
}
