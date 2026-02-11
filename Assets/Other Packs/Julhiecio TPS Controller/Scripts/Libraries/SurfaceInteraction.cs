using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.FX
{
    [System.Serializable]
    public class SurfaceAudios
    {
        public string SurfaceTag;
        public List<AudioClip> AudioClips = new List<AudioClip>(4);
        public static void PlayRandomAudio(AudioSource audioSource, List<SurfaceAudios> SurfaceAudioClips, string surfaceTag = "Untagged")
        {
            for (int i = 0; i < SurfaceAudioClips.Count; i++)
            {
                if (SurfaceAudioClips[i].SurfaceTag == surfaceTag)
                {
                    audioSource.PlayOneShot(SurfaceAudioClips[i].AudioClips[Random.Range(0, SurfaceAudioClips.Count)]);
                }
            }
        }
    }

    [System.Serializable]
    public class SurfaceAudiosWithFX
    {
        public string SurfaceTag;
        public List<AudioClip> AudioClips = new List<AudioClip>(4);
        public List<GameObject> Effects = new List<GameObject>(4);

        public SurfaceAudiosWithFX(string tagName = "Skin")
        {
            SurfaceTag = tagName;
        }

        public static void PlayRandomAudioFX(AudioSource audioSource, List<SurfaceAudiosWithFX> SurfaceAudioClips, string surfaceTag = "Untagged")
        {
            bool played = false;

            for (int i = 0; i < SurfaceAudioClips.Count; i++)
            {
                if (SurfaceAudioClips[i].SurfaceTag == surfaceTag)
                {
                    audioSource.PlayOneShot(SurfaceAudioClips[i].AudioClips[Random.Range(0, SurfaceAudioClips[i].AudioClips.Count)]);
                    return;
                }
            }

            if (played == false)
            {
                audioSource.PlayOneShot(SurfaceAudioClips[0].AudioClips[Random.Range(0, SurfaceAudioClips[0].AudioClips.Count)]);
            }
        }
        public static GameObject SpawnRandomFX(List<SurfaceAudiosWithFX> SurfaceAudioClips, Vector3 FXPosition, Quaternion FXRotation = default(Quaternion), string surfaceTag = "Untagged", float timeToDestroy = 5, bool HideInHierarchy = true)
        {
            bool spawned = false;

            for (int i = 0; i < SurfaceAudioClips.Count; i++)
            {
                if (SurfaceAudioClips[i].SurfaceTag == surfaceTag)
                {
                    if (SurfaceAudioClips[i].Effects.Count > 0)
                    {
                        //Spawn
                        GameObject obj = GameObject.Instantiate(SurfaceAudioClips[i].Effects[Random.Range(0, SurfaceAudioClips[i].Effects.Count)], FXPosition, FXRotation);

                        //Hide
                        if (HideInHierarchy) obj.hideFlags = HideFlags.HideInHierarchy;
                        //Destroy
                        GameObject.Destroy(obj, timeToDestroy);

                        //Finish
                        return obj;
                    }
                }
            }

            //If it couldn't find any SurfaceAudioClip with the same SurfaceTag, it will instantiate a random effect from the first SurfaceAudioClip 
            if (spawned == false)
            {
                if (SurfaceAudioClips[0].Effects.Count > 0)
                {
                    //Spawn
                    GameObject obj = GameObject.Instantiate(SurfaceAudioClips[0].Effects[Random.Range(0, SurfaceAudioClips[0].Effects.Count)], FXPosition, FXRotation);

                    //Hide
                    if (HideInHierarchy) obj.hideFlags = HideFlags.HideInHierarchy;

                    //Destroy
                    GameObject.Destroy(obj, timeToDestroy);

                    //Finish
                    return obj;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public static GameObject Play(AudioSource audioSource, SurfaceAudiosWithFX[] SurfaceAudioClips, Vector3 FXPosition, Quaternion FXRotation = default(Quaternion), Transform Parent = null, string surfaceTag = "Untagged", float timeToDestroy = 5, bool HideInHierarchy = true)
        {
            if (SurfaceAudioClips.Length == 0 || audioSource == null) return null;
            bool played = false;

            for (int i = 0; i < SurfaceAudioClips.Length; i++)
            {
                if (SurfaceAudioClips[i].SurfaceTag == surfaceTag)
                {
                    audioSource.PlayOneShot(SurfaceAudioClips[i].AudioClips[Random.Range(0, SurfaceAudioClips[i].AudioClips.Count)]);
                    if (SurfaceAudioClips[i].Effects.Count > 0)
                    {
                        GameObject obj = GameObject.Instantiate(SurfaceAudioClips[i].Effects[Random.Range(0, SurfaceAudioClips[i].Effects.Count)], FXPosition, FXRotation);
                        obj.transform.SetParent(Parent);
                        if (HideInHierarchy) { obj.hideFlags = HideFlags.HideInHierarchy; }
                        GameObject.Destroy(obj, timeToDestroy);

                        played = true;
                        return obj;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            if (played == false)
            {
                audioSource.PlayOneShot(SurfaceAudioClips[0].AudioClips[Random.Range(0, SurfaceAudioClips[0].AudioClips.Count)]);

                if (SurfaceAudioClips[0].Effects.Count > 0)
                {
                    GameObject obj = GameObject.Instantiate(SurfaceAudioClips[0].Effects[Random.Range(0, SurfaceAudioClips[0].Effects.Count)], FXPosition, FXRotation);
                    obj.transform.SetParent(Parent);
                    if (HideInHierarchy) { obj.hideFlags = HideFlags.HideInHierarchy; }
                    GameObject.Destroy(obj, timeToDestroy);
                    return obj;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }

    [System.Serializable]
    public class SurfaceFX
    {
        public string SurfaceTag;
        public GameObject ParticleFXPrefab;
        private static bool Instantiated;

        /// <summary>
        /// Instantiates a ParticleFX according to the surface tag
        /// </summary>
        /// <param name="SurfaceFx"> SurfaceFX variable type </param>
        /// <param name="SurfaceTag"> Tag of surface </param>
        /// <param name="Postion"> Position to instantiate ParticleFX </param>
        /// <param name="Rotation"> Rotation to instantiate ParticleFX </param>
        /// <param name="TimeToDestroy"> Time to auto destroy ParticleFX</param>
        public static void InstantiateParticleFX(SurfaceFX[] SurfaceFx, string SurfaceTag = "Untagged", Vector3 Postion = default, Quaternion Rotation = default, Transform parent = null, float TimeToDestroy = 5f)
        {
            Instantiated = false;

            for (int i = 0; i < SurfaceFx.Length; i++)
            {
                if (SurfaceTag == SurfaceFx[i].SurfaceTag && SurfaceFx[i].ParticleFXPrefab != null)
                {
                    GameObject pfx = GameObject.Instantiate(SurfaceFx[i].ParticleFXPrefab, Postion, Rotation);
                    pfx.transform.parent = parent;
                    //pfx.hideFlags = HideFlags.HideInHierarchy;

                    if (TimeToDestroy > 0)
                    {
                        GameObject.Destroy(pfx, TimeToDestroy);
                    }
                    Instantiated = true;
                }
            }

            //If the code has come this far and the particle has not yet been instantiated,
            //it means that a tag that matches the surface was not found, then it will instantiate the first ParticleFX in the list
            if (Instantiated == false && SurfaceFx.Length > 0)
            {
                GameObject pfx = GameObject.Instantiate(SurfaceFx[0].ParticleFXPrefab, Postion, Rotation);
                pfx.transform.parent = parent;
                //pfx.hideFlags = HideFlags.HideInHierarchy;

                if (TimeToDestroy > 0)
                {
                    GameObject.Destroy(pfx, TimeToDestroy);
                }
                Instantiated = true;
            }
        }
    }

}