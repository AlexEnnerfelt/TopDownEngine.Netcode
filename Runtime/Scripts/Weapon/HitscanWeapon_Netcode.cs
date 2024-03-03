using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Profiling;

public class HitscanWeapon_Netcode : HitscanWeapon, INetworkWeapon {


	public NetworkClient netOwner;
	public MMFeedbacks OwnerWeaponStartFeedback;
	public MMFeedbacks OwnerWeaponUsedFeedback;
	public MMFeedbacks OwnerWeaponStopFeedback;

	private WaitForSeconds _delayBetweenUseYieldCommand;
	private WaitForSeconds _initialDelayYieldCommand;
	private Coroutine _useRepeatCoroutine;

	protected virtual void Awake() {

	}

	public void RemoveOwner() {
		if (Owner != null) {
			Owner = null;
			CharacterHandleWeapon = null;
			_characterMovement = null;
			_controller = null;
			_ownerAnimator = null;
		}
	}





	public override void Initialization() {
		_delayBetweenUseYieldCommand = new WaitForSeconds(TimeBetweenUses);
		_initialDelayYieldCommand = new WaitForSeconds(DelayBeforeUse);
		base.Initialization();
	}
	public override void OnGainedOwnership() {
		base.OnGainedOwnership();
		if (IsOwner) {
			netOwner = NetworkManager.LocalClient;
		}
	}
	public override void OnLostOwnership() {
		base.OnLostOwnership();
		netOwner = null;
	}
	public override void WeaponUse() {
		if (IsOwner) {
			base.WeaponUse();
		}
	}
	public override void ApplyOffset() {
		//DON'T MF!!!! THIS COST ME 4 HOURS OF DEBUGGING
	}

	public override void UpdateAnimator() {
		if (IsOwner) {
			base.UpdateAnimator();
		}
	}
	protected override void DetermineDirection() {
		//base.DetermineDirection();
		if (RandomSpread) {
			_randomSpreadDirection.x = Random.Range(-Spread.x, Spread.x);
			_randomSpreadDirection.y = Random.Range(-Spread.y, Spread.y);
			_randomSpreadDirection.z = Random.Range(-Spread.z, Spread.z);
		} else {

			_randomSpreadDirection = Vector3.zero;
		}

		var spread = Quaternion.Euler(_randomSpreadDirection);

		if (Owner != null && Owner.CharacterDimension == Character.CharacterDimensions.Type3D) {
			_randomSpreadDirection = spread * transform.forward;
		} else {
			_randomSpreadDirection = spread * transform.right/* * (Flipped ? -1 : 1)*/;
		}

		if (RotateWeaponOnSpread) {
			this.transform.rotation = this.transform.rotation * spread;
		}
	}
	protected override void TriggerWeaponStartFeedback() {
		base.TriggerWeaponStartFeedback();
		if (IsOwner) {
			OwnerWeaponStartFeedback?.PlayFeedbacks(transform.position);
			if (WeaponStartMMFeedback.HasFeedbacks() || TriggerMode == TriggerModes.Auto) {
				if (IsHost) {
					TriggerWeaponStartFeedback_ClientRpc();
				} else {
					TriggerWeaponStartFeedback_ServerRpc();
				}
			}
		} else {
			if (TriggerMode == TriggerModes.Auto) {
				var isInitial = true;
				_useRepeatCoroutine = StartCoroutine(UseRepeatingCoRoutine(isInitial));
			}
		}
	}
	protected override void TriggerWeaponUsedFeedback() {
		base.TriggerWeaponUsedFeedback();
		if (IsOwner) {
			OwnerWeaponUsedFeedback?.PlayFeedbacks(transform.position);
			if (TriggerMode != TriggerModes.Auto && WeaponUsedMMFeedback.HasFeedbacks()) {
				if (IsHost) {
					TriggerWeaponUsedFeedback_ClientRpc();
				} else {
					TriggerWeaponUsedFeedback_ServerRpc();
				}
			}
		}
	}
	protected override void TriggerWeaponStopFeedback() {
		base.TriggerWeaponStopFeedback();
		if (TriggerMode == TriggerModes.Auto) {
			if (_useRepeatCoroutine != null) {
				StopCoroutine(_useRepeatCoroutine);
			}
		}

		if (IsOwner) {
			OwnerWeaponStopFeedback?.PlayFeedbacks(transform.position);
			if (WeaponStopMMFeedback.HasFeedbacks() || TriggerMode == TriggerModes.Auto) {
				if (IsHost) {
					TriggerWeaponStopFeedback_ClientRpc();
				} else {
					TriggerWeaponStopFeedback_ServerRpc();
				}
			}
		} else {
			if (_useRepeatCoroutine != null) {
				StopCoroutine(_useRepeatCoroutine);
			}
		}
	}
	private IEnumerator UseRepeatingCoRoutine(bool isInitial = false) {
		if (isInitial) {
			yield return _initialDelayYieldCommand;
		} else {
			yield return _delayBetweenUseYieldCommand;
		}
		TriggerWeaponUsedFeedback();
		_useRepeatCoroutine = StartCoroutine(UseRepeatingCoRoutine());
	}

	[ServerRpc]
	private void TriggerWeaponUsedFeedback_ServerRpc() {
		TriggerWeaponUsedFeedback_ClientRpc();
	}
	[ClientRpc]
	private void TriggerWeaponUsedFeedback_ClientRpc() {
		if (!IsOwner) {
			TriggerWeaponUsedFeedback();
		}
	}

	[ServerRpc]
	private void TriggerWeaponStartFeedback_ServerRpc() {
		TriggerWeaponStartFeedback_ClientRpc();
	}
	[ClientRpc]
	private void TriggerWeaponStartFeedback_ClientRpc() {
		if (!IsOwner) {
			TriggerWeaponStartFeedback();
		}
	}

	[ServerRpc]
	private void TriggerWeaponStopFeedback_ServerRpc() {
		TriggerWeaponStopFeedback_ClientRpc();
	}
	[ClientRpc]
	private void TriggerWeaponStopFeedback_ClientRpc() {
		if (!IsOwner) {
			TriggerWeaponStopFeedback();
		}
	}
}


public static class MMF_Extensions {
	public static bool HasFeedbacks(this MMFeedbacks feedbacks) {
		if (feedbacks is MMF_Player player) {
			return player.FeedbacksList.Count > 0;
		} else {
			return feedbacks.Feedbacks.Count > 0;
		}
	}
}

public interface INetworkWeapon {
	void RemoveOwner();
}