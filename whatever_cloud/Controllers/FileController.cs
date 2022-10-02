using Microsoft.AspNetCore.Http;
using storage;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using whatever_cloud;
using static System.Net.WebRequestMethods;

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
        private bool saveContent(HttpContent file, bool notify)
        {
            using (var stream = file.ReadAsStreamAsync())
            {
                var name = file.Headers.ContentDisposition.FileName.Trim('\"');
                if (!Services.ContentProvider.SaveContent(name, stream.Result, notify))
                {
                    return false;
                }
            }
            return true;
        }
        [HttpPost]
        public System.Web.Mvc.ActionResult Post()
        {
            var provider = new MultipartMemoryStreamProvider();
            Request.Content.ReadAsMultipartAsync(provider).Wait();
            var files = provider.Contents;
            if (files == null || files.Count == 0)
            {
                return new System.Web.Mvc.HttpStatusCodeResult(HttpStatusCode.BadRequest, "No files were received");
            }
            foreach (var file in files.Skip(1))
            {
                if (!saveContent(file, false))
                {
                    return new System.Web.Mvc.HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
            }
            if (files.Count > 0)
            {
                if (!saveContent(files.First(), true))
                {
                    return new System.Web.Mvc.HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
            }
            return new System.Web.Mvc.HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}
