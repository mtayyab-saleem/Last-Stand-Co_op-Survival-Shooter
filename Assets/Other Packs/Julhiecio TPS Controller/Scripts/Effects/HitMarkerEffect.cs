using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace JUTPS.FX
{
    public class HitMarkerEffect : MonoBehaviour
    {
        public static HitMarkerEffect instance;

        private Image HitImage;
        private AudioSource HitSound;

        [Header("Hit Effect")]
        public bool EnableHitEffect = true;
        public AudioClip HitAudioClip;
        public string[] HitTags;

        public Color HitColor = Color.white;
        public float Speed = 5;
        private Color ClearWhite = new Color(1, 1, 1, 0);

        [Header("Damage Count")]
        public bool ShowDamage;
        public AudioClip CriticalDamageAudioClip;
        public Text DamageText;
        public float CriticalHitMax = 50;
        public float TextFadeSpeed = 3;
        public Color NormalHitColor = Color.white, CriticalHitColor = Color.red;
        private Vector3 HitDamagePosition;
        private float CurrentDamage;
        void Awake()
        {
            HitSound = GetComponent<AudioSource>();
            HitImage = GetComponent<Image>();
            if (DamageText != null) DamageText.color = Color.clear;
        }
        private void OnEnable()
        {
            instance = this;
        }
        // Update is called once per frame
        void Update()
        {
            if (HitImage != null && EnableHitEffect)
            {
                HitImage.color = Color.Lerp(HitImage.color, ClearWhite, Speed * Time.deltaTime);
            }
            if (ShowDamage && DamageText != null)
            {
                if (DamageText.color != ClearWhite)
                {
                    JUTPS.UI.UIElementToWorldPosition.SetUIWorldPosition(DamageText.gameObject, HitDamagePosition, Vector3.zero);
                    DamageText.color = Color.Lerp(DamageText.color, ClearWhite, TextFadeSpeed * Time.deltaTime);
                }

            }
        }
        private void Hit()
        {
            if (HitImage != null)
            {
                HitImage.color = HitColor;
                HitSound.PlayOneShot(HitAudioClip);
            }

            if (DamageText != null && ShowDamage)
            {
                bool IsCriticalHit = CurrentDamage > CriticalHitMax;
                DamageText.text = ((int)CurrentDamage).ToString();
                DamageText.color = IsCriticalHit ? CriticalHitColor : NormalHitColor;
                if (CriticalDamageAudioClip != null && IsCriticalHit && HitSound != null)
                {
                    HitSound.Stop();
                    HitSound.PlayOneShot(CriticalDamageAudioClip);
                }
            }
        }
        public static void HitCheck(string CollidedObjectTag, Vector3 hitPosition = default(Vector3), float Damage = 0)
        {
            foreach (string tag in instance.HitTags)
            {
                if (CollidedObjectTag == tag)
                {
                    instance.HitDamagePosition = hitPosition;
                    instance.CurrentDamage = Damage;
                    instance.Hit();
                }
            }
        }
    }
}