using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace r.e.p.o_cheat
{
    public static class Teleport
    {
        // Moves the local player. Movement is driven by the local PlayerController (the PlayerAvatar only follows it),
        // and we own our own movement, so this works without being host.
        public static bool TeleportLocalPlayer(Vector3 position, Quaternion? rotation = null)
        {
            var controller = global::PlayerController.instance;
            if (controller == null)
            {
                Hax2.Log1("Teleport: local PlayerController not found.");
                return false;
            }

            controller.transform.position = position;
            if (rotation.HasValue) controller.transform.rotation = rotation.Value;
            var rb = controller.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = position;
                rb.velocity = Vector3.zero;
            }

            // Snap the avatar too so it doesn't visibly slide over from the old position
            var avatar = controller.playerAvatarScript;
            if (avatar != null)
            {
                avatar.transform.position = position;
                var avatarRb = avatar.GetComponent<Rigidbody>();
                if (avatarRb != null) avatarRb.position = position;
            }
            return true;
        }

        // Moving another player goes through PlayerAvatar.Spawn -> SpawnRPC, which is MasterOnlyRPC:
        // only the host can do it (the target's own game moves its PlayerController).
        public static void TeleportPlayerToMe(int selectedPlayerIndex, List<object> playerList, List<string> playerNames)
        {
            if (selectedPlayerIndex < 0 || selectedPlayerIndex >= playerList.Count || !(playerList[selectedPlayerIndex] is PlayerAvatar target) || target == null)
            {
                Hax2.Log1("Teleport: invalid player selected.");
                return;
            }
            string name = playerNames[selectedPlayerIndex];

            try
            {
                GameObject localPlayer = DebugCheats.GetLocalPlayer();
                if (localPlayer == null)
                {
                    Hax2.Log1("Teleport: local player not found.");
                    return;
                }

                // In front of us, facing us
                Vector3 position = localPlayer.transform.position + localPlayer.transform.forward * 1.5f + Vector3.up * 0.3f;
                Quaternion rotation = Quaternion.LookRotation(-new Vector3(localPlayer.transform.forward.x, 0f, localPlayer.transform.forward.z));

                if (target == PlayerAvatar.instance)
                {
                    TeleportLocalPlayer(position, rotation);
                    return;
                }
                if (SemiFunc.IsMultiplayer() && !PhotonNetwork.IsMasterClient)
                {
                    Hax2.Log1($"Teleport: can't move {name}, only the host can teleport other players (SpawnRPC is host-only).");
                    return;
                }

                target.Spawn(position, rotation);
                Hax2.Log1($"Teleport: {name} teleported to you.");
            }
            catch (Exception e)
            {
                Hax2.Log1($"Teleport: error teleporting {name} to you: {e.Message}");
            }
        }

        public static void TeleportMeToPlayer(int selectedPlayerIndex, List<object> playerList, List<string> playerNames)
        {
            if (selectedPlayerIndex < 0 || selectedPlayerIndex >= playerList.Count || !(playerList[selectedPlayerIndex] is PlayerAvatar target) || target == null)
            {
                Hax2.Log1("Teleport: invalid player selected.");
                return;
            }

            Transform targetTransform = target.transform;
            Vector3 flatForward = new Vector3(targetTransform.forward.x, 0f, targetTransform.forward.z).normalized;
            if (flatForward == Vector3.zero) flatForward = Vector3.forward;
            // In front of them, facing them
            Vector3 position = targetTransform.position + flatForward * 1.5f + Vector3.up * 0.3f;
            if (TeleportLocalPlayer(position, Quaternion.LookRotation(-flatForward)))
                Hax2.Log1($"Teleport: teleported to {playerNames[selectedPlayerIndex]}.");
        }
    }
}
