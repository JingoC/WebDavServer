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
using WebDavServer.Application.Contracts.FileStorage.Enums;
using WebDavServer.Application.Contracts.WebDav;
using WebDavServer.Application.Contracts.WebDav.Models.Request;
using WebDavServer.WebApi.Extensions;

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
        public async Task GetAsync(string? path, CancellationToken cancellationToken = default)
        {
            if (Request.Headers.IsIfLastModify())
            {
                StatusCode((int)HttpStatusCode.NotModified);

                return;
            }

            StatusCode((int) HttpStatusCode.OK);

            await using var stream = await _webDavService.GetAsync(GetPath(path), cancellationToken);

            await stream.CopyToAsync(Response.Body, cancellationToken);
            
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("PROPFIND")]
        public async Task<string> PropfindAsync(string? path, CancellationToken cancellationToken)
        {
            var returnXml = await _webDavService.PropfindAsync(new PropfindRequest
            {
                Url = $"{Request.GetDisplayUrl().TrimEnd('/')}/",
                Path = GetPath(path),
                Depth = Request.Headers.GetDepth()
            }, cancellationToken);

            Response.StatusCode = (int)HttpStatusCode.MultiStatus;

            return returnXml;
        }

        [HttpHead]
        public ActionResult Head(string? path)
        {
            string head = null!;

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
        public async Task<IActionResult> Delete(string? path)
        {
            if (path is null)
            {
                return StatusCode((int) HttpStatusCode.Conflict);
            }

            var errorType = await _webDavService.DeleteAsync(GetPath(path));

            if (errorType == ErrorType.ResourceNotExists)
            {
                return StatusCode((int) HttpStatusCode.NotFound);
            }

            return Ok();
        }

        [HttpPut]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> PutAsync(string? path, CancellationToken cancellationToken)
        {
            var contentLength = Request.ContentLength ?? 0;

            if (contentLength == 0)
            {
                return StatusCode((int)HttpStatusCode.NotModified);
            }
            
            await _webDavService.PutAsync(GetPath(path), Request.Body, cancellationToken);

            return StatusCode((int)HttpStatusCode.Created);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("MKCOL")]
        public async Task<IActionResult> MkCol(string? path, CancellationToken cancellationToken = default)
        {
            if ((Request.ContentLength ?? 0) > 0)
            {
                return StatusCode((int) HttpStatusCode.UnsupportedMediaType);
            }

            var errorType = await _webDavService.MkColAsync(GetPath(path), cancellationToken);

            if (errorType == ErrorType.ResourceExists)
            {
                return StatusCode((int) HttpStatusCode.MethodNotAllowed);
            }
            else if (errorType == ErrorType.PartResourcePathNotExists)
            {
                return StatusCode((int) HttpStatusCode.Conflict);
            }

            return StatusCode((int) HttpStatusCode.Created);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("MOVE")]
        public async Task<IActionResult> Move(string? path, CancellationToken cancellationToken = default)
        {
            var requestPath = GetPath(path);

            var errorType = await _webDavService.MoveAsync(new MoveRequest()
                {
                    SrcPath = requestPath,
                    DstPath = GetPathFromDestination(Request.Headers.GetDestination()),
                    IsForce = Request.Headers.IsOverwriteForse()
                },
                cancellationToken);

            if (errorType == ErrorType.ResourceExists)
            {
                return StatusCode((int) HttpStatusCode.PreconditionFailed);
            }

            return StatusCode((int)HttpStatusCode.Created);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("COPY")]
        public async Task<IActionResult> Copy(string? path, CancellationToken cancellationToken = default)
        {
            var requestPath = GetPath(path);

            var errorType = await _webDavService.CopyAsync(
                new CopyRequest()
                {
                    SrcPath = requestPath,
                    DstPath = GetPathFromDestination(Request.Headers.GetDestination()),
                    IsForce = Request.Headers.IsOverwriteForse()
                },
                cancellationToken);

            if (errorType == ErrorType.ResourceExists)
            {
                return StatusCode((int) HttpStatusCode.PreconditionFailed);
            }

            return StatusCode((int) HttpStatusCode.Created);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("LOCK")]
        public async Task<string> LockAsync(string? path, CancellationToken cancellationToken = default)
        {
            int timeoutSecond = Request.Headers.GetTimeoutSecond();
            
            var xml = await ReadXmlFromBodyAsync(cancellationToken);

            var response = await _webDavService.LockAsync(new LockRequest()
            {
                Url = Request.GetDisplayUrl(),
                Path = GetPath(path),
                TimeoutSecond = timeoutSecond,
                Xml = xml
            }, cancellationToken);

            Response.Headers.Add("LockAsync-Token", response.LockToken);
            Response.StatusCode = (int)HttpStatusCode.OK;

            return response.Xml;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("UNLOCK")]
        public ActionResult Unlock(string? path, CancellationToken cancellationToken = default)
        {
            _webDavService.UnlockAsync(GetPath(path));

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("PROPPATCH")]
        public string Propatch(string? path)
        {
            return string.Empty;
        }

        string GetPathFromDestination(string dst)
            => dst.Remove(0, $"{Request.Scheme}://{Request.Host}".Length).Trim('/');

        async Task<string> ReadXmlFromBodyAsync(CancellationToken cancellationToken = default)
        {
            var result = await Request.BodyReader.ReadAsync(cancellationToken);

            return Encoding.UTF8.GetString(result.Buffer.ToArray());
        }

        string GetPath(string? path) => path is null ? "/" : $"/{path}";
    }
}