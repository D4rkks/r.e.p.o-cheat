using UnityEngine;
using Photon.Pun;

public class ItemSpawner : MonoBehaviourPunCallbacks
{
    private static readonly System.Reflection.FieldInfo DollarValueOverrideField =
        typeof(ValuableObject).GetField("dollarValueOverride", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    // Same thresholds as ExtractionPoint.SpawnTaxReturn picks the bag size with
    private static GameObject BagPrefabFor(int value)
    {
        var assets = AssetManager.instance;
        if (value > 10000) return assets.surplusValuableBig;
        if (value > 5000) return assets.surplusValuableMedium;
        return assets.surplusValuableSmall;
    }

    // Spawns a money bag the way the game does (EnemySpinny.SpawnMoneyBag / ExtractionPoint.SpawnTaxReturn):
    // the prefab is used untouched (adding/replacing Photon sync components broke its physics and scale),
    // and the value goes through dollarValueOverride, which ValuableObject applies on its own setup.
    public static void SpawnItem(Vector3 position, int value = 45000)
    {
        if (AssetManager.instance == null)
        {
            Debug.LogError("AssetManager not ready, can't spawn money.");
            return;
        }

        GameObject prefab = BagPrefabFor(value);
        GameObject bag;
        if (!SemiFunc.IsMultiplayer())
        {
            bag = Object.Instantiate(prefab, position, Quaternion.identity);
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            bag = PhotonNetwork.InstantiateRoomObject("Valuables/" + prefab.name, position, Quaternion.identity, 0);
        }
        else
        {
            // Room objects can only be created by the host; the value override may be ignored by the host's copy
            bag = PhotonNetwork.Instantiate("Valuables/" + prefab.name, position, Quaternion.identity, 0);
        }

        if (bag == null)
        {
            Debug.LogError("Money spawn failed!");
            return;
        }

        // Must be set right after instantiation, before ValuableObject's own setup reads it
        var valuable = bag.GetComponent<ValuableObject>();
        if (valuable != null && DollarValueOverrideField != null) DollarValueOverrideField.SetValue(valuable, value);
        Debug.Log($"Money bag ({prefab.name}) spawned at {position} with value {value}");
    }
}
