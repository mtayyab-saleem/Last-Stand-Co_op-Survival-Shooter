using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
namespace JUTPSEditor.JUAddonInstallationWizard
{
    public class AddonInstallerWindowEditor : EditorWindow
    {
        private Vector2 scrollPos;
        private Texture2D Banner, DownloadIcon;

        string[] addonCategory = new string[] { "Default Addons", "External Addons" };
        private int currentCategory = 0;

        string DefaultAddonsPath;
        string ExternalAddonsPath;

        AddonInfo[] DefaultAddons;
        AddonInfo[] ExternalAddons;

        private bool LoadedAddons = false;
        private void OnGUI()
        {
            if (Banner == null || DownloadIcon == null)
            {
                Banner = CustomEditorUtilities.GetImage("JUTPSLOGO");
                DownloadIcon = CustomEditorUtilities.GetImage("DownloadIcon");
            }
            
            if (Banner != null)
            {
                GUILayout.BeginHorizontal();

                CustomEditorUtilities.RenderImageWithResize(Banner, new Vector2(64, 40));
                GUILayout.Label("Addon Installation Wizard", JUTPSEditor.CustomEditorStyles.Title(14), GUILayout.Height(28));

                GUILayout.EndHorizontal();
            }

            if (LoadedAddons == false || DefaultAddons == null)
            {
                DefaultAddonsPath = AddonInfo.GetAddonsFolder(ExternalAddonPath: false);
                ExternalAddonsPath = AddonInfo.GetAddonsFolder(ExternalAddonPath: true);

                DefaultAddons = AddonInfo.GetAllAddons(ExternalAddon: false);
                ExternalAddons = AddonInfo.GetAllAddons(ExternalAddon: true);
                LoadedAddons = true;
            }

            currentCategory = GUILayout.Toolbar(currentCategory, addonCategory);

            GUILayout.BeginHorizontal(CustomEditorStyles.Header());

            // >>> SCROLL
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (currentCategory)
            {
                case 0:
                    DrawDefaultAddonTabs();
                    break;
                case 1:
                    DrawExternalAddonTabs();
                    break;
            }

            GUILayout.EndScrollView();
            // <<< SCROLL

            GUILayout.EndHorizontal();


        }
        private void DrawDefaultAddonTabs()
        {
            if (DefaultAddons.Length > 0)
            {
                foreach (AddonInfo addon in DefaultAddons) DrawAddonTab(addon, DownloadIcon, 32, 200);
            }
            else
            {
                GUILayout.Label("No addons found", EditorStyles.centeredGreyMiniLabel);
            }
        }
        private void DrawExternalAddonTabs()
        {
            if (ExternalAddons.Length > 0)
            {
                foreach (AddonInfo addon in ExternalAddons) DrawAddonTab(addon, DownloadIcon, 32, 200);
            }
            else
            {
                GUILayout.Label("No addons found", EditorStyles.centeredGreyMiniLabel);
            }
        }
        [MenuItem("Window/JU TPS/Addons Installer")]
        public static void ShowWindow()
        {
            EditorWindow wnd = GetWindow<AddonInstallerWindowEditor>();

            wnd.titleContent = new GUIContent("JU Addon Installer", (Texture2D)Resources.Load("Editor Resources/Textures/PackageIcon"));
            const int width = 350;
            const int height = 310;

            var x = (Screen.currentResolution.width - width) / 2;
            var y = (Screen.currentResolution.height - height) / 2;

            wnd.position = new Rect(x, y, width, height);

            wnd.minSize = new Vector2(width, height);
            wnd.maxSize = new Vector2(width, height);
        }


        public static void DrawAddonTab(AddonInfo addon, Texture2D DownloadIcon = null, float Height = 32, float Width = 256)
        {                
            GUILayout.BeginHorizontal(CustomEditorStyles.Header(), GUILayout.Width(Width));

            CustomEditorUtilities.DrawTabItem(addon.Cover, addon.Name, addon.Description, Height, Width, true);

            if (DownloadIcon != null) {

                if (GUILayout.Button(DownloadIcon, CustomEditorStyles.LabelButton(), GUILayout.Width(Height/1.32f), GUILayout.Height(Height/1.32f)))
                {
                    addon.ImportAddon();
                }
            }
            GUILayout.EndHorizontal();
        }
    }

    public class AddonInfo
    {
        public Texture2D Cover;
        public string Name;
        public string Description;
        public string PathDirectory;
        public bool ExternalAddon;
        
