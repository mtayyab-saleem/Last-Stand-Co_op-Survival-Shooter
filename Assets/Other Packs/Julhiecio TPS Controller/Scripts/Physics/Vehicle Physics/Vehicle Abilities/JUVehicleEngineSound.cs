using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.VehicleSystem;
using JUTPSEditor.JUHeader;
namespace JUTPS.VehicleSystem
{
    [AddComponentMenu("JU TPS/Vehicle System/JU Vehicle Engine Sound")]
    public class JUVehicleEngineSound : MonoBehaviour
    {
        [JUHeader("Start Motor Audio Settings")]
        public AudioSource StartMotorAudioSource;
        public AudioClip StartMotorAudioClip;

        [JUHeader("Idle Motor Audio Settings")]
        public AudioSource MotorLoopAudioSource;
        public AudioClip MotorLoopAudioClip;
        [Range(0, 1)]
        public float IdleVolume = 0.7f;
        [Range(0, 1)]
        public float AccelerateVolume = 1;
        [Range(-3, 3)]
        public float IdlePitch = 1;
        [Range(-3, 3)]
        public float AcceleratePitch = 2f;

        public float AccelerateSpeed = 5f;
        public float DecelerateSpeed = 2f;
        public float StartDelay = 1;

        [JUHeader("Stop Motor Audio Settings")]
        [Range(-3, 3)]
        public float StoppingPitch = 0.3f;
        public float StoppingSpeed = 1f;

        private Vehicle vehicle;
        private bool startedMotor;
        private bool motorOff;
        void Start()
        {
            vehicle = GetComponent<Vehicle>();
            TurnOffMotor();
        }


        void Update()
        {
            if (!MotorLoopAudioSource) return;

            if (vehicle.IsOn)
            {
                bool accelerating = vehicle.FinalThrottle > 0;
                bool reversing = vehicle.FinalThrottle < 0;
                float engineMagnitude = new Vector2(vehicle.FinalSteer, vehicle.FinalThrottle).magnitude;
                if(!startedMotor) TurnOnMotor();

                //Accelerate Pitch Sound
                if (!MotorLoopAudioSource.isPlaying) return;
                float pitchDiff = Mathf.Abs(AcceleratePitch - IdlePitch);
                float pitch = accelerating ? (engineMagnitude * AcceleratePitch) : (reversing ? IdlePitch + pitchDiff/2 : IdlePitch);
                float volume = accelerating ? (engineMagnitude * AccelerateVolume) : IdleVolume;
                MotorLoopAudioSource.pitch = Mathf.Lerp(MotorLoopAudioSource.pitch, pitch, (accelerating ? AccelerateSpeed : DecelerateSpeed) * Time.deltaTime);
                MotorLoopAudioSource.volume = Mathf.Lerp(MotorLoopAudioSource.volume, volume, (accelerating ? AccelerateSpeed : DecelerateSpeed) * Time.deltaTime);
            }
            else
            {
                if(motorOff == false)
                {
                    MotorLoopAudioSource.pitch = Mathf.MoveTowards(MotorLoopAudioSource.pitch, StoppingPitch, StoppingSpeed * Time.deltaTime);
                    MotorLoopAudioSource.volume = Mathf.MoveTowards(MotorLoopAudioSource.volume, 0, StoppingSpeed * Time.deltaTime);
                    if(MotorLoopAudioSource.volume == 0)
                    {
                        TurnOffMotor();
                    }
                }
            }

        }
        private void TurnOnMotor()
        {
            //Start Motor
            if (startedMotor == true) return;

            //Start Motor Sound
            if (StartMotorAudioSource && StartMotorAudioClip)
            {
                StartMotorAudioSource.PlayOneShot(StartMotorAudioClip);
                StartMotorAudioSource.loop = false;
            }
            //Loop Sound
            if (MotorLoopAudioSource && MotorLoopAudioClip)
            {
                MotorLoopAudioSource.clip = MotorLoopAudioClip;
                MotorLoopAudioSource.volume = 0;
                MotorLoopAudioSource.loop = true;
                MotorLoopAudioSource.PlayDelayed(StartDelay);
            }

            motorOff = false;
            startedMotor = true;
        }

        private void TurnOffMotor()
        {
            if (motorOff == true) return;

            MotorLoopAudioSource.volume = 0;
            MotorLoopAudioSource.Stop();
            startedMotor = false;

            motorOff = true;
        }
    }
}