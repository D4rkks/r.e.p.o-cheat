using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Reflection;

namespace r.e.p.o_cheat
{
    public static class ItemTeleport
    {
        public class GameItem
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public object ItemObject { get; set; }

            public GameItem(string name, int value, object itemObject = null)
            {
                Name = name;
                Value = value;
                ItemObject = itemObject;
            }
        }

        public static void SetItemValue(GameItem selectedItem, int newValue)
        {
            if (selectedItem == null || selectedItem.ItemObject == null)
            {
                Hax2.Log1("Error: Selected item or ItemObject is null!");
                return;
            }

            try
            {
                var itemType = selectedItem.ItemObject.GetType();

                var valueField = itemType.GetField("dollarValueCurrent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (valueField == null)
                {
                    Hax2.Log1($"Error: Could not find 'dollarValueCurrent' field in {selectedItem.Name}");
                    return;
                }

                valueField.SetValue(selectedItem.ItemObject, Convert.ChangeType(newValue, valueField.FieldType));
                selectedItem.Value = newValue;

                Hax2.Log1($"Successfully set '{selectedItem.Name}' value to ${newValue}");
            }
            catch (Exception e)
            {
                Hax2.Log1($"Error setting value for '{selectedItem.Name}': {e.Message}");
            }
        }
        private static PhotonView punManagerPhotonView;

        private static void InitializePunManager()
        {
            if (punManagerPhotonView == null)
            {
                var punManagerType = Type.GetType("PunManager, Assembly-CSharp");
                var punManagerInstance = GameHelper.FindObjectOfType(punManagerType);
                if (punManagerInstance != null)
                {
                    punManagerPhotonView = (PhotonView)punManagerType.GetField("photonView", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(punManagerInstance);
                    if (punManagerPhotonView == null) { Hax2.Log1("PhotonView not found in PunManager."); }
                }
                else { Hax2.Log1("PunManager instance not found."); }
            }
        }

        public static List<GameItem> GetItemList()
        {
            List<GameItem> itemList = new List<GameItem>();

            foreach (var valuableObject in DebugCheats.valuableObjects)
            {
                if (valuableObject == null) continue;

                var transformProperty = valuableObject.GetType().GetProperty("transform", BindingFlags.Public | BindingFlags.Instance);
                if (transformProperty == null)
                {
                    Hax2.Log1($"Warning: Object '{valuableObject.GetType().Name}' does not have a 'transform' property. Skipping.");
                    continue;
                }

                var transform = transformProperty.GetValue(valuableObject) as Transform;
                if (transform == null || !transform.gameObject.activeInHierarchy)
                {
                    Hax2.Log1($"Warning: Object '{valuableObject.GetType().Name}' has an inactive or null transform. Skipping.");
                    continue;
                }

                string itemName;
                try
                {
                    itemName = valuableObject.GetType().GetProperty("name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(valuableObject) as string;
                    if (string.IsNullOrEmpty(itemName))
                    {
                        itemName = (valuableObject as UnityEngine.Object)?.name ?? "Unknown";
                    }
                }
                catch (Exception e)
                {
                    itemName = (valuableObject as UnityEngine.Object)?.name ?? "Unknown";
                    Hax2.Log1($"Error accessing 'name' of item: {e.Message}. Using GameObject name: {itemName}");
                }

                if (itemName.StartsWith("Valuable", StringComparison.OrdinalIgnoreCase))
                {
                    itemName = itemName.Substring("Valuable".Length).Trim();
                }
                if (itemName.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
                {
                    itemName = itemName.Substring(0, itemName.Length - "(Clone)".Length).Trim();
                }

                int itemValue = 0;

                // Only check dollarValueCurrent if it's NOT PlayerDeathHead
                if (valuableObject.GetType().Name != "PlayerDeathHead")
                {
                    var valueField = valuableObject.GetType().GetField("dollarValueCurrent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (valueField != null)
                    {
                        try
                        {
                            itemValue = Convert.ToInt32(valueField.GetValue(valuableObject));
                        }
                        catch (Exception e)
                        {
                            Hax2.Log1($"Error reading 'dollarValueCurrent' for '{itemName}': {e.Message}. Defaulting to 0.");
                        }
                    }
                    else
                    {
                        Hax2.Log1($"Info: '{itemName}' does not have 'dollarValueCurrent', assuming value 0.");
                    }
                }

                // Always add the item to the list, even if it's PlayerDeathHead
                itemList.Add(new GameItem(itemName, itemValue, valuableObject));
            }

            // Ensure there's at least one entry if nothing was found
            if (itemList.Count == 0)
            {
                itemList.Add(new GameItem("No items found", 0));
            }

            return itemList;
        }
        
        public static void TeleportItemToMe(GameItem selectedItem)
        {
            if (selectedItem == null || selectedItem.ItemObject == null)
            {
                Hax2.Log1("Item selecionado ou ItemObject é nulo!");
                return;
            }

            PerformTeleport(selectedItem);
        }

        private const float ImpactProtectionSeconds = 8f;

        public static void TeleportAllItemsToMe()
        {
            try
            {
                GameObject player = DebugCheats.GetLocalPlayer();
                if (player == null)
                {
                    Hax2.Log1("Local player not found!");
                    return;
                }

                Vector3 forward = player.transform.forward;
                forward.y = 0f;
                forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
                Vector3 center = player.transform.position + forward * 2.5f;

                int count = ItemPacker.Pack(GetItemList(), center, Quaternion.LookRotation(forward), 3f, 3f);
                Hax2.Log1($"Packed {count} items in front of the player.");
            }
            catch (Exception e)
            {
                Hax2.Log1($"Error teleporting all items: {e.Message}");
            }
        }

        public static void TeleportAllItemsToExtraction()
        {
            try
            {
                ExtractionPoint ep = FindTargetExtractionPoint();
                if (ep == null)
                {
                    Hax2.Log1("No extraction point found!");
                    return;
                }

                ExtractionZone.GetZone(ep, out Vector3 center, out Quaternion rotation);
                int count = ItemPacker.Pack(GetItemList(), center, rotation, ExtractionZone.sizeX, ExtractionZone.sizeZ);
                Hax2.Log1($"Packed {count} items on extraction '{ep.name}'.");
            }
            catch (Exception e)
            {
                Hax2.Log1($"Error teleporting items to extraction: {e.Message}");
            }
        }

        internal static ExtractionPoint FindTargetExtractionPoint()
        {
            var roundDirector = RoundDirector.instance;
            if (roundDirector != null)
            {
                var current = typeof(RoundDirector)
                    .GetField("extractionPointCurrent", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(roundDirector) as ExtractionPoint;
                if (current != null) return current;
            }

            GameObject player = DebugCheats.GetLocalPlayer();
            var stateField = typeof(ExtractionPoint).GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance);
            ExtractionPoint best = null;
            float bestDistance = float.MaxValue;

            foreach (var ep in UnityEngine.Object.FindObjectsOfType<ExtractionPoint>())
            {
                if (ep == null || !ep.gameObject.activeInHierarchy) continue;
                if (stateField?.GetValue(ep)?.ToString() == "Complete") continue;

                float distance = player != null ? Vector3.Distance(player.transform.position, ep.transform.position) : 0f;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = ep;
                }
            }
            return best;
        }

        public static void TeleportSelectedItemToMe(GameItem selectedItem)
        {
            if (selectedItem == null || selectedItem.ItemObject == null)
            {
                Hax2.Log1("Item selecionado ou ItemObject é nulo!");
                return;
            }

            PerformTeleport(selectedItem);
        }
        private static void PerformTeleport(GameItem item)
        {
            try
            {
                GameObject player = DebugCheats.GetLocalPlayer();
                if (player == null)
                {
                    Hax2.Log1("Jogador local não encontrado!");
                    return;
                }

                Vector3 targetPosition = player.transform.position + player.transform.forward * 1f + Vector3.up * 1.5f;
                Hax2.Log1($"Target position for teleport of '{item.Name}': {targetPosition}");

                Transform itemTransform = null;
                var itemObjectType = item.ItemObject.GetType();
                var transformProperty = itemObjectType.GetProperty("transform", BindingFlags.Public | BindingFlags.Instance);
                if (transformProperty != null)
                {
                    itemTransform = transformProperty.GetValue(item.ItemObject) as Transform;
                }
                else
                {
                    var itemMono = item.ItemObject as MonoBehaviour;
                    if (itemMono != null)
                    {
                        itemTransform = itemMono.transform;
                    }
                }

                if (itemTransform == null)
                {
                    Hax2.Log1($"Não foi possível obter o Transform do item '{item.Name}'!");
                    return;
                }

                MoveItem(item, itemTransform, targetPosition);
            }
            catch (Exception e)
            {
                Hax2.Log1($"Erro ao teleportar item '{item.Name}': {e.Message}");
            }
        }

        internal static void MoveItem(GameItem item, Transform itemTransform, Vector3 targetPosition, Quaternion? targetRotation = null)
        {
            Quaternion rotation = targetRotation ?? itemTransform.rotation;
            try
            {
                PhotonView itemPhotonView = itemTransform.GetComponent<PhotonView>();
                if (itemPhotonView != null && PhotonNetwork.IsConnected && !itemPhotonView.IsMine)
                {
                    itemPhotonView.RequestOwnership();
                }

                var impactDetector = itemTransform.GetComponent<PhysGrabObjectImpactDetector>();
                if (impactDetector != null)
                {
                    impactDetector.ImpactDisable(ImpactProtectionSeconds);
                }

                Rigidbody rb = itemTransform.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                var physGrabObject = itemTransform.GetComponent<PhysGrabObject>();
                if (physGrabObject != null)
                {
                    physGrabObject.Teleport(targetPosition, rotation);
                }
                else
                {
                    if (rb != null)
                    {
                        rb.position = targetPosition;
                        rb.rotation = rotation;
                    }
                    itemTransform.SetPositionAndRotation(targetPosition, rotation);
                }
            }
            catch (Exception e)
            {
                Hax2.Log1($"Error moving item '{item.Name}': {e.Message}");
            }
        }
    }
}
