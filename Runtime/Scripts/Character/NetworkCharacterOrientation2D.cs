using Unity.Netcode;

namespace MoreMountains.TopDownEngine.Netcode {
	public class NetworkCharacterOrientation2D : CharacterOrientation2D {
		public NetworkVariable<bool> n_IsFacingRight = new();

		protected override void Awake() {
			n_IsFacingRight.OnValueChanged += OnFacingChanged;
			base.Awake();
		}
		private void OnFacingChanged(bool previousValue, bool newValue) {
			if (!IsOwner) {
				IsFacingRight = newValue;
			}
		}

		protected override void HandleInput() {
			if (IsLocalPlayer) {
				base.HandleInput();
			}
		}

		public override void FaceDirection(int direction) {
			if (ModelShouldFlip) {
				FlipModel(direction); //This will not work
			}

			if (ModelShouldRotate) {
				RotateModel(direction);
			}
		}
	}
}