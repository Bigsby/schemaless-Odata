using System;
using System.Text.RegularExpressions;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Template;

namespace GenericOData.infrastructure
{
    public class DynamicPathHandler : DefaultODataPathHandler
    {
        private static Regex _collectionRegex = new Regex("^([^/(\\?%]+)", RegexOptions.Compiled);

        public override ODataPath Parse(string serviceRoot, string odataPath, IServiceProvider requestContainer)
        {
            return base.Parse(serviceRoot, _collectionRegex.Replace(odataPath, "data"), requestContainer);
        }

        public override ODataPathTemplate ParseTemplate(string odataPathTemplate, IServiceProvider requestContainer)
        {
            return base.ParseTemplate(odataPathTemplate, requestContainer);
        }

        internal static string GetCollection(string path)
        {
            return _collectionRegex.Match(path).Groups[1].Value;
        }
    }
}