        public AddonInfo(string name, string description, string pathdirectory, bool externalAddon) { Name = name; Description = description; PathDirectory = pathdirectory; ExternalAddon = externalAddon; }
        public void ImportAddon()
        {
            string package_directory = GetAddonUnityPackagePath(Name, ExternalAddon);
            //Debug.Log("import directory = " + package_directory);
            if (File.Exists(package_directory))
            {
                Application.OpenURL(package_directory);
                Debug.Log("Importing " + Name + "Unity Package...");
            }
            else { Debug.LogWarning("Unable to find addon unitypackage in directory: " + package_directory); }

        }
        public static AddonInfo SetupAddonInformation(string AddonName, bool externalAddon)
        {
            AddonInfo add = new AddonInfo(AddonName, "", "empty directory", externalAddon);

            string directoryPath = GetAddonFolderDirectoryPath(AddonName, externalAddon);
            //Debug.Log("directoryPath : " + directoryPath);
            add.PathDirectory = directoryPath;

            string descriptionDirectory = GetAddonFolderDirectoryPath(AddonName, externalAddon) + "/Description.txt";
            //Debug.Log("descriptionDirectory : " + ProjectFolder() + descriptionDirectory);

            if (File.Exists(ProjectFolder() + descriptionDirectory))
            {
                TextAsset description = AssetDatabase.LoadAssetAtPath(descriptionDirectory, typeof(TextAsset)) as TextAsset;
                add.Description = description.text;
            }
            Texture2D coverIcon = AssetDatabase.LoadAssetAtPath(directoryPath + "/Cover.png", typeof(Texture2D)) as Texture2D;
            //Debug.Log("coverIcon : " + directoryPath + "/Cover.png");
            add.Cover = coverIcon;

            return add;
        }
        public static string GetAddonsFolder(bool ExternalAddonPath)
        {
            var AddonScriptDirectory = AssetDatabase.FindAssets($"t:Script {"JUTPSAddonInstaller"}");
            string AddonFolderPath = AssetDatabase.GUIDToAssetPath(AddonScriptDirectory[0]);
            AddonFolderPath = AddonFolderPath.Replace("/JUTPSAddonInstaller.cs", ExternalAddonPath == false ? "/Addons/Default" : "/Addons/External");

            //Debug.Log(AddonFolderPath);

            return AddonFolderPath;
        }
        public static string GetAddonUnityPackagePath(string AddonName, bool ExternalAddonPath)
        {
            var AddonScriptDirectory = AssetDatabase.FindAssets($"t:Script {"JUTPSAddonInstaller"}");
            string AddonPackagePath = AssetDatabase.GUIDToAssetPath(AddonScriptDirectory[0]);
            AddonPackagePath = AddonPackagePath.Replace("/JUTPSAddonInstaller.cs", ExternalAddonPath == false ? "/Addons/Default" : "/Addons/External");
            AddonPackagePath = ProjectFolder() + AddonPackagePath +"/"+ AddonName + "/" + AddonName + ".unitypackage";
            //Debug.Log(AddonPackagePath);

            return AddonPackagePath;
        }
        public static string GetAddonFolderDirectoryPath(string AddonName, bool ExternalAddonPath)
        {
            var AddonScriptDirectory = AssetDatabase.FindAssets($"t:Script {"JUTPSAddonInstaller"}");
            string AddonPath = AssetDatabase.GUIDToAssetPath(AddonScriptDirectory[0]);
            AddonPath = AddonPath.Replace("/JUTPSAddonInstaller.cs", ExternalAddonPath == false ? "/Addons/Default" : "/Addons/External");
            AddonPath = AddonPath + "/" + AddonName;
            //Debug.Log(AddonPath);

            return AddonPath;
        }
        public static string ProjectFolder() { return Application.dataPath.Replace("/Assets", "/"); }
        
        public static AddonInfo[] GetAllAddons(bool ExternalAddon)
        {
            List<AddonInfo> addons = new List<AddonInfo>();

            string root_directory = GetAddonsFolder(ExternalAddon);
            string[] addons_folders = AssetDatabase.GetSubFolders(root_directory);

            foreach (string folderName in addons_folders)
            {
                string addonName = folderName.Replace(root_directory + "/", "");
                AddonInfo addon = new AddonInfo(addonName, "Addon", "empty directory", ExternalAddon);
                addon = SetupAddonInformation(addonName, ExternalAddon);
                addons.Add(addon);
            }

            return addons.ToArray();
        }

    }
}
