using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Commander
{
    public enum CommandType
    {
        GM,
        Lua,
        System,
        Group
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class EditorCommandAttribute : Attribute
    {
        public string Cmd
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }

        public string Comment
        {
            get;
            set;
        }

        public int ArgsCount
        {
            get;
            set;
        }

        public EditorCommandAttribute(string cmd, string name, string comment = "", int argsCount = 0)
        {
            this.Cmd = cmd;
            this.Name = name;
            this.Comment = comment;
            this.ArgsCount = argsCount;
        }
    }

    public class CommandData
    {
        public string cmd;
        public CommandType Type;
        public string Name;
        public string Comment;
        public Func<List<string>, bool> Method;
        public int argsCount = 0;
        public string format = "";
        public List<string> gmList = new List<string>();

        public string GetSelectTip(string input = "")
        {
            string str = string.Format("{0}\t{1}\t{2}", cmd, Name, Comment);
            if(string.IsNullOrEmpty(input))
            {
                return str;
            }
            int index = str.IndexOf(input);
            if(index < 0)
            {
                return str;
            }
            string str1 = str.Substring(0, index);
            string str2 = "<color=#00ffff>" + str.Substring(index, input.Length) + "</color>";
            string str3 = str.Substring(index + input.Length);
            return str1 + str2 + str3;
        }
    }

    public class LogData
    {
        public string Log = "";
        public Color Color;
    }

    public class CommandManager
    {
        private static CommandManager _instance = null;

        private List<CommandData> _commandList = new List<CommandData>();

        private List<LogData> _logList = new List<LogData>();


        public const int Max_History = 10;
        private List<string> _historyList = new List<string>();


        public static CommandManager Instance
        {
            get
            {
                return _instance ?? (_instance = new CommandManager());
            }
        }

        public void RegisterCommand(string cmd, Func<List<string>, bool> method, string name = "", string comment = "", int argsCount = 0, string format = "", CommandType type = CommandType.GM)
        {
            CommandData command = new CommandData();
            command.cmd = cmd;
            command.Name = name;
            command.Comment = comment;
            command.Type = CommandType.GM;
            command.Method = method;
            command.argsCount = argsCount;
            command.format = format;
            _commandList.Add(command);
        }

        public void RegisterDefaultCommands()
        {
            BindingFlags bindFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(bindFlags))
                {
                    EditorCommandAttribute attribute = Attribute.GetCustomAttribute(method, typeof(EditorCommandAttribute)) as EditorCommandAttribute;
                    if (attribute == null)
                    {
                        continue;
                    }
                    ParameterInfo[] methods_params = method.GetParameters();
                    Func<List<string>, bool> action = (Func<List<string>, bool>)Delegate.CreateDelegate(typeof(Func<List<string>, bool>), method);

                    this.RegisterCommand(attribute.Cmd, action, attribute.Name, attribute.Comment, attribute.ArgsCount);
                }
            }

        }


        public void RegisterAllCommands()
        {
            string path = Application.dataPath + "/GameMain/LauncherAssets/GMCommand.txt";
            string[] lines = File.ReadAllLines(path, Encoding.Unicode);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                line = line.Replace("\"", "");
                string[] items = line.Split('\t');
                string command = items[2];
                string name = items[0];
                string formatStr = items[3];
                string comment = items[1];
                //处理formatStr
                Regex reg = new Regex(@"\{[0-9]{1,2}\}");
                int maxArgsCount = reg.Matches(formatStr).Count;

                Func<List<string>, bool> callbackGM = (List<string> argsList) =>
                {
                    //TODO
                    return true;
                };

                Func<List<string>, bool> callbackLua = (List<string> argsList) =>
                {
                    return true;
                };

                Func<List<string>, bool> callback = callbackGM;
                if(command.Contains("$$"))
                {
                    callback = callbackLua;
                }
                this.RegisterCommand(command, callback, name, comment, maxArgsCount);
            }
        }

        private Color GetLogColor(LogType type)
        {
            Color color = Color.white;
            switch(type)
            {
                case LogType.Log:
                    color = Color.grey;
                    break;
                case LogType.Warning:
                    color = Color.yellow;
                    break;
                case LogType.Error:
                    color = Color.red;
                    break;
            }
            return color;
        }

        public void AddLog(string msg, LogType logType = LogType.Log)
        {
            LogData log = new LogData();
            log.Log = msg;
            log.Color = this.GetLogColor(logType);
            _logList.Add(log);
        }

        public List<LogData> GetLogs()
        {
            return _logList;
        }

        private List<CommandData> _cacheList = new List<CommandData>();
        public bool ExcuteCommand(string command)
        {
            _cacheList.Clear();
            command = command.Trim();
            bool result = false;
            string[] arr = command.Split(' ');
            if(arr.Length == 0)
            {
                return false;
            }
            List<string> args = new List<string>();
            for (int i = 0; i < arr.Length; i++)
            {
                if(!string.IsNullOrEmpty(arr[i]))
                {
                    args.Add(arr[i]);
                }
            }
            bool commandExists = false;


            if (commandExists==false)
            {
                for (int i = 0; i < _commandList.Count; i++)
                {
                    CommandData cmd = _commandList[i];
                    if (cmd.cmd == arr[0])
                    {
                        if (args.Count == cmd.argsCount + 1)
                        {
                            _cacheList.Add(cmd);
                            commandExists = true;
                        }
                    }
                }
            }
            if(commandExists==false)
            {
                if(command.Contains("$$"))
                {
                    //this.AddLog("前端Lua指令 = " + command);
                    this.AddLog(command);
                }
                else
                {
                    //this.AddLog(string.Format("\"{0}\" 没有配置 或参数个数不匹配", command), LogType.Warning);
                    //this.AddLog("已发送~~~");
                    //-----------------------------手动输入GM指令----------------------------------
                    this.AddLog(command);
                }
            }
            else
            {
                CommandData targetCmd = _cacheList[0];
                for (int cIndex = 0; cIndex < _cacheList.Count; cIndex++)
                {
                    if(_cacheList[cIndex].argsCount >= targetCmd.argsCount)
                    {
                        targetCmd = _cacheList[cIndex];
                    }
                }
                if(targetCmd != null)
                {
                    result = targetCmd.Method(args);
                    if(result)
					{
                        this.AddLog(command);
                    }
                }
            }
            if(!result)
            {
                //this.AddLog(string.Format("\"{0}\"执行失败", command), LogType.Warning);
            }
            else
            {
                this.AddHistoryCommand(command);
            }
            _cacheList.Clear();
            return result;
        }

        private void AddHistoryCommand(string command)
        {
            _historyList.Insert(0, command);
            if(_historyList.Count > 10)
            {
                _historyList.RemoveAt(_historyList.Count - 1);
            }
            _historyIndex = -1;
        }

        private string FormateParams(string command)
		{
            command = command.Trim();
            if(string.IsNullOrEmpty(command))
			{
                return command;
			}
            if (command.Contains("="))
			{
                return command;
			}
            //int index = command.IndexOf(" ");

            for (int index = 0; index < command.Length;  index++)
            {
                if(index>0 && index < command.Length-1)
				{
					if (command[index].ToString() == " " && command[index+1].ToString()!="=")
					{
                        command = command.Insert(index+1, "=");
                    }
				}
                
            }
				return command;
        }
        private string GetCurrentCommand()
        {
            if(_historyList.Count == 0)
            {
                return "";
            }
            if(_historyList.Count == 1)
            {
                return _historyList[0];
            }
            while(_historyIndex < 0)
            {
                _historyIndex = _historyIndex + _historyList.Count;
            }
            while(_historyIndex >= _historyList.Count)
            {
                _historyIndex = _historyIndex - _historyList.Count;
            }
            return _historyList[_historyIndex];
        }

        private int _historyIndex = -1;
        public string GetPreCommand()
        {
            _historyIndex = _historyIndex - 1;
            return this.GetCurrentCommand();
        }

        public string GetNextCommand()
        {
            _historyIndex = _historyIndex + 1;
            return this.GetCurrentCommand();
        }

        public List<string> GetHistory()
        {
            return _historyList;
        }

        private List<CommandData> tmpList = new List<CommandData>();
        public List<CommandData> GetFilterList(string command)
        {
            tmpList.Clear();
            for (int i = 0; i < _commandList.Count; i++)
            {
                CommandData cmd = _commandList[i];
                if(cmd.cmd.Contains(command))
                {
                    tmpList.Add(cmd);
                }
            }
            return tmpList;
        }
    }
}
