using System.Collections.Generic;
using UnityEngine;

namespace Commander
{
    /*
     * 添加此脚本至场景即可使用
     * 测试的部分指令是本项目自用的 无效可忽略
     * 注意多数指令都是需要发往服务器的
       故需要自己在CommandManager下面三个方法中补充TODO部分
        RegisterDefaultCommands
        RegisterAllCommands
        ExecuteCommand
        */
    public class Commander : MonoBehaviour
    {
        public enum State
        {
            Open,
            Close
        }
        string InputCaret = ">";

        public static class KeyEvent
        {
            public static Event Open = Event.KeyboardEvent("`");
            public static Event Enter = Event.KeyboardEvent("return");
            public static Event Esc = Event.KeyboardEvent("escape");
            public static Event Up = Event.KeyboardEvent("up");
            public static Event Down = Event.KeyboardEvent("down");
            public static Event Tab = Event.KeyboardEvent("tab");
        }

        Font ConsoleFont;

        float InputContrast = 0.0f;
        float InputAlpha = 0.5f;

        //Color BackgroundColor = new Color(0.16f, 0.16f, 0.16f, 0.5f); //灰色底
        Color BackgroundColor = new Color(0f, 0f, 0f, 0.5f);
        Color BackgroundSelectColor = new Color(0, 0, 0, 0.5f);
        Color ForegroundColor = Color.white;
        Color InputColor = Color.cyan;

        private State _state = State.Close;

        private bool m_NeedUpdateSelectList = false;
        private bool m_ShowSelectWindow = false;
        TextEditor editor_state;
        bool input_fix;
        bool move_cursor;
        bool initial_open;
        Rect window;
        float open_target;
        float real_window_size;
        string old_command_text; //上次的输入文本
        string command_text; //当前的输入文本
        string cached_command_text;
        Vector2 scroll_position;
        GUIStyle window_style;
        GUIStyle label_style;
        GUIStyle label_select_style;
        GUIStyle input_style;
        Texture2D background_texture;
        Texture2D select_background_texture;
        Texture2D select_label_background_texture; //下拉列表选装的背景颜色
        Texture2D input_background_texture;

        const string line = "--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------";
        Rect selectListWindow;
        private int m_SelectIndex = 0;
        GUIStyle select_window_style;
        List<CommandData> selectCommandList = new List<CommandData>();

        void Start()
        {
            if(!Application.isEditor)
            {
                return;
            }
            InitUI();

            //此处是CommandList里带标签的自定义Command
            CommandManager.Instance.RegisterDefaultCommands();
            //此处是GMCommand.txt里定义的Command
            CommandManager.Instance.RegisterAllCommands();
            //此处是组Command，用于批量执行 提了个测试模板 一般用于批量发送服务器的Command
            CommandManager.Instance.RegisterCommandsGroup();
            command_text = "";
            cached_command_text = command_text;
        }

