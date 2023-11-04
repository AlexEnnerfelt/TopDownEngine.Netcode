using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode {
	public class NetworkCharacterMovement : CharacterMovement {
		public NetworkVariable<bool> AbilityPlaying = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

		protected override void Awake() {
			base.Awake();
			if (!IsLocalPlayer) {
				AbilityPlaying.OnValueChanged += (oldVal, newVal) => {
					if (newVal) {
						PlayAbilityStartFeedbacks();
					} else {
						PlayAbilityStopFeedbacks();
					}
				};
			}
		}

		protected override void HandleInput() {
			//Allow only the local player to handle input
			if (IsOwner) {
				base.HandleInput();
			}
		}
		protected override void SetMovement() {
			//Allow only the local player to set movement
			if (IsOwner) {
				base.SetMovement();
			}
		}
		protected override void HandleMovement() {
			//Allow only the local player to handle movement
			if (IsOwner) {
				base.HandleMovement();
			}
		}

		public override void PlayAbilityStartFeedbacks() {
			base.PlayAbilityStartFeedbacks();
			if (IsOwner) {
				AbilityPlaying.Value = true;
			}
		}
		public override void PlayAbilityStopFeedbacks() {
			base.PlayAbilityStopFeedbacks();
			if (IsOwner) {
				AbilityPlaying.Value = false;
			}
		}
	}
}