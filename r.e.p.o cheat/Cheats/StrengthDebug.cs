using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Reflection;
using System.Collections.Generic;

namespace r.e.p.o_cheat
{
    static class Strength
    {
        private static object physGrabberInstance;
        private static float lastStrengthUpdateTime = 0f;
        private static float strengthUpdateCooldown = 0.1f;
        private static PhotonView physGrabberPhotonView;
        private static PhotonView punManagerPhotonView;
        private static float lastAppliedStrength = -1f;

        private static void InitializePlayerController()
        {
            if (PlayerController.playerControllerType == null)
            {
                Hax2.Log1("PlayerController type not found.");
                return;
            }
            if (PlayerController.playerControllerInstance == null)
            {
                PlayerController.playerControllerInstance = GameHelper.FindObjectOfType(PlayerController.playerControllerType);
                if (PlayerController.playerControllerInstance == null)
                {
                    Hax2.Log1("PlayerController instance not found.");
                }
            }
        }

        public static void MaxStrength()
        {
            var playerControllerType = Type.GetType("PlayerController, Assembly-CSharp");
            if (playerControllerType == null) { Hax2.Log1("PlayerController type not found."); return; }
            var playerControllerInstance = global::PlayerController.instance != null ? (object)global::PlayerController.instance : null; // live instance; FindObjectOfType(includeInactive) could return a stale one
            if (playerControllerInstance == null) { Hax2.Log1("PlayerController instance not found."); return; }
            var playerAvatarScriptField = playerControllerType.GetField("playerAvatarScript", BindingFlags.Public | BindingFlags.Instance);
            if (playerAvatarScriptField == null) { Hax2.Log1("playerAvatarScript field not found in PlayerController."); return; }
            var playerAvatarScriptInstance = playerAvatarScriptField.GetValue(playerControllerInstance);
            if (playerAvatarScriptInstance == null) { Hax2.Log1("playerAvatarScript instance is null."); return; }
            var physGrabberField = playerAvatarScriptInstance.GetType().GetField("physGrabber", BindingFlags.Public | BindingFlags.Instance);
            if (physGrabberField == null) { Hax2.Log1("physGrabber field not found in PlayerAvatarScript."); return; }
            physGrabberInstance = physGrabberField.GetValue(playerAvatarScriptInstance);
            if (physGrabberInstance == null) { Hax2.Log1("physGrabber instance is null."); return; }
            var physGrabberPhotonViewField = physGrabberInstance.GetType().GetField("photonView", BindingFlags.Public | BindingFlags.Instance);
            if (physGrabberPhotonViewField != null)
            {
                physGrabberPhotonView = (PhotonView)physGrabberPhotonViewField.GetValue(physGrabberInstance);
            }
            if (physGrabberPhotonView == null) { Hax2.Log1("PhotonView not found in PhysGrabber."); }
            var punManagerType = Type.GetType("PunManager, Assembly-CSharp");
            var punManagerInstance = GameHelper.FindObjectOfType(punManagerType);
            if (punManagerInstance != null)
            {
                punManagerPhotonView = (PhotonView)punManagerType.GetField("photonView", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(punManagerInstance);
                if (punManagerPhotonView == null) { Hax2.Log1("PhotonView not found in PunManager."); }
            }
            else { Hax2.Log1("PunManager instance not found."); }

            ApplyGrabStrength();
            SetServerGrabStrength(Hax2.sliderValueStrength);
        }

        private static void ApplyGrabStrength()
        {
            if (physGrabberInstance == null) { Hax2.Log1("physGrabberInstance is null in ApplyGrabStrength"); return; }

            var grabStrengthField = physGrabberInstance.GetType().GetField("grabStrength", BindingFlags.Public | BindingFlags.Instance);
            if (grabStrengthField != null)
            {
                float currentStrength = (float)grabStrengthField.GetValue(physGrabberInstance);
                if (currentStrength != Hax2.sliderValueStrength)
                {
                    grabStrengthField.SetValue(physGrabberInstance, Hax2.sliderValueStrength);
                    Hax2.Log1($"grabStrength forced locally to {Hax2.sliderValueStrength} (was {currentStrength})");

                    if (Hax2.sliderValueStrength <= 1f)
                    {
                        ResetGrabbedObject();
                    }
                }
            }
            else { Hax2.Log1("grabStrength field not found"); }

            var grabbedField = physGrabberInstance.GetType().GetField("grabbed", BindingFlags.Public | BindingFlags.Instance);
            bool isGrabbed = grabbedField != null && (bool)grabbedField.GetValue(physGrabberInstance);
            Hax2.Log1($"isGrabbed: {isGrabbed}");

            if (isGrabbed)
            {
                var grabbedObjectTransformField = physGrabberInstance.GetType().GetField("grabbedObjectTransform", BindingFlags.Public | BindingFlags.Instance);
                Transform grabbedObjectTransform = grabbedObjectTransformField != null ? (Transform)grabbedObjectTransformField.GetValue(physGrabberInstance) : null;
                if (grabbedObjectTransform == null) { Hax2.Log1("grabbedObjectTransform is null"); return; }

                Hax2.Log1($"Attempting to grab object: {grabbedObjectTransform.name}");

                PhysGrabObject physGrabObject = grabbedObjectTransform.GetComponent<PhysGrabObject>();
                if (physGrabObject == null)
                {
                    Hax2.Log1($"PhysGrabObject component not found on {grabbedObjectTransform.name}");
                    return;
                }

                Rigidbody rb = physGrabObject.rb;
                if (rb == null) { Hax2.Log1($"Rigidbody not found on {grabbedObjectTransform.name}"); return; }

                var physGrabPointField = physGrabberInstance.GetType().GetField("physGrabPoint", BindingFlags.Public | BindingFlags.Instance);
                Transform physGrabPoint = physGrabPointField != null ? (Transform)physGrabPointField.GetValue(physGrabberInstance) : null;
                if (physGrabPoint == null) { Hax2.Log1("physGrabPoint is null"); return; }

                var pullerPositionField = physGrabberInstance.GetType().GetField("physGrabPointPullerPosition", BindingFlags.Public | BindingFlags.Instance);
                Vector3 pullerPosition = pullerPositionField != null ? (Vector3)pullerPositionField.GetValue(physGrabberInstance) : Vector3.zero;
                if (pullerPosition == Vector3.zero && pullerPositionField == null) { Hax2.Log1("pullerPositionField is null"); return; }

                Vector3 direction = (pullerPosition - physGrabPoint.position).normalized;
                float forceMagnitude = Hax2.sliderValueStrength * 50000f;
                Hax2.Log1($"Calculated forceMagnitude: {forceMagnitude} for {grabbedObjectTransform.name}");

                var physGrabObjectPhotonViewField = physGrabObject.GetType().GetField("photonView", BindingFlags.NonPublic | BindingFlags.Instance);
                PhotonView physGrabObjectPhotonView = physGrabObjectPhotonViewField != null ? (PhotonView)physGrabObjectPhotonViewField.GetValue(physGrabObject) : null;

                if (physGrabObjectPhotonView != null)
                {
                    if (!physGrabObjectPhotonView.IsMine)
                    {
                        physGrabObjectPhotonView.RequestOwnership();
                        Hax2.Log1($"Requested ownership of {grabbedObjectTransform.name}");
                    }

                    if (physGrabObjectPhotonView.IsMine)
                    {
                        rb.AddForceAtPosition(direction * forceMagnitude, physGrabPoint.position, ForceMode.Force);
                        Hax2.Log1($"Applied extra force {forceMagnitude} to {grabbedObjectTransform.name} as owner");
                    }
                    else if (PhotonNetwork.IsMasterClient)
                    {
                        rb.AddForceAtPosition(direction * forceMagnitude, physGrabPoint.position, ForceMode.Force);
                        Hax2.Log1($"Applied extra force {forceMagnitude} to {grabbedObjectTransform.name} as Master Client");
                    }
                    else
                    {
                        physGrabObjectPhotonView.RPC("ApplyExtraForceRPC", RpcTarget.MasterClient, direction, forceMagnitude, physGrabPoint.position);
                        Hax2.Log1($"Requested extra force {forceMagnitude} to {grabbedObjectTransform.name} via RPC");
                        if (Hax2.sliderValueStrength == lastAppliedStrength)
                        {
                            rb.AddForceAtPosition(direction * forceMagnitude, physGrabPoint.position, ForceMode.Force);
                        }
                    }
                }
                else { Hax2.Log1($"physGrabObjectPhotonView is null on {grabbedObjectTransform.name}"); }
            }
        }

        private static void ResetGrabbedObject()
        {
            var grabbedObjectTransformField = physGrabberInstance.GetType().GetField("grabbedObjectTransform", BindingFlags.Public | BindingFlags.Instance);
            Transform grabbedObjectTransform = grabbedObjectTransformField != null ? (Transform)grabbedObjectTransformField.GetValue(physGrabberInstance) : null;
            if (grabbedObjectTransform != null)
            {
                Hax2.Log1($"Resetting object: {grabbedObjectTransform.name}");
                PhysGrabObject physGrabObject = grabbedObjectTransform.GetComponent<PhysGrabObject>();
                if (physGrabObject != null && physGrabObject.rb != null)
                {
                    physGrabObject.rb.velocity = Vector3.zero;
                    physGrabObject.rb.angularVelocity = Vector3.zero;
                    Hax2.Log1($"Reset velocity of {grabbedObjectTransform.name} due to strength reset ({Hax2.sliderValueStrength})");

                    var photonView = physGrabObject.GetComponent<PhotonView>();
                    if (photonView != null && !photonView.IsMine && PhotonNetwork.IsConnected)
                    {
                        photonView.RPC("ResetVelocityRPC", RpcTarget.MasterClient);
                        Hax2.Log1($"Requested velocity reset for {grabbedObjectTransform.name} via RPC");
                    }
                }
                else
                {
                    Hax2.Log1($"PhysGrabObject or Rigidbody not found on {grabbedObjectTransform.name} during reset");
                }
            }
            else
            {
                Hax2.Log1("No grabbed object to reset");
            }
        }

        public static void UpdateStrength()
        {
            if (physGrabberInstance != null)
            {
                ApplyGrabStrength();
                if (Hax2.sliderValueStrength != lastAppliedStrength)
                {
                    SetServerGrabStrength(Hax2.sliderValueStrength);
                    lastAppliedStrength = Hax2.sliderValueStrength;
                    lastStrengthUpdateTime = Time.time;
                    if (Hax2.sliderValueStrength <= 1f)
                    {
                        ResetGrabbedObject();
                    }
                }
            }
        }

        public static void SetServerGrabStrength(float strength)
        {
            if (physGrabberInstance == null)
            {
                MaxStrength();
                if (physGrabberInstance == null) return;
            }

            if (punManagerPhotonView == null)
            {
                Hax2.Log1("PunManager PhotonView not initialized.");
                return;
            }

            string steamID = SemiFunc.PlayerGetSteamID(SemiFunc.PlayerAvatarLocal());
            if (string.IsNullOrEmpty(steamID))
            {
                Hax2.Log1("Could not retrieve local SteamID.");
                return;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                var playerAvatar = SemiFunc.PlayerAvatarGetFromSteamID(steamID);
                if (playerAvatar != null)
                {
                    playerAvatar.physGrabber.grabStrength = strength;
                    Hax2.Log1($"Set grabStrength to {strength} directly on Master Client for SteamID: {steamID}");

                    var punManagerType = Type.GetType("PunManager, Assembly-CSharp");
                    var punManagerInstance = GameHelper.FindObjectOfType(punManagerType);
                    if (punManagerInstance != null)
                    {
                        var statsManagerField = punManagerType.GetField("statsManager", BindingFlags.NonPublic | BindingFlags.Instance);
                        var statsManager = statsManagerField?.GetValue(punManagerInstance);
                        if (statsManager != null)
                        {
                            var playerUpgradeStrengthField = statsManager.GetType().GetField("playerUpgradeStrength", BindingFlags.Public | BindingFlags.Instance);
                            var playerUpgradeStrength = (Dictionary<string, int>)playerUpgradeStrengthField?.GetValue(statsManager);
                            if (playerUpgradeStrength != null)
                            {
                                int targetUpgrades = Mathf.RoundToInt(strength);
                                playerUpgradeStrength[steamID] = targetUpgrades;
                                Hax2.Log1($"Updated playerUpgradeStrength to {targetUpgrades} upgrades for SteamID: {steamID}");
                            }
                        }
                    }
                }
            }
            else
            {
                int targetUpgrades = Mathf.RoundToInt(strength);
                int currentUpgrades = GetCurrentUpgradeCount(steamID);
                if (targetUpgrades != currentUpgrades)
                {
                    punManagerPhotonView.RPC("UpgradePlayerGrabStrengthRPC", RpcTarget.MasterClient, steamID, targetUpgrades);
                    Hax2.Log1($"Requested server-side grabStrength set to {strength} (upgrades: {targetUpgrades}) for SteamID: {steamID}");
                }
            }
        }

        private static int GetCurrentUpgradeCount(string steamID)
        {
            var punManagerType = Type.GetType("PunManager, Assembly-CSharp");
            var punManagerInstance = GameHelper.FindObjectOfType(punManagerType);
            if (punManagerInstance != null)
            {
                var statsManagerField = punManagerType.GetField("statsManager", BindingFlags.NonPublic | BindingFlags.Instance);
                var statsManager = statsManagerField?.GetValue(punManagerInstance);
                if (statsManager != null)
                {
                    var playerUpgradeStrengthField = statsManager.GetType().GetField("playerUpgradeStrength", BindingFlags.Public | BindingFlags.Instance);
                    var playerUpgradeStrength = (Dictionary<string, int>)playerUpgradeStrengthField?.GetValue(statsManager);
                    if (playerUpgradeStrength != null && playerUpgradeStrength.ContainsKey(steamID))
                    {
                        return playerUpgradeStrength[steamID];
                    }
                }
            }
            return 0;
        }

        public partial class PhysGrabObject : MonoBehaviour, IPunOwnershipCallbacks
        {
            public Rigidbody rb;
            private PhotonView photonView;

            private void Awake()
            {
                photonView = GetComponent<PhotonView>();
                PhotonNetwork.AddCallbackTarget(this);
            }

            private void OnDestroy()
            {
                PhotonNetwork.RemoveCallbackTarget(this);
            }

            [PunRPC]
            private void ApplyExtraForceRPC(Vector3 direction, float forceMagnitude, Vector3 position)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    rb.AddForceAtPosition(direction * forceMagnitude, position, ForceMode.Force);
                    Hax2.Log1($"Applied extra force {forceMagnitude} to {gameObject.name} via RPC from client");
                }
            }

            [PunRPC]
            private void ResetVelocityRPC()
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    Hax2.Log1($"Reset velocity of {gameObject.name} via RPC from client");
                }
            }

            public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer) { /* Implementação existente */ }
            public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner) { /* Implementação existente */ }
            public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest) { /* Implementação existente */ }
        }
    }
}