using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Zero.Converters
{
    public struct DataConverter
    {

        /// <summary>
        /// Does not insert spaces in output
        /// Do not supported Dictionary and List<T[]>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>adasd</returns>
        public static string ObjectToString(object obj)
        {
            return ObjectToString(obj, false);
        }
        /// <summary>
        /// Do not supported Dictionary<T> and List<T[]>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="spaces"></param>
        /// <returns></returns>
        public static string ObjectToString(object obj, bool spaces)
        {
            bool _isFirst = true;
            bool _isFirstInArray;
            string resultStr = "{";

            string space = spaces ? " " : "";
            foreach (var fieldInfo in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (!_isFirst) resultStr += "," + space;
                else _isFirst = false;
                resultStr += fieldInfo.Name + ":" + space;
                if (IsDataType(fieldInfo.FieldType.Name))
                {
                    if (fieldInfo.FieldType.IsArray)
                    { //Array[]
                        resultStr += "[";
                        _isFirstInArray = true;
                        foreach (var item in (Array)GetValue(fieldInfo, obj))
                        {
                            if (!_isFirstInArray) resultStr += "," + space;
                            else _isFirstInArray = false;
                            resultStr += "\"" + item.ToString() + "\"";
                        }
                        resultStr += "]";
                    }
                    else
                    {
                        resultStr += "\"" + GetValue(fieldInfo, obj).ToString() + "\""; //DataType
                    }
                }
                else
                {
                    if (fieldInfo.FieldType.Name.Contains("List"))
                    { //List<DataType>              
                        resultStr += "[";
                        _isFirstInArray = true;
                        var list = GetValue(fieldInfo, obj);
                        for (int i = 0; i < (int)list.GetType().GetProperty("Count").GetValue(list, null); i++)
                        {
                            if (!_isFirstInArray) resultStr += "," + space;
                            else _isFirstInArray = false;
                            resultStr += ObjectToString(list.GetType().GetProperty("Item")
                                .GetValue(list, new object[] { i }), spaces);
                        }
                        resultStr += "]";
                    }
                    else
                    {
                        if (fieldInfo.FieldType.IsArray)
                        { //Class[]
                            resultStr += "[";
                            _isFirstInArray = true;
                            var list = GetValue(fieldInfo, obj);
                            for (int i = 0; i < ((Array)list).Length; i++)
                            {
                                if (!_isFirstInArray) resultStr += "," + space;
                                else _isFirstInArray = false;
                                resultStr += ObjectToString(((Array)list).GetValue(i));
                            }
                            resultStr += "]";
                        }
                        else
                        { //Class(OK) or other(Err) 
                            resultStr += ObjectToString(fieldInfo.GetValue(obj), spaces);
                        }
                    }
                }
            }
            return resultStr + "}";
        }
        /// <summary>
        /// Do not supported Dictionary<T>, List<T[]>, array[]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="str"></param>
        public static void StringToObject<T>(T obj, string str)
        {
            Stack stack = new Stack();
            bool _valueReaded = false;
            bool _isArray = false;
            bool _isFirstClass = true;

            string _name = "";
            object _value = null;
            object _class = null;
            FieldInfo _fieldInfo = null;
            List<object> _list = new List<object>();

            string stringBuffer = "";
            for (int i = 0; i < str.Length; i++)
            {

                switch (str[i])
                {
                    case '[':
                        _isArray = true;
                        break;
                    case ']':
                        _isArray = false;
                        break;
                    case '{':
                        if (_isFirstClass) { _isFirstClass = false; stack.Push(obj); }
                        else
                        {
                            _fieldInfo = stack.Peek().GetType().GetField(_name);
                            _class = GetValue(_fieldInfo, stack.Peek());
                            stack.Push(_name);
                            if (_class != null)
                            {
                                stack.Push(_class);
                            }
                            else
                            {
                                stack.Push(Assembly.GetExecutingAssembly().CreateInstance(
                              _fieldInfo.FieldType.FullName));
                            }
                        }
                        break;
                    case '}':
                        if (stack.Count > 1)
                        {
                            stack.Pop();
                            _class = stack.Pop();
                            _name = (string)stack.Pop();
                            SetValue(stack.Peek(), stack.Peek().GetType().GetField(_name), _class);
                        }
                        break;
                    case ':':
                        _name = stringBuffer.Replace(" ", String.Empty);
                        stringBuffer = "";
                        break;
                    case '"':
                        if (!_valueReaded)
                        {
                            _valueReaded = true;
                        }
                        else
                        {
                            _valueReaded = false;
                            _value = stringBuffer;
                            stringBuffer = "";
                            if (!_isArray)
                            {
                                var field = stack.Peek().GetType().GetField(_name);
                                SetValue(stack.Peek(), field, StringToDataType(
                                        field.FieldType.Name,
                                        _value as string));

                                stack.Push(field.GetValue(stack.Peek()));
                            }
                            else
                            {
                                //_list.Add()
                            }
                        }
                        break;
                    case ',':
                        stack.Pop();
                        break;
                    default:
                        stringBuffer += str[i];
                        break;
                }
            }
        }

        private static object GetValue(FieldInfo fieldInfo, object parentObj)
        {
            if (fieldInfo.FieldType.Name.Equals("String"))
                return Base64Encode(fieldInfo.GetValue(parentObj) as string);
            return fieldInfo.GetValue(parentObj);
        }

        private static void SetValue(object parentObj, FieldInfo fieldInfo, object value)
        {
            if (fieldInfo.FieldType.Name.Equals("String"))
                fieldInfo.SetValue(parentObj, Base64Decode(value as string));
            else fieldInfo.SetValue(parentObj, value);
        }
        /// <summary>
        /// Convert string to base64
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Base64Encode(string text)
        {
            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(text));
        }
        /// <summary>
        /// Convert base64 to string
        /// </summary>
        /// <param name="base64EncodedData"></param>
        /// <returns></returns>
        public static string Base64Decode(string base64EncodedData)
        {
            return Encoding.UTF8.GetString(
                Convert.FromBase64String(base64EncodedData));
        }
        /// <summary>
        /// return true if these are standard data types.
        /// strings, ints, bytes and the rest
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private static bool IsDataType(string typeName)
        {
            if (typeName.Contains("String") || typeName.Contains("Int32")
                    || typeName.Contains("Double") || typeName.Contains("Char")
                    || typeName.Contains("Boolean") || typeName.Contains("Byte")
                    || typeName.Contains("Decimal") || typeName.Contains("Single")
                    || typeName.Contains("SByte") || typeName.Contains("Int16")
                    || typeName.Contains("UInt16") || typeName.Contains("UInt32")
                    || typeName.Contains("Int64") || typeName.Contains("UInt64"))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Convert string to T if there possible
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static object StringToDataType(string typeName, string str)
        {
            object value = null;
            switch (typeName)
            {
                case "String":
                    value = str;
                    break;
                case "Int32":
                    value = Int32.Parse(str);
                    break;
                case "Double":
                    value = Double.Parse(str);
                    break;
                case "Char":
                    value = Char.Parse(str);
                    break;
                case "Boolean":
                    value = Boolean.Parse(str);
                    break;
                case "Byte":
                    value = Byte.Parse(str);
                    break;
                case "Decimal":
                    value = Decimal.Parse(str);
                    break;
                case "Single":
                    value = Single.Parse(str);
                    break;
                case "SByte":
                    value = SByte.Parse(str);
                    break;
                case "Int16":
                    value = Int16.Parse(str);
                    break;
                case "UInt16":
                    value = UInt16.Parse(str);
                    break;
                case "UInt32":
                    value = UInt32.Parse(str);
                    break;
                case "Int64":
                    value = Int64.Parse(str);
                    break;
                case "UInt64":
                    value = UInt64.Parse(str);
                    break;

                default:
                    break;
            }
            return value;
        }
    }
}
