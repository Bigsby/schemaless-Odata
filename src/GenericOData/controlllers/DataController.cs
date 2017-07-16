using GenericOData.infrastructure;
using GenericOData.models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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

        public IHttpActionResult Post([FromBody]JToken item)
        {
            var jItem = (JObject)item;
            var collection = GetCollection();
            var id = item.Value<string>("id");
            jItem.Remove("id");
            var result = DataProvider.SaveItem(collection, id, jItem);
            if (result)
                return Created($"data/{collection}('{id}')", item);
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