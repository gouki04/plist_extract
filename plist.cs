using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

public static class Plist
{
    public class Value : IEnumerable<KeyValuePair<string, Value>>
    {
        public enum EType
        {
            Null = 0,
            String,
            Boolean,
            Real,
            Integer,
            Dict,
            Array,
        }

        public EType type = EType.Null;
        public bool bool_value = false;
        public string string_value = null;
        public float real_value = 0f;
        public int integer_value = 0;
        public Dictionary<string, Value> dict_value = null;
        public List<Value> array_value = null;

        public bool IsNull() { return type == EType.Null; }

        public static implicit operator bool(Value value)
        {
            if (value.type == EType.Boolean) {
                return value.bool_value;
            }
            return false;
        }

        public static implicit operator float(Value value)
        {
            if (value.type == EType.Real) {
                return value.real_value;
            }
            return 0f;
        }

        public static implicit operator int(Value value)
        {
            if (value.type == EType.Integer) {
                return value.integer_value;
            }
            return 0;
        }

        public static implicit operator string(Value value)
        {
            if (value.type == EType.String) {
                return value.string_value;
            }
            return string.Empty;
        }

        public Value this[string key]
        {
            get {
                if (type == EType.Dict && dict_value != null) {
                    Value ret;
                    if (dict_value.TryGetValue(key, out ret)) {
                        return ret;
                    }
                }
                return Null();
            }
        }

        public Value this[int index]
        {
            get {
                if (type == EType.Array && array_value != null) {
                    if (index >= 0 && index < array_value.Count) {
                        return array_value[index];
                    }
                }
                return Null();
            }
        }

        public int Count
        {
            get {
                if (type == EType.Array && array_value != null) {
                    return array_value.Count;
                }
                return 0;
            }
        }

        public override string ToString()
        {
            switch (type) {
                case EType.Null: return "null";
                case EType.Boolean: return bool_value.ToString();
                case EType.String: return string_value;
                case EType.Real: return real_value.ToString();
                case EType.Integer: return integer_value.ToString();
                case EType.Array: return array_value.ToString();
                case EType.Dict: return dict_value.ToString();
            }

            return string.Empty;
        }

        public static Value Null() { return new Value() { type = EType.Null }; }
        public static Value Bool(bool v) { return new Value() { type = EType.Boolean, bool_value = v }; }
        public static Value String(string v) { return new Value() { type = EType.String, string_value = v }; }
        public static Value Real(float v) { return new Value() { type = EType.Real, real_value = v }; }
        public static Value Integer(int v) { return new Value() { type = EType.Integer, integer_value = v }; }
        public static Value Dict(Dictionary<string, Value> v) { return new Value() { type = EType.Dict, dict_value = v }; }
        public static Value Array(List<Value> v) { return new Value() { type = EType.Array, array_value = v }; }

        public static Value FromXmlNode(XmlNode node)
        {
            switch (node.Name) {
                case "string": return String(node.InnerText);
                case "true": return Bool(true);
                case "false": return Bool(false);
                case "real": return Real(float.Parse(node.InnerText));
                case "integer": return Integer(int.Parse(node.InnerText));
                case "dict": {
                        var dict = new Dictionary<string, Value>();
                        foreach (XmlNode child_node in node) {
                            if (child_node.Name == "key") {
                                dict.Add(child_node.InnerText, FromXmlNode(child_node.NextSibling));
                            }
                        }

                        return Dict(dict);
                    }
                case "array": {
                        var array = new List<Value>();
                        foreach (XmlNode child_node in node) {
                            array.Add(FromXmlNode(child_node));
                        }

                        return Array(array);
                    }
                default: {
                        Console.WriteLine(string.Format("Invalid Key = {0}", node.Name));
                        return null;
                    }
            }
        }

        public IEnumerator<KeyValuePair<string, Value>> GetEnumerator()
        {
            if (type == EType.Dict && dict_value != null) {
                foreach (var kvp in dict_value) {
                    yield return kvp;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static Value Load(string file_path)
    {
        var doc = new XmlDocument();
        doc.Load(file_path);

        // DocumentElement是plist节点，从第一个孩子节点开始解析
        var root = doc.DocumentElement.FirstChild;
        return Value.FromXmlNode(root);
    }
}
