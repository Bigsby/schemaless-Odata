# Generic Odata
A generic and schemaless OData controller

Steps (all in Visual Studio):
1. New Project - ASP.Net Web Application (.Net Framework)

2. Add NuGet Pacakges:
   - Microsoft.Owin.Host.SystemWeb
   - Microsoft.AspNet.WebApi.Owin
   - Microsoft.AspNet.OData

3. Add custom OData Path Handler:
   ```csharp
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
    ```
    - Removes the collection part of ODataPath so that the generic *data* Entity Set receives the reques.

4. Add generic model:
   ```csharp
    using System.Collections.Generic;

    namespace GenericOData.models
    {
        public class GenericItem
        {
            public string id { get; set; }

            public IDictionary<string, object> DynamicProperties { get; set; }
        }
    }
   ```
   - A simple model that, besides the mandatory id, holds dynamic properties for [OData Open Type](https://docs.microsoft.com/en-us/aspnet/web-api/overview/odata-support-in-aspnet-web-api/odata-v4/use-open-types-in-odata-v4)

5. Add data reader, in this case, reading data from *App_Data* folder:
   ```csharp
    using GenericOData.models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Web.Hosting;

    namespace GenericOData.infrastructure
    {
        internal static class DataProvider
        {
            public static IEnumerable<GenericItem> GetItems(string collection)
            {
                var folderPath = HostingEnvironment.MapPath($"~/App_Data/{collection}");
                if (!Directory.Exists(folderPath))
                    yield break;

                foreach (var filePath in Directory.GetFiles(folderPath))
                    yield return ReadItem(filePath, Path.GetFileNameWithoutExtension(filePath));
            }

            public static GenericItem GetItem(string collection, string id)
            {
                var filePath = HostingEnvironment.MapPath($"~/App_Data/{collection}/{id}.json");
                if (!File.Exists(filePath))
                    return null;

                return ReadItem(filePath, id);
            }

            public static DataResult SaveItem(string collection, GenericItem item)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(item.DynamicProperties);
                    var filePath = HostingEnvironment.MapPath($"~/App_Data/{collection}/{item.id}.json");

                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    File.WriteAllText(filePath, json);
                    return DataResult.Successul;
                }
                catch (Exception ex)
                {
                    return DataResult.Fail(ex);
                }
            }

            public static DataResult DeleteItem(string collection, string id)
            {
                try
                {
                    var filePath = HostingEnvironment.MapPath($"~/App_Data/{collection}/{id}.json");
                    File.Delete(filePath);
                    return DataResult.Successul;
                }
                catch (Exception ex)
                {
                    return DataResult.Fail(ex);
                }
            }

            private static GenericItem ReadItem(string filePath, string id)
            {
                var fileContent = File.ReadAllText(filePath);
                return new GenericItem
                {
                    id = id,
                    DynamicProperties = ConvertDynamicProperties(id, JObject.Parse(fileContent))
                };
            }

            private static IDictionary<string, object> ConvertDynamicProperties(string id, JObject token)
            {
                var result = new Dictionary<string, object>();
                if (null == token)
                    return result;

                foreach (var prop in token?.Properties())
                {
                    if (null == prop.Value) continue;
                    result[prop.Name] = ConvertValue(prop.Name, id, prop.Value);
                }

                return result;
            }

            private static object ConvertValue(string propertyName, string parentId, JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Comment:
                    case JTokenType.Property:
                    case JTokenType.Constructor:
                    case JTokenType.None:
                    case JTokenType.Undefined:
                    case JTokenType.Null:
                        return null;
                    case JTokenType.Object:
                        return BuildItemFromToken(token, $"{parentId}_{propertyName}");
                    case JTokenType.Array:
                        return ConvertArray(parentId, (JArray)token);
                    case JTokenType.Integer:
                        return token.ToObject<int>();
                    case JTokenType.Float:
                        return token.ToObject<double>();
                    case JTokenType.String:
                    case JTokenType.Uri:
                    case JTokenType.Raw:
                    default:
                        return token.ToObject<string>();
                    case JTokenType.Boolean:
                        return token.ToObject<bool>();
                    case JTokenType.Date:
                        return token.ToObject<DateTime>();
                    case JTokenType.Bytes:
                        return token.ToObject<byte[]>();
                    case JTokenType.Guid:
                        return token.ToObject<Guid>();
                    case JTokenType.TimeSpan:
                        return token.ToObject<TimeSpan>();
                }
            }

            private static object ConvertArray(string parentId, JArray array)
            {
                if (array.Count == 0)
                    return new string[0];

                switch (array.First.Type)
                {
                    case JTokenType.Integer:
                        return array.ToObject<int[]>();
                    case JTokenType.Float:
                        return array.ToObject<double[]>();
                    case JTokenType.Boolean:
                        return array.ToObject<bool[]>();
                    default:
                    case JTokenType.String:
                    case JTokenType.Undefined:
                    case JTokenType.Null:
                    case JTokenType.Raw:
                        return array.ToObject<string[]>();
                    case JTokenType.Date:
                        return array.ToObject<DateTime[]>();
                    case JTokenType.Bytes:
                        return array.ToObject<byte[]>();
                    case JTokenType.Guid:
                        return array.ToObject<Guid[]>();
                    case JTokenType.Uri:
                        return array.ToObject<Uri[]>();
                    case JTokenType.TimeSpan:
                        return array.ToObject<TimeSpan[]>();
                    case JTokenType.Object:
                        return ConvertObjectArray(parentId, array);
                }
            }

            private static GenericItem BuildItemFromToken(JToken token, string computedId)
            {
                var id = token.Value<string>("id") ?? computedId;
                return new GenericItem
                {
                    id = id,
                    DynamicProperties = ConvertDynamicProperties(id, (JObject)token)
                };
            }

            private static IEnumerable<GenericItem> ConvertObjectArray(string parentId, JArray array)
            {
                var count = 1;
                foreach (var item in array)
                    yield return BuildItemFromToken(item, $"{parentId}_{count++}");
            }
        }

        internal class DataResult
        {
            public bool Success { get; private set; }
            public Exception Error { get; private set; }

            public static DataResult Successul => new DataResult { Success = true };

            public static DataResult Fail(Exception ex) => new DataResult { Error = ex };

            public static implicit operator bool(DataResult result)
            { return result.Success; }

            private DataResult() { }
        }
    }
   ```
   - The *special* deserialization of dynamic properties is necessary because [JSON.NET](http://www.newtonsoft.com/json):
     - Deserializes integers as *System.Int64* instead of *System.Int32*, which is what **OData** defaults to;
     - Deserializes *arrays* to *JArray* which is unknown to **OData**;
     - Deserializes *objects* to *JObject* which is unknown to **OData**.

6. Add controller, with *Add > Class* instead of *Add > Controller*:
   ```csharp
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
   ```

7. *Add > Owin Startup class*:
   ```csharp
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
                config.MessageHandlers.Add(new MethodOverrideHandler());
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
   ```
   - *MethodOverrideHandler* is used because some hostings (like **GoDaddy**) do not accept *DELETE* HTTP method. Code was *stolen* from [this Scott Hanselman post.](https://www.hanselman.com/blog/HTTPPUTOrDELETENotAllowedUseXHTTPMethodOverrideForYourRESTServiceWithASPNETWebAPI.aspx).

8. Run and test:
   1. Hit *F5*
   2. Browse to [http://localhost:PORT/data/collection](http://localhost:PORT/data/collection)
      - Replace PORT with the port generated by Visual Studio

    The restul should be an empty OData Entity Set:
    ```
    {"@odata.context":"http://localhost:51130/data/$metadata#data","value":[]}
    ```

9. Add data
   1. *Add > Add ASP.Net Folder > App_Data*
   2. Add a collection in *App_Data* by *Add > Add Folder*. The name of this(ese) folder(s) will be the collection name to use in the URL.
   3. Add data items. 
      - Unfortunately Visual Studio does not *allow* adding JSON files in *App_Data* folder. Just add a *JSON File* anywhere in the project and drag it to *App_Data* folder. From then on, one can just copy/paste it to the relevant collection folder and renaming the file.
      - *.json* extension is mandatory.
      - The name of the file (without the extension) will be used as the item's *id* value.

    Sample items...
    - *App_Data/collection/one.json*
      ```json
      {
        "name": "Item one",
        "value": 3,
        "specific": "this is specific property",
        "strings": [
            "this is one string",
            "this is another string"
        ],
        "integers": [
            1,
            2,
            3
        ],
        "objects": [
        {
            "name": "item one"
        },
        {
            "name": "item two"
        }
        ],
        "innerItem": {
            "name": "Inner item",
            "innerProperty": 23
        }
      }
      ```
    - *App_Data/collection/two.json*
      ```json
      {
        "name": "Item two",
        "value": 5,
        "another": "this is another property"
      }
      ```

10. Run and test. Use [this Postman collection].