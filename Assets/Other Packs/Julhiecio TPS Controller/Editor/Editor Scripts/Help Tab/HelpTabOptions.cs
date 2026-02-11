using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace JUTPS.CustomEditors
{
    public class HelpTabOptions
    {
        [MenuItem("Window/JU TPS/Help/Open Documentation")]
        public static void OpenDocumentation()
        {
            //Application.OpenURL(Application.dataPath + "/Julhiecio TPS Controller/Documentation JU TPS.pdf");
            Application.OpenURL("https://julhiecio.gitbook.io/ju-tps-documentation/");
        }
        [MenuItem("Window/JU TPS/Help/Open OFFLINE Documentation")]
        public static void OpenOfflineDocumentation()
        {
            Application.OpenURL(Application.dataPath + "/Julhiecio TPS Controller/JU TPS 3 Offline Documentation.pdf");
        }
        [MenuItem("Window/JU TPS/Help/Open Community")]
        public static void OpenCommunity()
        {
            //Application.OpenURL(Application.dataPath + "/Julhiecio TPS Controller/Documentation JU TPS.pdf");
            Application.OpenURL("https://discord.gg/aBsr2ZkqFc");
        }
        //[MenuItem("JU TPS/Help/Open Tutorials Playlist")]
        public static void OpenTutorialPlaylists()
        {
            Application.OpenURL("https://youtube.com/playlist?list=PLznOHnSwmVcGcbDpXtElYKFVFYE9DvCgz");
        }
        [MenuItem("Window/JU TPS/Help/Support Email")]
        public static void OpenSupportEmail()
        {
            Application.OpenURL("mailto:julhieciogames1@gmail.com");
        }
    }
}
