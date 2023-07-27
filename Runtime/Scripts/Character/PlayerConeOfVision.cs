using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode
{
    public class PlayerConeOfVision : MMConeOfVision2D
    {
        private Character_Netcode character;
        [SerializeField]
        private GameObject coneOfVisionObject;

        protected override void Awake() {
            base.Awake();
            character = GetComponent<Character_Netcode>();
        }

        protected override void LateUpdate() {
            if (character.IsLocalPlayer) {
                base.LateUpdate();
            }
            coneOfVisionObject?.SetActive(character.IsLocalPlayer);
        }
    }
}