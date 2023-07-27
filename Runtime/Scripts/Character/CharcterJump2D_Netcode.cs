using MoreMountains.TopDownEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode
{
    public class CharcterJump2D_Netcode : CharacterJump2D
    {
        protected override void HandleInput() {
            if (IsLocalPlayer) {
                base.HandleInput();
            }
        }
    }
}