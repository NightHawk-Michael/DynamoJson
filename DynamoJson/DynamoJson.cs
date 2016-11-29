using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Autodesk.DesignScript.Runtime;

namespace DynamoJson
{
    public class DynamoJson
    {
        public static string ToJsonString([ArbitraryDimensionArrayImport]object data, bool indented = false)
        {
            return JsonConvert.SerializeObject(
                RemoveReferences(data),
                indented ? Formatting.Indented : Formatting.None);
        }

        private static object RemoveReferences(object data)
        {
            // If a list, get all list items
            if (data is ArrayList)
            {
                var list = new List<object>();
                foreach (var item in data as ArrayList)
                    list.Add(RemoveReferences(item));
                return list;
            }

            var datatype = data.GetType();

            // If a primitive, return the value
            if (datatype.IsPrimitive) return data;

            // If an object, reflect all properties
            var properties = datatype.GetProperties();
            var dict = new Dictionary<string, object>();
            dict["Type"] = datatype.FullName;
            foreach (var prop in properties)
                dict[prop.Name] = prop.GetValue(data, null).ToString();
            return dict;
        }

        public static void ToJsonFile([ArbitraryDimensionArrayImport]object data, string filePath, bool formatting = false)
        {
            System.IO.File.WriteAllText(filePath, ToJsonString(data, formatting));
        }

        public static object FromJsonString(string json)
        {
            var jdata = JToken.Parse(json);
            return ParseJToken(jdata);
        }

        private static object ParseJToken(JToken jtoken)
        {
            // Convert Array to Dynamo list
            if (jtoken.Type == JTokenType.Array)
            {
                var list = new List<object>();
                foreach (var child in jtoken.Children())
                    list.Add(ParseJToken(child));
                return list;
            }

            // Convert Object to Dynamo dictionary
            if (jtoken.Type == JTokenType.Object)
            {
                var dict = new Dictionary<string, object>();
                foreach (var property in (jtoken as JObject).Properties())
                {
                    var key = property.Name;
                    var value = property.Value;
                    dict[key] = ParseJToken(value);
                }
                return dict;
            }

            // For all other types
            return jtoken.ToString();
        }
    }
}
