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