using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TopDownEngine.Netcode {
	/// <summary>
	/// Used to be able to reference scriptable objects in the project by a unique Id
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface INetworkReferableObject<T> where T : ScriptableObject {
		public ushort Id { get; set; }
		public static Dictionary<ushort, T> objects;

		public static T GetObject(ushort id) {
			objects.TryGetValue(id, out var val);
			return val;
		}
		public static T GetObject() {
			return objects.First().Value;
		}
		public static void TryRegisterValue(T value) {
			objects ??= new();
			//Does not contain value
			if (objects.ContainsValue(value))
				return;

			var val = value as INetworkReferableObject<T>;
			var id = val.Id;
			while (objects.ContainsKey(id)) {
				id = (ushort)Random.Range(ushort.MinValue, ushort.MaxValue);
			}
			objects.Add(id, value);
			val.Id = id;

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(value);
			UnityEditor.AssetDatabase.SaveAssetIfDirty(value);
			if (!UnityEditor.AssetDatabase.IsAssetImportWorkerProcess()) {
				UnityEditor.AssetDatabase.Refresh();
			}
#endif
		}
	}
}
