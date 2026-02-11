using UnityEngine;
using UnityEngine.UI;

namespace JUTPS.WeaponSystem
{
    /// <summary>
    /// Used to show a scope UI when the player <see cref="JUCharacterController"/> <see cref="Weapon"/> is on Aim Mode.
    /// </summary>
    public class JUScopeSystemUI : MonoBehaviour
    {
        /// <summary>
        /// The scope UI image.
        /// </summary>
        public Image ScopeImage;

        /// <summary>
        /// The target player controller.
        /// </summary>
        public JUCharacterController PlayerCharacter;

        /// <summary>
        /// Return true if the scope UI is active.
        /// </summary>
        public bool IsShowingScope { get; private set; }

        private void Start()
        {
            if (!PlayerCharacter)
                PlayerCharacter = GameObject.FindGameObjectWithTag("Player")?.GetComponent<JUCharacterController>();
        }

        private void Update()
        {
            if (!PlayerCharacter || !PlayerCharacter.HoldableItemInUseRightHand)
                return;

            IsShowingScope = false;

            //if item is a weapon
            if (PlayerCharacter.HoldableItemInUseRightHand is Weapon weapon)
            {
                //if is aiming and Weapon Aim Mode is Scope Mode
                if (PlayerCharacter.IsAiming && weapon.AimMode == Weapon.WeaponAimMode.Scope)
                {
                    IsShowingScope = true;
                    if (weapon.ScopeTexture)
                        ScopeImage.sprite = weapon.ScopeTexture;
                }
            }

            ScopeImage.gameObject.SetActive(IsShowingScope);
        }
    }
}
