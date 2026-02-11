using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JUTPSEditor
{
    public class MessageWindow : EditorWindow
    {       
        private static Texture2D Banner;

        static string Title, Message, ButtonText;
        static int FontSize;
        static UnityEditor.MessageType MessageTypeIcon;

        /// <summary>
        /// Show a editor window with a message
        /// </summary>
        public static void ShowMessage(string message, string title = "Message", string buttonText = "OK", int Height = 256, int Width = 512, int fontSize = 12, UnityEditor.MessageType messageType = MessageType.None)
        {
            //Set Text Parameters
            Title = title;
            Message = message;
            ButtonText = buttonText;
            FontSize = fontSize;
            MessageTypeIcon = messageType;

            GetWindow(typeof(MessageWindow));
            GetWindow(typeof(MessageWindow)).titleContent.text = Title;
            int width = Width;
            int height = Height;

            var x = (Screen.currentResolution.width - width) / 2;
            var y = (Screen.currentResolution.height - height) / 2;

            GetWindow<MessageWindow>().position = new Rect(x, y, width, height);
        }

        private void OnGUI()
        {
            //Load banner
            if (Banner == null) Banner = CustomEditorUtilities.GetImage("JUTPSLOGO");
            
            //Render banner
            if (Banner != null)
            {
                GUILayout.BeginHorizontal();

                CustomEditorUtilities.RenderImageWithResize(Banner, new Vector2(64, 40));
                GUILayout.Label("| " + Title, JUTPSEditor.CustomEditorStyles.Title(16), GUILayout.Height(30));

                GUILayout.EndHorizontal();
            }

            //Get Style
            var style = new GUIStyle(EditorStyles.label);
            switch (MessageTypeIcon)
            {
                case MessageType.None:
                    break;
                case MessageType.Info:
                    style = new GUIStyle(EditorStyles.helpBox);
                    break;
                case MessageType.Warning:
                    break;
                case MessageType.Error:
                    break;
            }
            style.fontSize = FontSize;
            style.wordWrap = true;

            if (MessageTypeIcon == MessageType.None)
            {
                GUILayout.Label(Message, style);
            }
            else
            {
                EditorGUILayout.HelpBox(Message, MessageTypeIcon, true);
            }
            //Space
            GUILayout.Space(15);

            //OK Button
            if (GUILayout.Button(ButtonText))
            {
                GetWindow<MessageWindow>().Close();
            }
        }
    }
}
