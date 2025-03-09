using System;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon;
using System.Linq;

namespace r.e.p.o_cheat
{

    public static class UIHelper
    {
        private static float x, y, width, height, margin, controlHeight, controlDist, nextControlY;
        private static int columns = 1;
        private static int currentColumn = 0;
        private static int currentRow = 0;

        private static float debugX, debugY, debugWidth, debugHeight, debugMargin, debugControlHeight, debugControlDist, debugNextControlY;
        private static int debugCurrentColumn = 0;
        private static int debugCurrentRow = 0;
        private static int debugColumns = 1;

        private static GUIStyle debugLabelStyle = null;

        public static bool ButtonBool(string text, bool value, float? customX = null, float? customY = null)
        {
            Rect rect = NextControlRect(customX, customY);
            string displayText = $"{text} {(value ? "✔" : " ")}";
            GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, normal = { textColor = value ? Color.green : Color.red } };
            return GUI.Button(rect, displayText, style) ? !value : value;
        }

        public static bool Checkbox(string text, bool value, float? customX = null, float? customY = null)
        {
            return GUI.Toggle(NextControlRect(customX, customY), value, text);
        }

        public static void Begin(string text, float _x, float _y, float _width, float _height, float _margin, float _controlHeight, float _controlDist)
        {
            x = _x; y = _y; width = _width; height = _height; margin = _margin; controlHeight = _controlHeight; controlDist = _controlDist;
            nextControlY = y + margin + 60;
            GUI.Box(new Rect(x, y, width, height), text);
            ResetGrid();
        }

