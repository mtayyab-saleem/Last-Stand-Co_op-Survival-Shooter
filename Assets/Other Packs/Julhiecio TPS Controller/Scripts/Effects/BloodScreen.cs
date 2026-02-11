using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Blood Screen")]
    [RequireComponent(typeof(Image))]
    public class BloodScreen : MonoBehaviour
    {
        public static BloodScreen instance;
        JUTPS.CharacterBrain.JUCharacterBrain pl;
        Image img;
        float healthvalue;
        Color currentColor;
        void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            pl = (player != null) ? player.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>() : null;
            img = GetComponent<Image>();
            instance = this;
        }

        // Update is called once per frame
        void Update()
        {
            if (pl == null) return;
            if (pl.CharacterHealth != null)
            {
                healthvalue = Mathf.Lerp(healthvalue, pl.CharacterHealth.Health / pl.CharacterHealth.MaxHealth, 15 * Time.deltaTime);
                currentColor = Color.Lerp(Color.white, Color.clear, healthvalue);
                img.color = Color.Lerp(img.color, currentColor, 5 * Time.deltaTime);
            }
        }
        private void PlayerHasHited()
        {
            img.color = Color.white;
        }
        public static void PlayerTakingDamaged()
        {
            if (instance == null) { return; }

            instance.PlayerHasHited();
        }
    }
}