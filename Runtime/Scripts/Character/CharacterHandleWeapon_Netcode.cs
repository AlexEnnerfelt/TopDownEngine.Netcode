using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode
{
    public class CharacterHandleWeapon_Netcode : CharacterHandleWeapon, IWeaponAnchorHandler
    {
        [Space]
        [Header("Netcode")]

        public NetworkVariable<ulong> n_currentWeaponHandleId = new NetworkVariable<ulong>(ulong.MinValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        protected override void InternalHandleInput() {
            if (IsLocalPlayer) {
                base.InternalHandleInput();
            }
        }

        public override void OnNetworkSpawn() {
            base.OnNetworkSpawn();
            //If late joiner
            NetworkObject no = null;
            foreach (var item in NetworkManager.SpawnManager.SpawnedObjectsList) {
                if (item.NetworkObjectId == n_currentWeaponHandleId.Value) {
                    no = item; 
                    break;
                }
            }

            if (no != null) {
                Weapon weapon;
                if (no.TryGetComponent(out weapon)) {
                    CurrentWeapon = weapon;
                }
            }

        }
        public override void ChangeWeapon(Weapon newWeapon, string weaponID = "", bool combo = false) {
            //Turn off current weapon
            // if the character already has a weapon, we make it stop shooting
            if (CurrentWeapon != null) {
                CurrentWeapon.TurnWeaponOff();
                if (_weaponAim != null) {
                    _weaponAim.RemoveReticle();
                    _weaponAim = null;
                }
            }

            if (newWeapon == null) {
                if (CurrentWeapon != null) {
                    INetworkWeapon weapon;
                    if (CurrentWeapon.TryGetComponent(out weapon)) {
                        weapon?.RemoveOwner();
                    }
                    CurrentWeapon = null;
                    _weaponAim = null;
                }
            } else {
                CurrentWeapon = newWeapon;
                CurrentWeapon.SetOwner(_character, this);
                CurrentWeapon.FlipWeapon();
                _weaponAim = CurrentWeapon.gameObject.MMGetComponentNoAlloc<WeaponAim>();

                HandleWeaponAim();
                HandleWeaponIK();
                HandleWeaponModel(newWeapon, weaponID, combo, CurrentWeapon);

                CurrentWeapon.Initialization();
                CurrentWeapon.InitializeComboWeapons();
                CurrentWeapon.InitializeAnimatorParameters();
                InitializeAnimatorParameters();


            }
        }

        public Transform GetAnchor() {
            return WeaponAttachment;
        }

        private void OnDrawGizmos() {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(WeaponAttachment.position, 0.1f);
        }
    }
    public interface IWeaponAnchorHandler
    {
        public Transform GetAnchor();
    }
}