using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerProfile", menuName = "Last Stand/Player Profile")]
public class PlayerProfileSO : ScriptableObject
{
    public string playerName = "Player";

    private string SavePath
    {
        get
        {
#if UNITY_EDITOR
            return Path.Combine(Application.persistentDataPath, "playerProfile_editor.json");
#else
            return Path.Combine(Application.persistentDataPath, "playerProfile_build.json");
#endif
        }
    }

    public void SaveProfile()
    {
        string json = JsonUtility.ToJson(this, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Profile saved to: " + SavePath);
    }

    public void LoadProfile()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            JsonUtility.FromJsonOverwrite(json, this);
            Debug.Log("Profile loaded from: " + SavePath);
        }
        else
        {
            playerName = "Player " + Random.Range(1000, 9999);
            SaveProfile();
        }
    }
}
