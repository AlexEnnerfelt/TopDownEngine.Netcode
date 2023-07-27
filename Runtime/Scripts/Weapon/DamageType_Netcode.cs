using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDownEngine.Netcode
{
    [CreateAssetMenu(menuName = "MoreMountains/TopDownEngine/DamageType_Netcode", fileName = "DamageType_Netcode")]
    public class DamageType_Netcode : DamageType
    {
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

            for (int i = 0; i < s_DamageTypes.Count; i++) {
                if (s_DamageTypes[i] == null) {
                    s_DamageTypes.RemoveAt(i);
                }
            }
        }
        [ContextMenu("Print All DamageTypes ID's")]
        void PrintAllDamageTypesIDs() {
            string message = "";
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
    }
}