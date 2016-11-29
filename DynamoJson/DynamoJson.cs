using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Autodesk.DesignScript.Runtime;

namespace DynamoJson
{
    public class JsonBuilder
    {
        private JsonBuilder() { }

        public static void StringToFile(string str, string filePath)
        {
            System.IO.File.WriteAllText(filePath, str);
        }

        public static string ToJsonString([ArbitraryDimensionArrayImport]object data, bool formatted = false)
        {
            return JsonConvert.SerializeObject(
                RemoveReferences(data),
                formatted ? Formatting.Indented : Formatting.None);
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
    }

    public class JsonParser
    {
        private JsonParser() { }

        public static string StringFromFile(string filePath)
        {
            return System.IO.File.ReadAllText(filePath);
        }

        public static object ToDictionary(string json)
        {
            var jdata = JToken.Parse(json);
            return ParseToken(jdata, true);
        }

        public static object ToSublists(string json)
        {
            var jdata = JToken.Parse(json);
            return ParseToken(jdata, false);
        }

        private static object ParseToken(JToken jtoken, bool dictOrList)
        {
            // Convert Array to Dynamo list
            if (jtoken.Type == JTokenType.Array)
            {
                var list = new List<object>();
                foreach (var child in jtoken.Children())
                    list.Add(ParseToken(child, dictOrList));
                return list;
            }

            // Convert Object to Dynamo dictionary
            if (jtoken.Type == JTokenType.Object && dictOrList)
            {
                var dict = new Dictionary<string, object>();
                foreach (var property in (jtoken as JObject).Properties())
                {
                    var key = property.Name;
                    var value = property.Value;
                    dict[key] = ParseToken(value, dictOrList);
                }
                return dict;
            }

            // Convert Object to Dynamo sublist
            if (jtoken.Type == JTokenType.Object && !dictOrList)
            {
                var list = new List<object>();
                foreach (var property in (jtoken as JObject).Properties())
                {
                    var key = property.Name;
                    var value = property.Value;
                    list.Add(new List<object>() { key, ParseToken(value, dictOrList) });
                }
                return list;
            }

            // For all other types
            return jtoken.ToString();
        }
    }
}
