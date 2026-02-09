using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JUTPS;

namespace JUTPS.UI
{

    [AddComponentMenu("JU TPS/UI/UI Health Bar")]
    public class UIHealhBar : MonoBehaviour
    {
        [Header("UI Health Bar Settings")]
        [SerializeField] private JUHealth HealthComponent;
        [SerializeField] private bool IsPlayerHealthBar = true;
        [SerializeField] private Image HealthBarImage;
        [SerializeField] private float Speed = 6;
        [SerializeField] private Text HealthPointsText;

        [Header("Health Bar Color Change")]
        [SerializeField] private Color EmptyHPColor = Color.red;
        [SerializeField] private Color FullHPColor = Color.green;
        [SerializeField] private Color HPHealingColor = Color.cyan;
        [SerializeField] private Color HPLossColor = Color.yellow;
        [SerializeField] private bool ChangeHPTextColorToo = true;

        private float oldFillAmount;
        void Start()
        {
            if (IsPlayerHealthBar)
            {
                GameObject pl = GameObject.FindGameObjectWithTag("Player");
                HealthComponent = pl.GetComponent<JUHealth>();
            }

            oldFillAmount = HealthBarImage.fillAmount;
        }

        void Update()
        {
            if (HealthComponent == null || HealthBarImage == null) return;


            float healthValueNormalized = HealthComponent.Health / HealthComponent.MaxHealth;
            HealthBarImage.fillAmount = Mathf.MoveTowards(HealthBarImage.fillAmount, healthValueNormalized, Speed * Time.deltaTime);

            HealthBarImage.color = Color.Lerp(EmptyHPColor, FullHPColor, HealthBarImage.fillAmount);

            if (HealthPointsText != null)
            {
                HealthPointsText.text = HealthComponent.Health.ToString("000") + "/" + HealthComponent.MaxHealth;
                if (ChangeHPTextColorToo) HealthPointsText.color = Color.Lerp(HealthBarImage.color, Color.white, 0.6f);
            }
            if (oldFillAmount != HealthBarImage.fillAmount)
            {
                //Health Healing
                if (oldFillAmount < HealthBarImage.fillAmount)
                {
                    HealthBarImage.color = HPHealingColor;
                }
                //Health Loss
                if (oldFillAmount > HealthBarImage.fillAmount)
                {
                    HealthBarImage.color = HPLossColor;
                }

                oldFillAmount = HealthBarImage.fillAmount;
            }

        }
    }

}