        void InitUI()
        {
            if (ConsoleFont == null)
            {
                ConsoleFont = Font.CreateDynamicFontFromOSFont("微软雅黑", 18);
            }

            background_texture = new Texture2D(1, 1);
            background_texture.SetPixel(0, 0, BackgroundColor);
            background_texture.Apply();

            window_style = new GUIStyle();
            window_style.normal.background = background_texture;
            window_style.padding = new RectOffset(4, 4, 4, 4);
            window_style.normal.textColor = ForegroundColor;
            window_style.font = ConsoleFont;

            select_background_texture = new Texture2D(1, 1);
            select_background_texture.SetPixel(0, 0, BackgroundSelectColor);
            select_background_texture.Apply();
            
            select_window_style = new GUIStyle();
            select_window_style.normal.background = select_background_texture;
            select_window_style.padding = new RectOffset(4, 4, 4, 4);
            select_window_style.normal.textColor = ForegroundColor;
            select_window_style.font = ConsoleFont;

            label_style = new GUIStyle();
            label_style.font = ConsoleFont;
            label_style.normal.textColor = ForegroundColor;
            label_style.wordWrap = false;
            label_style.alignment = TextAnchor.MiddleLeft;

            select_label_background_texture = new Texture2D(1, 1);
            //select_label_background_texture.SetPixel(0, 0, new Color(245/ 255f, 245/255f, 245/255f, 0.5f)); //
            select_label_background_texture.SetPixel(0, 0, new Color(135f/255, 206/255f, 235/255f, 0.5f)); //天空蓝
            //select_label_background_texture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.5f)); //白底
            //select_label_background_texture.SetPixel(0, 0, new Color(0, 1f, 1f, 0.5f)); //蓝绿色
            //select_label_background_texture.SetPixel(0, 0, new Color(0.690f, 0.768f, 0.870f, 0.7f));  灰蓝色
            //select_label_background_texture.SetPixel(0, 0, new Color(0.745f, 0.745f, 0.745f, 0.8f)); 灰色
            select_label_background_texture.Apply();
            label_select_style = new GUIStyle();
            label_select_style.font = ConsoleFont;
            label_select_style.normal.textColor = Color.white;
            label_select_style.normal.background = select_label_background_texture;
            label_select_style.wordWrap = true;
            label_select_style.alignment = TextAnchor.MiddleLeft;
            
            input_style = new GUIStyle();
            input_style.padding = new RectOffset(4, 4, 4, 4);
            input_style.font = ConsoleFont;
            input_style.fixedHeight = ConsoleFont.fontSize * 1.6f;
            input_style.normal.textColor = InputColor;

            var dark_background = new Color();
            dark_background.r = BackgroundColor.r - InputContrast;
            dark_background.g = BackgroundColor.g - InputContrast;
            dark_background.b = BackgroundColor.b - InputContrast;
            dark_background.a = InputAlpha;

            input_background_texture = new Texture2D(1, 1);
            input_background_texture.SetPixel(0, 0, dark_background);
            input_background_texture.Apply();
            input_style.normal.background = input_background_texture;
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (IsKeyDown(KeyEvent.Open))
            {
                if(_state == State.Close)
                {
                    SetState(State.Open);
                }
                else if(_state == State.Open)
                {
                    SetState(State.Close);
                }
            }
            if (_state == State.Close)
            {
                return;
            }
            window = GUILayout.Window(88, window, DrawConsole, "", window_style);
            this.CheckSelectList();

            if(m_ShowSelectWindow)
			{
                ShowSelectWindow();
            }
        }

        void CheckSelectList()
        {
            if (string.IsNullOrEmpty(command_text))
            {
                selectCommandList.Clear();
                return;
            }

            if(m_NeedUpdateSelectList==false)
			{
                return;
			}

            m_NeedUpdateSelectList = false;

			selectCommandList = CommandManager.Instance.GetFilterList(command_text);


			if (selectCommandList.Count > 0)
			{
				m_SelectIndex = 0; //输入框有改变 默认选中第一个
			}
			else
			{
				m_SelectIndex = -1;
			}

		}

        //显示选择列表界面
        void ShowSelectWindow()
		{
			if (selectCommandList.Count == 0)
			{
				return;
			}

			if (m_SelectIndex >= selectCommandList.Count)
			{
				m_SelectIndex = -1;
			}

			float realHeight = (selectCommandList.Count + 1) * (ConsoleFont.fontSize + 4);
			selectListWindow = new Rect(0, open_target - realHeight - 40, Screen.width, realHeight);
			selectListWindow = GUILayout.Window(89, selectListWindow, DrawSelectList, "", select_window_style);
		}

