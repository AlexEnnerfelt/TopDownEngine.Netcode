using MoreMountains.TopDownEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TopDownEngine.Netcode
{
    public class CharacterCrouch_Netcode : CharacterCrouch
    {
        [SerializeField]
        private NetworkVariable<bool> netIsCrouching;

        protected override void Awake() {
            base.Awake();
            netIsCrouching = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
            netIsCrouching.OnValueChanged   += (oldVal, newVal) => {
                if (IsOwner)
                    return;

                if (newVal) {
                    base.Crouch();    
                }
                else {
                    base.ExitCrouch();
                }
            };
        }
        protected override void HandleInput() {
            if (IsOwner) {
                base.HandleInput();
            }
        }
        protected override void Crouch() {
            if (IsOwner) {
                base.Crouch();
                netIsCrouching.Value = true;
            }
        }
        protected override void ExitCrouch() {
            if (IsOwner) {
                base.ExitCrouch();
                netIsCrouching.Value = false;
            }
        }
    }
}
