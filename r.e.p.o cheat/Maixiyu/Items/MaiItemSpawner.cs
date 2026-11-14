using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat;

public class MaiItemSpawner : MonoBehaviourPunCallbacks
{
	private class ExcludeFromMapTracking : MonoBehaviour
	{
		private void Start()
		{
			((Component)this).gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
		}
	}

	private static Dictionary<string, GameObject> itemPrefabCache = new Dictionary<string, GameObject>();

	private static List<string> availableItems = new List<string>();

	public static List<string> GetAvailableItems()
	{
		List<string> fromGame = NativeGameApi.GetItemNames();
		if (fromGame.Count > 0)
		{
			availableItems = fromGame;
			return availableItems;
		}
		if (availableItems.Count == 0)
		{
			availableItems.Add("Item Cart Medium");
			availableItems.Add("Item Cart Small");
			availableItems.Add("Item Drone Battery");
			availableItems.Add("Item Drone Feather");
			availableItems.Add("Item Drone Indestructible");
			availableItems.Add("Item Drone Torque");
			availableItems.Add("Item Drone Zero Gravity");
			availableItems.Add("Item Extraction Tracker");
			availableItems.Add("Item Grenade Duct Taped");
			availableItems.Add("Item Grenade Explosive");
			availableItems.Add("Item Grenade Human");
			availableItems.Add("Item Grenade Shockwave");
			availableItems.Add("Item Grenade Stun");
			availableItems.Add("Item Gun Handgun");
			availableItems.Add("Item Gun Shotgun");
			availableItems.Add("Item Gun Tranq");
			availableItems.Add("Item Health Pack Large");
			availableItems.Add("Item Health Pack Medium");
			availableItems.Add("Item Health Pack Small");
			availableItems.Add("Item Melee Baseball Bat");
			availableItems.Add("Item Melee Frying Pan");
			availableItems.Add("Item Melee Inflatable Hammer");
			availableItems.Add("Item Melee Sledge Hammer");
			availableItems.Add("Item Melee Sword");
			availableItems.Add("Item Mine Explosive");
			availableItems.Add("Item Mine Shockwave");
			availableItems.Add("Item Mine Stun");
			availableItems.Add("Item Orb Zero Gravity");
			availableItems.Add("Item Power Crystal");
			availableItems.Add("Item Rubber Duck");
			availableItems.Add("Item Upgrade Map Player Count");
			availableItems.Add("Item Upgrade Player Energy");
			availableItems.Add("Item Upgrade Player Extra Jump");
			availableItems.Add("Item Upgrade Player Grab Range");
			availableItems.Add("Item Upgrade Player Grab Strength");
			availableItems.Add("Item Upgrade Player Health");
			availableItems.Add("Item Upgrade Player Sprint Speed");
			availableItems.Add("Item Upgrade Player Tumble Launch");
			availableItems.Add("Item Valuable Tracker");
			availableItems.Add("Valuable Small");
			availableItems.Add("Valuable Medium");
			availableItems.Add("Valuable Large");
		}
		return availableItems;
	}

