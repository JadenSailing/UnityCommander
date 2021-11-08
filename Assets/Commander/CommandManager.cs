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

        private List<CommandData> m_CommandList = new List<CommandData>();

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
            m_CommandList.Add(command);
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


        public void RegisterCommandsGroup()
        {
            string path = Application.dataPath + "/Commander/GMCommandGroup.txt";
            string[] lines = File.ReadAllLines(path, Encoding.Unicode);
            string command = "";
            string comment = "";
            List<string> cmdList = new List<string>();
            for (int i = 1; i < lines.Length; i++)
            {
                if(lines[i].StartsWith("#gms"))
                {
                    if(!string.IsNullOrEmpty(command))
                    {
                        this.RegisterGroupCommand(command, comment, cmdList);
                    }
                    string[] arr = lines[i].Trim().Split('\t');
                    command = arr[0].Substring(1);
                    comment = arr[1];
                    cmdList.Clear();
                }
                else
                {
                    if(!string.IsNullOrEmpty(command))
                    {
                        cmdList.Add(lines[i].Trim());
                    }
                }
            }
            this.RegisterGroupCommand(command, comment, cmdList);
        }

        private void RegisterGroupCommand(string command, string comment, List<string> cmdList)
        {
            CommandData groupCommand = new CommandData();
            groupCommand.cmd = command;
            groupCommand.Comment = comment;
            for (int gmIndex = 0; gmIndex < cmdList.Count; gmIndex++)
            {
                groupCommand.gmList.Add(cmdList[gmIndex].Trim().Trim('\"'));
            }
            Func<List<string>, bool> callbackGM = (List<string> argsList) =>
            {
                if (groupCommand.Type == CommandType.Group)
                {
                    for (int index = 0; index < groupCommand.gmList.Count; index++)
                    {
                        //项目内批指令的处理
                        //TODO:此处是项目内发往服务器的处理
                    }
                }
                return true;
            };
            groupCommand.Type = CommandType.Group;
            groupCommand.Method = callbackGM;
            groupCommand.format = "";
            groupCommand.argsCount = 0;
            m_CommandList.Add(groupCommand);
        }

        public void RegisterAllCommands()
        {
            string path = Application.dataPath + "/Commander/GMCommand.txt";
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
                    //项目内发往服务器的指令
                    //TODO:此处是项目内发往服务器的处理
                    return true;
                };

                Func<List<string>, bool> callbackLua = (List<string> argsList) =>
                {
                    //int luaValue = 0;
                    //if(argsList.Count > 1)
                    //{
                    //    int.TryParse(argsList[1], out luaValue);
                    //}
                    command = command.Replace("$$", "");
                    //项目内调用Lua的指令
                    //GameEntry.Lua.CallLuaGM(command);
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
        public bool ExecuteCommand(string cmdText)
        {
            _cacheList.Clear();
            cmdText = cmdText.Trim();
            bool result = false;
            string[] strArray = cmdText.Split(' ');
            if(strArray.Length == 0)
            {
                return false;
            }
            List<string> args = new List<string>();
            for (int i = 0; i < strArray.Length; i++)
            {
                if(!string.IsNullOrEmpty(strArray[i]))
                {
                    args.Add(strArray[i]);
                }
            }
            bool commandExists = false;

            //完整指令的
            /*
            for (int i = 0; i < _commandList.Count; i++)
            {
                CommandData cmd = _commandList[i];
                if (command == cmd.cmd && !string.IsNullOrEmpty(cmd.format))
                {
                    _cacheList.Add(_commandList[i]);
                    commandExists = true;
                }
            }
            */

            if (commandExists==false)
            {
                for (int i = 0; i < m_CommandList.Count; i++)
                {
                    CommandData cmd = m_CommandList[i];
                    if (cmd.cmd == strArray[0])
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
                if(cmdText.Contains("$$"))
                {
                    //this.AddLog("前端Lua指令 = " + command);
                    //TODO:此处是为了方便在Lua层扩展加的处理 不用lua可不用管
                }
                else
                {
                    //this.AddLog(string.Format("\"{0}\" 没有配置 或参数个数不匹配", command), LogType.Warning);
                    //this.AddLog("已发送~~~");
                    //-----------------------------手动输入GM指令----------------------------------
                    //TODO:此处是项目内发往服务器的处理
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
                        this.AddLog(cmdText);
                    }
                }
            }
            if(!result)
            {
                //this.AddLog(string.Format("\"{0}\"执行失败", command), LogType.Warning);
            }
            else
            {
                this.AddHistoryCommand(cmdText);
            }
            _cacheList.Clear();
            return result;
        }

        private void AddHistoryCommand(string command)
        {
            int nIndex = _historyList.IndexOf(command);
            if (nIndex>=0)
			{
                _historyList.RemoveAt(nIndex);
			}
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
            for (int i = 0; i < m_CommandList.Count; i++)
            {
                CommandData cmd = m_CommandList[i];
                if(cmd.cmd.Contains(command))
                {
                    tmpList.Add(cmd);
                }
            }
            return tmpList;
        }
    }
}
