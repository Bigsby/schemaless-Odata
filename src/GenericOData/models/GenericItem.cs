using System.Collections.Generic;

namespace GenericOData.models
{
    public class GenericItem
    {
        public string id { get; set; }

        public IDictionary<string, object> DynamicProperties { get; set; }
    }
}