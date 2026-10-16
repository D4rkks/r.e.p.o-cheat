using System;
using System.Collections;
using System.Reflection;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

namespace r.e.p.o_cheat
{
    // EnemyHealth.healthCurrent is only updated on the host: clients receive HurtRPC(damage, direction) / DeathRPC
    // but the game never applies the damage to their local copy, so the ESP always showed full health as a client.
    // This listens to incoming PUN RPC events and applies the damage to the local healthCurrent ourselves.
    // OnSpawn/Awake still reset it locally when an enemy respawns, so it stays in sync.
    public static class EnemyHealthSync
    {
        private const byte RpcEventCode = 200;
        private static readonly FieldInfo HealthCurrentField = typeof(EnemyHealth).GetField("healthCurrent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static Photon.Realtime.LoadBalancingClient subscribedClient;

        // Called every frame: (re)subscribes when the networking client changes
        public static void Tick()
        {
            var client = PhotonNetwork.NetworkingClient;
            if (client == subscribedClient) return;
            Unsubscribe();
            if (client == null) return;
            client.EventReceived += OnEvent;
            subscribedClient = client;
        }

        // Must be called on unload, otherwise a hot-reloaded copy would apply every hit twice
        public static void Unsubscribe()
        {
            if (subscribedClient != null) subscribedClient.EventReceived -= OnEvent;
            subscribedClient = null;
        }

        private static void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != RpcEventCode || PhotonNetwork.IsMasterClient || HealthCurrentField == null) return;
            try
            {
                if (!(photonEvent.CustomData is IDictionary rpc) || !rpc.Contains((byte)0)) return;

                string method = null;
                if (rpc.Contains((byte)5))
                {
                    int index = Convert.ToInt32(rpc[(byte)5]);
                    var list = PhotonNetwork.PhotonServerSettings?.RpcList;
                    if (list != null && index >= 0 && index < list.Count) method = list[index];
                }
                else if (rpc.Contains((byte)3))
                {
                    method = rpc[(byte)3] as string;
                }
                if (method != "HurtRPC" && method != "DeathRPC") return;

                PhotonView view = PhotonView.Find(Convert.ToInt32(rpc[(byte)0]));
                if (view == null) return;
                EnemyHealth health = view.GetComponent<EnemyHealth>() ?? view.GetComponentInChildren<EnemyHealth>();
                if (health == null) return;

                int current = (int)HealthCurrentField.GetValue(health);
                if (method == "DeathRPC")
                {
                    HealthCurrentField.SetValue(health, 0);
                    return;
                }

                object[] parameters = rpc.Contains((byte)4) ? rpc[(byte)4] as object[] : null;
                if (parameters == null || parameters.Length == 0) return;
                int damage = Convert.ToInt32(parameters[0]);
                HealthCurrentField.SetValue(health, Mathf.Max(0, current - damage));
            }
            catch (Exception e)
            {
                Hax2.Log1($"EnemyHealthSync error: {e.Message}");
            }
        }
    }
}
