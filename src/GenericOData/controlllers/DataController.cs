using GenericOData.infrastructure;
using GenericOData.models;
using System.Linq;
using System.Web.Http;
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

        public IHttpActionResult Post(GenericItem item)
        {
            var collection = GetCollection();

            var result = DataProvider.SaveItem(collection, item);
            if (result)
                return Created($"data/{collection}('{item.id}')", item);
            return InternalServerError(result.Error);
        }

        public IHttpActionResult Delete([FromODataUri]string key)
        {
            var result = DataProvider.DeleteItem(GetCollection(), key);
            if (result)
                return Ok();
            return InternalServerError(result.Error);
        }

        private string GetCollection()
        {
            return DynamicPathHandler.GetCollection((string)RequestContext.RouteData.Values["odataPath"]);
        }
    }
}