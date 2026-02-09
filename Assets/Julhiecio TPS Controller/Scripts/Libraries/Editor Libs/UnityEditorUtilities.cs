using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
namespace JUTPSEditor
{
    public static class CustomEditorStyles
    {
        public static GUIStyle WarningStyle()
        {
            GUIStyle WarningStyle = new GUIStyle(EditorStyles.helpBox);
            WarningStyle.normal.textColor = Color.yellow;
            WarningStyle.fontSize = 12;
            return WarningStyle;
        }

        public static GUIStyle NormalStateStyle()
        {
            GUIStyle normalStateStyle = new GUIStyle(EditorStyles.objectField);
            //normalStateStyle.normal.textColor = new Color(0.3f, 0.7f, 0.3f, 1F);
            normalStateStyle.fontSize = 12;
            return normalStateStyle;
        }
        public static GUIStyle EnabledStyle()
        {
            GUIStyle enabledStyle = new GUIStyle(EditorStyles.objectField);
            enabledStyle.normal.textColor = new Color(0.3f, 0.7f, 0.3f, 1F);
            enabledStyle.fontSize = 12;
            return enabledStyle;
        }
        public static GUIStyle DisabledStyle()
        {
            GUIStyle disabledStyle = new GUIStyle(EditorStyles.objectField);
            disabledStyle.normal.textColor = new Color(1f, 0.3f, 0.3f, 1F);
            disabledStyle.fontSize = 12;
            return disabledStyle;
        }
        public static GUIStyle MiniButtonStyle()
        {
            GUIStyle minibuttonstyle = new GUIStyle(EditorStyles.miniButtonMid);
            minibuttonstyle.fontSize = 12;
            minibuttonstyle.alignment = TextAnchor.MiddleCenter;
            return minibuttonstyle;
        }
        public static GUIStyle MiniLeftButtonStyle()
        {
            GUIStyle minibuttonleftstyle = new GUIStyle(EditorStyles.miniButtonLeft);
            minibuttonleftstyle.fontSize = 12;
            minibuttonleftstyle.alignment = TextAnchor.MiddleLeft;
            return minibuttonleftstyle;
        }
        public static GUIStyle DangerButtonStyle()
        {
            GUIStyle DangerButton = new GUIStyle(EditorStyles.miniButtonMid);
            DangerButton.normal.textColor = new Color(1, 0.6f, 0.6f, 1F);
            DangerButton.fontSize = 12;
            DangerButton.fontStyle = FontStyle.Bold;
            return DangerButton;
        }
        public static GUIStyle StateStyle()
        {
            GUIStyle DangerButton = new GUIStyle(EditorStyles.radioButton);
            //DangerButton.font = JUTPSEditorFont();
            DangerButton.fontSize = 11;
            DangerButton.fontStyle = FontStyle.Normal;
            return DangerButton;
        }
        public static void Icon(string iconName = "ToolIcon", int PixelSize = 22)
        {
            Texture2D icon = Resources.Load("Resources/Editor Resources/Textures/" + iconName) as Texture2D;
            GUILayout.Label(icon, EditorStyles.toolbar, GUILayout.Width(PixelSize), GUILayout.Height(PixelSize));
        }
        public static GUIStyle Toolbar()
        {
            GUIStyle toolbar = new GUIStyle(EditorStyles.toolbarButton);

            //GUIStyle toolbar = new GUIStyle(EditorStyles.objectField);
            toolbar.normal.textColor = new Color(.8f, .8f, .8f, 1F);
            toolbar.alignment = TextAnchor.MiddleLeft;
            //toolbar.font = JUTPSEditorFont();
            toolbar.fontSize = 13;
            toolbar.normal.background = CustomEditorUtilities.GetImage("TabBackground");
            toolbar.fontStyle = FontStyle.Normal;
            return toolbar;
        }
        public static GUIStyle MiniToolbar()
        {
            GUIStyle toolbar = new GUIStyle(EditorStyles.objectField);
            //HeaderStyle.normal.textColor = new Color(1, 1, 1, 1F);
            //toolbar.font = JUTPSEditorFont();
            toolbar.fontSize = 14;
            toolbar.fontStyle = FontStyle.Normal;
            toolbar.alignment = TextAnchor.MiddleCenter;
            return toolbar;
        }
        public static GUIStyle LabelButton()
        {
            GUIStyle toolbar = new GUIStyle("textfield");

            toolbar.imagePosition = ImagePosition.ImageOnly;
            return toolbar;
        }
        public static GUIStyle Title(int fontsize = 0)
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.label);
            //titleStyle.normal.textColor = new Color(1, 1, 1, 1F);
            //titleStyle.font = JUTPSEditorFont();

