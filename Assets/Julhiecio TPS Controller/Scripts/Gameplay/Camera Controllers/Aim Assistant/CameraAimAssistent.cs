using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.CameraSystems
{

    [AddComponentMenu("JU TPS/Third Person System/Cameras/Additional/Aim Assistent")]
    public class CameraAimAssistent : MonoBehaviour
    {
        [System.Serializable]
        public class TargetTagOffset
        {
            public string Tag = "Enemy";
            public float UpOffset;
            public TargetTagOffset(string tag, float upOffset)
            {
                Tag = tag;
                UpOffset = upOffset;
            }
            public static float GetUpOffset(TargetTagOffset[] targetTagList, GameObject objectTag)
            {
                if (objectTag == null || targetTagList == null) return 0;

                foreach (TargetTagOffset tag in targetTagList)
                {
                    if (tag.Tag == objectTag.tag)
                    {
                        return tag.UpOffset;
                    }
                }

                return 0;
            }
        }
        private JUCameraController targetCamera;

        public float DistanceToDetect = 50;
        public float AssistentForce = 3;
        private float UpOffset => TargetTagOffset.GetUpOffset(TargetsTagsAndOffsets, ObjectInCameraCenter);
        public LayerMask TargetLayer;
        public TargetTagOffset[] TargetsTagsAndOffsets = new[] { new TargetTagOffset("Enemy", 1) };

        private string[] AllTags;
        private GameObject ObjectInCameraCenter;

        void Start()
        {
            targetCamera = GetComponent<JUCameraController>();

            List<string> taglist = new List<string>();
            foreach (TargetTagOffset tag in TargetsTagsAndOffsets)
            {
                taglist.Add(tag.Tag);
            }
            AllTags = taglist.ToArray();
        }

        void Update()
        {
            ObjectInCameraCenter = targetCamera.GetObjectOnCameraCenter(DistanceToDetect, TargetLayer);
            if (ObjectInCameraCenter == null) return;
            if (JUTPS.AI.JUCharacterArtificialInteligenceBrain.TagMatches(ObjectInCameraCenter.tag, AllTags))
            {
                Vector3 TargetRotationEuler = Quaternion.LookRotation((ObjectInCameraCenter.transform.position + transform.up * UpOffset - targetCamera.mCamera.transform.position).normalized).eulerAngles;

                targetCamera.rotytarget = Mathf.LerpAngle(targetCamera.rotytarget, TargetRotationEuler.y, AssistentForce * Time.deltaTime);
                targetCamera.rotxtarget = Mathf.LerpAngle(targetCamera.rotxtarget, TargetRotationEuler.x, AssistentForce * Time.deltaTime);
            }
        }
    }

}