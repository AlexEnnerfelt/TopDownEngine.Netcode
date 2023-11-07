using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using Unity.Netcode;

namespace TopDownEngine.Netcode {
	public class NetworkCharacterAbility : CharacterAbility {
		public MMF_Player ownerAbilityStartFeedback;
		public MMF_Player ownerAbilityStopFeedback;

		public override void PlayAbilityStartFeedbacks() {
			if (IsOwner) {
				ownerAbilityStartFeedback?.PlayFeedbacks();
				if (AbilityStartFeedbacks == null) {
					return;
				}
				if (IsServer) {
					PlayAbilityStartFeedbacksClientRpc();
				} else {
					//Notify server
					PlayAbilityStartFeedbacksServerRpc();
				}
			}
			base.PlayAbilityStartFeedbacks();
		}
		public override void PlayAbilityStopFeedbacks() {
			if (IsOwner) {
				ownerAbilityStopFeedback?.PlayFeedbacks();
				if (AbilityStopFeedbacks == null) {
					return;
				}

				if (IsServer) {
					PlayAbilityStopFeedbacksClientRpc();
				} else {
					//Notify server
					PlayAbilityStopFeedbacksServerRpc();
				}
			}
			base.PlayAbilityStopFeedbacks();
		}

		[ServerRpc]
		private void PlayAbilityStartFeedbacksServerRpc() {
			if (!IsOwner) {
				base.PlayAbilityStartFeedbacks();
			}
			PlayAbilityStartFeedbacksClientRpc();
		}
		[ClientRpc]
		private void PlayAbilityStartFeedbacksClientRpc() {
			if (!IsOwner) {
				base.PlayAbilityStartFeedbacks();
			}
		}
		[ServerRpc]
		private void PlayAbilityStopFeedbacksServerRpc() {
			if (!IsOwner) {
				base.PlayAbilityStopFeedbacks();
			}
			PlayAbilityStopFeedbacksClientRpc();
		}
		[ClientRpc]
		private void PlayAbilityStopFeedbacksClientRpc() {
			if (!IsOwner) {
				base.PlayAbilityStopFeedbacks();
			}
		}
	}
}