            if (fontsize == 0)
            {
                titleStyle.fontSize = 15;
            }
            else
            {
                titleStyle.fontSize = fontsize;
            }
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.LowerLeft;
            return titleStyle;
        }
        public static GUIStyle Header()
        {
            GUIStyle header = new GUIStyle(EditorStyles.objectFieldThumb);
            //GUIStyle header = new GUIStyle(EditorStyles.objectFieldThumb);
            //titleStyle.normal.textColor = new Color(1, 1, 1, 1F);
            header.font = JUTPSEditorFont();
            header.fontSize = 18;
            return header;
        }
        public static GUIStyle ErrorStyle()
        {
            GUIStyle ErrorStyle = new GUIStyle(EditorStyles.helpBox);
            ErrorStyle.normal.textColor = new Color(1, 0.4f, 0.4f, 1F);
            ErrorStyle.fontSize = 12;
            ErrorStyle.wordWrap = true;
            return ErrorStyle;
        }
        public static Font JUTPSEditorFont()
        {
            Font font = Resources.Load("Resources/Editor Resources/Fonts/Hyperjump Bold.otf") as Font;
            return font;
        }
    }
    public static class CustomEditorUtilities
    {
        private static Texture2D logo;
        public static void JUTPSTitle(string TitleName)
        {
            if (logo == null)
            {
                logo = GetImage("JUTPSLOGO");
                return;
            }
            else
            {
                GUILayout.BeginHorizontal();
                RenderImageWithResize(logo, new Vector2(70,20));
                GUILayout.Label(TitleName, JUTPSEditor.CustomEditorStyles.Title());
                GUILayout.EndHorizontal();
            }
        }

        public static void DrawTabItem(Texture2D Cover = null, string Title = "Title", string LabelText = "Description", float Height = 32, float Width = 256, bool NoBackground = false)
        {
            if (NoBackground == false)
            {
                GUILayout.BeginHorizontal(JUTPSEditor.CustomEditorStyles.MiniToolbar(), GUILayout.Height(Height), GUILayout.Width(Width));
            }
            else
            {
                GUILayout.BeginHorizontal(GUILayout.Height(Height), GUILayout.Width(Width));
            }
            if (Cover != null) RenderImageWithResize(Cover, new Vector2(64, 64));
           // GUILayout.Space(10);

          GUILayout.BeginVertical();
            GUILayout.Label(Title, JUTPSEditor.CustomEditorStyles.Title(16), GUILayout.Width(Width + (Cover == null ? Height + 37 : 0)));
            GUILayout.Label(LabelText, GUILayout.Width(Width + (Cover == null ? Height+37 : 0)));

            GUILayout.EndVertical();


        GUILayout.EndHorizontal();
        }
        public static Texture2D GetImage(string ImageName)
        {
            if (Resources.Load("Editor Resources/Textures/" + ImageName) == null)
            {
                Debug.Log("Unable to load image, check image name and directory path.");
                return null;
            }
            
            Texture2D img = Resources.Load("Editor Resources/Textures/" + ImageName) as Texture2D;

            return img;
        }
        public static void RenderImage(Texture2D image)
        {
            GUILayout.Label(image, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        }
        public static void RenderImageWithResize(Texture2D image, Vector2 Size)
        {
            GUILayout.Label(image, GUILayout.Width(Size.x), GUILayout.Height(Size.y));
        }
    }
    public static class LayerMaskUtilities
    {
        public static LayerMask GroundMask()
        {
            LayerMask groundmask = LayerMask.GetMask("Default", "Terrain", "Walls", "VehicleMeshCollider");
            return groundmask;
        }
        public static LayerMask CrosshairMask()
        {
            LayerMask crosshair = LayerMask.GetMask("Default", "Terrain", "Walls", "VehicleMeshCollider");
            return crosshair;
        }
        public static LayerMask CameraCollisionMask()
        {
            LayerMask crosshair = LayerMask.GetMask("Default", "Terrain", "Walls", "VehicleMeshCollider");
            return crosshair;
        }
        public static LayerMask DrivingCameraCollisionMask()
        {
            LayerMask crosshair = LayerMask.GetMask("Default", "Terrain", "Walls");
            return crosshair;
        }
    }
}
#endif
