using MoreMountains.Feedbacks;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode
{
    public class MeleeWeapon_Netcode : MeleeWeapon, INetworkWeapon
    {
        public MMFeedbacks OwnerWeaponStartFeedback;
        public MMFeedbacks OwnerWeaponUsedFeedback;
        public MMFeedbacks OwnerWeaponStopFeedback;

        private WaitForSeconds _delayBetweenUseYieldCommand;
        private WaitForSeconds _initialDelayYieldCommand;
        private Coroutine _useRepeatCoroutine;

        public void RemoveOwner() {
            if (Owner != null) {
                Owner = null;
                CharacterHandleWeapon = null;
                _characterMovement = null;
                _controller = null;
                _ownerAnimator = null;
            }
        }
        public override void WeaponUse() {
            if (IsOwner) {
                base.WeaponUse();
            }
        }
        public override void ApplyOffset() {
            //DON'T MF!!!! THIS COST ME 4 HOURS OF DEBUGGING
        }

        protected override void TriggerWeaponStartFeedback() {
            base.TriggerWeaponStartFeedback();
            if (IsOwner) {
                OwnerWeaponStartFeedback?.PlayFeedbacks(transform.position);
                if (WeaponStartMMFeedback.HasFeedbacks()) {
                    if (IsHost) {
                        TriggerWeaponStartFeedback_ClientRpc();
                    }
                    else {
                        TriggerWeaponStartFeedback_ServerRpc();
                    }
                }
            }
            else {
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
                    }
                    else {
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
                if (WeaponStopMMFeedback.HasFeedbacks()) {
                    if (IsHost) {
                        TriggerWeaponStopFeedback_ClientRpc();
                    }
                    else {
                        TriggerWeaponStopFeedback_ServerRpc();
                    }
                }
            }
            else {
                if (_useRepeatCoroutine != null) {
                    StopCoroutine(_useRepeatCoroutine);
                }
            }
        }
        private IEnumerator UseRepeatingCoRoutine(bool isInitial = false) {
            if (isInitial) {
                yield return _initialDelayYieldCommand;
            }
            else {
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
}