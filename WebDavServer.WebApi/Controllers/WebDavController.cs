using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebDavServer.WebApi.Helpers;
using WebDavService.Application.Contracts.FileStorage.Models;
using WebDavService.Application.Contracts.WebDav;
using WebDavService.Application.Contracts.WebDav.Models;

namespace WebDavServer.WebApi.Controllers
{
    [Route("{**path}")]
    public class WebDavController : ControllerBase
    {
        private readonly IWebDavService _webDavService;
        private readonly ILogger<WebDavController> _logger;

        public WebDavController(
            IWebDavService webDavService, ILogger<WebDavController> logger)
        {
            _webDavService = webDavService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(string? path, CancellationToken cancellationToken)
        {
            var request = Request;

            bool lm = HeaderHelper.GetIfLastModify(request.Headers);
            
            if (lm)
                return StatusCode((int)HttpStatusCode.NotModified);

            var content = await _webDavService.GetAsync(path ?? string.Empty, cancellationToken);
            await Response.Body.WriteAsync(content, cancellationToken);

            return StatusCode((int) HttpStatusCode.OK);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("PROPFIND")]
        public async Task<string> PropfindAsync(string? path, CancellationToken cancellationToken)
        {
            var request = Request;

            var depth = HeaderHelper.GetDepth(request.Headers);
            string xml = null!;
            path = path ?? string.Empty;
            var url = request.GetDisplayUrl().TrimEnd('/');

            if (request.ContentLength > 0)
            {
                var result = await request.BodyReader.ReadAsync(cancellationToken);

                xml = Encoding.UTF8.GetString(result.Buffer.ToArray());
            }

            try
            {
                var returnXml = await _webDavService.PropfindAsync(new PropfindRequest()
                {
                    Url = $"{url}/",
                    Path = path,
                    Depth = depth,
                    Xml = xml
                }, cancellationToken);

                Response.StatusCode = (int)HttpStatusCode.MultiStatus;

                return returnXml;
            }
            catch(Exception e)
            {
                _logger.LogError(e, e.Message);

                //Response.StatusCode = (int)HttpStatusCode.NotFound;
                return string.Empty;
            }
            
        }

        [HttpHead]
        public ActionResult Head(string? path)
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
        public ActionResult Options(string? path)
        {
            var methods = new string[]
            {
                "OPTIONS", "GET", "HEAD", "PROPFIND", "MKCOL", "PUT", "DELETE", "COPY", "MOVE", "LOCK", "UNLOCK", "PROPPATCH"
            };

            Response.Headers.Add("Allow", String.Join(',', methods));
            Response.Headers.Add("DAV", "1,2,extend");

            return Ok();
        }

        [HttpDelete]
        public ActionResult Delete(string? path)
        {
            _webDavService.Delete(path ?? string.Empty);

            return Ok();
        }

        [HttpPut]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> PutAsync(string? path, CancellationToken cancellationToken)
        {
            var contentLength = Request.ContentLength.HasValue ? (long) Request.ContentLength.Value : 0;

            if (contentLength == 0)
                return StatusCode((int)HttpStatusCode.NotModified);

            var data = new byte[contentLength];
            var _ = await Request.Body.ReadAsync(data, cancellationToken);
            
            await _webDavService.PutAsync(path ?? string.Empty, data, cancellationToken);

            return StatusCode((int)HttpStatusCode.Created);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("MKCOL")]
        public ActionResult MkCol(string? path)
        {
            _webDavService.MkCol(path ?? string.Empty);

            return StatusCode((int)HttpStatusCode.Created);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("MOVE")]
        public ActionResult Move(string? path)
        {
            var request = Request;

            var destination = HeaderHelper.GetDestination(request.Headers);

            var schemeAndHost = $"{request.Scheme}://{request.Host}";
            var area = "webdav";

            var dstPath = GetPathFromDestination(schemeAndHost, area, destination);

            _webDavService.Move(new MoveRequest()
            {
                 SrcPath = path ?? string.Empty,
                 DstPath = dstPath
            });

            // send correct response
            var statusCode = (int)(true ? HttpStatusCode.Created : HttpStatusCode.NoContent);
            return StatusCode(statusCode);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("COPY")]
        public ActionResult Copy(string? path)
        {
            var request = Request;

            var destination = HeaderHelper.GetDestination(request.Headers);

            var schemeAndHost = $"{request.Scheme}://{request.Host}";
            var area = "webdav";

            var dstPath = GetPathFromDestination(schemeAndHost, area, destination);

            _webDavService.Copy(new CopyRequest()
            {
                SrcPath = path ?? string.Empty,
                DstPath = dstPath
            });

            var statusCode = (int)(true ? HttpStatusCode.Created : HttpStatusCode.NoContent);
            return StatusCode(statusCode);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("LOCK")]
        public async Task<string> LockAsync(string? path, CancellationToken cancellationToken)
        {
            var request = Request;

            int timeoutSecond = HeaderHelper.GetTimeoutSecond(request.Headers);

            var result = await request.BodyReader.ReadAsync(cancellationToken);

            var xml = Encoding.UTF8.GetString(result.Buffer.ToArray());

            var response = _webDavService.Lock(new LockRequest()
            {
                Url = request.GetDisplayUrl(),
                Path = path ?? string.Empty,
                TimeoutSecond = timeoutSecond,
                Xml = xml
            });

            Response.Headers.Add("Lock-Token", response.LockToken);
            Response.StatusCode = (int)HttpStatusCode.OK;
            return response.Xml;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("UNLOCK")]
        public ActionResult Unlock(string? path)
        {
            _webDavService.Unlock(path ?? string.Empty);

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("PROPPATCH")]
        public string Propatch(string? path)
        {
            var request = Request;

            return string.Empty;
        }
        
        string GetPathFromDestination(string schemeAndHost, string area, string dst)
        {
            if (!dst.StartsWith(schemeAndHost))
                throw new Exception("Different scheme and host");

            var dstWithoutSchemeAndHost = dst.Remove(0, schemeAndHost.Length).Trim('/');

            if (!dstWithoutSchemeAndHost.StartsWith(area))
                throw new Exception("Different area");

            var dstWithoutSchemeAndHostAndArea = dstWithoutSchemeAndHost.Remove(0, area.Length).Trim('/');
            
            return dstWithoutSchemeAndHostAndArea;
        }
    }
}