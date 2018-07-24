using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace SerializationExercise {
    class Person {
        public string Name { get; set; }
        public int Age { get; set; }
        // public static DateTime DateOfBirth { get; set;}
        public Person Spause { get; set; }
    }

    public static class Serializer {
        private static void WriteToXml (XmlDocument doc, XmlElement first, Item item) {
            XmlElement element = doc.CreateElement ("", "item", "");
            XmlElement elementHash = doc.CreateElement ("", "hashCode", "");
            XmlText textHash = doc.CreateTextNode (item.HashCode.ToString ());
            XmlElement elementType = doc.CreateElement ("", "type", "");
            XmlText textType = doc.CreateTextNode (item.Type);
            XmlElement elementValue = doc.CreateElement ("", "value", "");
            XmlText textValue = doc.CreateTextNode (item.Value);

            XmlElement elementItems = doc.CreateElement ("", "items", "");

            foreach (var subItem in item.Fields) {
                XmlElement sub = doc.CreateElement ("", "item", "");
                XmlElement name = doc.CreateElement ("", "name", "");
                XmlText textName = doc.CreateTextNode (subItem.Key);
                XmlElement val = doc.CreateElement ("", "value", "");
                XmlText textVal = doc.CreateTextNode (subItem.Value.ToString ());
                name.AppendChild (textName);
                val.AppendChild (textVal);
                sub.AppendChild (name);
                sub.AppendChild (val);
                sub.AppendChild (val);
                sub.AppendChild (val);
                elementItems.AppendChild (sub);
            }

            elementHash.AppendChild (textHash);
            element.AppendChild (elementHash);
            elementType.AppendChild (textType);
            element.AppendChild (elementType);
            elementValue.AppendChild (textValue);
            element.AppendChild (elementValue);

            element.AppendChild (elementItems);

            first.AppendChild (element);
        }

        public static void Serialize<T> (T root, Stream stm) where T : class {
            var helper = new SerializationHelper ();
            var dict = helper.SerializeFields (root);            
            var rootHashCode = root.GetHashCode ();

            XmlDocument doc = new XmlDocument ();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration ("1.0", "UTF-8", null);            
            doc.InsertBefore (xmlDeclaration, null);

            XmlElement first = doc.CreateElement ("", "first", "");
            XmlText rootHash = doc.CreateTextNode (rootHashCode.ToString ());
            first.AppendChild (rootHash);

            foreach (var item in dict.Values) {
                WriteToXml (doc, first, item);
            }

            doc.AppendChild (first);

            doc.Save (stm);
        }
        public static T Deserialize<T> (Stream stm) where T : class {
            XmlDocument doc = new XmlDocument ();
            doc.Load (stm);
            XmlNodeList nodelist = doc.SelectNodes ("first/item");

            Dictionary<int, Item> itemCache = new Dictionary<int, Item> ();

            foreach (XmlNode node in nodelist) {
                Item item = new Item ();
                item.HashCode = int.Parse (node.SelectSingleNode ("hashCode").FirstChild.Value);
                item.Type = node.SelectSingleNode ("type").FirstChild.Value;
                var value = node.SelectSingleNode ("value")?.FirstChild?.Value;
                if (value != null) {
                    item.Value = value;
                }
                var fields = node.SelectNodes ("items/item");
                if (fields != null && fields.Count > 0) {
                    item.Fields = new Dictionary<string, int> (fields.Count);

                    foreach (XmlNode sub in fields) {
                        var name = sub.SelectSingleNode ("name").FirstChild.Value;
                        var fieldHashCode = int.Parse (sub.SelectSingleNode ("value").FirstChild.Value);
                        item.Fields.Add (name, fieldHashCode);
                    }
                }

                if (!itemCache.ContainsKey (item.HashCode))
                    itemCache.Add (item.HashCode, item);
            }

            int rootHashCode = int.Parse (doc.SelectSingleNode ("first").FirstChild.Value);

            DeserializationHelper helper = new DeserializationHelper (itemCache);
            var result = helper.DeserializeFromCache (rootHashCode);

            return (T) result;
        }
    }

    class Program {      
        static void Main (string[] args) {
            var p1 = new Person { Name = "Homer", Age = 40 };
            var p2 = new Person { Name = "Marge", Age = 30 };
            p1.Spause = p2;
            p2.Spause = p1;
            using (var ms = new MemoryStream ()) {
                Serializer.Serialize (p1, ms);
                ms.Position = 0;
                var p3 = Serializer.Deserialize<Person> (ms);
                Debug.Assert (p3.Name == "Homer");
                Debug.Assert (p3.Age == 40);
                var p4 = p3.Spause;
                Debug.Assert (p4.Spause == p3);
                Debug.Assert (p4.Age == 30);
            }
        }
    }
}