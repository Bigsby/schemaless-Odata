using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace GenericOData.models
{
    public class GenericItem
    {
        public string id { get; set; }

        public IDictionary<string, object> DynamicProperties { get; set; }

        internal static IDictionary<string, object> ConvertDynamicProperties(string id, JObject token)
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
                DynamicProperties = GenericItem.ConvertDynamicProperties(id, (JObject)token)
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