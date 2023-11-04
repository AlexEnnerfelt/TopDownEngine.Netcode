using System.Collections.Generic;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using Unity.Netcode;
using UnityEngine;

namespace TopDownEngine.Netcode {
	[CreateAssetMenu(menuName = "MoreMountains/TopDownEngine/DamageType_Netcode", fileName = "DamageType_Netcode")]
	public class DamageType_Netcode : DamageType {
		[field: SerializeField, MMReadOnly]
		public sbyte UID { get; private set; }

		private static List<DamageType_Netcode> s_DamageTypes;

		private void OnEnable() {
			ValidateDamageTypes();
#if UNITY_EDITOR
			AssignUniqueId();
			UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
		}
#if UNITY_EDITOR
		private void OnValidate() {
			ValidateDamageTypes();
			AssignUniqueId();
		}
		void AssignUniqueId() {
			if (UID == 0) {
				UID = (sbyte)Random.Range(sbyte.MinValue, sbyte.MaxValue);
				UnityEditor.EditorUtility.SetDirty(this);
			}
			foreach (var item in s_DamageTypes) {
				if (item != this && item.UID == UID) {
					while (UID == item.UID) {
						UID = (sbyte)Random.Range(sbyte.MinValue, sbyte.MaxValue);
					}
					UnityEditor.EditorUtility.SetDirty(this);
				}
			}
		}
#endif
		private void OnDisable() {
#if UNITY_EDITOR
			UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
		}
		void ValidateDamageTypes() {
			if (s_DamageTypes == null) {
				s_DamageTypes = new List<DamageType_Netcode>();
			}
			if (!s_DamageTypes.Contains(this)) {
				s_DamageTypes.Add(this);
			}

			for (var i = 0; i < s_DamageTypes.Count; i++) {
				if (s_DamageTypes[i] == null) {
					s_DamageTypes.RemoveAt(i);
				}
			}
		}
		[ContextMenu("Print All DamageTypes ID's")]
		void PrintAllDamageTypesIDs() {
			var message = "";
			foreach (var item in s_DamageTypes) {
				message += $"{item.name} __ UID: {item.UID}\n";

			}
			Debug.Log(message);
		}

		/// <summary>
		/// A way to access all of the available Damagetypes for netcode when recieving their ID 
		/// </summary>
		/// <returns>Damagetype that has the provided ID (should never return null if using an ID recieved from another client)</returns>
		public static DamageType_Netcode GetDamageTypeWithId(sbyte id) {
			foreach (var item in s_DamageTypes) {
				if (item.UID == id) {
					return item;
				}
			}
			Debug.LogError($"The provided ID {id} does not exist! Make sure that the assets are saved before building");
			return null;
		}
		public static DamageType_Netcode GetRandomDamageType() {
			return s_DamageTypes[Random.Range(0, s_DamageTypes.Count)];
		}
	}
	public struct NetworkTypedDamage : INetworkSerializable {
		public NetworkTypedDamage(TypedDamage typedDamage) {
			var net = typedDamage.AssociatedDamageType as DamageType_Netcode;
			associatedDamageType = net.UID;
			minDamageCaused = typedDamage.MinDamageCaused;
			maxDamageCaused = typedDamage.MaxDamageCaused;
			forcedCondition = typedDamage.ForceCharacterCondition ? (byte)typedDamage.ForcedCondition : byte.MaxValue;
			forcedConditionDuration = typedDamage.ForcedConditionDuration;
		}
		public TypedDamage ToReferenceType() {
			var reference = new TypedDamage();
			reference.AssociatedDamageType = DamageType_Netcode.GetDamageTypeWithId(associatedDamageType);
			reference.MinDamageCaused = minDamageCaused;
			reference.MaxDamageCaused = maxDamageCaused;
			reference.ForceCharacterCondition = forcedCondition != byte.MaxValue;
			if (reference.ForceCharacterCondition) {
				reference.ForcedCondition = (CharacterStates.CharacterConditions)forcedCondition;
				reference.ForcedConditionDuration = forcedConditionDuration;
			}
			return reference;
		}
		public sbyte associatedDamageType;
		public float minDamageCaused;
		public float maxDamageCaused;
		public byte forcedCondition;
		public float forcedConditionDuration;

		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
			serializer.SerializeValue(ref associatedDamageType);
			serializer.SerializeValue(ref minDamageCaused);
			serializer.SerializeValue(ref maxDamageCaused);
			serializer.SerializeValue(ref forcedCondition);
			serializer.SerializeValue(ref forcedConditionDuration);
		}
	}
}