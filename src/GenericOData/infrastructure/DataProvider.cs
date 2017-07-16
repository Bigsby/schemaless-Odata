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

        public static DataResult SaveItem(string collection, string id, JObject item)
        {
            try
            {
                var json = JsonConvert.SerializeObject(item);
                var filePath = HostingEnvironment.MapPath($"~/App_Data/{collection}/{id}.json");

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
                DynamicProperties = GenericItem.ConvertDynamicProperties(id, JObject.Parse(fileContent))
            };
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