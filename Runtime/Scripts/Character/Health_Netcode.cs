using System.Collections.Generic;
using TopDownEngine.Netcode;
using Unity.Netcode;
using UnityEngine;
using static MoreMountains.TopDownEngine.DamageOnTouch;

namespace MoreMountains.TopDownEngine.Netcode
{
    public class Health_Netcode : Health
    {
        public NetworkVariable<float> netHealth = new();
        public NetworkVariable<bool> netIsdead = new(false);

        public bool IsDead { get { return netIsdead.Value; } }

        public override void OnNetworkSpawn() {
            base.OnNetworkSpawn();
            if (IsServer) {
                netHealth.Value = InitialHealth;
            }
            CurrentHealth = netHealth.Value;
            if (IsDead) {
                Kill();
            }
        }
        public override void Initialization() {
            base.Initialization();

            netHealth.OnValueChanged += (oldValue, newValue) => {
                Debug.Assert(Mathf.RoundToInt(CurrentHealth) == Mathf.RoundToInt(newValue), $"Health Desync: CurrentHealth: {CurrentHealth}, NetHealth old:{oldValue} new:{newValue}", this); 
            };
            netIsdead.OnValueChanged += (oldValue, newValue) => {
                Debug.Assert((CurrentHealth == 0) == newValue, $"Health Desync: CurrentHealth: {CurrentHealth}, NetIsDead {newValue}", this);
            };
        }

        [ClientRpc]
        public void Damage_ClientRpc(DamageData_Net info) {
            //Exclude host because they have already run this logic in the ServerRpc
            if (IsServer)
                return;

            ProcessNetworkDamage(info);
        }
        [ClientRpc]
        private void KillClientRpc(DamageData_Net p) {
            if (IsHost)
                return;
            var damageObject = ProcessNetworkDamage(p);
            ProcessNetworkKill(damageObject);
        }
        [ServerRpc(RequireOwnership = false)]
        public void Damage_ServerRpc(DamageData_Net p) {
            if (netIsdead.Value) return;

            var damageObject = ProcessNetworkDamage(p);

            if (CurrentHealth <=0) {
                ProcessNetworkKill(damageObject);
                KillClientRpc(p);
            }
            else {
                //Perform the same logic on the client
                Damage_ClientRpc(p);
            }
        }
        [ServerRpc]
        private void SetHealthServerRpc(float newHealth) {
            CurrentHealth = newHealth;
            netHealth.Value = newHealth;
        }


        /// <summary>
        /// Handles deserializing values and grabbing refeerences to types and applying the damage
        /// This gets called on both the client and the Server 
        /// </summary>
        /// <param name="info">The networkserializable object containing all the information about the damage</param>
        /// <returns>The serialized data but deserialized into managed objects</returns>
        protected virtual DamageData ProcessNetworkDamage(DamageData_Net info) {
            //Get the instigator object
            var instigatorObject = NetworkManager.SpawnManager.SpawnedObjects.GetValueOrDefault(info.instigatorId);
            var instigatorGameObject = instigatorObject.gameObject;

            //Get typed damage from serialized values
            var damageType = DamageType_Netcode.GetDamageTypeWithId(info.damageType);
            var typed = new List<TypedDamage>();
            //Use only to identify type, no additional damage
            var t = new TypedDamage {
                AssociatedDamageType = damageType,
                MinDamageCaused = 0,
                MaxDamageCaused = 0,
            };
            typed.Add(t);

            //Perform the damage logic
            //Note! This will call SetHealth that will in turn set the netHealth
            //var deg = Mathf.Atan2(info.damageDirection.x, info.damageDirection.y) * Mathf.Rad2Deg;
            //DamageMMFeedbacks.transform.rotation = Quaternion.AngleAxis(deg, new Vector3(0,0,1));
            base.Damage(info.damage, instigatorGameObject, info.DamageCausedInvincibilityDuration, info.DamageCausedInvincibilityDuration, info.damageDirection, typed);


            //Build a damage object that has all the information recieved from the network but converted to managed objects 
            var damageObject = new DamageData {
                damage = info.damage,
                DamageCausedInvincibilityDuration = info.DamageCausedInvincibilityDuration,
                damageDirection = info.damageDirection,
                Instigator = instigatorObject,
                typedDamages = typed,
                positionWhenDamaged = transform.position,
            };
            return damageObject;
        }
        public override void SetHealth(float newValue) {
            base.SetHealth(newValue);
            if (IsServer) {
                netHealth.Value = newValue;
            }
        }
        public override void ResetHealthToMaxHealth() {
            base.ResetHealthToMaxHealth();
            if (IsOwner) {
                SetHealthServerRpc(MaximumHealth);
            }
        }
        protected virtual void ProcessNetworkKill(DamageData killingBlow) {
            if (IsServer) {
                netIsdead.Value = true;
            }
        }
        public override void Revive() {
            if (IsServer) {
                netIsdead.Value = false;
            }
            base.Revive();
        }

        public void Damage(float damage, NetworkObject instigator, float flickerDuration, float invincibilityDuration, Vector3 damageDirection, List<TypedDamage> typedDamages = null) {
            sbyte damageTypeId = 0;
            TypedDamage type;
            if (typedDamages.Count > 0) { 
                type = typedDamages[0];
                if (type.AssociatedDamageType is DamageType_Netcode dmgNet) {
                    damageTypeId = dmgNet.UID;
                }
            }

            var networkDamage = new DamageData_Net {
                damage = damage,
                damageDirection = damageDirection,
                damageType = damageTypeId,
                instigatorId = instigator.NetworkObjectId,    
                instigatorClient = instigator.OwnerClientId
            };

            Damage_ServerRpc(networkDamage);
        }
    }

    public struct DamageData
    {
        public List<TypedDamage> typedDamages;
        public float damage;
        public Vector3 damageDirection;
        public float DamageCausedInvincibilityDuration;
        public NetworkObject Instigator;
        public Vector3 positionWhenDamaged;
    }
    public struct DamageData_Net : INetworkSerializable
    {
        public float damage;
        public Vector3 damageDirection;
        public float DamageCausedInvincibilityDuration;
        public sbyte damageType;
        public ulong instigatorId;
        public ulong instigatorClient;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref damageDirection);
            serializer.SerializeValue(ref DamageCausedInvincibilityDuration);
            serializer.SerializeValue(ref damageType);
            serializer.SerializeValue(ref instigatorId);
            serializer.SerializeValue(ref instigatorClient);
        }
    }
}