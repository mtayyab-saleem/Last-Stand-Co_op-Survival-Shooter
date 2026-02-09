using UnityEngine;
using UnityEditor;

using JUTPS.InventorySystem;

namespace JUTPS.CustomEditors
{
    /// <summary>
    /// Editor inspector of the <see cref="JUInventory"/> script component.
    /// </summary>
    [CustomEditor(typeof(JUInventory))]
    public class InventoryUIManagerEditor : Editor
    {
        private bool _isStateOpened;

        /// <summary>
        /// The inventory target to edit.
        /// </summary>
        public JUInventory Inventory => (JUInventory)target;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            if (GUILayout.Button("Setup Items"))
                Inventory.SetupItems();

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
                ShowStateInfo();
        }

        private void ShowStateInfo()
        {
            _isStateOpened = EditorGUILayout.BeginFoldoutHeaderGroup(_isStateOpened, "State");

            if (_isStateOpened)
            {
                EditorGUILayout.Toggle("Is Picking an Item", Inventory.IsPickingItem);
                EditorGUILayout.Toggle("Is Item Selected", Inventory.IsItemSelected);
                EditorGUILayout.Toggle("Is Dual Wielding", Inventory.IsDualWielding);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
