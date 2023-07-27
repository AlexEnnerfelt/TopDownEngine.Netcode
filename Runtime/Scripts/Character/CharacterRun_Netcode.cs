using MoreMountains.TopDownEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode
{
    public class CharacterRun_Netcode : CharacterRun
    {
        protected override void HandleInput() {
            if (NetworkObject.IsLocalPlayer) {
                base.HandleInput();
            }
        }

        protected override void InternalHandleInput() {
            if (IsLocalPlayer) {
                base.InternalHandleInput();
            }
        }
    } 
}