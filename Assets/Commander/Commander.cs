
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Commander
{
    //add this component
    public class Commander : MonoBehaviour
    {
        public enum State
        {
            Open,
            Close
        }

        public static class KeyEvent
        {
            public static Event Open = Event.KeyboardEvent("`");
            public static Event Enter = Event.KeyboardEvent("return");
            public static Event Esc = Event.KeyboardEvent("escape");
            public static Event Up = Event.KeyboardEvent("up");
            public static Event Down = Event.KeyboardEvent("down");
            public static Event Tab = Event.KeyboardEvent("tab");
        }

        string InputCaret = ">";
        Font ConsoleFont;
        private State _state = State.Close;
        private bool m_NeedUpdateSelectList = false;
        private bool m_ShowSelectWindow = false;
        TextEditor editor_state;
        bool input_fix;
        bool move_cursor;
        Rect window;
        float open_target;
        float real_window_size;
        string old_command_text; //上次的输入文本
        string command_text; //当前的输入文本
        string default_command_text = ""; //清空后的默认文本
        Vector2 scroll_position;
        GUIStyle window_style;
        GUIStyle label_style;
        GUIStyle label_select_style;
        GUIStyle input_style;
        Texture2D background_texture;
        Texture2D select_label_background_texture; //下拉列表选装的背景颜色

        const string line = "--------------------------------------------------------------------------------------" +
            "------------------------------------------------------------------------------------------------------" +
            "------------------------------------------------------------------------------------------------------" +
            "------------------------------------------------------------------------------------------------------";
        Rect selectListWindow;
        private int m_SelectIndex = 0;
        List<CommandData> selectCommandList = new List<CommandData>();

        void Start()
        {
            if(!Application.isEditor)
            {
                return;
            }
            InitUI();

            CommandManager.Instance.RegisterDefaultCommands();
            command_text = "";
        }

        void InitUI()
        {
            if (ConsoleFont == null)
            {
                ConsoleFont = Font.CreateDynamicFontFromOSFont("微软雅黑", 18);
            }

            background_texture = new Texture2D(1, 1);
            background_texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.5f));
            background_texture.Apply();

            window_style = new GUIStyle();
            window_style.normal.background = background_texture;
            window_style.padding = new RectOffset(4, 4, 4, 4);
            window_style.normal.textColor = Color.white;
            window_style.font = ConsoleFont;

            label_style = new GUIStyle();
            label_style.font = ConsoleFont;
            label_style.normal.textColor = Color.white;
            label_style.wordWrap = false;
            label_style.alignment = TextAnchor.MiddleLeft;

            select_label_background_texture = new Texture2D(1, 1);
            select_label_background_texture.SetPixel(0, 0, new Color(135f/255, 206/255f, 235/255f, 0.5f)); //天空蓝
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
            input_style.normal.textColor = Color.cyan;
            input_style.normal.background = background_texture;
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
				m_SelectIndex = 0; 
			}
			else
			{
				m_SelectIndex = -1;
			}

		}

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
			selectListWindow = GUILayout.Window(89, selectListWindow, DrawSelectList, "", window_style);
		}

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
            input_fix = true;
            selectCommandList.Clear();
            editor_state = null;
            if (_state == State.Open)
            {
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
            bool clickEnterBtn = false;
            bool findHistory = false;
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
                    command_text = selectCommandList[m_SelectIndex].cmd;
                    m_SelectIndex = -1;
					m_NeedUpdateSelectList = false;
					m_ShowSelectWindow = false;
				}
                else
                {
                    EnterCommand();
                }
                clickEnterBtn = true;
            }
            else if (IsKeyDown(KeyEvent.Up))
            {
                move_cursor = true;
                if(selectCommandList.Count == 0)
                {
                    command_text = CommandManager.Instance.GetNextCommand();
                    findHistory = true;
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
                    findHistory = true;
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

                if(clickEnterBtn==false && findHistory == false)
				{
                    m_NeedUpdateSelectList = true;
                    m_ShowSelectWindow = true;
				}
            }

            if (input_fix && command_text.Length > 0)
            {
                command_text = default_command_text;
                input_fix = false;                 
            }

            GUI.FocusControl("command_text_field");
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
			if (CommandManager.Instance.ExcuteCommand(command_text))
			{
				SetState(State.Close);
			}
			command_text = "";
		}
#endif

	}
}
