using System;
using System.Reflection;
using UnityEngine;
using Photon.Pun;

namespace r.e.p.o_cheat
{
    static class Health_Player
    {
        static public object playerHealthInstance;
        static public object playerMaxHealthInstance;

        public static void HealPlayer(object targetPlayer, int healAmount, string playerName)
        {
            if (targetPlayer == null)
            {
                Hax2.Log1("Jogador alvo é nulo!");
                return;
            }

            try
            {
                Hax2.Log1($"Tentando curar: {playerName} | MasterClient: {PhotonNetwork.IsMasterClient}");
                var photonViewField = targetPlayer.GetType().GetField("photonView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (photonViewField == null) { Hax2.Log1("PhotonViewField não encontrado!"); return; }
                var photonView = photonViewField.GetValue(targetPlayer) as PhotonView;
                if (photonView == null) { Hax2.Log1("PhotonView não é válido!"); return; }

                var playerHealthField = targetPlayer.GetType().GetField("playerHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (playerHealthField == null) { Hax2.Log1("Campo 'playerHealth' não encontrado!"); return; }
                var playerHealthInstance = playerHealthField.GetValue(targetPlayer);
                if (playerHealthInstance == null) { Hax2.Log1("Instância de playerHealth é nula!"); return; }

                var healthType = playerHealthInstance.GetType();
                var healMethod = healthType.GetMethod("Heal", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (healMethod != null)
                {
                    healMethod.Invoke(playerHealthInstance, new object[] { healAmount, true });
                    Hax2.Log1($"Método 'Heal' chamado localmente com {healAmount} HP.");
                }
                else
                {
                    Hax2.Log1("Método 'Heal' não encontrado!");
                }

                if (PhotonNetwork.IsConnected && photonView != null)
                {
                    var currentHealthField = healthType.GetField("currentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var maxHealthField = healthType.GetField("maxHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    int currentHealth = currentHealthField != null ? (int)currentHealthField.GetValue(playerHealthInstance) : 0;
                    int maxHealth = maxHealthField != null ? (int)maxHealthField.GetValue(playerHealthInstance) : 100;
                    Hax2.Log1(maxHealthField != null ? $"maxHealth encontrado: {maxHealth}" : "Campo 'maxHealth' não encontrado, usando valor padrão: 100");

                    photonView.RPC("UpdateHealthRPC", RpcTarget.AllBuffered, new object[] { maxHealth, maxHealth, true, false });
                    Hax2.Log1($"RPC 'UpdateHealthRPC' enviado para todos com saúde={currentHealth + maxHealth}, maxHealth={maxHealth}, effect=true.");

                    try
                    {
                        photonView.RPC("HealRPC", RpcTarget.AllBuffered, new object[] { healAmount, true });
                        Hax2.Log1($"RPC 'HealRPC' enviado com {healAmount} HP.");
                    }
                    catch
                    {
                        Hax2.Log1("RPC 'HealRPC' não registrado, confiando no UpdateHealthRPC.");
                    }
                }
                else
                {
                    Hax2.Log1("Não conectado ao Photon, cura apenas local.");
                }
                Hax2.Log1($"Tentativa de curar concluída.");
            }
            catch (Exception e)
            {
                Hax2.Log1($"Erro ao tentar curar: {e.Message}");
            }
        }

        public static void DamagePlayer(object targetPlayer, int damageAmount, string playerName)
        {
            if (targetPlayer == null)
            {
                Hax2.Log1("Jogador alvo é nulo!");
                return;
            }

            try
            {
                Hax2.Log1($"Tentando causar dano: {playerName} | MasterClient: {PhotonNetwork.IsMasterClient}");
                var photonViewField = targetPlayer.GetType().GetField("photonView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (photonViewField == null) { Hax2.Log1("PhotonViewField não encontrado!"); return; }
                var photonView = photonViewField.GetValue(targetPlayer) as PhotonView;
                if (photonView == null) { Hax2.Log1("PhotonView não é válido!"); return; }

                var playerHealthField = targetPlayer.GetType().GetField("playerHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (playerHealthField == null) { Hax2.Log1("Campo 'playerHealth' não encontrado!"); return; }
                var playerHealthInstance = playerHealthField.GetValue(targetPlayer);
                if (playerHealthInstance == null) { Hax2.Log1("Instância de playerHealth é nula!"); return; }

                var healthType = playerHealthInstance.GetType();
                var hurtMethod = healthType.GetMethod("Hurt", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (hurtMethod != null)
                {
                    hurtMethod.Invoke(playerHealthInstance, BuildArgs(hurtMethod, damageAmount, true, -1, false));
                    Hax2.Log1($"Método 'Hurt' chamado localmente com {damageAmount} de dano.");
                }
                else
                {
                    Hax2.Log1("Método 'Hurt' não encontrado!");
                }

                if (PhotonNetwork.IsConnected && photonView != null)
                {
                    var maxHealthField = healthType.GetField("maxHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    int maxHealth = maxHealthField != null ? (int)maxHealthField.GetValue(playerHealthInstance) : 100;
                    Hax2.Log1(maxHealthField != null ? $"maxHealth encontrado: {maxHealth}" : "Campo 'maxHealth' não encontrado, usando valor padrão: 100");

                    photonView.RPC("HurtOtherRPC", RpcTarget.AllBuffered, new object[] { damageAmount, Vector3.zero, false, -1, false });
                    Hax2.Log1($"RPC 'HurtOtherRPC' enviado com {damageAmount} de dano.");

                    try
                    {
                        photonView.RPC("HurtRPC", RpcTarget.AllBuffered, new object[] { damageAmount, true, -1 });
                        Hax2.Log1($"RPC 'HurtRPC' enviado com {damageAmount} de dano.");
                    }
                    catch
                    {
                        Hax2.Log1("RPC 'HurtRPC' não registrado, confiando no HurtOtherRPC.");
                    }
                }
                else
                {
                    Hax2.Log1("Não conectado ao Photon, dano apenas local.");
                }
                Hax2.Log1($"Tentativa de causar dano concluída.");
            }
            catch (Exception e)
            {
                Hax2.Log1($"Erro ao tentar causar dano: {e.Message}");
            }
        }

        public static void MaxHealth()
        {
            var playerControllerType = Type.GetType("PlayerController, Assembly-CSharp");
            if (playerControllerType != null)
            {
                Hax2.Log1("PlayerController encontrado.");
                var playerControllerInstance = GameHelper.FindObjectOfType(playerControllerType);
                if (playerControllerInstance != null)
                {
                    var playerAvatarScriptField = playerControllerInstance.GetType().GetField("playerAvatarScript", BindingFlags.Public | BindingFlags.Instance);
                    if (playerAvatarScriptField != null)
                    {
                        var playerAvatarScriptInstance = playerAvatarScriptField.GetValue(playerControllerInstance);
                        var playerHealthField = playerAvatarScriptInstance.GetType().GetField("playerHealth", BindingFlags.Public | BindingFlags.Instance);
                        if (playerHealthField != null)
                        {
                            var playerHealthInstance = playerHealthField.GetValue(playerAvatarScriptInstance);
                            var healthType = playerHealthInstance.GetType();
                            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                            var maxHealthField = healthType.GetField("maxHealth", flags);
                            int maxHealth = maxHealthField != null ? (int)maxHealthField.GetValue(playerHealthInstance) : 100;
                            int newHealth = Hax2.infiniteHealthActive ? 999999 : maxHealth;

                            // health is private now; set it directly so it works even if the RPC rejects a local call
                            var healthField = healthType.GetField("health", flags);
                            if (healthField != null) healthField.SetValue(playerHealthInstance, newHealth);
                            else Hax2.Log1("Campo 'health' não encontrado em playerHealth.");

                            var updateMethod = healthType.GetMethod("UpdateHealthRPC", flags);
                            if (updateMethod != null)
                            {
                                try { updateMethod.Invoke(playerHealthInstance, BuildArgs(updateMethod, newHealth, maxHealth, true, false)); }
                                catch (Exception e) { Hax2.Log1($"UpdateHealthRPC local call failed: {(e.InnerException ?? e).Message}"); }
                            }
                            Hax2.Log1($"Health set to {newHealth}/{maxHealth} (infinite: {Hax2.infiniteHealthActive}).");
                        }
                        else
                        {
                            Hax2.Log1("Campo 'playerHealth' não encontrado em playerAvatarScript.");
                        }
                    }
                    else
                    {
                        Hax2.Log1("Campo 'playerAvatarScript' não encontrado em PlayerController.");
                    }
                }
                else
                {
                    Hax2.Log1("playerControllerInstance não encontrado.");
                }
            }
            else
            {
                Hax2.Log1("Tipo PlayerController não encontrado.");
            }
        }

        // Fills the leading parameters with the given values and the rest with defaults
        // (e.g. the trailing PhotonMessageInfo), so calls survive signature changes.
        internal static object[] BuildArgs(MethodInfo method, params object[] values)
        {
            var parameters = method.GetParameters();
            var args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < values.Length) args[i] = values[i];
                else
                {
                    var type = parameters[i].ParameterType;
                    args[i] = type.IsValueType ? Activator.CreateInstance(type) : null;
                }
            }
            return args;
        }
    }
}