	public static void SpawnMoney(Vector3 position, int value = 45000)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			SpawnItem("Valuable Small", position, value);
		}
		catch (Exception)
		{
			try
			{
				CreateMoneyDirectly(position, value);
			}
			catch (Exception)
			{
			}
		}
	}

	private static void CreateMoneyDirectly(Vector3 position, int value)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		bool num = SemiFunc.IsMultiplayer();
		GameObject val = new GameObject("Valuable_Spawned");
		val.transform.position = position;
		Rigidbody obj = val.AddComponent<Rigidbody>();
		obj.mass = 1f;
		obj.drag = 0.1f;
		obj.angularDrag = 0.05f;
		obj.useGravity = true;
		obj.isKinematic = false;
		obj.interpolation = (RigidbodyInterpolation)1;
		obj.collisionDetectionMode = (CollisionDetectionMode)1;
		BoxCollider obj2 = val.AddComponent<BoxCollider>();
		obj2.size = new Vector3(0.2f, 0.2f, 0.2f);
		obj2.center = Vector3.zero;
		val.AddComponent(Type.GetType("PhysGrabObject, Assembly-CSharp"));
		Component val2 = val.AddComponent(Type.GetType("ValuableObject, Assembly-CSharp"));
		if ((Object)(object)val2 != (Object)null)
		{
			FieldInfo fieldInfo = ((object)val2).GetType().GetField("dollarValueCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? ((object)val2).GetType().GetField("dollarValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo field = ((object)val2).GetType().GetField("dollarValueOriginal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo field2 = ((object)val2).GetType().GetField("dollarValueSet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (fieldInfo != null)
			{
				fieldInfo.SetValue(val2, (float)value);
			}
			if (field != null)
			{
				field.SetValue(val2, (float)value);
			}
			if (field2 != null)
			{
				field2.SetValue(val2, true);
			}
		}
		GameObject obj3 = GameObject.CreatePrimitive((PrimitiveType)3);
		obj3.transform.SetParent(val.transform, false);
		obj3.transform.localPosition = Vector3.zero;
		obj3.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
		Renderer component = obj3.GetComponent<Renderer>();
		if ((Object)(object)component != (Object)null)
		{
			component.material.color = new Color(1f, 0.84f, 0f);
			component.material.SetFloat("_Metallic", 1f);
			component.material.SetFloat("_Glossiness", 0.8f);
		}
		if (num)
		{
			ConfigureSyncComponents(val);
		}
	}

	public static void SpawnItem(string itemName, Vector3 position, int value = 0)
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (value <= 0 && NativeGameApi.SpawnItemNative(itemName, position))
			{
				return;
			}
			bool flag = SemiFunc.IsMultiplayer();
			if (flag && !PhotonNetwork.IsMasterClient)
			{
				SpawnItemNonHost(itemName, position, value);
				return;
			}
			GameObject itemPrefab = GetItemPrefab(itemName);
			if ((Object)(object)itemPrefab == (Object)null && itemName.Contains("Valuable"))
			{
				if (CreateValuableDirectly(itemName, position, value))
				{
					return;
				}
			}
			else if ((Object)(object)itemPrefab == (Object)null)
			{
				return;
			}
			object[] array = null;
			if (itemName.Contains("Valuable") && value > 0)
			{
				array = new object[1] { value };
			}
			GameObject val;
			if (!flag)
			{
				val = Object.Instantiate<GameObject>(itemPrefab, position, Quaternion.identity);
				ConfigureSyncComponents(val);
				EnsureItemVisibility(val);
			}
			else
			{
				string prefabPath = GetPrefabPath(itemName);
				if (string.IsNullOrEmpty(prefabPath))
				{
					return;
				}
				try
				{
					val = PhotonNetwork.InstantiateRoomObject(prefabPath, position, Quaternion.identity, (byte)0, array);
				}
				catch (Exception)
				{
					return;
				}
			}
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			if (itemName.Contains("Valuable") && value > 0)
			{
				ConfigureValuableObject(val, value, flag);
			}
			ConfigurePhysicsProperties(val, position, flag);
		}
		catch (Exception)
		{
			if (itemName.Contains("Valuable"))
			{
				CreateValuableDirectly(itemName, position, value);
			}
		}
	}

	private static void SpawnItemNonHost(string itemName, Vector3 position, int value)
	{
		try
		{
			string prefabPath = GetPrefabPath(itemName);
			if (string.IsNullOrEmpty(prefabPath))
			{
				return;
			}
			GameObject val = PhotonNetwork.Instantiate(prefabPath, position, Quaternion.identity, 0);
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			if (itemName.Contains("Valuable") && value > 0)
			{
				ConfigureValuableObject(val, value, isMultiplayer: true);
			}
		}
		catch (Exception)
		{
		}
	}

	private static IEnumerator SetupItemAndNotifyHost(GameObject item, GameObject prefabResource, string itemName, Vector3 position, int value)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		yield return (object)new WaitForEndOfFrame();
		try
		{
			Renderer[] componentsInChildren = item.GetComponentsInChildren<Renderer>(true);
			foreach (Renderer val in componentsInChildren)
			{
				string pathRelativeToRoot = GetPathRelativeToRoot(((Component)val).transform);
				Transform val2 = prefabResource.transform;
				string[] array = pathRelativeToRoot.Split('/');
				foreach (string text in array)
				{
					if (!string.IsNullOrEmpty(text))
					{
						val2 = val2.Find(text);
						if ((Object)(object)val2 == (Object)null)
						{
							break;
						}
					}
				}
				if (!((Object)(object)val2 != (Object)null))
				{
					continue;
				}
				Renderer component = ((Component)val2).GetComponent<Renderer>();
				if (!((Object)(object)component != (Object)null) || component.sharedMaterials.Length == 0)
				{
					continue;
				}
				Material[] array2 = (Material[])(object)new Material[component.sharedMaterials.Length];
				for (int k = 0; k < component.sharedMaterials.Length; k++)
				{
					if ((Object)(object)component.sharedMaterials[k] != (Object)null)
					{
						array2[k] = new Material(component.sharedMaterials[k]);
					}
				}
				val.materials = array2;
			}
			item.AddComponent<MaterialPreserver>().PreserveMaterials();
			if (itemName.Contains("Valuable") && value > 0)
			{
				Component component2 = item.GetComponent(Type.GetType("ValuableObject, Assembly-CSharp"));
				if ((Object)(object)component2 != (Object)null)
				{
					ApplyDollarValue(component2, value);
				}
			}
			Component component3 = item.GetComponent(Type.GetType("PhysGrabObject, Assembly-CSharp"));
			if ((Object)(object)component3 != (Object)null)
			{
				SetFieldValue(component3, "spawnTorque", Random.insideUnitSphere * 0.05f);
			}
		}
		catch (Exception)
		{
		}
		yield return (object)new WaitForSeconds(0.1f);
		try
		{
			PlayerCheatSync playerCheatSync = FindOrCreateCheatSync();
			if ((Object)(object)playerCheatSync != (Object)null && (Object)(object)playerCheatSync.photonView != (Object)null)
			{
				playerCheatSync.photonView.RPC("SpawnItemMirrorRPC", (RpcTarget)2, new object[4]
				{
					itemName,
					position,
					value,
					PhotonNetwork.LocalPlayer.ActorNumber
				});
			}
		}
		catch (Exception)
		{
		}
	}

	private static IEnumerator ApplyMaterialsWithDelay(GameObject item, Dictionary<string, Material> materials, string itemName, int value)
	{
		yield return (object)new WaitForEndOfFrame();
		yield return (object)new WaitForEndOfFrame();
		try
		{
			Renderer[] componentsInChildren = item.GetComponentsInChildren<Renderer>(true);
			foreach (Renderer val in componentsInChildren)
			{
				if (!((Object)(object)val != (Object)null))
				{
					continue;
				}
				List<Material> list = new List<Material>();
				Material[] sharedMaterials = val.sharedMaterials;
				foreach (Material val2 in sharedMaterials)
				{
					if ((Object)(object)val2 != (Object)null && materials.ContainsKey(((Object)val2).name))
					{
						list.Add(materials[((Object)val2).name]);
						continue;
					}
					string text = null;
					foreach (string key in materials.Keys)
					{
						if (((Object)(object)val2 != (Object)null && ((Object)val2).name.Contains(key)) || key.Contains(((Object)(object)val2 != (Object)null) ? ((Object)val2).name : "default"))
						{
							text = key;
							break;
						}
					}
					if (text != null)
					{
						list.Add(materials[text]);
					}
					else
					{
						list.Add((Material)(((Object)(object)val2 != (Object)null) ? ((object)val2) : ((object)new Material(Shader.Find("Standard")))));
					}
				}
				if (list.Count > 0)
				{
					val.materials = list.ToArray();
				}
				val.enabled = false;
				val.enabled = true;
			}
			if (itemName.Contains("Valuable") && value > 0)
			{
				Component component = item.GetComponent(Type.GetType("ValuableObject, Assembly-CSharp"));
				if ((Object)(object)component != (Object)null)
				{
					ApplyDollarValue(component, value);
				}
			}
			Component component2 = item.GetComponent(Type.GetType("PhysGrabObject, Assembly-CSharp"));
			if ((Object)(object)component2 != (Object)null)
			{
				SetFieldValue(component2, "spawnTorque", Random.insideUnitSphere * 0.05f);
			}
		}
		catch (Exception)
		{
		}
	}

	private static void CopyRenderersFromPrefab(GameObject target, GameObject prefab)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Expected O, but got Unknown
		if ((Object)(object)target == (Object)null || (Object)(object)prefab == (Object)null)
		{
			return;
		}
		Renderer[] componentsInChildren = target.GetComponentsInChildren<Renderer>(true);
		if (componentsInChildren != null && componentsInChildren.Length != 0)
		{
			return;
		}
		Renderer[] componentsInChildren2 = prefab.GetComponentsInChildren<Renderer>(true);
		foreach (Renderer val in componentsInChildren2)
		{
			Transform val2 = FindCorrespondingChild(target.transform, ((Component)val).transform);
			if ((Object)(object)val2 == (Object)null)
			{
				val2 = new GameObject(((Object)val).name).transform;
				val2.SetParent(target.transform);
				val2.localPosition = ((Component)val).transform.localPosition;
				val2.localRotation = ((Component)val).transform.localRotation;
				val2.localScale = ((Component)val).transform.localScale;
			}
			Renderer val3 = ((Component)val2).GetComponent<Renderer>();
			if ((Object)(object)val3 == (Object)null)
			{
				if (val is MeshRenderer)
				{
					val3 = (Renderer)(object)((Component)val2).gameObject.AddComponent<MeshRenderer>();
					MeshFilter component = ((Component)val).GetComponent<MeshFilter>();
					if ((Object)(object)component != (Object)null && (Object)(object)component.sharedMesh != (Object)null)
					{
						MeshFilter val4 = ((Component)val2).GetComponent<MeshFilter>();
						if ((Object)(object)val4 == (Object)null)
						{
							val4 = ((Component)val2).gameObject.AddComponent<MeshFilter>();
						}
						val4.sharedMesh = component.sharedMesh;
					}
				}
				else if (val is SkinnedMeshRenderer)
				{
					SkinnedMeshRenderer val5 = ((Component)val2).gameObject.AddComponent<SkinnedMeshRenderer>();
					SkinnedMeshRenderer val6 = (SkinnedMeshRenderer)(object)((val is SkinnedMeshRenderer) ? val : null);
					if ((Object)(object)val6.sharedMesh != (Object)null)
					{
						val5.sharedMesh = val6.sharedMesh;
					}
				}
			}
			if (!((Object)(object)val3 != (Object)null) || val.sharedMaterials.Length == 0)
			{
				continue;
			}
			Material[] array = (Material[])(object)new Material[val.sharedMaterials.Length];
			for (int j = 0; j < val.sharedMaterials.Length; j++)
			{
				if (!((Object)(object)val.sharedMaterials[j] != (Object)null))
				{
					continue;
				}
				array[j] = new Material(val.sharedMaterials[j]);
				string[] texturePropertyNames = array[j].GetTexturePropertyNames();
				foreach (string text in texturePropertyNames)
				{
					Texture texture = array[j].GetTexture(text);
					if ((Object)(object)texture != (Object)null)
					{
						array[j].SetTexture(text, texture);
					}
				}
			}
			val3.materials = array;
			val3.sharedMaterials = val.sharedMaterials;
		}
	}

	private static Transform FindCorrespondingChild(Transform parent, Transform prefabChild)
	{
		Transform val = parent.Find(((Object)prefabChild).name);
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		string[] array = GetPathRelativeToRoot(prefabChild).Split('/');
		Transform val2 = parent;
		for (int i = 0; i < array.Length; i++)
		{
			Transform val3 = val2.Find(array[i]);
			if ((Object)(object)val3 == (Object)null)
			{
				return null;
			}
			val2 = val3;
		}
		return val2;
	}

	private static string GetPathRelativeToRoot(Transform transform)
	{
		List<string> list = new List<string>();
		Transform val = transform;
		while ((Object)(object)val.parent != (Object)null)
		{
			list.Add(((Object)val).name);
			val = val.parent;
		}
		list.Reverse();
		return string.Join("/", list);
	}

	private static void SetFieldValue(object obj, string fieldName, object value)
	{
		FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (field == null && fieldName == "dollarValueCurrent")
		{
			field = obj.GetType().GetField("dollarValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		if (field != null)
		{
			field.SetValue(obj, value);
		}
	}

	private static IEnumerator FinalizeItemSpawn(GameObject spawnedItem, GameObject prefab, string itemName, int value)
	{
		yield return (object)new WaitForEndOfFrame();
		CopyRenderersFromPrefab(spawnedItem, prefab);
		if (itemName.Contains("Valuable") && value > 0)
		{
			Component component = spawnedItem.GetComponent(Type.GetType("ValuableObject, Assembly-CSharp"));
			if ((Object)(object)component != (Object)null)
			{
				ApplyDollarValue(component, value);
			}
		}
		Renderer[] componentsInChildren = spawnedItem.GetComponentsInChildren<Renderer>(true);
		foreach (Renderer val in componentsInChildren)
		{
			if ((Object)(object)val != (Object)null && val.materials.Length != 0)
			{
				Material[] materials = val.materials;
				val.materials = materials;
			}
		}
		EnsureItemVisibility(spawnedItem);
	}

	private static PlayerCheatSync FindOrCreateCheatSync()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		PlayerCheatSync playerCheatSync = Object.FindObjectOfType<PlayerCheatSync>();
		if ((Object)(object)playerCheatSync == (Object)null)
		{
			GameObject val = new GameObject("CheatSync");
			playerCheatSync = val.AddComponent<PlayerCheatSync>();
			Object.DontDestroyOnLoad((Object)val);
		}
		return playerCheatSync;
	}

	private static bool CreateValuableDirectly(string itemName, Vector3 position, int value)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			float num = 1f;
			if (itemName.Contains("Medium"))
			{
				num = 1.5f;
			}
			else if (itemName.Contains("Large"))
			{
				num = 2f;
			}
			GameObject val = new GameObject(itemName + "_Spawned");
			val.transform.position = position;
			Rigidbody obj = val.AddComponent<Rigidbody>();
			obj.mass = 1f * num;
			obj.drag = 0.1f;
			obj.angularDrag = 0.05f;
			obj.useGravity = true;
			obj.isKinematic = false;
			obj.interpolation = (RigidbodyInterpolation)1;
			obj.collisionDetectionMode = (CollisionDetectionMode)1;
			BoxCollider obj2 = val.AddComponent<BoxCollider>();
			obj2.size = new Vector3(0.2f, 0.2f, 0.2f) * num;
			obj2.center = Vector3.zero;
			Type type = Type.GetType("PhysGrabObject, Assembly-CSharp");
			if (type != null)
			{
				Component obj3 = val.AddComponent(type);
				FieldInfo field = type.GetField("midPoint", BindingFlags.Instance | BindingFlags.Public);
				if (field != null)
				{
					field.SetValue(obj3, position);
				}
				FieldInfo field2 = type.GetField("targetPosition", BindingFlags.Instance | BindingFlags.Public);
				if (field2 != null)
				{
					field2.SetValue(obj3, position);
				}
			}
			Type type2 = Type.GetType("ValuableObject, Assembly-CSharp");
			if (type2 != null)
			{
				Component obj4 = val.AddComponent(type2);
				FieldInfo fieldInfo = type2.GetField("dollarValueCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? type2.GetField("dollarValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				FieldInfo field3 = type2.GetField("dollarValueOriginal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				FieldInfo field4 = type2.GetField("dollarValueSet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (fieldInfo != null)
				{
					fieldInfo.SetValue(obj4, (float)value);
				}
				if (field3 != null)
				{
					field3.SetValue(obj4, (float)value);
				}
				if (field4 != null)
				{
					field4.SetValue(obj4, true);
				}
				ApplyDollarValue(obj4, value);
			}
			GameObject obj5 = GameObject.CreatePrimitive((PrimitiveType)3);
			obj5.transform.SetParent(val.transform, false);
			obj5.transform.localPosition = Vector3.zero;
			obj5.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f) * num;
			Renderer component = obj5.GetComponent<Renderer>();
			if ((Object)(object)component != (Object)null)
			{
				component.material.color = new Color(1f, 0.84f, 0f);
				component.material.SetFloat("_Metallic", 1f);
				component.material.SetFloat("_Glossiness", 0.8f);
			}
			if (SemiFunc.IsMultiplayer())
			{
				ConfigureSyncComponents(val);
			}
			val.tag = "SpawnedValuable";
			if (typeof(Map).GetField("Instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) != null)
			{
				val.AddComponent<ExcludeFromMapTracking>();
			}
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static string GetPrefabPath(string itemName)
	{
		if (itemName.Contains("Valuable"))
		{
			return "Valuables/" + itemName;
		}
		itemName.StartsWith("Item ");
		return "Items/" + itemName;
	}

	private static GameObject GetItemPrefab(string itemName)
	{
		if (itemPrefabCache.ContainsKey(itemName) && (Object)(object)itemPrefabCache[itemName] != (Object)null)
		{
			return itemPrefabCache[itemName];
		}
		GameObject val = null;
		if (itemName.Contains("Valuable"))
		{
			if (itemName.Contains("Valuable"))
			{
				if (itemName == "Valuable Small" && (Object)(object)AssetManager.instance.surplusValuableSmall != (Object)null)
				{
					val = AssetManager.instance.surplusValuableSmall;
				}
				else
				{
					string text = null;
					switch (itemName)
					{
					case "Valuable Small":
						text = "surplusValuableSmall";
						break;
					case "Valuable Medium":
						text = "surplusValuableMedium";
						break;
					case "Valuable Large":
						text = "surplusValuableLarge";
						break;
					}
					if (text != null && (Object)(object)AssetManager.instance != (Object)null)
					{
						FieldInfo field = ((object)AssetManager.instance).GetType().GetField(text, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (field != null)
						{
							object value = field.GetValue(AssetManager.instance);
							val = (GameObject)((value is GameObject) ? value : null);
						}
					}
				}
				if ((Object)(object)val == (Object)null)
				{
					Object[] array = Object.FindObjectsOfType(Type.GetType("ValuableObject, Assembly-CSharp"));
					if (array != null && array.Length != 0)
					{
						Object[] array2 = array;
						foreach (Object obj in array2)
						{
							Object obj2 = ((obj is MonoBehaviour) ? obj : null);
							GameObject val2 = ((obj2 != null) ? ((Component)obj2).gameObject : null);
							if ((Object)(object)val2 != (Object)null)
							{
								val = val2;
								break;
							}
						}
					}
				}
				if ((Object)(object)val == (Object)null)
				{
					string[] array3 = new string[6] { "Valuables/Valuable", "Valuables/ValuableSmall", "Valuables/ValuableMedium", "Valuables/ValuableLarge", "Prefabs/Valuable", "Prefabs/ValuableSmall" };
					for (int i = 0; i < array3.Length; i++)
					{
						val = Resources.Load<GameObject>(array3[i]);
						if ((Object)(object)val != (Object)null)
						{
							break;
						}
					}
				}
			}
			else
			{
				val = Resources.Load<GameObject>("Valuables/" + itemName);
			}
		}
		else
		{
			StatsManager instance = StatsManager.instance;
			if ((Object)(object)instance != (Object)null)
			{
				FieldInfo field2 = ((object)instance).GetType().GetField("itemDictionary", BindingFlags.Instance | BindingFlags.Public);
				if (field2 != null && field2.GetValue(instance) is Dictionary<string, Item> dictionary && dictionary.ContainsKey(itemName))
				{
					Item val3 = dictionary[itemName];
					if ((Object)(object)val3 != (Object)null && val3.prefab != null)
					{
						object prefab = val3.prefab;
						GameObject val4 = (GameObject)((prefab is GameObject) ? prefab : null);
						if (val4 != null)
						{
							val = val4;
						}
						else
						{
							PropertyInfo property = prefab.GetType().GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
							if (property != null)
							{
								object value2 = property.GetValue(prefab);
								val = (GameObject)((value2 is GameObject) ? value2 : null);
							}
							else
							{
								PropertyInfo property2 = prefab.GetType().GetProperty("prefab", BindingFlags.Instance | BindingFlags.Public);
								if (property2 != null)
								{
									object value3 = property2.GetValue(prefab);
									val = (GameObject)((value3 is GameObject) ? value3 : null);
								}
							}
						}
					}
				}
			}
			if ((Object)(object)val == (Object)null)
			{
				val = Resources.Load<GameObject>("Items/" + itemName);
			}
		}
		if ((Object)(object)val != (Object)null)
		{
			itemPrefabCache[itemName] = val;
		}
		return val;
	}

	private static void ConfigureSyncComponents(GameObject item)
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		PhotonView val = item.GetComponent<PhotonView>() ?? item.AddComponent<PhotonView>();
		val.ViewID = PhotonNetwork.AllocateViewID(0);
		PhotonTransformView item2 = item.GetComponent<PhotonTransformView>() ?? item.AddComponent<PhotonTransformView>();
		Rigidbody component = item.GetComponent<Rigidbody>();
		if ((Object)(object)component != (Object)null)
		{
			PhotonRigidbodyView obj = item.GetComponent<PhotonRigidbodyView>() ?? item.AddComponent<PhotonRigidbodyView>();
			obj.m_SynchronizeVelocity = true;
			obj.m_SynchronizeAngularVelocity = true;
		}
		if ((Object)(object)item.GetComponent<MaiItemSync>() == (Object)null)
		{
			item.AddComponent<MaiItemSync>();
		}
		val.ObservedComponents = new List<Component> { (Component)(object)item2 };
		if ((Object)(object)component != (Object)null)
		{
			PhotonRigidbodyView component2 = item.GetComponent<PhotonRigidbodyView>();
			if ((Object)(object)component2 != (Object)null)
			{
				val.ObservedComponents.Add((Component)(object)component2);
			}
		}
		val.Synchronization = (ViewSynchronization)1;
		EnsureItemVisibility(item);
	}

	private static void ApplyDollarValue(Component valuable, float value)
	{
		if ((Object)(object)valuable == (Object)null)
		{
			return;
		}
		SetFieldValue(valuable, "dollarValueOriginal", value);
		SetFieldValue(valuable, "dollarValueCurrent", value);
		SetFieldValue(valuable, "dollarValueSet", true);
		if (!SemiFunc.IsMultiplayer() || !PhotonNetwork.IsMasterClient)
		{
			return;
		}
		PhotonView view = valuable.GetComponent<PhotonView>();
		if ((Object)(object)view != (Object)null)
		{
			view.RPC("DollarValueSetRPC", RpcTarget.Others, value);
		}
	}

	private static void ConfigureValuableObject(GameObject spawnedItem, int value, bool isMultiplayer)
	{
		Component component = spawnedItem.GetComponent(Type.GetType("ValuableObject, Assembly-CSharp"));
		if ((Object)(object)component == (Object)null)
		{
			return;
		}
		SetFieldValue(component, "dollarValueOverride", value);
		ApplyDollarValue(component, value);
		FieldInfo field2 = ((object)component).GetType().GetField("discovered", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (field2 != null)
		{
			field2.SetValue(component, true);
		}
		try
		{
			StatsManager instance = StatsManager.instance;
			if (!((Object)(object)instance != (Object)null))
			{
				return;
			}
			string text = "";
			FieldInfo fieldInfo = ((object)spawnedItem.GetComponent(Type.GetType("ItemAttributes, Assembly-CSharp")))?.GetType().GetField("instanceName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (fieldInfo != null)
			{
				text = fieldInfo.GetValue(spawnedItem.GetComponent(Type.GetType("ItemAttributes, Assembly-CSharp"))) as string;
			}
			if (!string.IsNullOrEmpty(text))
			{
				MethodInfo method2 = ((object)instance).GetType().GetMethod("RemoveValuableFromHaul", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method2 != null)
				{
					method2.Invoke(instance, new object[1] { text });
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private static void ConfigurePhysicsProperties(GameObject spawnedItem, Vector3 position, bool isMultiplayer)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		Component component = spawnedItem.GetComponent(Type.GetType("PhysGrabObject, Assembly-CSharp"));
		if ((Object)(object)component == (Object)null)
		{
			return;
		}
		SetFieldValue(component, "spawnTorque", Random.insideUnitSphere * 0.05f);
		if (isMultiplayer)
		{
			PhotonView component2 = spawnedItem.GetComponent<PhotonView>();
			if (component2 != null)
			{
				component2.RPC("SetPositionRPC", (RpcTarget)2, new object[2]
				{
					position,
					Quaternion.identity
				});
			}
		}
	}

	private static void EnsureItemVisibility(GameObject item)
	{
		Renderer[] componentsInChildren = item.GetComponentsInChildren<Renderer>(true);
		foreach (Renderer val in componentsInChildren)
		{
			bool flag = false;
			Material[] materials = val.materials;
			foreach (Material val2 in materials)
			{
				if ((Object)(object)val2 != (Object)null && val2.HasProperty("_MainTex") && (Object)(object)val2.mainTexture == (Object)null)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			GameObject val3 = Resources.Load<GameObject>(GetPrefabPath(((Object)item).name.Replace("(Clone)", "").Trim()));
			if ((Object)(object)val3 != (Object)null)
			{
				Renderer componentInChildren = val3.GetComponentInChildren<Renderer>();
				if ((Object)(object)componentInChildren != (Object)null)
				{
					val.sharedMaterials = componentInChildren.sharedMaterials;
				}
			}
		}
	}

	public static void SpawnSelectedItemMultiple(int count, List<string> availableItemsList, int selectedItemToSpawnIndex, int itemSpawnValue)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		if (availableItemsList == null || availableItemsList.Count == 0 || selectedItemToSpawnIndex >= availableItemsList.Count)
		{
			return;
		}
		GameObject localPlayer = DebugCheats.GetLocalPlayer();
		if ((Object)(object)localPlayer == (Object)null)
		{
			return;
		}
		string text = availableItemsList[selectedItemToSpawnIndex];
		bool flag = text.Contains("Valuable");
		Vector3 val = localPlayer.transform.position + localPlayer.transform.forward * 1.5f + Vector3.up * 1f;
		for (int i = 0; i < count; i++)
		{
			Vector3 val2 = Random.insideUnitSphere * 1f;
			Vector3 position = val + val2;
			if (flag)
			{
				SpawnItem(text, position, itemSpawnValue);
			}
			else
			{
				SpawnItem(text, position);
			}
		}
	}
}
