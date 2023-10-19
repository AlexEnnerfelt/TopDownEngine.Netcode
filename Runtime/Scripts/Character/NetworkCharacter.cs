using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode
{
    public class NetworkCharacter : Character
    {
        public override void RespawnAt(Transform spawnPoint, FacingDirections facingDirection) {
            base.RespawnAt(spawnPoint, facingDirection);
            if (IsOwner) {
                RespawnServerRpc();
            }
        }
        
        protected override void UpdateAnimators() {
            if (IsOwner) {
                base.UpdateAnimators();
            }
        }

        [ServerRpc]
        private void RespawnServerRpc() {
            RespawnClientRpc();
        }
        [ClientRpc]
        private void RespawnClientRpc() {
            if (!IsOwner) {
                RespawnAt(transform, FacingDirections.East);
            }
        }
    } 
}