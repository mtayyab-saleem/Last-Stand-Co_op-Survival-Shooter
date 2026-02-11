using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.FX
{

    [AddComponentMenu("JU TPS/FX/Shaker")]
    public class Shaker : MonoBehaviour
    {
        // >>> Inspector Properties
        public Transform ShakeTarget;
        [Range(0, 60)] public float MaxAngle = 5;


        [Range(0, 1)] public float ShakeIntensity = 1;
        [Range(0, 20)] public float ShakeStartIntensity = 3;
        [Range(0, 20)] public float ShakeEndIntensity = 3f;
        [Range(0, 20)] public float ShakeSpeed = 2f;
        private float CurrentTime;
        private float ShakeDuration;

        public bool AwaysShaking;

        //Shaking Runtime Properties
        private float CurrentShakeIntensity;
        [HideInInspector] public bool IsShaking;

        //Perlin Noise Cordinates
        private float CoordX, CoordY, CoordZ;

        //Local Euler Rotation
        private float RotX, RotY, RotZ;
        private Vector3 ShakingEulerRotation;


        public Vector3 GetShakeLocalEulerRotation { get => ShakingEulerRotation; }
        public Quaternion GetShakeLocalRotation { get => Quaternion.Euler(ShakingEulerRotation); }




        void Start()
        {
            CoordX = Random.Range(-1000, 1000);
            CoordY = Random.Range(-1000, 1000);
            CoordZ = Random.Range(-1000, 1000);

            if (ShakeTarget == null) ShakeTarget = transform;
        }
        void Update()
        {
            //Clamp intensity value
            CurrentShakeIntensity = Mathf.Clamp(CurrentShakeIntensity, 0, 1);

            //Modify shake curve
            float IntensityQuadratic = CurrentShakeIntensity * CurrentShakeIntensity;

            float time = Time.time * ShakeSpeed;

            RotX = (ShakeIntensity * IntensityQuadratic) * MaxAngle * PerlinNoise(CoordX, time);
            RotY = (ShakeIntensity * IntensityQuadratic) * MaxAngle * PerlinNoise(CoordY, time);
            RotZ = (ShakeIntensity * IntensityQuadratic) * MaxAngle * PerlinNoise(CoordZ, time);

            ShakingEulerRotation.Set(RotX, RotY, RotZ);
            ShakeTarget.localEulerAngles = ShakingEulerRotation;


            if (!AwaysShaking)
            {
                switch (IsShaking)
                {
                    case true:
                        StartShaking();
                        break;
                    case false:
                        EndShaking();
                        break;
                }
            }
            else
            {
                StartShaking();
            }

            if (CurrentTime < ShakeDuration)
            {
                CurrentTime += Time.deltaTime;
                IsShaking = true;
            }
            else
            {
                IsShaking = false;
            }
        }
        private void EndShaking()
        {
            CurrentShakeIntensity -= ShakeEndIntensity * Time.deltaTime;
        }
        private void StartShaking()
        {
            CurrentShakeIntensity += ShakeStartIntensity * Time.deltaTime;
        }

        private static Shaker currentCameraInstance;
        public static Shaker GetCurrentCameraInstance()
        {
            if (currentCameraInstance == null)
            {
                if (Camera.current != null)
                {
                    currentCameraInstance = Camera.current.GetComponent<Shaker>();
                    return currentCameraInstance;
                }
                else
                {
                    return FindObjectOfType<Shaker>(true);
                    //Debug.LogWarning("Camera Current no found");
                    //return null;
                }
            }
            else
            {
                if (Camera.current != null && Camera.current != currentCameraInstance.GetComponent<Camera>())
                {
                    currentCameraInstance = Camera.current.GetComponent<Shaker>();
                    return currentCameraInstance;
                }
                else
                {
                    return currentCameraInstance;
                }
            }
        }

        /// <summary>
        /// Shake...
        /// </summary>
        public void Shake(float Speed = 3, float Duration = 0.5f, float StartIntensity = 15, float EndIntensity = 3, float MaxRotationAngle = 5, float Intensity = 1)
        {
            CurrentTime = 0;
            ShakeSpeed = Speed;
            ShakeDuration = Duration;
            ShakeStartIntensity = StartIntensity;
            ShakeEndIntensity = EndIntensity;
            MaxAngle = MaxRotationAngle;
            ShakeIntensity = Intensity;
        }
        public float PerlinNoise(float coordinate, float time)
        {
            return (1 - 2 * Mathf.PerlinNoise(coordinate + time, coordinate + time));
        }
    }

}