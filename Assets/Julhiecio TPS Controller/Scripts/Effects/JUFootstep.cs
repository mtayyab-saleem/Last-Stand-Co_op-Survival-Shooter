using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Footstep")]
    public class JUFootstep : MonoBehaviour
    {
        private Animator anim;
        [Header("FX Settings")]
        public AudioSource audioSource;
        public SurfaceAudiosWithFX[] FootstepAudioClips ;
        public bool InvertX;
        [Range(0, 1)]
        public float MinTimeToPlayAudio = 0.3f;

        [Header("Ground Check")]
        public LayerMask GroundLayers;
        [Range(0, 1)]
        public float CheckRadius = 0.1f;
        [Header("Ground Check Position Offset")]
        [Range(-0.2f, 0.2f)]
        public float UpOffset = -0.07f;
        [Range(-0.2f, 0.2f)]
        public float ForwardOffset = 0.07f;

        [Space]
        public Transform LeftFoot;
        public Transform RightFoot;


        private bool LeftFootsteped;
        private bool RightFootsteped;

        private float CurrentTimeToLeftFootstep;
        private float CurrentTimeToRightFootstep;
        void Start()
        {
            audioSource = audioSource == null ? GetComponent<AudioSource>() : audioSource;
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                anim = animator;
                if (LeftFoot == null || RightFoot == null)
                {
                    LeftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                    RightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                }
            }
            if (GroundLayers.value == 0)
            {
                GroundLayers = LayerMask.GetMask("Default");
            }
        }


        // Update is called once per frame
        protected virtual void Update()
        {
            if(LeftFoot == null || RightFoot == null)
            {
                LeftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                RightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
            }
            Vector3 LeftFootPosition = LeftFoot.position + transform.forward * ForwardOffset + transform.up * UpOffset;
            Vector3 RightFootPosition = RightFoot.position + transform.forward * ForwardOffset + transform.up * UpOffset;

            Collider[] LeftFootCheck = Physics.OverlapSphere(LeftFootPosition, CheckRadius, GroundLayers);
            Collider[] RightFootCheck = Physics.OverlapSphere(RightFootPosition, CheckRadius, GroundLayers);

            if (CurrentTimeToLeftFootstep < MinTimeToPlayAudio) { CurrentTimeToLeftFootstep += Time.deltaTime; }
            if (CurrentTimeToRightFootstep < MinTimeToPlayAudio) { CurrentTimeToRightFootstep += Time.deltaTime; }

            //Left Footstep
            if (LeftFootCheck.Length == 0)
            {
                LeftFootsteped = false;
            }
            else
            {
                if (LeftFootCheck.Length > 0 && LeftFootsteped == false && CurrentTimeToLeftFootstep > MinTimeToPlayAudio)
                {
                    DoFootstep(LeftFoot, LeftFootCheck[0].tag);
                    CurrentTimeToLeftFootstep = 0;
                    LeftFootsteped = true;
                }
            }


            //Right Footstep
            if (RightFootCheck.Length == 0)
            {
                RightFootsteped = false;
            }
            else
            {
                if (RightFootCheck.Length > 0 && RightFootsteped == false && CurrentTimeToRightFootstep > MinTimeToPlayAudio)
                {
                    DoFootstep(RightFoot, RightFootCheck[0].tag);
                    CurrentTimeToRightFootstep = 0;
                    RightFootsteped = true;
                }
            }
        }
        public virtual void DoFootstep(Transform Foot, string SurfaceTag = "Untagged")
        {
            // Ground Raycast to get ground angle
            RaycastHit hit;
            Physics.Raycast(Foot.position, -transform.up, out hit, 1, GroundLayers);

            // Play random footstep audio, instantiate decal and return the decal gameobject
            GameObject FootstepDecal = SurfaceAudiosWithFX.Play(audioSource, FootstepAudioClips, hit.point, Quaternion.identity, null, SurfaceTag);

            // Finish current footstep if decal is null
            if (FootstepDecal == null) { return; }
            Transform Decal = FootstepDecal.transform;
            // Set Footstep Decal rotation
            Decal.rotation = Foot.rotation;

            // Aligh Footstep Decal to ground angle
            Decal.rotation = Quaternion.FromToRotation(Foot.up, hit.normal) * Foot.rotation;

            // Invert Footstep Decal direction
            Decal.rotation = Quaternion.LookRotation(-Decal.forward);

            // Fix Footstep Decal sides fix
            if (Foot == RightFoot)
            {
                Decal.localScale = new Vector3(InvertX ? Decal.localScale.x : -Decal.localScale.x, Decal.localScale.y, Decal.localScale.z);
            }
            else
            {
                Decal.localScale = new Vector3(InvertX ? -Decal.localScale.x : Decal.localScale.x, Decal.localScale.y, Decal.localScale.z);
            }

            // Draw a line in the upward direction of the Footstep Decal
            Debug.DrawRay(FootstepDecal.transform.position, FootstepDecal.transform.up * 2, Color.red, 1);

        }

