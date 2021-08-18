#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Commander
{
    public static class CommandList
    {
		[EditorCommand("quit", "退出", "停止运行Unity程序")]
        static bool SystemCommand_Quit(List<string> args)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return true;
        }

        [EditorCommand("clear", "清屏", "清空当前屏幕", 0, false)]
        static bool SystemCommand_Clear(List<string> args)
        {
            CommandManager.Instance.GetLogs().Clear();
            return true;
        }

        [EditorCommand("log", "测试log", "单纯测试log用，需要\"log xxx\"", 1, false)]
        static bool SystemCommand_Log(List<string> args)
        {
            if(args.Count > 0)
            {
                CommandManager.Instance.AddLog("> " + args[1]);
            }
            return true;
        }

        [EditorCommand("history", "历史指令", "显示历史指令", 0, false)]
        static bool SystemCommand_History(List<string> args)
        {
            List<string> history = CommandManager.Instance.GetHistory();
            for (int i = 0; i < history.Count; i++)
            {
                CommandManager.Instance.AddLog("> " + history[i]);
            }
            return true;
        }

        [EditorCommand("help", "帮助", "显示帮助", 0, false)]
        static bool SystemCommand_Help(List<string> args)
        {
            CommandManager.Instance.AddLog("帮助说明:");
            CommandManager.Instance.AddLog("GM 指令");
            CommandManager.Instance.AddLog("\"~\"控制台开关，ESC也可以关闭控制台");
            CommandManager.Instance.AddLog("Tab键或者↑↓箭头可以切换待选列表，然后Enter键选中");
            CommandManager.Instance.AddLog("无待选列表时↑↓箭头可以切换历史命令");
            CommandManager.Instance.AddLog("quit 停止Unity");
            CommandManager.Instance.AddLog("clear 清屏");
            CommandManager.Instance.AddLog("log 测试log");
            CommandManager.Instance.AddLog("history 显示历史指令");
            return true;
        }

    }

}

#endif