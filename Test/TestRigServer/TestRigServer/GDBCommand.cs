using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace TestRigServer
{
    public enum CommandStatus
    {
        Done,
        Running,
        Error
    }
    public class ResultData : IEnumerable
    {
        Hashtable props;
        object[] array;
        bool isArrayProperty;

        public int Count
        {
            get
            {
                if (array != null)
                    return array.Length;
                else if (props != null)
                    return props.Count;
                else
                    return 0;
            }
        }

        public string GetValue(string name)
        {
            return (string)props[name];
        }

        public int GetInt(string name)
        {
            return int.Parse(GetValue(name));
        }

        public string GetValue(int index)
        {
            return (string)array[index];
        }

        public ResultData GetObject(string name)
        {
            return (ResultData)props[name];
        }

        public ResultData GetObject(int index)
        {
            return (ResultData)array[index];
        }

        public object[] GetAllValues(string name)
        {
            object ob = props[name];
            if (ob == null)
                return new object[0];
            ResultData rd = ob as ResultData;
            if (rd != null && rd.isArrayProperty)
                return rd.array;
            else
                return new object[] { ob };
        }

        protected void ReadResults(string str, int pos)
        {
            ReadTuple(str, ref pos, this);
        }

        void ReadResult(string str, ref int pos, out string name, out object value)
        {
            name = null;
            value = null;

            name = ReadString(str, '=', ref pos);
            ReadChar(str, ref pos, '=');
            value = ReadValue(str, ref pos);
        }

        string ReadString(string str, char term, ref int pos)
        {
            StringBuilder sb = new StringBuilder();
            while (pos < str.Length && str[pos] != term)
            {
                if (str[pos] == '\\')
                {
                    pos++;
                    if (pos >= str.Length)
                        break;
                }
                sb.Append(str[pos]);
                pos++;
            }
            return sb.ToString();
        }

        object ReadValue(string str, ref int pos)
        {
            if (str[pos] == '"')
            {
                pos++;
                string ret = ReadString(str, '"', ref pos);
                pos++;
                return ret;
            }
            if (str[pos] == '{')
            {
                pos++;
                ResultData data = new ResultData();
                ReadTuple(str, ref pos, data);
                return data;
            }
            if (str[pos] == '[')
            {
                pos++;
                return ReadArray(str, ref pos);
            }

            // Single value tuple
            string name;
            object val;
            ReadResult(str, ref pos, out name, out val);
            ResultData sdata = new ResultData();
            sdata.props = new Hashtable();
            sdata.props[name] = val;
            return sdata;
        }

        void ReadTuple(string str, ref int pos, ResultData data)
        {
            if (data.props == null)
                data.props = new Hashtable();

            while (pos < str.Length && str[pos] != '}')
            {
                string name;
                object val;
                ReadResult(str, ref pos, out name, out val);
                if (data.props.ContainsKey(name))
                {
                    object ob = data.props[name];
                    ResultData rd = ob as ResultData;
                    if (rd != null && rd.isArrayProperty)
                    {
                        object[] newArr = new object[rd.array.Length + 1];
                        Array.Copy(rd.array, newArr, rd.array.Length);
                        newArr[rd.array.Length] = val;
                        rd.array = newArr;
                    }
                    else
                    {
                        rd = new ResultData();
                        rd.isArrayProperty = true;
                        rd.array = new object[2];
                        rd.array[0] = ob;
                        rd.array[1] = val;
                        data.props[name] = rd;
                    }
                }
                else
                {
                    data.props[name] = val;
                }
                TryReadChar(str, ref pos, ',');
            }
            TryReadChar(str, ref pos, '}');
        }

        ResultData ReadArray(string str, ref int pos)
        {
            ArrayList list = new ArrayList();
            while (pos < str.Length && str[pos] != ']')
            {
                object val = ReadValue(str, ref pos);
                list.Add(val);
                TryReadChar(str, ref pos, ',');
            }
            TryReadChar(str, ref pos, ']');
            ResultData arr = new ResultData();
            arr.array = list.ToArray();
            return arr;
        }

        void ReadChar(string str, ref int pos, char c)
        {
            if (!TryReadChar(str, ref pos, c))
                ThrownParseError(str, pos);
        }

        bool TryReadChar(string str, ref int pos, char c)
        {
            if (pos >= str.Length || str[pos] != c)
                return false;
            pos++;
            return true;
        }

        void ThrownParseError(string str, int pos)
        {
            if (pos > str.Length)
                pos = str.Length;
            str = str.Insert(pos, "[!]");
            throw new InvalidOperationException("Error parsing result: " + str);
        }

        public IEnumerator GetEnumerator()
        {
            if (props != null)
                return props.Values.GetEnumerator();
            else if (array != null)
                return array.GetEnumerator();
            else
                return new object[0].GetEnumerator();
        }
    }
    public class GdbCommandResult : ResultData
    {
        public CommandStatus Status;
        public string ErrorMessage;

        public GdbCommandResult(string line)
        {
            if (line.StartsWith("^done"))
            {
                Status = CommandStatus.Done;
                ReadResults(line, 6);
            }
            else if (line.StartsWith("^error"))
            {
                Status = CommandStatus.Error;
                if (line.Length > 7)
                {
                    ReadResults(line, 7);
                    ErrorMessage = GetValue("msg");
                }
            }
            else if (line.StartsWith("^running"))
            {
                Status = CommandStatus.Running;
            }
        }
    }
    public class GdbEvent : ResultData
    {
        public string Name;
        public string Reason;

        public GdbEvent(string line)
        {
            int i = line.IndexOf(',');
            if (i == -1)
                i = line.Length;
            Name = line.Substring(1, i - 1);
            ReadResults(line, i + 1);
            object[] reasons = GetAllValues("reason");
            if (reasons.Length > 0)
                Reason = (string)reasons[0];
        }
    }
}