        //绘制选择列表
        void DrawSelectList(int Window2D)
        {
            GUILayout.BeginVertical();
            scroll_position = GUILayout.BeginScrollView(scroll_position, false, false, GUIStyle.none, GUIStyle.none);
            GUILayout.FlexibleSpace();
            label_style.normal.textColor = Color.white;
            GUILayout.Label(line, label_style);
            GUILayout.Space(4.0f);
            for (int i = 0; i < selectCommandList.Count; i++)
            {
                label_style.normal.textColor = Color.white;
                if(i != m_SelectIndex)
                {
                    GUILayout.Label(selectCommandList[i].GetSelectTip(command_text.Trim()), label_style);
                }
                else
                {
                    GUILayout.Label(selectCommandList[i].GetSelectTip(command_text.Trim()), label_select_style);
                }
                GUILayout.Space(4.0f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        void SetState(State state)
        {
            _state = state;
            command_text = "";
            initial_open = true;
            input_fix = true;
            selectCommandList.Clear();
            editor_state = null;
            if (_state == State.Open)
            {
                //CommandManager.Instance.RegisterFavouriteCommands();
                open_target = Screen.height;
                real_window_size = open_target;
                scroll_position.y = int.MaxValue;
                window = new Rect(0, open_target - real_window_size, Screen.width, real_window_size);
            }
            else if(_state == State.Close)
            {
                open_target = 0;
            }
        }

        private bool IsKeyDown(Event keyEvent)
        {
            return Event.current.Equals(keyEvent);
        }

        void DrawConsole(int Window2D)
        {

            GUILayout.BeginVertical();

            scroll_position = GUILayout.BeginScrollView(scroll_position, false, false, GUIStyle.none, GUIStyle.none);
            GUILayout.FlexibleSpace();
            if(selectCommandList.Count==0)
			{
                DrawLogs();
            }
            GUILayout.EndScrollView();
            bool ClickEnterBtn = false;
            bool bFnidHistory = false;
            if (move_cursor)
            {
                CursorToEnd();
                move_cursor = false;
            }

            if (IsKeyDown(KeyEvent.Open))
            {
                SetState(State.Close);
            }
            else if(IsKeyDown(KeyEvent.Esc))
            {
                if(m_SelectIndex == -1)
                {
                    SetState(State.Close);
                }
                else
                {
                    m_SelectIndex = -1;
                }
            }
            else if (IsKeyDown(KeyEvent.Enter))
            {
				if (m_SelectIndex != -1)
                {
                    move_cursor = true;
                    //将选中的文本复制到输入框
                    string txt = selectCommandList[m_SelectIndex].cmd;
                    //自动补全修正指令
                    txt = CommandCompleteList.GetInstace().AutoComplete(txt);
                    command_text = txt;
                    m_SelectIndex = -1;
					m_NeedUpdateSelectList = false;
					m_ShowSelectWindow = false;
				}
                else
                {
                    EnterCommand();
                }
                ClickEnterBtn = true;
            }
            else if (IsKeyDown(KeyEvent.Up))
            {
                move_cursor = true;
                if(selectCommandList.Count == 0)
                {
                    command_text = CommandManager.Instance.GetNextCommand();
                    bFnidHistory = true;
					m_NeedUpdateSelectList = false;
					m_ShowSelectWindow = false;
                    m_SelectIndex = -1;
                }
                else
                {
                    m_SelectIndex--;
                    if(m_SelectIndex < 0)
                    {
                        m_SelectIndex = selectCommandList.Count - 1;
                    }
                }
            }
            else if (IsKeyDown(KeyEvent.Down))
            {
                move_cursor = true;
                if(selectCommandList.Count == 0)
                {
                    command_text = CommandManager.Instance.GetPreCommand();
                    bFnidHistory = true;
					m_NeedUpdateSelectList = false;
					m_ShowSelectWindow = false;
                    m_SelectIndex = -1;
                }
                else
                {
                    m_SelectIndex++;
                    if (m_SelectIndex > selectCommandList.Count - 1)
                    {
                        m_SelectIndex = 0;
                    }
                }
            }
			else if (IsKeyDown(KeyEvent.Tab))
			{
				move_cursor = true;
				if (selectCommandList.Count > 0)
				{
					m_SelectIndex++;
					if (m_SelectIndex > selectCommandList.Count - 1)
					{
						m_SelectIndex = 0;
					}
				}
			}
			else if (Event.current.Equals(Event.KeyboardEvent("tab")))
            {
                //CompleteCommand();
                move_cursor = true;
            }


            GUILayout.Space(10.0f);
            GUILayout.BeginHorizontal();

            if (InputCaret != "")
            {
                GUILayout.Label(InputCaret, input_style, GUILayout.Width(ConsoleFont.fontSize));
            }

            GUI.SetNextControlName("command_text_field");
            command_text = GUILayout.TextField(command_text, input_style);
            if(command_text != old_command_text)
            {
                old_command_text = command_text;

                //没有按回车键的情况下 需要更新选择列表
                if(ClickEnterBtn==false && bFnidHistory == false)
				{
                    m_NeedUpdateSelectList = true;
                    m_ShowSelectWindow = true;
				}
            }

            if (input_fix && command_text.Length > 0)
            {
                command_text = cached_command_text;
                input_fix = false;                 
            }

                GUI.FocusControl("command_text_field");
            if (initial_open)
            {
                initial_open = false;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

		void DrawLogs()
		{
			List<LogData> logs = CommandManager.Instance.GetLogs();
			for (int i = 0; i < logs.Count; i++)
			{
				label_style.normal.textColor = logs[i].Color;
				GUILayout.Label(logs[i].Log, label_style);
				GUILayout.Space(4.0f);
			}
		}

        //光标移动到末尾
		void CursorToEnd()
		{
			if (editor_state == null)
			{
				editor_state = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			}
			editor_state.MoveCursorToPosition(new Vector2(999, 999));
		}

		void EnterCommand()
		{
			if (string.IsNullOrEmpty(command_text))
			{
				return;
			}
			scroll_position.y = int.MaxValue;
			if (CommandManager.Instance.ExecuteCommand(command_text))
			{
				SetState(State.Close);
			}
			command_text = "";
		}
#endif

	}
}
