using UnityEngine;
using UnityEngine.UI;
namespace JUTPS.Utilities
{

    [AddComponentMenu("JU TPS/Utilities/Trigger Menssage")]
    [RequireComponent(typeof(BoxCollider))]
    public class UIMenssengerTrigger : MonoBehaviour
    {
        private GameObject TextPanel;
        private Text TextTarget;
        [TextArea(0, 10)]
        public string TextToShow;
        [SerializeField] private string PlayerTag = "Player";
        [SerializeField] private string MessageFieldName = "MenssagesPanel";

        BoxCollider boxcollider;
        public void Start()
        {
            TextPanel = GameObject.Find(MessageFieldName);
            TextTarget = TextPanel.GetComponentInChildren<Text>();
        }

        public void ShowMenssage()
        {
            TextPanel.SetActive(true);
            TextTarget.text = TextToShow;
        }
        public void HideMenssage()
        {
            TextPanel.SetActive(false);
            TextTarget.text = "";
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == PlayerTag)
                ShowMenssage();
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.tag == PlayerTag)
                HideMenssage();
        }

        private void OnDrawGizmos()
        {
            if (boxcollider == null) boxcollider = GetComponent<BoxCollider>();

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawCube(boxcollider.center, boxcollider.size);
            Gizmos.color = new Color(1, 1, 1, 0.2f);
            Gizmos.DrawWireCube(boxcollider.center, boxcollider.size);
        }
    }
}