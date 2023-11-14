using System.Collections.Generic;
using System.Linq;
using TopDownEngine.Netcode;
using Unity.Netcode;
using UnityEngine;

namespace MoreMountains.TopDownEngine.Netcode {
	public class NetworkHealth : Health {
		protected NetworkVariable<float> netHealth = new();
		protected NetworkVariable<bool> neIsinvulnerable = new(false);
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
				//SetHealth(newValue);
			};
			netIsDead.OnValueChanged += (oldValue, newValue) => {
				Debug.Assert(CurrentHealth == 0 == newValue, $"Health Desync: CurrentHealth: {CurrentHealth}, NetIsDead {newValue}", this);
			};
		}

		#region Damage

		public override void Damage(float damage, GameObject instigator, float flickerDuration, float invincibilityDuration, Vector3 damageDirection, List<TypedDamage> typedDamages = null) {
			if (instigator.TryGetComponentInParent<NetworkObject>(out var networkInstigator)) {
				var networkDamage = new NetworkDamageInfo {
					damage = damage,
					damageDirection = damageDirection,
					typedDamage = new NetworkTypedDamage(typedDamages.First()),
					instigatorId = networkInstigator.NetworkObjectId,
					instigatorClientId = networkInstigator.OwnerClientId
				};
				DamageServerRpc(networkDamage);
			}
		}
		protected virtual void NetworkDamage(NetworkDamageInfo info, out DamageInfo damageInfo) {
			//Get the instigator object
			var instigator = GetInstigatorObject(out var instigatorObject);

			//Get typed damage from serialized values
			var damageType = info.typedDamage.ToReferenceType();
			var typed = new List<TypedDamage> {
				damageType
			};

			//Perform the damage logic
			Damage(info.damage, instigator, info.DamageCausedInvincibilityDuration, info.DamageCausedInvincibilityDuration, info.damageDirection, typed);


			//Build a damage object that has all the information received from the network but converted to managed objects 
			damageInfo = new DamageInfo {
				damage = info.damage,
				DamageCausedInvincibilityDuration = info.DamageCausedInvincibilityDuration,
				damageDirection = info.damageDirection,
				Instigator = instigatorObject,
				typedDamages = typed,
				positionWhenDamaged = transform.position,
			};

			///Function containing all the logic for damage 
			void Damage(float damage, GameObject instigator, float flickerDuration, float invincibilityDuration, Vector3 damageDirection, List<TypedDamage> typedDamages = null) {
				if (!CanTakeDamageThisFrame()) {
					return;
				}

				damage = ComputeDamageOutput(damage, typedDamages, true);

				// we decrease the character's health by the damage
				var previousHealth = CurrentHealth;
				if (MasterHealth != null) {
					previousHealth = MasterHealth.CurrentHealth;
					MasterHealth.SetHealth(MasterHealth.CurrentHealth - damage);
				} else {
					//Call the base set health to avoid sendingg rpcs
					base.SetHealth(CurrentHealth - damage);
				}

				LastDamage = damage;
				LastDamageDirection = damageDirection;
				if (OnHit != null) {
					OnHit();
				}

				// we prevent the character from colliding with Projectiles, Player and Enemies
				if (invincibilityDuration > 0) {
					DamageDisabled();
					StartCoroutine(DamageEnabled(invincibilityDuration));
				}

				// we trigger a damage taken event
				MMDamageTakenEvent.Trigger(this, instigator, CurrentHealth, damage, previousHealth);

				// we update our animator
				if (TargetAnimator != null) {
					TargetAnimator.SetTrigger("Damage");
				}

				// we play our feedback
				if (FeedbackIsProportionalToDamage) {
					DamageMMFeedbacks?.PlayFeedbacks(this.transform.position, damage);
				} else {
					DamageMMFeedbacks?.PlayFeedbacks(this.transform.position);
				}

				// we update the health bar
				UpdateHealthBar(true);

				// we process any condition state change
				ComputeCharacterConditionStateChanges(typedDamages);
				ComputeCharacterMovementMultipliers(typedDamages);

				// if health has reached zero we set its health to zero (useful for the healthbar)
				if (MasterHealth != null) {
					if (MasterHealth.CurrentHealth <= 0) {
						MasterHealth.CurrentHealth = 0;
						MasterHealth.Kill();
					}
				} else {
					if (CurrentHealth <= 0) {
						CurrentHealth = 0;
						Kill();
					}
				}
			}

			GameObject GetInstigatorObject(out NetworkObject networkObject) {
				networkObject = NetworkManager.SpawnManager.SpawnedObjects.GetValueOrDefault(info.instigatorId);
				return networkObject != null ? networkObject.gameObject : null;
			}
		}


		[ServerRpc(RequireOwnership = false)]
		public void DamageServerRpc(NetworkDamageInfo networkDamageInfo) {
			if (IsDead)
				return;

			NetworkDamage(networkDamageInfo, out var damageInfo);

			if (CurrentHealth <= 0) {
				ProcessNetworkKill(damageInfo);
				KillClientRpc(networkDamageInfo, NetworkManager.SendExceptToHost(true));
			} else {
				//Perform the same logic on the client
				DamageClientRpc(networkDamageInfo, NetworkManager.SendExceptToHost(true));
			}
		}
		[ClientRpc]
		public void DamageClientRpc(NetworkDamageInfo info, ClientRpcParams rpcParams) {
			//Exclude host because they have already run this logic in the ServerRpc
			NetworkDamage(info, out var damageInfo);
		}

		#endregion

		[ClientRpc]
		private void KillClientRpc(NetworkDamageInfo p, ClientRpcParams rpcParams) {
			NetworkDamage(p, out var damageObject);
			ProcessNetworkKill(damageObject);
		}



		#region Network Set health 

		public override void SetHealth(float newValue) {
			//Check that the current health is not the same
			if (newValue.Equals(CurrentHealth))
				return;
			//Do the base health set to update bar and current value
			base.SetHealth(newValue);

			if (IsServer) {
				netHealth.Value = newValue;
				SetHealthClientRpc(newValue, NetworkManager.SendExceptToHost(true));
			} else if (IsOwner) {
				SetHealthServerRpc(newValue);
			}
		}
		public override void ReceiveHealth(float health, GameObject instigator) {
			base.ReceiveHealth(health, instigator);
			if (instigator.TryGetComponent<NetworkObject>(out var networkObject)) {

			}
		}

		[ServerRpc]
		private void SetHealthServerRpc(float newValue) {
			var list = NetworkManager.ConnectedClientsIds.ToList();
			list.Remove(OwnerClientId);
			list.Remove(NetworkManager.ServerClientId);
			var send = new ClientRpcParams() {
				Send = new() {
					TargetClientIds = list,
				}
			};
			SetHealthClientRpc(newValue, send);
		}
		[ClientRpc]
		private void SetHealthClientRpc(float newHealth, ClientRpcParams send) {
			base.SetHealth(newHealth);
		}

		#endregion



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