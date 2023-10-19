using MoreMountains.TopDownEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode
{
    public class NetworkCharcterJump2D : CharacterJump2D
    {
        protected override void HandleInput() {
            if (IsLocalPlayer) {
                base.HandleInput();
            }
        }
    }
}