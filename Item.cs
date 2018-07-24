using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SerializationExercise {
    /// <summary>
    /// This container is used for storing object data
    /// </summary>
    public class Item {
        /// <summary>
        /// The HashCode of the object
        /// </summary>
        public int HashCode;

        /// <summary>
        /// The type of the object
        /// </summary>
        public string Type;

        /// <summary>
        /// The value of the object if it's primitive/builtin 'base' type from namespace System
        /// </summary>
        public string Value;

        /// <summary>
        /// For objects that are not primitive/builtin, this contains Key:(field name) of the field, Value:(HashCode) of the field 
        /// </summary>
        public Dictionary<string, int> Fields = new Dictionary<string, int> ();
    }

    public class SerializationHelper {
        private Dictionary<int, Item> cache = new Dictionary<int, Item> ();
        private BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public Dictionary<int, Item> SerializeFields<T> (T t) {
            CreateCache (t);
            return cache;
        }

        private void CreateCache<T> (T t) {
            var hashCode = t.GetHashCode ();

            Item item;
            if (cache.TryGetValue (hashCode, out item))
                return;
            else
                item = new Item ();

            item.Type = t.GetType ().FullName;
            item.HashCode = hashCode;

            cache.Add (hashCode, item);

            if (t.GetType ().Namespace == "System") {
                item.Value = t.ToString ();
            } else {
                var fields = t.GetType ().GetFields (bindingFlags);
                foreach (var field in fields) {                    
                    CreateCache (field.GetValue (t));
                    item.Fields.Add (field.Name, field.GetValue (t).GetHashCode ());
                }
            }
        }
    }

    public class DeserializationHelper {
        private Dictionary<int, object> objectCache = new Dictionary<int, object> ();
        private Dictionary<int, Item> itemCache;
        private BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public DeserializationHelper (Dictionary<int, Item> itemCache) {
            this.itemCache = itemCache;
        }

        public object DeserializeFromCache (int rootHashCode) {
            var itemRoot = itemCache[rootHashCode];
            return Generate (itemRoot);
        }

        private object Generate (Item item) {
            if (objectCache.ContainsKey (item.HashCode)) {
                return objectCache[item.HashCode];
            }

            // System.String does not have default constructor and is initiated as a special case.
            // Obviously this would have to be extended for other types that dont have default constructors.
            // This is the reason why deserialization by default requires default constructors
            var instance = item.Type == "System.String" ? Activator.CreateInstance (Type.GetType (item.Type),
                new object[] { item.Value.ToCharArray () }) : Activator.CreateInstance (Type.GetType (item.Type));

            if (!objectCache.ContainsKey (item.HashCode))
                objectCache.Add (item.HashCode, instance);

            var fields = instance.GetType ().GetFields (bindingFlags);

            if (Type.GetType (item.Type).Namespace == "System" && item.Type != "System.String") {
                var value = Convert.ChangeType (item.Value, Type.GetType (item.Type));
                fields.First ().SetValue (instance, value);
            } else {
                int hash;
                foreach (var field in fields) {                    
                    if(item.Fields.TryGetValue(field.Name, out hash)){                                              
                        if (objectCache.ContainsKey (hash)) {
                            field.SetValue (instance, objectCache[hash]);
                        } else {
                            var val = Generate (itemCache[hash]);
                            field.SetValue (instance, val);
                        }
                    }                  
                }
            }

            return instance;
        }
    }
}