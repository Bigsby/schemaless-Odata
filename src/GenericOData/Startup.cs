using Microsoft.Owin;
using Owin;
using System.Web.Http;
using System.Web.OData.Extensions;
using System.Web.OData.Routing.Conventions;
using System.Linq;
using Microsoft.OData.Edm;
using System.Web.OData.Builder;
using GenericOData.infrastructure;
using GenericOData.models;

[assembly: OwinStartup(typeof(GenericOData.Startup))]

namespace GenericOData
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("api", "api/{controller}/{action}");
            ConfigureOdata(config);
            app.UseWebApi(config);
        }

        static void ConfigureOdata(HttpConfiguration config)
        {
            var routePrefix = "data";
            var routeName = "odata";

            config.MapODataServiceRoute(
                routeName,
                routePrefix,
                GetModel(),
                new DynamicPathHandler(),
                ODataRoutingConventions.CreateDefault());
            config.Count().Filter().Select().OrderBy();
            config.AddODataQueryFilter();
        }

        static IEdmModel GetModel()
        {
            var builder = new ODataModelBuilder();
            var itemType = builder.EntityType<GenericItem>();
            itemType.HasKey(i => i.id);
            itemType.HasDynamicProperties(i => i.DynamicProperties);
            builder.EntitySet<GenericItem>("data");
            return builder.GetEdmModel();
        }
    }
}
