using storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;

namespace whatever_cloud
{
    public static class Services
    {
        public static IconProvider IconProvider { get; set; }
        public static ContentProvider ContentProvider { get; set; }
        public static List<object> services = new List<object>();
        public static string RootFolder() 
        {
            
            return Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "bin"); 
        }

    }
    public static class WebApiConfig
    {
        
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            var rootFolder = System.IO.Path.Combine(Services.RootFolder(), "content");
            var services = Services.services;

            var messageService = new messager.Service(hostname: "localhost");
            services.Add(messageService);
            
                var contentProvider = new storage.ContentProvider(rootFolder, (filename) =>
                {
                    return MimeMapping.GetMimeMapping(filename);
                });
                services.Add(contentProvider);
            Services.ContentProvider = contentProvider;
            Services.IconProvider = new storage.IconProvider(contentProvider);

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

                    foreach (var action in args.Items.OrderByDescending((x) => x.Index))
                    {
                        if (action.Action == "Remove")
                        {
                            context.RemoveFile(action.Index);
                        }
                    }
                };
                services.Add(searchMessager);
            }
        }
    }
}
