﻿using System;
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

        public bool CloseConcole
        {
            get;
            set;
        }

        public EditorCommandAttribute(string cmd, string name, string comment = "", int argsCount = 0, bool closeConcole = true)
        {
            this.Cmd = cmd;
            this.Name = name;
            this.Comment = comment;
            this.ArgsCount = argsCount;
            this.CloseConcole = closeConcole;
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
        public bool closeConcole = true;
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

        public void RegisterCommand(string cmd, Func<List<string>, bool> method, string name = "",
            string comment = "", int argsCount = 0, bool closeConcole = true, string format = "", CommandType type = CommandType.GM)
        {
            CommandData command = new CommandData();
            command.cmd = cmd;
            command.Name = name;
            command.Comment = comment;
            command.Type = CommandType.GM;
            command.Method = method;
            command.argsCount = argsCount;
            command.closeConcole = closeConcole;
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

                    this.RegisterCommand(attribute.Cmd, action, attribute.Name, attribute.Comment, attribute.ArgsCount, attribute.CloseConcole);
                }
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
        public bool ExcuteCommand(string cmdText)
        {
            this.AddLog(cmdText, LogType.Warning);
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
            bool closeConcolse = true;

            if (commandExists==false)
            {
                for (int i = 0; i < _commandList.Count; i++)
                {
                    CommandData cmd = _commandList[i];
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
                this.AddLog(string.Format("\"{0}\" command not exists or args count not match", cmdText), LogType.Warning);
                result = false;
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
                    closeConcolse = targetCmd.closeConcole;
                    if(!result)
                    {
                        this.AddLog(string.Format("Excute \"{0}\" failed", cmdText), LogType.Warning);
                    }
                }
            }
            if(!result)
            {
                closeConcolse = false;
            }
            else
            {
                this.AddHistoryCommand(cmdText);
            }
            _cacheList.Clear();
            return closeConcolse;
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
