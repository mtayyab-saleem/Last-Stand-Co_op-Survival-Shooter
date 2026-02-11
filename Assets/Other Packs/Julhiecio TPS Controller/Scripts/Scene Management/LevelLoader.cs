using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Scene Management/Scene Loader")]
    public class LevelLoader : MonoBehaviour
    {
        public string LevelName = "Sample Scene";
        public int LevelBuildID = -1;
        public bool LoadOnAwake = false;
        void Awake()
        {
            if (LoadOnAwake)
            {
                LoadLevel();
            }
        }

        public void LoadLevel()
        {
            if (LevelBuildID > -1)
            {
                SceneManager.LoadScene(LevelBuildID);
            }
            else
            {
                SceneManager.LoadScene(LevelName);
            }
        }
        public void LoadLevel(string levelName)
        {
            SceneManager.LoadScene(levelName);
        }
        public void LoadLevel(int levelID)
        {
            SceneManager.LoadScene(levelID);
        }
        public void LoadLevelInSeconds(float Seconds)
        {
            Invoke(nameof(LoadLevel), Seconds);
        }
    }
}