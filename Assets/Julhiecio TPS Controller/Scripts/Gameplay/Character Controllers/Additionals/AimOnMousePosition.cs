using UnityEngine;
using JUTPSActions;
namespace JUTPS.ActionScripts
{
    [AddComponentMenu("JU TPS/Third Person System/Additionals/Aim On Mouse Position")]
    public class AimOnMousePosition : JUTPSAction
    {
        [HideInInspector] public static Vector3 AimPosition;

        [Header("Settings")]
        public bool Enabled = true;
        public float NormalOffset = 0.1f;
        public bool PreventResetingAimPosition;
        [Header("Two Dimensional Settings")]
        public bool TwoDimensional;

        void Update()
        {
            if (Enabled == false || cam == null)
            {
                AimPosition = Vector3.zero;
                TPSCharacter.LookAtPosition = AimPosition;
                return;
            }
            Vector2 mousePosition = JUInputSystem.JUInput.GetMousePosition();
            if (TwoDimensional)
            {
                //Create a ray on mouse position
                Ray MouseRay = cam.ScreenPointToRay(mousePosition);

                //Get Pivot Position
                Vector3 pivotPosition = transform.position;
                pivotPosition.y = TPSCharacter.HumanoidSpine.position.y;

                //Get Mouse Position
                Vector3 MousePosition = MouseRay.origin + MouseRay.direction * Vector3.Distance(pivotPosition, MouseRay.origin);
                MousePosition.z = transform.position.z;

                //Get Horizontal Distance
                Vector3 mousePosNoHeight = MousePosition; mousePosNoHeight.y = pivotPosition.y;
                float HorizontalDistance = Vector3.Distance(pivotPosition, mousePosNoHeight);

                //Modify mouse position
                MousePosition.z = Mathf.Lerp(TPSCharacter.transform.position.z - 3f, pivotPosition.z, HorizontalDistance);
                //Set Aim Position
                AimPosition = Vector3.Lerp(AimPosition, MousePosition, 10 * Time.deltaTime);

                //Draw Line Current Position
                Debug.DrawLine(pivotPosition, AimPosition, Color.red);
            }
            else
            {
                var crosshairRayLayer = !TPSCharacter.MyPivotCamera ? default : TPSCharacter.MyPivotCamera.CrosshairRaycastLayerMask;
                Physics.Raycast(cam.ScreenPointToRay(mousePosition), out var hit, float.MaxValue, crosshairRayLayer);

                if (PreventResetingAimPosition)
                {
                    if (hit.point != Vector3.zero)
                        AimPosition = Vector3.Lerp(AimPosition, hit.point + hit.normal * NormalOffset, 10 * Time.deltaTime);
                }
                else
                {
                    AimPosition = Vector3.Lerp(AimPosition, hit.point + hit.normal * NormalOffset, 10 * Time.deltaTime);
                    if (hit.point == Vector3.zero)
                        AimPosition = Vector3.zero;
                }
            }
            TPSCharacter.LookAtPosition = AimPosition;
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (TwoDimensional)
            {
                Gizmos.DrawWireCube(AimPosition, new Vector3(0.1f, 0.1f, 0));
                Gizmos.DrawWireCube(AimPosition, new Vector3(0.5f, 0.5f, 0));
            }
            else
            {
                Gizmos.DrawWireCube(AimPosition, new Vector3(0.1f, 0, 0.1f));
                Gizmos.DrawWireCube(AimPosition, new Vector3(0.5f, 0, 0.5f));
            }
        }
    }

}