        public static void BeginDebugMenu(string text, float _x, float _y, float _width, float _height, float _margin, float _controlHeight, float _controlDist)
        {
            debugX = _x; debugY = _y; debugWidth = _width; debugHeight = _height; debugMargin = _margin; debugControlHeight = _controlHeight; debugControlDist = _controlDist;
            debugNextControlY = debugY + debugMargin + 30;
            GUI.Box(new Rect(debugX, debugY, debugWidth, debugHeight), text);
            if (debugLabelStyle == null)
            {
                debugLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    clipping = TextClipping.Clip,
                    fontSize = 12,
                    padding = new RectOffset(2, 2, 2, 2)
                };
            }
        }

        private static Rect NextControlRect(float? customX = null, float? customY = null)
        {
            float controlX = customX ?? (x + margin + currentColumn * ((width - (columns + 1) * margin) / columns));
            float controlY = customY ?? nextControlY;
            float controlWidth = customX == null ? ((width - (columns + 1) * margin) / columns) : width - 2 * margin;

            Rect rect = new Rect(controlX, controlY, controlWidth, controlHeight);

            if (customX == null && customY == null)
            {
                currentColumn++;
                if (currentColumn >= columns)
                {
                    currentColumn = 0;
                    currentRow++;
                    nextControlY += controlHeight + controlDist;
                }
            }

            return rect;
        }
        private static Rect NextDebugControlRect()
        {
            float controlX = debugX + debugMargin + debugCurrentColumn * (debugWidth / debugColumns);
            float controlY = debugNextControlY;
            Rect rect = new Rect(controlX, controlY, debugWidth - debugMargin * 2, debugControlHeight);
            debugCurrentColumn++;
            if (debugCurrentColumn >= debugColumns)
            {
                debugCurrentColumn = 0;
                debugCurrentRow++;
            }
            return rect;
        }
        public static bool Button(string text, float? customX = null, float? customY = null)
        {
            return GUI.Button(NextControlRect(customX, customY), text);
        }

        public static bool Button(string text, float customX, float customY, float width, float height)
        {
            Rect rect = new Rect(customX, customY, width, height);
            return GUI.Button(rect, text);
        }
        public static string MakeEnable(string text, bool state) => $"{text}{(state ? "ON" : "OFF")}";
        public static void Label(string text, float? customX = null, float? customY = null) => GUI.Label(NextControlRect(customX, customY), text);
        public static float Slider(float val, float min, float max, float? customX = null, float? customY = null) => Mathf.Round(GUI.HorizontalSlider(NextControlRect(customX, customY), val, min, max));
        public static void DebugLabel(string text)
        {
            Rect rect = NextDebugControlRect();
            float textHeight = debugLabelStyle.CalcHeight(new GUIContent(text), rect.width);
            rect.height = Mathf.Max(textHeight, debugControlHeight); 
            GUI.Label(rect, text, debugLabelStyle);
            debugNextControlY = rect.y + rect.height + 5;
        }

        public static void ResetGrid() { currentColumn = 0; currentRow = 0; nextControlY = y + margin + 60; }
        public static void ResetDebugGrid() { debugCurrentColumn = 0; debugCurrentRow = 0; debugNextControlY = debugY + debugMargin; }
    }

    public class Hax2 : MonoBehaviour
    {
        private float nextUpdateTime = 0f;
        private const float updateInterval = 10f;

        private int selectedPlayerIndex = 0;
        private List<string> playerNames = new List<string>();
        private List<object> playerList = new List<object>();
        private int selectedEnemyIndex = 0;
        private List<string> enemyNames = new List<string>();
        private List<Enemy> enemyList = new List<Enemy>();
        private float oldSliderValue = 0.5f;
        private float oldSliderValueStrength = 0.5f;
        private float sliderValue = 0.5f;
        public static float sliderValueStrength = 0.5f;
        public static float offsetESp = 0.5f;
        private bool showMenu = true;
        public static bool godModeActive = false;
        public static bool infiniteHealthActive = false;
        public static bool stamineState = false;
        public static List<DebugLogMessage> debugLogMessages = new List<DebugLogMessage>();
        private bool showDebugMenu = false;
        private Vector2 playerScrollPosition = Vector2.zero;
        private Vector2 enemyScrollPosition = Vector2.zero;

        private enum MenuCategory { Player, ESP, Combat, Misc, Enemies, Items }
        private MenuCategory currentCategory = MenuCategory.Player;

        public static float staminaRechargeDelay = 1f;
        public static float staminaRechargeRate = 1f;
        public static float oldStaminaRechargeDelay = 1f;
        public static float oldStaminaRechargeRate = 1f;

        private List<ItemTeleport.GameItem> itemList = new List<ItemTeleport.GameItem>();
        private int selectedItemIndex = 0;
        private Vector2 itemScrollPosition = Vector2.zero;
        private float lastItemListUpdateTime = 0f;
        private const float itemListUpdateInterval = 2f; 
        public void Start()
        {
            UpdateCursorState();

            DebugCheats.texture2 = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            DebugCheats.texture2.SetPixels(new[] { Color.red, Color.red, Color.red, Color.red });
            DebugCheats.texture2.Apply();

            var playerHealthType = Type.GetType("PlayerHealth, Assembly-CSharp");
            if (playerHealthType != null)
            {
                Log1("playerHealthType is not null");
                Health_Player.playerHealthInstance = FindObjectOfType(playerHealthType);
                Log1(Health_Player.playerHealthInstance != null ? "playerHealthInstance is not null" : "playerHealthInstance is null");
            }
            else
                Log1("playerHealthType is null");

            var playerMaxHealth = Type.GetType("ItemUpgradePlayerHealth, Assembly-CSharp");
            if (playerMaxHealth != null)
            {
                Health_Player.playerMaxHealthInstance = FindObjectOfType(playerMaxHealth);
                Log1("playerMaxHealth is not null");
            }
            else
                Log1("playerMaxHealth is null");
        }
        
        public void Update()
        {
            if (Time.time >= nextUpdateTime)
            {
                DebugCheats.UpdateEnemyList();
                Log1("Enemy list updated!");
                nextUpdateTime = Time.time + updateInterval;
            }
            if (Time.time - lastItemListUpdateTime > itemListUpdateInterval)
            {
                UpdateItemList();
                itemList = ItemTeleport.GetItemList();
                lastItemListUpdateTime = Time.time;
            }
            if (oldSliderValue != sliderValue)
            {
                PlayerController.RemoveSpeed(sliderValue);
                oldSliderValue = sliderValue;
            }

            if (oldSliderValueStrength != sliderValueStrength)
            {
                PlayerController.MaxStrength();
                oldSliderValueStrength = sliderValueStrength;
            }

            if (playerColor.isRandomizing)
                playerColor.colorRandomizer();

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                showMenu = !showMenu;
                Debug.Log("MENU " + showMenu);
                if (!showMenu)
                {
                    TryUnlockCamera();
                }
                UpdateCursorState();
            }
            if (Input.GetKeyDown(KeyCode.F5)) Start();
            if (Input.GetKeyDown(KeyCode.F10))
            {
                showMenu = false;
                TryUnlockCamera();
                UpdateCursorState();
                Loader.UnloadCheat();
            }
            if (Input.GetKeyDown(KeyCode.F12)) showDebugMenu = !showDebugMenu;

            for (int i = debugLogMessages.Count - 1; i >= 0; i--)
            {
                if (Time.time - debugLogMessages[i].timestamp > 3f)
                    debugLogMessages.RemoveAt(i);
            }

            if (showMenu)
            {
                TryLockCamera();
            }
        }

        private void TryLockCamera()
        {
            if (InputManager.instance != null)
            {
                Type type = typeof(InputManager);
                FieldInfo field = type.GetField("disableAimingTimer", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    float currentValue = (float)field.GetValue(InputManager.instance);
                    if (currentValue < 2f || currentValue > 10f)
                    {
                        float clampedValue = Mathf.Clamp(currentValue, 2f, 10f);
                        field.SetValue(InputManager.instance, clampedValue);
                    }
                }
                else
                {
                    Debug.LogError("Failed to find field disableAimingTimer.");
                }
            }
            else
            {
                Debug.LogWarning("InputManager.instance not found!");
            }
        }

        private void TryUnlockCamera()
        {
            if (InputManager.instance != null)
            {
                Type type = typeof(InputManager);
                FieldInfo field = type.GetField("disableAimingTimer", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    float currentValue = (float)field.GetValue(InputManager.instance);
                    field.SetValue(InputManager.instance, 0f);
                    Debug.Log("disableAimingTimer reset to 0 (menu closed).");
                }
                else
                {
                    Debug.LogError("Failed to find field disableAimingTimer.");
                }
            }
            else
            {
                Debug.LogWarning("InputManager.instance not found!");
            }
        }

        private void UpdateCursorState()
        {
            Cursor.visible = showMenu;
            Cursor.lockState = showMenu ? CursorLockMode.None : CursorLockMode.Locked;
        }
        
        private void UpdateItemList()
        {
            DebugCheats.valuableObjects.Clear();
            var valuableArray = UnityEngine.Object.FindObjectsOfType(Type.GetType("ValuableObject, Assembly-CSharp"));
            if (valuableArray != null)
            {
                DebugCheats.valuableObjects.AddRange(valuableArray);
            }

            itemList = ItemTeleport.GetItemList();
            Hax2.Log1($"Lista de itens atualizada: {itemList.Count} itens encontrados.");
        }
        private void UpdateEnemyList()
        {
            enemyNames.Clear();
            enemyList.Clear();

            DebugCheats.UpdateEnemyList(); 
            enemyList = DebugCheats.enemyList;

            foreach (var enemy in enemyList)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    string enemyName = "Enemy";
                    var enemyParent = enemy.GetComponentInParent(Type.GetType("EnemyParent, Assembly-CSharp"));
                    if (enemyParent != null)
                    {
                        var nameField = enemyParent.GetType().GetField("enemyName", BindingFlags.Public | BindingFlags.Instance);
                        enemyName = nameField?.GetValue(enemyParent) as string ?? "Enemy";
                    }

                    int health = GetEnemyHealth(enemy);
                    string healthText = health >= 0 ? $"HP: {health}" : "HP: Unknown";
                    enemyNames.Add($"{enemyName} [{healthText}]");
                }
            }

            if (enemyNames.Count == 0) enemyNames.Add("No enemies found");
        }
        private void KillSelectedEnemy()
        {
            if (selectedEnemyIndex < 0 || selectedEnemyIndex >= enemyList.Count)
            {
                Log1("Índice de inimigo inválido!");
                return;
            }

            var selectedEnemy = enemyList[selectedEnemyIndex];
            if (selectedEnemy == null)
            {
                Log1("Inimigo selecionado é nulo!");
                return;
            }

            try
            {
                var healthField = selectedEnemy.GetType().GetField("Health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (healthField != null)
                {
                    var healthComponent = healthField.GetValue(selectedEnemy);
                    if (healthComponent != null)
                    {
                        var healthType = healthComponent.GetType();
                        var hurtMethod = healthType.GetMethod("Hurt", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (hurtMethod != null)
                        {
                            hurtMethod.Invoke(healthComponent, new object[] { 9999, Vector3.zero });
                            Log1($"Inimigo {enemyNames[selectedEnemyIndex]} ferido com 9999 de dano via Hurt");
                        }
                        else
                            Log1("Método 'Hurt' não encontrado em EnemyHealth");
                    }
                    else
                        Log1("Componente EnemyHealth é nulo");
                }
                else
                    Log1("Campo 'Health' não encontrado em Enemy");

                UpdateEnemyList();
            }
            catch (Exception e)
            {
                Log1($"Erro ao matar inimigo {enemyNames[selectedEnemyIndex]}: {e.Message}");
            }
        }
        private int GetEnemyHealth(Enemy enemy)
        {
            try
            {
                var healthField = enemy.GetType().GetField("Health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (healthField == null) return -1;

                var healthComponent = healthField.GetValue(enemy);
                if (healthComponent == null) return -1;

                var healthValueField = healthComponent.GetType().GetField("health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (healthValueField == null) return -1;

                return (int)healthValueField.GetValue(healthComponent);
            }
            catch (Exception e)
            {
                Hax2.Log1($"Erro ao obter vida do inimigo: {e.Message}");
                return -1;
            }
        }
        private void UpdatePlayerList()
        {
            var fakePlayers = playerNames.Where(name => name.Contains("FakePlayer")).ToList();
            var fakePlayerCount = fakePlayers.Count;

            playerNames.Clear();
            playerList.Clear();

            var players = SemiFunc.PlayerGetList();
            foreach (var player in players)
            {
                playerList.Add(player);
                string baseName = SemiFunc.PlayerGetName(player) ?? "Unknown Player";
                bool isAlive = IsPlayerAlive(player, baseName);
                string statusText = isAlive ? "<color=green>[LIVE]</color> " : "<color=red>[DEAD]</color> ";
                playerNames.Add(statusText + baseName);
            }

            for (int i = 0; i < fakePlayerCount; i++)
            {
                playerNames.Add(fakePlayers[i]);
                playerList.Add(null);
            }

            if (playerNames.Count == 0) playerNames.Add("No player Found");
        }

        private void AddFakePlayer()
        {
            int fakePlayerId = playerNames.Count(name => name.Contains("FakePlayer")) + 1;
            string fakeName = $"<color=green>[LIVE]</color> FakePlayer{fakePlayerId}";
            playerNames.Add(fakeName);
            playerList.Add(null);
            Log1($"Added fake player: {fakeName}");
        }

        private bool IsPlayerAlive(object player, string playerName)
        {
            try
            {
                var playerHealthField = player.GetType().GetField("playerHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (playerHealthField == null) return true;

                var playerHealthInstance = playerHealthField.GetValue(player);
                if (playerHealthInstance == null) return true;

                var healthField = playerHealthInstance.GetType().GetField("health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (healthField == null) return true;

                int health = (int)healthField.GetValue(playerHealthInstance);
                return health > 0;
            }
            catch (Exception e)
            {
                Hax2.Log1($"Erro ao verificar vida de {playerName}: {e.Message}");
                return true;
            }
        }

        private void ReviveSelectedPlayer()
        {
            if (selectedPlayerIndex < 0 || selectedPlayerIndex >= playerList.Count)
            {
                Log1("Índice de jogador inválido!");
                return;
            }
            var selectedPlayer = playerList[selectedPlayerIndex];
            if (selectedPlayer == null)
            {
                Log1("Jogador selecionado é nulo!");
                return;
            }

            try
            {
               
                var playerDeathHeadField = selectedPlayer.GetType().GetField("playerDeathHead", BindingFlags.Public | BindingFlags.Instance);
                if (playerDeathHeadField != null)
                {
                    var playerDeathHeadInstance = playerDeathHeadField.GetValue(selectedPlayer);
                    if (playerDeathHeadInstance != null)
                    {
                        var inExtractionPointField = playerDeathHeadInstance.GetType().GetField("inExtractionPoint", BindingFlags.NonPublic | BindingFlags.Instance);
                        var reviveMethod = playerDeathHeadInstance.GetType().GetMethod("Revive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (inExtractionPointField != null)
                        {
                            inExtractionPointField.SetValue(playerDeathHeadInstance, true);
                            Log1("Campo 'inExtractionPoint' definido como true.");
                        }
                        if (reviveMethod != null)
                        {
                            reviveMethod.Invoke(playerDeathHeadInstance, null);
                            Log1("Método 'Revive' chamado com sucesso para: " + playerNames[selectedPlayerIndex]);
                        }
                        else
                        {
                            Log1("Método 'Revive' não encontrado!");
                        }
                    }
                    else
                    {
                        Log1("Instância de playerDeathHead não encontrada.");
                    }
                }
                else
                {
                    Log1("Campo 'playerDeathHead' não encontrado.");
                }

          
                var playerHealthField = selectedPlayer.GetType().GetField("playerHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (playerHealthField != null)
                {
                    var playerHealthInstance = playerHealthField.GetValue(selectedPlayer);
                    if (playerHealthInstance != null)
                    {
                        var healthType = playerHealthInstance.GetType();
                        var maxHealthField = healthType.GetField("maxHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var healthField = healthType.GetField("health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                        int maxHealth = maxHealthField != null ? (int)maxHealthField.GetValue(playerHealthInstance) : 100;
                        Log1($"maxHealth obtido: {maxHealth}");

                        if (healthField != null)
                        {
                          
                            healthField.SetValue(playerHealthInstance, maxHealth);
                            Log1($"Saúde definida diretamente para {maxHealth} via healthField.");
                        }
                        else
                        {
                            Log1("Campo 'health' não encontrado, tentando HealPlayer como fallback.");
                            Health_Player.HealPlayer(selectedPlayer, maxHealth, playerNames[selectedPlayerIndex]);
                        }

                        int currentHealth = healthField != null ? (int)healthField.GetValue(playerHealthInstance) : -1;
                        Log1($"Saúde atual após revive: {currentHealth}");
                    }
                    else
                    {
                        Log1("Instância de playerHealth é nula, não foi possível restaurar saúde.");
                    }
                }
                else
                {
                    Log1("Campo 'playerHealth' não encontrado, cura não realizada.");
                }
            }
            catch (Exception e)
            {
                Log1($"Erro ao reviver e curar {playerNames[selectedPlayerIndex]}: {e.Message}");
            }
        }

        private void KillSelectedPlayer()
        {
            if (selectedPlayerIndex < 0 || selectedPlayerIndex >= playerList.Count) { Log1("Índice de jogador inválido!"); return; }
            var selectedPlayer = playerList[selectedPlayerIndex];
            if (selectedPlayer == null) { Log1("Jogador selecionado é nulo!"); return; }
            try
            {
                Log1($"Tentando matar: {playerNames[selectedPlayerIndex]} | MasterClient: {PhotonNetwork.IsMasterClient}");
                var photonViewField = selectedPlayer.GetType().GetField("photonView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (photonViewField == null) { Log1("PhotonViewField não encontrado!"); return; }
                var photonView = photonViewField.GetValue(selectedPlayer) as PhotonView;
                if (photonView == null) { Log1("PhotonView não é válido!"); return; }
                var playerHealthField = selectedPlayer.GetType().GetField("playerHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (playerHealthField == null) { Log1("Campo 'playerHealth' não encontrado!"); return; }
                var playerHealthInstance = playerHealthField.GetValue(selectedPlayer);
                if (playerHealthInstance == null) { Log1("Instância de playerHealth é nula!"); return; }
                var healthType = playerHealthInstance.GetType();
                var deathMethod = healthType.GetMethod("Death", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (deathMethod == null) { Log1("Método 'Death' não encontrado!"); return; }
                deathMethod.Invoke(playerHealthInstance, null);
                Log1($"Método 'Death' chamado localmente para {playerNames[selectedPlayerIndex]}.");
                var playerAvatarField = healthType.GetField("playerAvatar", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (playerAvatarField != null)
                {
                    var playerAvatarInstance = playerAvatarField.GetValue(playerHealthInstance);
                    if (playerAvatarInstance != null)
                    {
                        var playerAvatarType = playerAvatarInstance.GetType();
                        var playerDeathMethod = playerAvatarType.GetMethod("PlayerDeath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (playerDeathMethod != null) { playerDeathMethod.Invoke(playerAvatarInstance, new object[] { -1 }); Log1($"Método 'PlayerDeath' chamado localmente para {playerNames[selectedPlayerIndex]}."); }
                        else Log1("Método 'PlayerDeath' não encontrado em PlayerAvatar!");
                    }
                    else Log1("Instância de PlayerAvatar é nula!");
                }
                else Log1("Campo 'playerAvatar' não encontrado em PlayerHealth!");
                if (PhotonNetwork.IsConnected && photonView != null)
                {
                    var maxHealthField = healthType.GetField("maxHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    int maxHealth = maxHealthField != null ? (int)maxHealthField.GetValue(playerHealthInstance) : 100;
                    Log1(maxHealthField != null ? $"maxHealth encontrado: {maxHealth}" : "Campo 'maxHealth' não encontrado, usando valor padrão: 100");
                    photonView.RPC("UpdateHealthRPC", RpcTarget.AllBuffered, new object[] { 0, maxHealth, true });
                    Log1($"RPC 'UpdateHealthRPC' enviado para todos com saúde=0, maxHealth={maxHealth}, effect=true.");
                    try { photonView.RPC("PlayerDeathRPC", RpcTarget.AllBuffered, new object[] { -1 }); Log1("Tentando RPC 'PlayerDeathRPC' para forçar morte..."); }
                    catch { Log1("RPC 'PlayerDeathRPC' não registrado, tentando alternativa..."); }
                    photonView.RPC("HurtOtherRPC", RpcTarget.AllBuffered, new object[] { 9999, Vector3.zero, false, -1 });
                    Log1("RPC 'HurtOtherRPC' enviado com 9999 de dano para garantir morte.");
                }
                else Log1("Não conectado ao Photon, morte apenas local.");
                Log1($"Tentativa de matar {playerNames[selectedPlayerIndex]} concluída.");
            }
            catch (Exception e) { Log1($"Erro ao tentar matar {playerNames[selectedPlayerIndex]}: {e.Message}"); }
        }
        private void SendSelectedPlayerToVoid()
        {
            if (selectedPlayerIndex < 0 || selectedPlayerIndex >= playerList.Count)
            {
                Log1("Índice de jogador inválido!");
                return;
            }
            var selectedPlayer = playerList[selectedPlayerIndex];
            if (selectedPlayer == null)
            {
                Log1("Jogador selecionado é nulo!");
                return;
            }

            try
            {
                Log1($"Tentando enviar {playerNames[selectedPlayerIndex]} para o void | MasterClient: {PhotonNetwork.IsMasterClient}");

                var photonViewField = selectedPlayer.GetType().GetField("photonView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (photonViewField == null)
                {
                    Log1("PhotonViewField não encontrado!");
                    return;
                }
                var photonView = photonViewField.GetValue(selectedPlayer) as PhotonView;
                if (photonView == null)
                {
                    Log1("PhotonView não é válido!");
                    return;
                }

                var playerMono = selectedPlayer as MonoBehaviour;
                if (playerMono == null)
                {
                    Log1("selectedPlayer não é um MonoBehaviour!");
                    return;
                }

                var transform = playerMono.transform;
                if (transform == null)
                {
                    Log1("Transform é nulo!");
                    return;
                }

                Vector3 voidPosition = new Vector3(0, -10, 0);
                transform.position = voidPosition;
                Log1($"Jogador {playerNames[selectedPlayerIndex]} enviado localmente para o void: {voidPosition}");

                // Sincronizar via Photon, se conectado
                if (PhotonNetwork.IsConnected && photonView != null)
                {
                    photonView.RPC("SpawnRPC", RpcTarget.AllBuffered, new object[] { voidPosition, transform.rotation });
                    Log1($"RPC 'SpawnRPC' enviado para todos com posição: {voidPosition}");
                }
                else
                {
                    Log1("Não conectado ao Photon, teleporte apenas local.");
                }
            }
            catch (Exception e)
            {
                Log1($"Erro ao enviar {playerNames[selectedPlayerIndex]} para o void: {e.Message}");
            }
        }
        public void OnGUI()
        {
            if (DebugCheats.drawEspBool || DebugCheats.drawItemEspBool || DebugCheats.drawExtractionPointEspBool || DebugCheats.drawPlayerEspBool || DebugCheats.draw3DPlayerEspBool || DebugCheats.draw3DItemEspBool) DebugCheats.DrawESP();

            GUI.Label(new Rect(10, 10, 200, 30), "D.A.R.K CHEAT | DEL - MENU");
            GUI.Label(new Rect(198, 10, 200, 30), "MADE BY Github/D4rkks");

            if (showMenu)
            {
                UIHelper.Begin("DARK Menu", 50, 50, 600, 730, 30, 30, 10);

                float tabWidth = 90f; 
                float tabHeight = 40f; 
                float spacing = 10f; 
                float totalWidth = 6 * tabWidth + 5 * spacing; 
                float startX = 50 + (600 - totalWidth) / 2f; 

                GUIStyle tabStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14, 
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };

                if (GUI.Button(new Rect(startX, 80, tabWidth, tabHeight), "Player", tabStyle)) currentCategory = MenuCategory.Player;
                if (GUI.Button(new Rect(startX + tabWidth + spacing, 80, tabWidth, tabHeight), "ESP", tabStyle)) currentCategory = MenuCategory.ESP;
                if (GUI.Button(new Rect(startX + 2 * (tabWidth + spacing), 80, tabWidth, tabHeight), "Combat", tabStyle)) currentCategory = MenuCategory.Combat;
                if (GUI.Button(new Rect(startX + 3 * (tabWidth + spacing), 80, tabWidth, tabHeight), "Misc", tabStyle)) currentCategory = MenuCategory.Misc;
                if (GUI.Button(new Rect(startX + 4 * (tabWidth + spacing), 80, tabWidth, tabHeight), "Enemies", tabStyle)) currentCategory = MenuCategory.Enemies;
                if (GUI.Button(new Rect(startX + 5 * (tabWidth + spacing), 80, tabWidth, tabHeight), "Items", tabStyle)) currentCategory = MenuCategory.Items;

                UIHelper.Label("Press F5 to reload! Press DEL to close! Press F10 to unload!", 70, 120);

                switch (currentCategory)
                {
                    case MenuCategory.Player:
                        UpdatePlayerList();
                        UIHelper.Label("Select a player:", 70, 160);
    
                          playerScrollPosition = GUI.BeginScrollView(new Rect(70, 180, 540, 150), playerScrollPosition, new Rect(0, 0, 520, playerNames.Count * 35), false, true);
                        for (int i = 0; i < playerNames.Count; i++)
                        {
                            if (i == selectedPlayerIndex) GUI.color = Color.white;
                            else GUI.color = Color.gray;
                            if (GUI.Button(new Rect(0, i * 35, 520, 30), playerNames[i])) selectedPlayerIndex = i;
                            GUI.color = Color.white;
                        }
                        GUI.EndScrollView();

                        if (UIHelper.Button("Heal Player", 70, 340))
                        {
                            if (selectedPlayerIndex >= 0 && selectedPlayerIndex < playerList.Count)
                            {
                                Health_Player.HealPlayer(playerList[selectedPlayerIndex], 50, playerNames[selectedPlayerIndex]);
                                Hax2.Log1($"Player {playerNames[selectedPlayerIndex]} healed.");
                            }
                            else
                            {
                                Hax2.Log1("Nenhum jogador válido selecionado para curar!");
                            }
                        }
                        if (UIHelper.Button("Damage Player", 70, 380))
                        {
                            if (selectedPlayerIndex >= 0 && selectedPlayerIndex < playerList.Count)
                            {
                                Health_Player.DamagePlayer(playerList[selectedPlayerIndex], 1, playerNames[selectedPlayerIndex]);
                                Hax2.Log1($"Player {playerNames[selectedPlayerIndex]} damaged.");
                            }
                            else
                            {
                                Hax2.Log1("Nenhum jogador válido selecionado para causar dano!");
                            }
                        }
                        bool newHealState = UIHelper.ButtonBool("Toggle Infinite Health", infiniteHealthActive, 70, 420);
                        if (newHealState != infiniteHealthActive) { infiniteHealthActive = newHealState; Health_Player.MaxHealth(); }
                        bool newStaminaState = UIHelper.ButtonBool("Toggle Infinite Stamina", stamineState, 70, 460);
                        if (newStaminaState != stamineState) { stamineState = newStaminaState; PlayerController.MaxStamina(); Hax2.Log1("God mode toggled: " + stamineState); }
                        bool newGodModeState = UIHelper.ButtonBool("Toggle God Mode", godModeActive, 70, 500);
                        if (newGodModeState != godModeActive) { PlayerController.GodMode(); godModeActive = newGodModeState; Hax2.Log1("God mode toggled: " + godModeActive); }

                        UIHelper.Label("Speed Value " + sliderValue, 70, 540);
                        oldSliderValue = sliderValue;
                        sliderValue = UIHelper.Slider(sliderValue, 1f, 30f, 70, 560);

                        UIHelper.Label("Strength Value: " + sliderValueStrength, 70, 590);
                        oldSliderValueStrength = sliderValueStrength;
                        sliderValueStrength = UIHelper.Slider(sliderValueStrength, 1f, 30f, 70, 610);

                        // Controles para Stamina Recharge
                        UIHelper.Label("Stamina Recharge Delay: " + Hax2.staminaRechargeDelay, 70, 640);
                        Hax2.staminaRechargeDelay = UIHelper.Slider(Hax2.staminaRechargeDelay, 0f, 2f, 70, 660);

                        UIHelper.Label("Stamina Recharge Rate: " + Hax2.staminaRechargeRate, 70, 690);
                        Hax2.staminaRechargeRate = UIHelper.Slider(Hax2.staminaRechargeRate, 0.1f, 20f, 70, 710);

                        if (Hax2.staminaRechargeDelay != oldStaminaRechargeDelay || Hax2.staminaRechargeRate != oldStaminaRechargeRate)
                        {
                            PlayerController.DecreaseStaminaRechargeDelay(Hax2.staminaRechargeDelay, Hax2.staminaRechargeRate);
                            Hax2.Log1($"Stamina recharge updated: Delay={Hax2.staminaRechargeDelay}x, Rate={Hax2.staminaRechargeRate}x");
                            oldStaminaRechargeDelay = Hax2.staminaRechargeDelay;
                            oldStaminaRechargeRate = Hax2.staminaRechargeRate;
                        }
                        break;

                    case MenuCategory.ESP:
                        DebugCheats.drawEspBool = UIHelper.Checkbox("Enemy ESP", DebugCheats.drawEspBool, 70, 160);

                        if (DebugCheats.drawEspBool)
                        {
                            DebugCheats.showEnemyNames = UIHelper.Checkbox("Show Enemy Names", DebugCheats.showEnemyNames, 100, 190);
                            DebugCheats.showEnemyDistance = UIHelper.Checkbox("Show Enemy Distance", DebugCheats.showEnemyDistance, 100, 220);
                        }

                        DebugCheats.drawItemEspBool = UIHelper.Checkbox("Item ESP", DebugCheats.drawItemEspBool, 70, 260);

                        if (DebugCheats.drawItemEspBool)
                        {
                            DebugCheats.showItemNames = UIHelper.Checkbox("Show Item Names", DebugCheats.showItemNames, 100, 290);
                            DebugCheats.showItemDistance = UIHelper.Checkbox("Show Item Distance", DebugCheats.showItemDistance, 100, 320);
                            DebugCheats.showItemValue = UIHelper.Checkbox("Show Item Value", DebugCheats.showItemValue, 100, 350);
                            DebugCheats.draw3DItemEspBool = UIHelper.Checkbox("3D Item ESP", DebugCheats.draw3DItemEspBool, 100, 380);
                        }

                        DebugCheats.drawExtractionPointEspBool = UIHelper.Checkbox("Extraction ESP", DebugCheats.drawExtractionPointEspBool, 70, 410);

                        if (DebugCheats.drawExtractionPointEspBool)
                        {
                            DebugCheats.showExtractionNames = UIHelper.Checkbox("Show Extraction Names", DebugCheats.showExtractionNames, 100, 440);
                            DebugCheats.showExtractionDistance = UIHelper.Checkbox("Show Extraction Distance", DebugCheats.showExtractionDistance, 100, 470);
                        }

                        DebugCheats.drawPlayerEspBool = UIHelper.Checkbox("2D Player ESP", DebugCheats.drawPlayerEspBool, 70, 500);
                        DebugCheats.draw3DPlayerEspBool = UIHelper.Checkbox("3D Player ESP", DebugCheats.draw3DPlayerEspBool, 70, 530);

                        if (DebugCheats.drawPlayerEspBool || DebugCheats.draw3DPlayerEspBool)
                        {
                            DebugCheats.showPlayerNames = UIHelper.Checkbox("Show Player Names", DebugCheats.showPlayerNames, 100, 560);
                            DebugCheats.showPlayerDistance = UIHelper.Checkbox("Show Player Distance", DebugCheats.showPlayerDistance, 100, 590);
                            DebugCheats.showPlayerHP = UIHelper.Checkbox("Show Player HP", DebugCheats.showPlayerHP, 100, 620);
                        }
                        break;

                    case MenuCategory.Combat:
                        // ... (Código de Combat inalterado)
                        UpdatePlayerList();
                        UIHelper.Label("Select a player:", 70, 160);

                        playerScrollPosition = GUI.BeginScrollView(new Rect(70, 180, 540, 200), playerScrollPosition, new Rect(0, 0, 520, playerNames.Count * 35), false, true);
                        for (int i = 0; i < playerNames.Count; i++)
                        {
                            if (i == selectedPlayerIndex) GUI.color = Color.white;
                            else GUI.color = Color.gray;
                            if (GUI.Button(new Rect(0, i * 35, 520, 30), playerNames[i])) selectedPlayerIndex = i;
                            GUI.color = Color.white;
                        }
                        GUI.EndScrollView();

                        if (UIHelper.Button("Revive", 70, 390)) { ReviveSelectedPlayer(); Hax2.Log1("Player revived: " + playerNames[selectedPlayerIndex]); }
                        if (UIHelper.Button("Kill Selected Player", 70, 430)) { KillSelectedPlayer(); Hax2.Log1("Tentativa de matar o jogador selecionado realizada."); }
                        if (UIHelper.Button("Send Player To Void", 70, 470)) SendSelectedPlayerToVoid();
                        if (UIHelper.Button("Teleport Player To Me", 70, 510)) { Teleport.TeleportPlayerToMe(selectedPlayerIndex, playerList, playerNames); Hax2.Log1($"Teleportado {playerNames[selectedPlayerIndex]} até você."); }
                        if (UIHelper.Button("Teleport Me To Player", 70, 550)) { Teleport.TeleportMeToPlayer(selectedPlayerIndex, playerList, playerNames); Hax2.Log1($"Teleportado você até {playerNames[selectedPlayerIndex]}."); }
                        break;

                    case MenuCategory.Misc:
                        if (UIHelper.Button("Spawn Money", 70, 160))
                        {
                            Hax2.Log1("Botão 'Spawn Money' clicado!");
                            GameObject localPlayer = DebugCheats.GetLocalPlayer();
                            if (localPlayer == null)
                            {
                                Hax2.Log1("Jogador local não encontrado!");
                                return;
                            }
                            Vector3 targetPosition = localPlayer.transform.position + Vector3.up * 1.5f;
                            transform.position = targetPosition;
                            ItemSpawner.SpawnItem(targetPosition);
                            Hax2.Log1("Money spawned.");
                        }
                        bool newPlayerColorState = UIHelper.ButtonBool("RGB Player", playerColor.isRandomizing, 70, 200);
                        if (newPlayerColorState != playerColor.isRandomizing)
                        {
                            playerColor.isRandomizing = newPlayerColorState;
                            Hax2.Log1("Randomize toggled: " + playerColor.isRandomizing);
                        }

                        break;

                    case MenuCategory.Enemies: // Nova aba "Enemies"
                        UpdateEnemyList();
                        UIHelper.Label("Select an enemy:", 70, 160);

                        enemyScrollPosition = GUI.BeginScrollView(new Rect(70, 180, 540, 200), enemyScrollPosition, new Rect(0, 0, 520, enemyNames.Count * 35), false, true);
                        for (int i = 0; i < enemyNames.Count; i++)
                        {
                            if (i == selectedEnemyIndex) GUI.color = Color.white;
                            else GUI.color = Color.gray;
                            if (GUI.Button(new Rect(0, i * 35, 520, 30), enemyNames[i])) selectedEnemyIndex = i;
                            GUI.color = Color.white;
                        }
                        GUI.EndScrollView();

                        if (UIHelper.Button("Kill Selected Enemy", 70, 390))
                        {
                            KillSelectedEnemy();
                            Hax2.Log1($"Tentativa de matar o inimigo selecionado realizada: {enemyNames[selectedEnemyIndex]}");
                        }
                        if (UIHelper.Button("Kill All Enemies", 70, 430))
                        {
                            DebugCheats.KillAllEnemies();
                            Hax2.Log1("Tentativa de matar todos os inimigos realizada.");
                        }
                        break;
                    case MenuCategory.Items:
                        UIHelper.Label("Select an item:", 70, 160);

                        itemScrollPosition = GUI.BeginScrollView(new Rect(70, 180, 540, 200), itemScrollPosition, new Rect(0, 0, 520, itemList.Count * 35), false, true);
                        for (int i = 0; i < itemList.Count; i++)
                        {
                            if (i == selectedItemIndex) GUI.color = Color.white;
                            else GUI.color = Color.gray;
                            if (GUI.Button(new Rect(0, i * 35, 520, 30), $"{itemList[i].Name} [Value: {itemList[i].Value}$]"))
                            {
                                selectedItemIndex = i;
                            }
                            GUI.color = Color.white;
                        }
                        GUI.EndScrollView();

                        if (UIHelper.Button("Teleport Item to Me", 70, 390))
                        {
                            ItemTeleport.TeleportItemToMe(itemList[selectedItemIndex]);
                            Hax2.Log1($"Teleported item: {itemList[selectedItemIndex].Name}");
                        }
                        break;
                }
            }

            if (showDebugMenu)
            {
                UIHelper.ResetDebugGrid();
                UIHelper.BeginDebugMenu("Debug Log", 600, 50, 500, 500, 30, 30, 10);
                UIHelper.Label("Press F12 to close debug log", 630, 70);
                foreach (var logMessage in debugLogMessages)
                {
                    if (!string.IsNullOrEmpty(logMessage.message)) UIHelper.DebugLabel(logMessage.message);
                }
            }
        }

        public static void Log1(string message) => debugLogMessages.Add(new DebugLogMessage(message, Time.time));

        public class DebugLogMessage
        {
            public string message;
            public float timestamp;
            public DebugLogMessage(string msg, float time) { message = msg; timestamp = time; }
        }
    }
}