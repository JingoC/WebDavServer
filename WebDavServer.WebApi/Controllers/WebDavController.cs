using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebDavServer.FileStorage.Models;
using WebDavServer.WebApi.Helpers;
using WebDavServer.WebDav;
using WebDavServer.WebDav.Models;
using WebDavServer.WebDav.Services;

namespace WebDavServer.WebApi.Controllers
{
    [Area("webdav")]
    [Route("[area]/{drive}/{**path}")]
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
        public async Task<IActionResult> GetAsync(string drive, string path)
        {
            var request = Request;

            bool lm = HeaderHelper.GetIfLastModify(request.Headers);
            
            if (lm)
                return StatusCode((int)HttpStatusCode.NotModified);

            var content = await _webDavService.GetAsync(drive, path);
            await Response.Body.WriteAsync(content);

            return StatusCode((int) HttpStatusCode.OK);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("PROPFIND")]
        public async Task<string> PropfindAsync(string drive, string path)
        {
            var request = Request;

            var depth = HeaderHelper.GetDepth(request.Headers);
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
                var returnXml = await _webDavService.PropfindAsync(new PropfindRequest()
                {
                    Url = $"{url}/",
                    Path = path,
                    Drive = drive,
                    Depth = depth,
                    Xml = xml
                });

                if (returnXml != null)
                {
                    //Response.Headers.Add("Content-Type", "application/xml");

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
        public ActionResult Head(string drive, string path)
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
            Response.Headers.Add("DAV", "1,2,extend");

            return Ok();
        }

        [HttpDelete]
        public ActionResult Delete(string drive, string path)
        {
            _webDavService.Delete(drive, path);

            return Ok();
        }

        [HttpPut]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> PutAsync(string drive, string path)
        {
            var request = Request;

            var data = new byte[0];

            if (request.ContentLength.HasValue)
            {
                data = new byte[request.ContentLength.Value];
                await request.Body.ReadAsync(data);
            }

            await _webDavService.PutAsync(drive, path, data);

            return StatusCode((int)HttpStatusCode.Created);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("MKCOL")]
        public ActionResult MkCol(string drive, string path)
        {
            _webDavService.MkCol(drive, path);

            return StatusCode((int)HttpStatusCode.Created);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("MOVE")]
        public ActionResult Move(string drive, string path)
        {
            var request = Request;

            var destination = HeaderHelper.GetDestination(request.Headers);

            var schemeAndHost = $"{request.Scheme}://{request.Host}";
            var area = "webdav";

            var dst = GetPathFromDestination(schemeAndHost, area, destination);

            _webDavService.Move(new MoveRequest()
            {
                 SrcDrive = drive,
                 SrcPath = path,
                 DstDrive = dst.drive,
                 DstPath = dst.path
            });

            // send correct response
            var statusCode = (int)(true ? HttpStatusCode.Created : HttpStatusCode.NoContent);
            return StatusCode(statusCode);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("COPY")]
        public ActionResult Copy(string drive, string path)
        {
            var request = Request;

            var destination = HeaderHelper.GetDestination(request.Headers);

            var schemeAndHost = $"{request.Scheme}://{request.Host}";
            var area = "webdav";

            var dst = GetPathFromDestination(schemeAndHost, area, destination);

            _webDavService.Copy(new CopyRequest()
            {
                SrcDrive = drive,
                SrcPath = path,
                DstDrive = dst.drive,
                DstPath = dst.path
            });

            var statusCode = (int)(true ? HttpStatusCode.Created : HttpStatusCode.NoContent);
            return StatusCode(statusCode);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("LOCK")]
        public async Task<string> LockAsync(string drive, string path)
        {
            var request = Request;

            int timeoutSecond = HeaderHelper.GetTimeoutSecond(request.Headers);

            var result = await request.BodyReader.ReadAsync();

            var xml = Encoding.UTF8.GetString(result.Buffer.ToArray());

            var response = _webDavService.Lock(new LockRequest()
            {
                Url = request.GetDisplayUrl(),
                Drive = drive,
                Path = path,
                TimeoutSecond = timeoutSecond,
                Xml = xml
            });

            Response.Headers.Add("Lock-Token", response.LockToken);
            Response.StatusCode = (int)HttpStatusCode.OK;
            return response.Xml;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AcceptVerbs("UNLOCK")]
        public ActionResult Unlock(string drive, string path)
        {
            _webDavService.Unlock(drive, path);

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

        (string drive, string path) GetPathFromDestination(string schemeAndHost, string area, string dst)
        {
            if (!dst.StartsWith(schemeAndHost))
                throw new Exception("Different scheme and host");

            var dstWithoutSchemeAndHost = dst.Remove(0, schemeAndHost.Length).Trim('/');

            if (!dstWithoutSchemeAndHost.StartsWith(area))
                throw new Exception("Different area");

            var dstWithoutSchemeAndHostAndArea = dstWithoutSchemeAndHost.Remove(0, area.Length).Trim('/');

            var contents = dstWithoutSchemeAndHostAndArea.Split('/');

            var d = contents.First();
            var p = string.Join('/', contents.Skip(1));

            return (d, p);
        }

        #endregion
    }
}