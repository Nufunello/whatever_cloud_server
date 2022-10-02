using storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace whatever_cloud
{
    public static class Services
    {
        static Services()
        {
            Service = new messager.Service(hostname: "localhost");
            var rootFolder = Path.Combine(Services.RootFolder, "content");
            var services = Services.services;

            var messageService = Services.Service;
            services.Add(messageService);

            var contentProvider = new ContentProvider(rootFolder, (filename) =>
            {
                return MimeMapping.GetMimeMapping(filename);
            });
            services.Add(contentProvider);
            Services.ContentProvider = contentProvider;
            Services.IconProvider = new IconProvider(contentProvider);

            var contextProvider = new search.ContextFactory(contentProvider);
            {
                var messager = new messager.RabbitmqMessager(messageService.Channel,
                    receiveRoutingKey: "server/search/size",
                    publishRoutingKey: "client/search/size");

                var searchMessager = new messager.SearchSizeMessager(messager);
                searchMessager.OnSearchRequest += (sender, args) =>
                {
                    var context1 = contextProvider.GetContext(args.Context);
                    searchMessager.SendSearchResult(new messager.SearchSizeResponse
                    {
                        Context = args.Context,
                        Size = context1.GetSize()
                    });
                };
                services.Add(searchMessager);

                contextProvider.AddContext("home", new search.Pattern { Path = "" });
                var context = contextProvider.GetContext("home");
                context.FilesUpdated += () =>
                {
                    var sizeMessager = searchMessager;
                    sizeMessager.SendSearchResult(new messager.SearchSizeResponse
                    {
                        Context = "home",
                        Size = 0
                    });
                    sizeMessager.SendSearchResult(new messager.SearchSizeResponse
                    {
                        Context = "home",
                        Size = context.GetSize()
                    });
                };
                services.Add(contextProvider);
                services.Add(context);
            }
            {
                var messager = new messager.RabbitmqMessager(messageService.Channel,
                    receiveRoutingKey: "server/search/content",
                    publishRoutingKey: "client/search/content");
                var searchMessager = new messager.SearchContentMessager(messager);
                searchMessager.OnSearchRequest += (sender, args) =>
                {
                    var context = contextProvider.GetContext(args.Context);
                    searchMessager.SendSearchResult(new messager.SearchContentResponse
                    {
                        Context = args.Context,
                        Items = Enumerable.Range(args.Index, args.Count)
                            .Select(x => {
                                var path = context.GetFilePath(x);
                                return new messager.SearchContentResponse.SearchResponseItem { Index = x, Path = path };
                            })
                    });
                };
                services.Add(searchMessager);
            }

            {
                var messager = new messager.RabbitmqMessager(messageService.Channel,
                    receiveRoutingKey: "server/search/update",
                    publishRoutingKey: "client/search/size");

                var searchMessager = new messager.SearchUpdateItemMessager(messager);
                searchMessager.OnSearchRequest += (sender, args) =>
                {
                    var context = contextProvider.GetContext(args.Context);
                    context.RemoveFile(args.Items
                        .Where(x => x.Action == "Remove")
                        .Select(x => x.Index)
                    );
                };
                services.Add(searchMessager);
            }
        }
        public static messager.Service Service { get; set; }
        public static IconProvider IconProvider { get; set; }
        public static ContentProvider ContentProvider { get; set; }
        public static List<object> services = new List<object>();
        public static string RootFolder =>
            HttpContext.Current.ApplicationInstance.Server.MapPath("~/App_Data");
    }
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
