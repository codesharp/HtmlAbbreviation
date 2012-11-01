using System;
using System.Configuration;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CodeSharp.Core.Castles;
using Abbreviation.Service;
using Raven.Client;
using Raven.Client.Embedded;
using DependencyResolver = CodeSharp.Core.Services.DependencyResolver;

namespace Abbreviation.Web
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            var version = ConfigurationManager.AppSettings["EnvironmentVersionFlag"];
            var rootPath = Server.MapPath("~") + "application_config";

            //如果是以Release的方式编译，则强制将version设置为Release
            #if !DEBUG
                version = "Release";
            #endif

            CodeSharp.Core.Configuration
                .ConfigWithEmbeddedXml(version, rootPath, Assembly.GetExecutingAssembly(), "Abbreviation.Web.ConfigFiles")
                .RenderProperties()
                .Castle(x => Resolve(x.Container));

            RegisterSnippetTextProviders();
        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        private void Resolve(IWindsorContainer container)
        {
            //常规注册
            var assemblies = new Assembly[] { Assembly.Load("Abbreviation.Service") };

            container.RegisterRepositories(assemblies);
            container.RegisterServices(assemblies);
            container.RegisterComponent(assemblies);

            //注册controller
            ControllerBuilder.Current.SetControllerFactory(new WindsorControllerFactory(container));
            container.RegisterControllers(Assembly.GetExecutingAssembly());

            //注册RavenDb的DocumentStore，为了方便部署，采用内嵌的DocumentStore，这样就不需要独立的部署一个RavenDb Server；
            //RavenDb的DocumentStore被用来存储url经解析过的缩略html
            var documentStore = new EmbeddableDocumentStore { DataDirectory = "HtmlCacheDB" };
            documentStore.Initialize();
            container.Register(Component.For<IDocumentStore>().Instance(documentStore).LifeStyle.Singleton);
        }

        /// <summary>
        /// 注册所有用户自定义的HtmlParserManager，目前只实现了用于解析github issue的一个html parser；
        /// 用户如果实现了更多的解析器，需要在此处添加进来
        /// </summary>
        private void RegisterSnippetTextProviders()
        {
            var providerManager = DependencyResolver.Resolve<IHtmlParserManager>();

            //注册用于解析github issue的一个HtmlParser
            providerManager.RegisterHtmlParser(
                new HtmlParserKey { UrlRegexPattern = @"https://github\.com/([^/]+?)/([^/]+?)/issues/(\d+)$" },
                new GithubIssueHtmlParser()
            );

            //Here, register other customize HtmlParsers
            //...
        }
    }

    /// <summary>为Controller提供基于Windsor的依赖注入支持
    /// </summary>
    public class WindsorControllerFactory : DefaultControllerFactory
    {
        private IWindsorContainer _container;

        public WindsorControllerFactory(IWindsorContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            _container = container;
        }

        protected override IController GetControllerInstance(RequestContext context, Type controllerType)
        {
            if (controllerType == null)
            {
                throw new HttpException(
                    404,
                    string.Format("The controller for path '{0}' could not be found or it does not implement IController.", context.HttpContext.Request.Path)
                );
            }
            return _container.Kernel.HasComponent(controllerType) ? _container.Resolve(controllerType) as IController : base.GetControllerInstance(context, controllerType);
        }
        public override void ReleaseController(IController controller)
        {
            var disposableController = controller as IDisposable;

            if (disposableController != null)
            {
                disposableController.Dispose();
            }

            _container.Release(controller);
        }
    }
}