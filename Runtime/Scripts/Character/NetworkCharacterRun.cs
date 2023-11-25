using System.Collections;
using System.Collections.Generic;
using MoreMountains.TopDownEngine;
using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode {
	public class NetworkCharacterRun : CharacterRun {
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