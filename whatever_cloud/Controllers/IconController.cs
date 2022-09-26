using storage;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web.Http;
using whatever_cloud;
using System.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace whatever_cloud_server.Controllers
{
    public class IconController : ApiController
    {

        [HttpGet]
        public HttpResponseMessage Get(string id)
        {
            var query = Request.GetQueryNameValuePairs();
            var file = Services.IconProvider.GetIcon(id, 
                width: (int)Convert.ToDouble(query.First(x => x.Key == "width").Value), 
                height: (int)Convert.ToDouble(query.First(x => x.Key == "height").Value)
            );
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = file.Stream;
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(file.MimeType);
            result.Content.Headers.ContentLength = stream.Length;
            return result;
        }
    }
}
