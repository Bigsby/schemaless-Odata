# Generic Odata
A schemaless OData controller

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

4. Add generic item model:
   ```csharp
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    namespace GenericOData.models
    {
        public class GenericItem
        {
            public string id { get; set; }

            public JObject InnerObject { get; set; }

            public IDictionary<string, object> DynamicProperties
            {
                get
                {
                    return ConvertDynamicProperties(id, InnerObject);
                }
            }

            private static IDictionary<string, object> ConvertDynamicProperties(string id, JObject token)
            {
                var result = new Dictionary<string, object>();
                foreach (var prop in token.Properties())
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
                return new GenericItem
                {
                    id = token.Value<string>("id") ?? computedId,
                    InnerObject = (JObject)token
                };
            }

            private static IEnumerable<GenericItem> ConvertObjectArray(string parentId, JArray array)
            {
                var count = 1;
                foreach (var item in array)
                    yield return BuildItemFromToken(item, $"{parentId}_{count++}");
            }
        }
    }
   ```

5. Add data reader, in this case, reading data from *App_Data* folder:
   ```csharp
    using GenericOData.models;
    using Newtonsoft.Json.Linq;
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

            private static GenericItem ReadItem(string filePath, string id)
            {
                var fileContent = File.ReadAllText(filePath);
                return new GenericItem
                {
                    id = id,
                    InnerObject = JObject.Parse(fileContent)
                };
            }
        }
    }
   ```

6. Add controller, with *Add > Class* instead of *Add > Controller*:
   ```csharp
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
        "specific": "this is specific property"
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

10. Run and test
    > Replace PORT with the port generated by Visual Studio

    1. Hit *F5*
    2. Browse to [http://localhost:PORT/data/collection](http://localhost:PORT/data/collection)

    The response should be an empty OData Entity Set:
    ```
    {"@odata.context":"http://localhost:51130/data/$metadata#data","value":[{"id":"one","name":"Item one","value":3,"specific":"this is specific property"},{"id":"two","name":"Item two","value":5,"another":"this is another property"}]}
    ```

    Prettied would look like this:
    ```json
    {  
      "@odata.context":"http://localhost:51130/data/$metadata#data",
      "value":[  
        {  
          "id":"one",
          "name":"Item one",
          "value":3,
          "specific":"this is specific property"
        },
        {  
          "id":"two",
          "name":"Item two",
          "value":5,
          "another":"this is another property"
        }
      ]
    }
    ```
    3. Test single item. Browse to [http://localhost:PORT/data/collection('two')](http://localhost:PORT/data/collection('two'))

    The (prettied) response would look like this:
    ```json
    {  
        "@odata.context":"http://localhost:51130/data/$metadata#data/$entity",
        "id":"two",
        "name":"Item two",
        "value":5,
        "another":"this is another property"
    }
    ```

11. Test filter
    > Replace PORT with the port generated by Visual Studio.

    1. Browse to [http://localhost:PORT/data/collection?$filter=value eq 3](http://localhost:PORT/data/collection?$filter=value eq 3)
       
       Result should be (prettied):
       ```json
        {  
            "@odata.context":"http://localhost:51130/data/$metadata#data",
            "value":[  
                {  
                "id":"one",
                "name":"Item one",
                "value":3,
                "specific":"this is specific property"
                }
            ]
        }
       ```
    2. Browse to [http://localhost:PORT/data/collection?$filter=contains(another,'this')](http://localhost:PORT/data/collection?$filter=contains(another,'this'))
       
       Result should be (prettied):
       ```json
        {  
            "@odata.context":"http://localhost:51130/data/$metadata#data",
            "value":[  
                {  
                "id":"two",
                "name":"Item two",
                "value":5,
                "another":"this is another property"
                }
            ]
        }
       ```
         > Note that *another* is (dynamic) property that only exists in one of the objects.

12. Add complex properties
    1. E.g. *App_Data/collection/one.json*
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
    2. Browse to [http://localhost:PORT/data/collection('one')](http://localhost:PORT/data/collection('one'))

       The (prettied) response would look like this:
       ```json
        {  
            "@odata.context":"http://localhost:51130/data/$metadata#data/$entity",
            "id":"one",
            "name":"Item one",
            "value":3,
            "specific":"this is specific property",
            "strings@odata.type":"#Collection(String)",
            "strings":[  
                "this is one string",
                "this is another string"
            ],
            "integers@odata.type":"#Collection(Int32)",
            "integers":[  
                1,
                2,
                3
            ],
            "objects@odata.type":"#Collection(GenericOData.models.GenericItem)",
            "objects":[  
                {  
                "@odata.type":"#GenericOData.models.GenericItem",
                "id":"one_1",
                "name":"item one"
                },
                {  
                "@odata.type":"#GenericOData.models.GenericItem",
                "id":"one_2",
                "name":"item two"
                }
            ],
            "innerItem":{  
                "@odata.type":"#GenericOData.models.GenericItem",
                "id":"one_innerItem",
                "name":"Inner item",
                "innerProperty":23
            }
        }
       ```