using System.Collections.Generic;
using System.Linq;
using TopDownEngine.Netcode;
using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode {
	public class NetworkHealth : Health {
		protected NetworkVariable<float> netHealth = new();
		protected NetworkVariable<bool> netIsDead = new(false);

		public bool IsDead { get { return netIsDead.Value; } }

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
			netIsDead.OnValueChanged += (oldValue, newValue) => {
				Debug.Assert(CurrentHealth == 0 == newValue, $"Health Desync: CurrentHealth: {CurrentHealth}, NetIsDead {newValue}", this);
			};
		}

		[ClientRpc]
		public void Damage_ClientRpc(NetworkDamageInfo info) {
			//Exclude host because they have already run this logic in the ServerRpc
			if (IsServer)
				return;

			ProcessNetworkDamage(info);
		}
		[ClientRpc]
		private void KillClientRpc(NetworkDamageInfo p) {
			if (IsHost)
				return;
			var damageObject = ProcessNetworkDamage(p);
			ProcessNetworkKill(damageObject);
		}
		[ServerRpc(RequireOwnership = false)]
		public void Damage_ServerRpc(NetworkDamageInfo p) {
			if (netIsDead.Value)
				return;

			var damageObject = ProcessNetworkDamage(p);

			if (CurrentHealth <= 0) {
				ProcessNetworkKill(damageObject);
				KillClientRpc(p);
			} else {
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
		protected virtual DamageInfo ProcessNetworkDamage(NetworkDamageInfo info) {
			//Get the instigator object
			var instigatorObject = NetworkManager.SpawnManager.SpawnedObjects.GetValueOrDefault(info.instigatorId);
			GameObject instigatorGameObject = null;
			if (instigatorObject != null) {
				instigatorGameObject = instigatorObject.gameObject;
			}

			//Get typed damage from serialized values
			var damageType = info.typedDamage.ToReferenceType();
			var typed = new List<TypedDamage> {
				//Use only to identify type, no additional damage
				damageType
			};

			//Perform the damage logic
			//Note! This will call SetHealth that will in turn set the netHealth
			base.Damage(info.damage, instigatorGameObject, info.DamageCausedInvincibilityDuration, info.DamageCausedInvincibilityDuration, info.damageDirection, typed);


			//Build a damage object that has all the information received from the network but converted to managed objects 
			var damageObject = new DamageInfo {
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
		public virtual void ProcessNetworkKill(DamageInfo killingBlow) {
			if (IsServer) {
				netIsDead.Value = true;
			}
		}
		public override void Revive() {
			if (IsServer) {
				netIsDead.Value = false;
			}
			base.Revive();
		}

		public void Damage(float damage, NetworkObject instigator, float flickerDuration, float invincibilityDuration, Vector3 damageDirection, List<TypedDamage> typedDamages = null) {
			//TypedDamage type;
			//if (typedDamages.Count > 0) {
			//	type = typedDamages[0];
			//	if (type.AssociatedDamageType is DamageType_Netcode dmgNet) {
			//		_ = dmgNet.UID;
			//	}
			//}

			var networkDamage = new NetworkDamageInfo {
				damage = damage,
				damageDirection = damageDirection,
				typedDamage = new NetworkTypedDamage(typedDamages.First()),
				instigatorId = instigator.NetworkObjectId,
				instigatorClientId = instigator.OwnerClientId
			};

			Damage_ServerRpc(networkDamage);
		}
	}

	public struct DamageInfo {
		public List<TypedDamage> typedDamages;
		public float damage;
		public Vector3 damageDirection;
		public float DamageCausedInvincibilityDuration;
		public NetworkObject Instigator;
		public Vector3 positionWhenDamaged;
	}
	public struct NetworkDamageInfo : INetworkSerializable {
		public float damage;
		public Vector3 damageDirection;
		public float DamageCausedInvincibilityDuration;
		public NetworkTypedDamage typedDamage;
		public ulong instigatorId;
		public ulong instigatorClientId;

		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
			serializer.SerializeValue(ref damage);
			serializer.SerializeValue(ref damageDirection);
			serializer.SerializeValue(ref DamageCausedInvincibilityDuration);
			serializer.SerializeValue(ref typedDamage);
			serializer.SerializeValue(ref instigatorId);
			serializer.SerializeValue(ref instigatorClientId);
		}
	}
}