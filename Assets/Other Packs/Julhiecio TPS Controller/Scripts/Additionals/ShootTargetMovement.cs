using UnityEngine;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Utilities/ShootTarget Movement")]
    public class ShootTargetMovement : MonoBehaviour
    {
        public float Speed = 3f;
        private bool rightmovement;

        void Update()
        {
            if (Physics.Raycast(transform.position + transform.up * 0.5f, transform.right, 0.5f))
            {
                rightmovement = !rightmovement;
            }
            if (Physics.Raycast(transform.position + transform.up * 0.5f, -transform.right, 0.5f))
            {
                rightmovement = !rightmovement;
            }
            if (rightmovement == true)
            {
                transform.Translate(Speed * Time.deltaTime, 0, 0);
            }
            else
            {
                transform.Translate(-Speed * Time.deltaTime, 0, 0);
            }
        }
    }
}
