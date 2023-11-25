using System.Linq;
using Unity.Netcode;
using UnityEngine;

public static class NetworkManagerExtensions {
	public static NetworkObject GetNetworkObjectByID(this NetworkManager nm, ulong id) {
		var objects = nm.SpawnManager.SpawnedObjectsList;
		foreach (var obj in objects) {
			if (obj.NetworkObjectId == id) {
				return obj;
			}
		}
		return null;
	}
	public static NetworkClient GetClientByID(this NetworkManager nm, ulong id = 0) {
		if (!nm.IsServer) {
			if (id != nm.LocalClientId) {
				Debug.LogError("Only the server can access the full client list. Do this server side instead");
				return null;
			} else {
				return nm.LocalClient;
			}
		}
		foreach (var client in nm.ConnectedClientsList) {
			if (client.ClientId == id) {
				return client;
			}
		}
		Debug.LogError($"Couldn't find a NetworkClient with ID: {id}");
		return null;
	}
	public static bool IsUnconnected(this NetworkObject ob) {
		return ob.NetworkManager == null || !ob.NetworkManager.IsListening;
	}
	public static ClientRpcParams SendExceptToHost(this NetworkManager networkManager, bool excludeSender = false) {
		var connected = networkManager.ConnectedClientsIds.ToList();
		connected.Remove(NetworkManager.ServerClientId);
		if (excludeSender) {
			connected.Remove(networkManager.LocalClientId);
		}
		return new() {
			Send = new() {
				TargetClientIds = connected
			}
		};
	}
	/// <summary>
	/// Looks for the component requested, first in the current ovject, then in parent
	/// </summary>
	public static bool TryGetComponentInParent<T>(this GameObject obj, out T component) where T : Component {
		if (obj.TryGetComponent(out component)) {
			return true;
		} else {
			component = obj.GetComponentInParent<T>();
		}
		return component != null;
	}
}