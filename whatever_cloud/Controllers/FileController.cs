using Microsoft.AspNetCore.Http;
using storage;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using whatever_cloud;

namespace whatever_cloud_server.Controllers
{
    public class FileController : ApiController
    {

        [HttpGet()]
        public HttpResponseMessage Get(string id)
        {
            var file = Services.ContentProvider.GetContent(id);
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = file.Stream;
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            result.Content.Headers.ContentDisposition.FileName = id;
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(file.MimeType);
            result.Content.Headers.ContentLength = stream.Length;
            return result;
        }
        [HttpPost]
        public System.Web.Mvc.ActionResult Post(IFormCollection files)
        {
            if (files == null || files.Count == 0)
            {
                return new System.Web.Mvc.HttpStatusCodeResult(HttpStatusCode.BadRequest, "No files were received");
            }
            foreach (var file in files.Files)
            {
                using (var stream = file.OpenReadStream())
                {
                    if (!Services.ContentProvider.SaveContent(file.FileName, stream))
                    {
                        return new System.Web.Mvc.HttpStatusCodeResult(HttpStatusCode.BadRequest);
                    }
                }
            }
            return new System.Web.Mvc.HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}
