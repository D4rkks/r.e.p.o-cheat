using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviourPunCallbacks
{
    public static void SpawnItem(Vector3 position, int value = 45000)
    {
        try
        {
            GameObject gameObject = AssetManager.instance.surplusValuableSmall;
            GameObject gameObject2;

            if (!SemiFunc.IsMultiplayer())
            {
                Debug.Log("Offline mode: Spawning locally.");
                gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, position, Quaternion.identity);
                EnsureItemVisibility(gameObject2);
            }
            else
            {
                Debug.Log("Item spawned at: " + position);
                gameObject2 = PhotonNetwork.InstantiateRoomObject("Valuables/" + gameObject.name, position, Quaternion.identity, 0, null);
                ConfigureSyncComponents(gameObject2);
            }

            // Use reflection to modify dollarValueOverride
            var valuableComponent = gameObject2.GetComponent(Type.GetType("ValuableObject, Assembly-CSharp"));
            if (valuableComponent != null)
            {
                // Get the field using reflection, searching both public and private fields
                FieldInfo dollarValueField = valuableComponent.GetType().GetField("dollarValueOverride",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                if (dollarValueField != null)
                {
                    // Set the value as int
                    dollarValueField.SetValue(valuableComponent, value);
                    Debug.Log($"Set dollarValueOverride to {value}");
                }
                else
                {
                    Debug.LogError("dollarValueOverride field not found");
                }
            }
            else
            {
                Debug.LogError("ValuableObject component not found");
            }

            // Use reflection to modify spawnTorque
            var physComponent = gameObject2.GetComponent(Type.GetType("PhysGrabObject, Assembly-CSharp"));
            if (physComponent != null)
            {
                // Get the field using reflection
                FieldInfo spawnTorqueField = physComponent.GetType().GetField("spawnTorque",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                if (spawnTorqueField != null)
                {
                    // Set the value
                    Vector3 randomTorque = UnityEngine.Random.insideUnitSphere * 0.05f;
                    spawnTorqueField.SetValue(physComponent, randomTorque);
                    Debug.Log($"Set spawnTorque to {randomTorque}");
                }
                else
                {
                    Debug.LogError("spawnTorque field not found");
                }
            }
            else
            {
                Debug.LogError("PhysGrabObject component not found");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in SpawnItem: {ex.Message}");
        }
    }

    private static void ConfigureSyncComponents(GameObject item)
    {
        PhotonView pv = item.GetComponent<PhotonView>();
        if (pv == null)
        {
            pv = item.AddComponent<PhotonView>();
            pv.ViewID = PhotonNetwork.AllocateViewID(0);
            Debug.Log("PhotonView adicionado ao item: " + pv.ViewID);
        }

        PhotonTransformView transformView = item.GetComponent<PhotonTransformView>();
        if (transformView == null)
        {
            transformView = item.AddComponent<PhotonTransformView>();
            Debug.Log("PhotonTransformView adicionado ao item");
        }

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            PhotonRigidbodyView rigidbodyView = item.GetComponent<PhotonRigidbodyView>();
            if (rigidbodyView == null)
            {
                rigidbodyView = item.AddComponent<PhotonRigidbodyView>();
                rigidbodyView.m_SynchronizeVelocity = true;
                rigidbodyView.m_SynchronizeAngularVelocity = true;
                Debug.Log("PhotonRigidbodyView adicionado e configurado no item");
            }
        }

        if (item.GetComponent<ItemSync>() == null)
        {
            item.AddComponent<ItemSync>();
        }

        pv.ObservedComponents = new List<Component> { transformView };
        if (rb != null)
        {
            PhotonRigidbodyView rigidbodyView = item.GetComponent<PhotonRigidbodyView>();
            if (rigidbodyView != null)
            {
                pv.ObservedComponents.Add(rigidbodyView);
            }
        }
        pv.Synchronization = ViewSynchronization.ReliableDeltaCompressed;

        EnsureItemVisibility(item);
    }

    private static void EnsureItemVisibility(GameObject item)
    {
        item.SetActive(true);
        foreach (var renderer in item.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }
        item.layer = LayerMask.NameToLayer("Default");
    }
}
