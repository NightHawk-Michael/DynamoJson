using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Autodesk.DesignScript.Runtime;

namespace DynamoJson
{
    public class FileHelper
    {
        private FileHelper() { }

        /// <summary>
        /// Writes a string into a file.
        /// </summary>
        /// <param name="str">The string to be written into a file</param>
        /// <param name="filePath">The file path</param>
        /// <returns>The full path of the written file</returns>
        public static string WriteToFile(string str, string filePath)
        {
            System.IO.File.WriteAllText(filePath, str);
            return new System.IO.FileInfo(filePath).FullName;
        }

        /// <summary>
        /// Reads the contents of a file into a string.
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <returns>File contents</returns>
        public static string ReadFromFile(string filePath)
        {
            return System.IO.File.ReadAllText(filePath);
        }
    }

    public class JsonBuilder
    {
        private JsonBuilder() { }

        /// <summary>
        /// Serializes Dynamo data into a JSON string. Set the "formatted" boolean to true for
        /// multi-line JSON string, or to false for one-line JSON. To write the result into a file,
        /// connect this node to a "FileHelper.WriteToFile" node.
        /// </summary>
        /// <param name="data">Dynamo object(s) of any dimension</param>
        /// <param name="formatted">True for multi-line JSON string, false for one-line JSON string</param>
        /// <returns>Serialized JSON string</returns>
        public static string CreateJsonString([ArbitraryDimensionArrayImport]object data, bool formatted = false)
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
            if (datatype.IsPrimitive || datatype.ToString().Equals("System.String")) return data;

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

        /// <summary>
        /// Deserializes a JSON string. This node will convert JSON objects, if any, into Dynamo
        /// dictionaries. Connect this node to a "GetKeys" node to get the keys of the dictionary.
        /// </summary>
        /// <param name="json">JSON string to be deserialized</param>
        /// <returns>Deserialized JSON data</returns>
        public static object ToDictionary(string json)
        {
            var jdata = JToken.Parse(json);
            return ParseToken(jdata, true);
        }

        /// <summary>
        /// Deserializes a JSON string. This node will convert JSON objects, if any, into Dynamo
        /// sublists in terms of {key,value} pairs.
        /// </summary>
        /// <param name="json">JSON string to be deserialized</param>
        /// <returns>Deserialized JSON data</returns>
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

            // For primitive-like types
            if (jtoken.Type == JTokenType.Boolean)
                return jtoken.ToString().Equals("True");

            if (jtoken.Type == JTokenType.Integer)
                return int.Parse(jtoken.ToString());

            if (jtoken.Type == JTokenType.Float)
                return double.Parse(jtoken.ToString());

            return jtoken.ToString();
        }
    }
}
