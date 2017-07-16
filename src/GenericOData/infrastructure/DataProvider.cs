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