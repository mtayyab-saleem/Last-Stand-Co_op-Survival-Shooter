using UnityEngine;
using UnityEditor;
using System.Reflection;
namespace JUTPSEditor.JUHeader
{ 

    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class JUHeader : PropertyAttribute
    {
        public string text;

        public JUHeader(string text)
        {
            this.text = text;
        }
    }



    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class JUSubHeader : PropertyAttribute
    {
        public string text;
        public JUSubHeader(string text)
        {
            this.text = text;
        }
    }



    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class JUReadOnly : PropertyAttribute
    {
        public string ConditionPropertyName;
        public bool Inverse;
        public bool DisableOnFalse;
        public JUReadOnly(string conditionPropertyName = "", bool inverse = false, bool disableonfalse = true)
        {
            this.ConditionPropertyName = conditionPropertyName;
            this.Inverse = inverse;
            this.DisableOnFalse = disableonfalse;
        }
    }



    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class JUButton : PropertyAttribute
    {
        public string methodName;
        public string labelText;

        private System.Type classType;

        public System.Type ClassType
        {
            get { return classType; }
            set { classType = value; }
        }

        public JUButton(string labelText = "Button", System.Type scriptType = default(System.Type), string methodName = "")
        {
            this.methodName = methodName;
            this.labelText = labelText;
            ClassType = scriptType;
        }
    }


#if UNITY_EDITOR && !ODIN_INSPECTOR

    [CustomPropertyDrawer(typeof(JUHeader))]
    class JUHeaderDecoratorDrawer : DecoratorDrawer
    {
        JUHeader header
        {
            get { return ((JUHeader)attribute); }
        }

        public override float GetHeight()
        {
            return base.GetHeight() + 5;
        }

        public override void OnGUI(Rect position)
        {
            //float lineX = (position.x + (position.width / 2)) - header.lineWidth / 2;
            float lineY = position.y + 0;
            //float lineWidth = header.lineWidth;

            var g = new GUIStyle(EditorStyles.toolbar);
            //g.fontStyle = FontStyle.Bold;
            g.alignment = TextAnchor.LowerLeft;
            //g.font = JUEditor.CustomEditorStyles.JUEditorFont();

            if (EditorGUIUtility.isProSkin == false)
            {
                g.normal.textColor = Color.black;
            }
            else
            {
                g.normal.textColor = Color.white;
            }

            //g.normal.textColor = new Color(1f, 0.7f, 0.5f);
            g.fontSize = 16;
            g.richText = true;
            Rect newposition = new Rect(position.x - 17, lineY, position.width + 28, position.height);
            EditorGUI.LabelField(newposition, "  " + header.text, g);
        }
    }


    [CustomPropertyDrawer(typeof(JUSubHeader))]
    class JUSubHeaderDecoratorDrawer : DecoratorDrawer
    {
        JUSubHeader header
        {
            get { return ((JUSubHeader)attribute); }
        }

        public override float GetHeight()
        {
            return base.GetHeight() + 5;
        }
        public override void OnGUI(Rect position)
        {
            //float lineX = (position.x + (position.width / 2)) - header.lineWidth / 2;
            float lineY = position.y + 1;
            //float lineWidth = header.lineWidth;
            var g = new GUIStyle(EditorStyles.boldLabel);
            g.fontStyle = FontStyle.Bold;
            //g.font = JUEditor.CustomEditorStyles.JUEditorFont();
            g.alignment = TextAnchor.MiddleLeft;

            if (EditorGUIUtility.isProSkin == false)
            {
                g.normal.textColor = Color.black;
            }
            else
            {
                g.normal.textColor = Color.white;
            }

            //g.normal.textColor = new Color(1f, 0.7f, 0.5f);
            g.fontSize = 15;
            g.richText = true;

            Rect newposition = new Rect(position.x - 17, lineY, position.width + 19, position.height);
            EditorGUI.LabelField(newposition, "   " + header.text, g);
        }
    }


    [CustomPropertyDrawer(typeof(JUReadOnly))]
    class JUReadOnlyPropertyDrawer : PropertyDrawer
    {
        JUReadOnly jureadonly
        {
            get { return ((JUReadOnly)attribute); }
        }
        private bool drawing;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (drawing == true)
            {
                return base.GetPropertyHeight(property, label);
            }
            else
            {
                return 0;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (jureadonly.DisableOnFalse == true && GetAttributeConditionValue(jureadonly, property) == false)
            {
                drawing = false;
                return;
            }
            drawing = true;
            if (jureadonly.ConditionPropertyName == "")
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }
            else
            {
                //var cond = (JUReadOnly)attribute;

                GUI.enabled = GetAttributeConditionValue(jureadonly, property);
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }
        }
        public bool GetAttributeConditionValue(JUReadOnly jureadonly, SerializedProperty property)
        {
            if (jureadonly.ConditionPropertyName == "") return true;

            bool enabled = true;
            var booleanCondition = property.serializedObject.FindProperty(jureadonly.ConditionPropertyName);

            enabled = booleanCondition.boolValue;

            if (jureadonly.Inverse == false)
            {
                return enabled;
            }
            else
            {
                return !enabled;
            }
        }
    }



    [CustomPropertyDrawer(typeof(JUButton))]
    class JUButtonPropertyDrawer : DecoratorDrawer
    {
        JUButton jubutton
        {
            get { return ((JUButton)attribute); }
        }
        public override float GetHeight()
        {
            return base.GetHeight() + 5;
        }

        public override void OnGUI(Rect position)
        {
            JUButton tAttribute = attribute as JUButton;

            UnityEngine.Object theObject = Selection.activeGameObject.GetComponent(tAttribute.ClassType) as UnityEngine.Object;
            
            if (jubutton.methodName == "")
            {
                GUI.Button(position, jubutton.labelText);
            }
            else
            {
                if (GUI.Button(position, jubutton.labelText))
                {
                    MethodInfo tMethod = theObject.GetType().GetMethod(tAttribute.methodName);
                    tMethod.Invoke(theObject, null);
                }
            }
        }
    }

    

#endif
}