#if UNITY_EDITOR
        private static AudioClip clip;

        [ContextMenu("Load Default Footstep Audios", false, 100)]
        public void LoadDefaultFootstepInInspector()
        {
            LoadDefaultFootstepAudios(this);
        }
        public static void LoadDefaultFootstepAudios(JUFootstep footsteper, string path = "Assets/Julhiecio TPS Controller/Audio/Footstep/")
        {
            if (!System.IO.Directory.Exists(path))
            {
                Debug.LogError("Unable to load default footstep audios as the indicated path does not exist.");
                return;
            }

            var footstepClips = new List<SurfaceAudiosWithFX>();
            //Add empty cases
            for (int i = 0; i < 4; i++)
            {
                footstepClips.Add(new SurfaceAudiosWithFX());
                for (int x = 0; x < 4; x++)
                {
                    footstepClips[i].AudioClips.Add(clip);
                }
            }

            footsteper.FootstepAudioClips = footstepClips.ToArray();

            //Load Footstep Audios
            footsteper.FootstepAudioClips[0].SurfaceTag = "Untagged";
            for (int i = 0; i < 4; i++)
            {
                string audioClipPath = path + "Concrete/Footstep on Concrete 0" + (i + 1) + " OGG.ogg";
                if (!System.IO.File.Exists(audioClipPath))
                {
                    Debug.LogWarning("Unable to load default audio: " + audioClipPath);
                }
                footsteper.FootstepAudioClips[0].AudioClips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath);
            }

            footsteper.FootstepAudioClips[1].SurfaceTag = "Stone";
            for (int i = 0; i < 4; i++)
            {
                //Assets/Julhiecio TPS Controller/Audio/Footstep/CC0 Sounds/Stones/Footsteps-on-stone01.ogg
                string audioClipPath = path + "Stones/Footsteps-on-stone0" + (i + 1) + ".ogg";
                if (!System.IO.File.Exists(audioClipPath))
                {
                    Debug.LogWarning("Unable to load default audio: " + audioClipPath);
                }
                footsteper.FootstepAudioClips[1].AudioClips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath);
            }

            footsteper.FootstepAudioClips[2].SurfaceTag = "Grass";
            for (int i = 0; i < 4; i++)
            {
                string audioClipPath = path + "Grass/Footsteps-on-grass0" + (i + 1) + ".ogg";
                if (!System.IO.File.Exists(audioClipPath))
                {
                    Debug.LogWarning("Unable to load default audio: " + audioClipPath);
                }
                footsteper.FootstepAudioClips[2].AudioClips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath);
            }

            footsteper.FootstepAudioClips[3].SurfaceTag = "Tiles";
            for (int i = 0; i < 4; i++)
            {
                string audioClipPath = path + "Tiles/Footstep-on-tiles0" + (i + 1) + ".ogg";
                if (!System.IO.File.Exists(audioClipPath))
                {
                    Debug.LogWarning("Unable to load default audio: " + audioClipPath);
                }
                footsteper.FootstepAudioClips[3].AudioClips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath);
            }
        }
#endif
        void OnDrawGizmos()
        {
            if (LeftFoot == null || RightFoot == null)
            {
                anim = GetComponent<Animator>();
                LeftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                RightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                return;
            }
            Color CollisionColor = Color.green;
            CollisionColor.a = 0.4f;
            Color NoCollisionColor = Color.red;
            NoCollisionColor.a = 0.2f;

            Vector3 LeftFootPosition = LeftFoot.position + transform.forward * ForwardOffset + transform.up * UpOffset;
            Vector3 RightFootPosition = RightFoot.position + transform.forward * ForwardOffset + transform.up * UpOffset;

            if (LeftFootsteped) { Gizmos.color = CollisionColor; } else { Gizmos.color = NoCollisionColor; }
            Gizmos.DrawSphere(LeftFootPosition, CheckRadius);
            Gizmos.DrawWireSphere(LeftFootPosition, CheckRadius);

            if (RightFootsteped) { Gizmos.color = CollisionColor; } else { Gizmos.color = NoCollisionColor; }
            Gizmos.DrawSphere(RightFootPosition, CheckRadius);
            Gizmos.DrawWireSphere(RightFootPosition, CheckRadius);
        }
    }
}
