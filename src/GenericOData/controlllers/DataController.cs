using GenericOData.infrastructure;
using GenericOData.models;
using System.Linq;
using System.Web.OData;

namespace GenericOData.controlllers
{
    public class DataController : ODataController
    {
        public IQueryable<GenericItem> Get()
        {
            return DataProvider.GetItems(GetCollection()).AsQueryable();
        }

        public GenericItem Get([FromODataUri]string key)
        {
            return DataProvider.GetItem(GetCollection(), key);
        }

        private string GetCollection()
        {
            return DynamicPathHandler.GetCollection((string)RequestContext.RouteData.Values["odataPath"]);
        }
    }
}