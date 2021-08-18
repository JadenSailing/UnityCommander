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
		[EditorCommand("quit", "退出", "")]
		static bool SystemCommand_Quit(List<string> args)
		{
			UnityEditor.EditorApplication.isPlaying = false;
			return true;
		}

		[EditorCommand("clear", "清屏", "")]
		static bool SystemCommand_Clear(List<string> args)
		{
			CommandManager.Instance.GetLogs().Clear();
			return true;
		}

		[EditorCommand("history", "历史指令", "")]
		static bool SystemCommand_History(List<string> args)
		{
			List<string> history = CommandManager.Instance.GetHistory();
			for (int i = 0; i < history.Count; i++)
			{
				CommandManager.Instance.AddLog("> " + history[i]);
			}
			return false;
		}

		[EditorCommand("help", "帮助", "")]
		static bool SystemCommand_Help(List<string> args)
		{
			CommandManager.Instance.AddLog("帮助说明:");
			CommandManager.Instance.AddLog("GM 指令，可在 Assets/GameMain/LauncherAssets/GMCommand.txt 配置");
			CommandManager.Instance.AddLog("\"~\"控制台开关，ESC也可以关闭控制台");
			CommandManager.Instance.AddLog("Tab键或者↑↓箭头可以切换待选列表，然后Enter键选中");
			CommandManager.Instance.AddLog("无待选列表时↑↓箭头可以切换历史命令");
			CommandManager.Instance.AddLog("quit 停止Unity");
			CommandManager.Instance.AddLog("clear 清屏");
			CommandManager.Instance.AddLog("history 显示历史指令");
			return false;
		}


		static void SendMsgToServer(string cmd)
		{
			//项目内发送至服务器的处理
		}

		[EditorCommand("useskill", "让怪物释放技能 参数1: 怪物ID  参数2: 技能id  参数3: 技能目标ID  参数4:施法点X  参数5:施法点Z ", "", 5)]
		static bool SystemCommand_useskillwithtarget(List<string> args)
		{
			string cmd = string.Format("useskill ={0} ={1} ={2} ={3} ={4}", args[1], args[2], args[3], args[4], args[5]);
			SendMsgToServer(cmd);
			return true;
		}

    }


	//gm指令自动补全
	public  class CommandCompleteList
	{
		private static CommandCompleteList m_Instance = new CommandCompleteList();
		public static CommandCompleteList GetInstace()
		{
			return m_Instance;
		}
		private Dictionary<string, Func<string, string>> m_dic = new Dictionary<string, Func<string, string>>();
		public CommandCompleteList()
		{
			m_dic.Add("useskill", UseSkill_AutoComplate);
		}
		public string AutoComplete(string inputText)
		{
			if(string.IsNullOrEmpty(inputText))
			{
				return inputText;
			}
			if(m_dic.ContainsKey(inputText)==false)
			{
				return inputText;
			}
			Func<string, string> func = m_dic[inputText];
			if (func!=null)
			{
				inputText = func(inputText);
			}
			return inputText;
		}
		//参数1: 怪物ID  参数2: 技能id  参数3: 技能目标ID  参数4:施法点X  参数5:施法点Z 
		public string UseSkill_AutoComplate(string inputText)
		{
			//自动补全的示例
			//int myPlayerId = GameEntry.Entity.GetMyPlayerCharacter().Id; //以主角作为技能目标
			int myPlayerId = 10000;
			//int nMonsterId = GameEntry.DataCache.SelectedTargetId;// 以选中的目标作为施法者
			int nMonsterId = 20000;
			string text = string.Format("useskill {0} {1} {2} {3} {4}", nMonsterId, -1,  myPlayerId, -1, -1);
			return text;
		}
	}
}

#endif