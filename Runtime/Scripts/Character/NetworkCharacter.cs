using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode {
	public class NetworkCharacter : Character {
		public override void RespawnAt(Transform spawnPoint, FacingDirections facingDirection) {
			base.RespawnAt(spawnPoint, facingDirection);
			if (IsOwner) {
				RespawnServerRpc((byte)facingDirection);
			}
		}

		protected override void UpdateAnimators() {
			if (IsOwner) {
				base.UpdateAnimators();
			}
		}

		[ServerRpc]
		private void RespawnServerRpc(byte dir) {
			RespawnClientRpc(dir);
		}
		[ClientRpc]
		private void RespawnClientRpc(byte dir) {
			if (!IsOwner) {
				RespawnAt(transform, (FacingDirections)dir);
			}
		}
	}
}