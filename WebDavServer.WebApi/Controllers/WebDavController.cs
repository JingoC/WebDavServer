using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebDavServer.FileStorage.Entities;
using WebDavServer.WebApi.Filters;
using WebDavServer.WebDav;
using WebDavServer.WebDav.Models;
using WebDavServer.WebDav.Services;

namespace WebDavServer.WebApi.Controllers
{
    [Area("webdav")]
    [Route("[area]/{drive}/{**path}")]
    //[Authorize(WebDavConstants.Policy)]
    [AddHeader("DAV", "1,2,1#extend")]
    public class WebDavController : ControllerBase
    {
        private readonly IWebDavService _webDavService;

        public WebDavController(
            IWebDavService webDavService
            )
        {
            _webDavService = webDavService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string drive, string path)
        {
            var request = Request;

            bool lm = GetIfLastModify(request);
            
            if (lm)
                return StatusCode((int)HttpStatusCode.NotModified);

            return StatusCode((int) HttpStatusCode.OK);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("PROPFIND")]
        public async Task<string> Propfind(string drive, string path)
        {
            var request = Request;

            var depth = GetDepth(request);
            string xml = null;
            path = path ?? string.Empty;
            var url = request.GetDisplayUrl().TrimEnd('/');

            if (request.ContentLength > 0)
            {
                var result = await request.BodyReader.ReadAsync();

                xml = Encoding.UTF8.GetString(result.Buffer.ToArray());
            }

            try
            {
                var returnXml = await _webDavService.Propfind(new PropfindRequest()
                {
                    Url = $"{url}/",
                    Path = path,
                    Drive = drive,
                    Depth = depth,
                    Xml = xml
                });

                if (returnXml != null)
                {
                    //Response.Headers.Add("ContentType", "text/xml; charset=\"utf-8\"");

                    Response.StatusCode = (int)HttpStatusCode.MultiStatus;
                }

                return returnXml;
            }
            catch(Exception e)
            {
                //Response.StatusCode = (int)HttpStatusCode.NotFound;
                return string.Empty;
            }
            
        }

        [HttpHead]
        public async Task<ActionResult> Head(string drive, string path)
        {
            var request = Request;

            string head = null;

            if (head != null)
            {
                Response.Headers.Add("Last-Modified", DateTime.Now.ToString());
                return Ok();
            }
            else
                return NotFound();
        }

        [HttpOptions]
        public ActionResult Options(string drive, string path)
        {
            var methods = new string[]
            {
                "OPTIONS", "GET", "HEAD", "PROPFIND", "MKCOL", "PUT", "DELETE", "COPY", "MOVE", "LOCK", "UNLOCK", "PROPPATCH"
            };

            Response.Headers.Add("Allow", String.Join(',', methods));

            return Ok();
        }

        [HttpDelete]
        public async Task<ActionResult> Delete(string drive, string path)
        {
            var request = Request;

            return Ok();
        }

        [HttpPut]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> Put(string drive, string path)
        {
            var request = Request;

            return StatusCode((int)HttpStatusCode.Created);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("MKCOL")]
        public async Task<ActionResult> MkCol(string drive, string path)
        {
            var request = Request;

            return StatusCode((int)HttpStatusCode.Created);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("MOVE")]
        public async Task<ActionResult> Move(string drive, string path)
        {
            var request = Request;

            // send correct response
            var statusCode = (int)(true ? HttpStatusCode.Created : HttpStatusCode.NoContent);
            return StatusCode(statusCode);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("COPY")]
        public async Task<ActionResult> Copy(string drive, string path)
        {
            var request = Request;

            var statusCode = (int)(true ? HttpStatusCode.Created : HttpStatusCode.NoContent);
            return StatusCode(statusCode);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("LOCK")]
        public string Lock(string drive, string path)
        {
            var request = Request;

            return string.Empty;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("UNLOCK")]
        public ActionResult Unlock(string drive, string path)
        {
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("PROPPATCH")]
        public string Propatch(string drive, string path)
        {
            var request = Request;

            return string.Empty;
        }

        #region private_methods

        DepthType GetDepth(HttpRequest request)
        {
            if (request.Headers.TryGetValue("Depth", out StringValues v))
            {
                switch(v)
                {
                    case "0": return DepthType.Zero;
                    case "1": return DepthType.One;
                    case "Infinity": return DepthType.Infinity;
                    default: return DepthType.None;
                }
            }

            return DepthType.None;
        }

        bool GetIfLastModify(HttpRequest request)
        {
            if (request.Headers.TryGetValue("If-Modified-Since", out var v))
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}