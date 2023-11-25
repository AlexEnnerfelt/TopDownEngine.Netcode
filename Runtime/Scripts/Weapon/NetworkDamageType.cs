using System.Collections.Generic;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using Unity.Netcode;
using UnityEngine;

namespace TopDownEngine.Netcode {
	[CreateAssetMenu(menuName = "MoreMountains/TopDownEngine/DamageType_Netcode", fileName = "DamageType_Netcode")]
	public class NetworkDamageType : DamageType, INetworkReferableObject<NetworkDamageType> {
		[field: SerializeField, MMReadOnly]
		public ushort Id { get; set; }

		public void OnEnable() {
			INetworkReferableObject<NetworkDamageType>.TryRegisterValue(this);
		}
		public void OnValidate() {
			INetworkReferableObject<NetworkDamageType>.TryRegisterValue(this);
		}
	}
	public struct NetworkTypedDamage : INetworkSerializable {
		public NetworkTypedDamage(TypedDamage typedDamage) {
			var net = typedDamage.AssociatedDamageType as NetworkDamageType;
			associatedDamageType = net.Id;
			minDamageCaused = typedDamage.MinDamageCaused;
			maxDamageCaused = typedDamage.MaxDamageCaused;
			forcedCondition = typedDamage.ForceCharacterCondition ? (byte)typedDamage.ForcedCondition : byte.MaxValue;
			forcedConditionDuration = typedDamage.ForcedConditionDuration;
		}
		public TypedDamage ToReferenceType() {
			var reference = new TypedDamage();
			reference.AssociatedDamageType = INetworkReferableObject<NetworkDamageType>.GetObject(associatedDamageType);
			reference.MinDamageCaused = minDamageCaused;
			reference.MaxDamageCaused = maxDamageCaused;
			reference.ForceCharacterCondition = forcedCondition != byte.MaxValue;
			if (reference.ForceCharacterCondition) {
				reference.ForcedCondition = (CharacterStates.CharacterConditions)forcedCondition;
				reference.ForcedConditionDuration = forcedConditionDuration;
			}
			return reference;
		}
		public ushort associatedDamageType;
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