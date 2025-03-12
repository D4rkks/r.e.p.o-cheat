using System;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
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
        private static GUIStyle sliderStyle;
        private static GUIStyle thumbStyle;

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

        public static void Begin(string text, float _x, float _y, float _width, float _height, float InstructionHeight, float _controlHeight, float _controlDist)
        {
            x = _x; y = _y; width = _width; height = _height; margin = InstructionHeight; controlHeight = _controlHeight; controlDist = _controlDist;
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

        public static void InitSliderStyles()
        {
            // Estilo personalizado para o slider
            if (sliderStyle == null)
            {
                sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
                {
                    normal = { background = MakeSolidBackground(new Color(0.7f, 0.7f, 0.7f), 1f) },
                    hover = { background = MakeSolidBackground(new Color(0.8f, 0.8f, 0.8f), 1f) },
                    active = { background = MakeSolidBackground(new Color(0.9f, 0.9f, 0.9f), 1f) }
                };
            }
            if (thumbStyle == null)
            {
                thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
                {
                    normal = { background = MakeSolidBackground(Color.white, 1f) },
                    hover = { background = MakeSolidBackground(new Color(0.9f, 0.9f, 0.9f), 1f) },
                    active = { background = MakeSolidBackground(Color.green, 1f) }
                };
            }
        }

        public static string MakeEnable(string text, bool state) => $"{text}{(state ? "ON" : "OFF")}";
        public static void Label(string text, float? customX = null, float? customY = null) => GUI.Label(NextControlRect(customX, customY), text);
        public static float Slider(float val, float min, float max, float? customX = null, float? customY = null)
        {
            Rect rect = NextControlRect(customX, customY);

            return Mathf.Round(GUI.HorizontalSlider(rect, val, min, max, sliderStyle, thumbStyle));
        }
        private static Texture2D MakeSolidBackground(Color color, float alpha)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(color.r, color.g, color.b, alpha));
            texture.Apply();
            return texture;
        }
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

        private GUIStyle menuStyle;
        private bool initialized = false;
        private static Dictionary<Color, Texture2D> solidTextures = new Dictionary<Color, Texture2D>();

        private enum MenuCategory { Player, ESP, Combat, Misc, Enemies, Items, Hotkeys }
        private MenuCategory currentCategory = MenuCategory.Player;

        public static float staminaRechargeDelay = 1f;
        public static float staminaRechargeRate = 1f;
        public static float oldStaminaRechargeDelay = 1f;
        public static float oldStaminaRechargeRate = 1f;

        public static float jumpForce = 1f;
        public static float customGravity = 1f;
        public static int extraJumps = 1;
        public static float flashlightIntensity = 1f;
        public static float crouchDelay = 1f;
        public static float crouchSpeed = 1f;
        public static float grabRange = 1f;
        public static float throwStrength = 1f;
        public static float slideDecay = 1f;

        public static float OldflashlightIntensity = 1f;
        public static float OldcrouchDelay = 1f;
        public static float OldjumpForce = 1f;
        public static float OldcustomGravity = 1f;
        public static float OldextraJumps = 1f;
        public static float OldcrouchSpeed = 1f;
        public static float OldgrabRange = 1f;
        public static float OldthrowStrength = 1f;
        public static float OldslideDecay = 1f;

        private List<ItemTeleport.GameItem> itemList = new List<ItemTeleport.GameItem>();
        private int selectedItemIndex = 0;
        private Vector2 itemScrollPosition = Vector2.zero;
        private float lastItemListUpdateTime = 0f;
        private const float itemListUpdateInterval = 2f;
        private bool isDragging = false;
        private Vector2 dragOffset;
        private float menuX = 50f;
        private float menuY = 50f;
        private const float titleBarHeight = 30f;

        private Dictionary<KeyCode, Action> hotkeyBindings = new Dictionary<KeyCode, Action>();
        private List<HotkeyAction> availableActions = new List<HotkeyAction>();
        private int selectedHotkeySlot = 0;
        private bool configuringHotkey = false;
        private KeyCode[] defaultHotkeys = new KeyCode[12];
        private KeyCode pendingHotkey = KeyCode.None;
        private Vector2 actionSelectorScroll = Vector2.zero;
        private bool showingActionSelector = false;
        private int currentHotkeySlot = -1;
        private KeyCode currentHotkeyKey = KeyCode.None;
        private HashSet<KeyCode> reservedKeys = new HashSet<KeyCode> {
        KeyCode.Delete,
        KeyCode.F5,
        KeyCode.F10,
        KeyCode.F12 
    };

        private float actionSelectorX = 300f;
        private float actionSelectorY = 200f;
        private bool isDraggingActionSelector = false;
        private Vector2 dragOffsetActionSelector;
        private GUIStyle overlayDimStyle;
        private GUIStyle actionSelectorBoxStyle;

        private static bool cursorStateInitialized = false;

        public void Start()
        {
            menuStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeSolidBackground(new Color(0.21f, 0.21f, 0.21f), 0.7f) },
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 10, 10),
                border = new RectOffset(5, 5, 5, 5)
            };

            overlayDimStyle = new GUIStyle();
            overlayDimStyle.normal.background = MakeSolidBackground(new Color(0f, 0f, 0f, 0.5f), 0.5f);

            actionSelectorBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeSolidBackground(new Color(0.25f, 0.25f, 0.25f), 0.95f) },
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 10, 10)
            };

            UIHelper.InitSliderStyles();
            UpdateCursorState();
            InitializeHotkeyActions();

            DebugCheats.texture2 = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            DebugCheats.texture2.SetPixels(new[] { Color.red, Color.red, Color.red, Color.red });
            DebugCheats.texture2.Apply();

            var playerHealthType = Type.GetType("PlayerHealth, Assembly-CSharp");
            if (playerHealthType != null)
            {
                Log1("playerHealthType não é null");
                Health_Player.playerHealthInstance = FindObjectOfType(playerHealthType);
                Log1(Health_Player.playerHealthInstance != null ? "playerHealthInstance não é null" : "playerHealthInstance null");
            }
            else Log1("playerHealthType null");

            var playerMaxHealth = Type.GetType("ItemUpgradePlayerHealth, Assembly-CSharp");
            if (playerMaxHealth != null)
            {
                Health_Player.playerMaxHealthInstance = FindObjectOfType(playerMaxHealth);
                Log1("playerMaxHealth não é null");
            }
            else Log1("playerMaxHealth null");
        }

        public void Update()
        {
            Strength.UpdateStrength();

            // Limit update frequency to prevent lag
            if (Time.time >= nextUpdateTime)
            {
                DebugCheats.UpdateEnemyList();
                Log1("Lista de inimigos atualizada!");
                nextUpdateTime = Time.time + updateInterval;
            }

            // Reduce item list updates from every frame to every 5 seconds
            if (Time.time - lastItemListUpdateTime > 5f)
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
                Strength.MaxStrength();
                oldSliderValueStrength = sliderValueStrength;
            }

            if (playerColor.isRandomizing)
            {
                playerColor.colorRandomizer();
            }

            // Prevent excessive logging by adding a cooldown
            if (Time.time - lastItemListUpdateTime > 10f)
            {
                Log1($"Item list contains {itemList.Count} items.");
                lastItemListUpdateTime = Time.time;
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                showMenu = !showMenu;
                Debug.Log("MENU " + showMenu);
                if (!showMenu) TryUnlockCamera();
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

            if (Input.GetKeyDown(KeyCode.F12))
            {
                showDebugMenu = !showDebugMenu;
            }

            // Remove outdated logs efficiently
            debugLogMessages.RemoveAll(msg => Time.time - msg.timestamp > 3f);

            if (configuringHotkey)
            {
                foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key) && key != KeyCode.Escape)
                    {
                        if (reservedKeys.Contains(key))
                        {
                            // key is reserved, show warning in the debug log as users already have a red forewarning in GUI
                            Log1($"Cannot assign {key} as hotkey - it's reserved for cheat operation!");
                            configuringHotkey = false;
                            pendingHotkey = KeyCode.None;
                        }
                        else
                        {
                            pendingHotkey = key;
                            configuringHotkey = false;
                            Log1("hotkey set to: " + pendingHotkey);
                        }
                        break;
                    }
                    else if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        configuringHotkey = false;
                        pendingHotkey = KeyCode.None;
                        Log1("hotkey configuration canceled");
                        break;
                    }
                }
            }
            else
            {
                foreach (var kvp in hotkeyBindings)
                {
                    if (Input.GetKeyDown(kvp.Key) && kvp.Value != null)
                    {
                        kvp.Value.Invoke();
                        break;
                    }
                }
            }

            debugLogMessages.RemoveAll(msg => Time.time - msg.timestamp > 3f);

            if (showMenu) TryLockCamera();
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
                else Debug.LogError("Failed to find field disableAimingTimer.");
            }
            else Debug.LogWarning("InputManager.instance not found!");
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
                else Debug.LogError("Failed to find field disableAimingTimer.");
            }
            else Debug.LogWarning("InputManager.instance not found!");
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

            var playerDeathHeadArray = UnityEngine.Object.FindObjectsOfType(Type.GetType("PlayerDeathHead, Assembly-CSharp"));
            if (playerDeathHeadArray != null)
            {
                DebugCheats.valuableObjects.AddRange(playerDeathHeadArray);
            }

            itemList = ItemTeleport.GetItemList();
            Hax2.Log1($"Lista de itens atualizada: {itemList.Count} itens encontrados (incluindo ValuableObject e PlayerDeathHead).");
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

        private void TeleportEnemyToMe()
        {
            if (selectedEnemyIndex < 0 || selectedEnemyIndex >= enemyList.Count)
            {
                Log1($"Índice de inimigo inválido! selectedEnemyIndex={selectedEnemyIndex}, enemyList.Count={enemyList.Count}");
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
                GameObject localPlayer = DebugCheats.GetLocalPlayer();
                if (localPlayer == null)
                {
                    Log1("Jogador local não encontrado!");
                    return;
                }

                Vector3 forwardDirection = localPlayer.transform.forward;
                Vector3 targetPosition = localPlayer.transform.position + forwardDirection * 1f + Vector3.up * 1.5f;

                var photonView = selectedEnemy.GetComponent<PhotonView>();
                if (PhotonNetwork.IsConnected && photonView != null && !photonView.IsMine)
                {
                    photonView.RequestOwnership();
                    Log1($"Solicitada posse do inimigo {enemyNames[selectedEnemyIndex]} para garantir controle local.");
                }

                var navMeshAgentField = selectedEnemy.GetType().GetField("NavMeshAgent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                object navMeshAgent = null;
                if (navMeshAgentField != null)
                {
                    navMeshAgent = navMeshAgentField.GetValue(selectedEnemy);
                    if (navMeshAgent != null)
                    {
                        var enabledProperty = navMeshAgent.GetType().GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
                        if (enabledProperty != null)
                        {
                            enabledProperty.SetValue(navMeshAgent, false);
                            Log1($"NavMeshAgent de {enemyNames[selectedEnemyIndex]} desativado para evitar movimento imediato.");
                        }
                    }
                }

                selectedEnemy.transform.position = targetPosition;
                Log1($"Inimigo {enemyNames[selectedEnemyIndex]} teleportado localmente para {targetPosition}");

                Vector3 currentPosition = selectedEnemy.transform.position;
                Log1($"Posição atual do inimigo após teleporte: {currentPosition}");

                if (PhotonNetwork.IsConnected && photonView != null)
                {
                    var enemyType = selectedEnemy.GetType();
                    var teleportMethod = enemyType.GetMethod("EnemyTeleported", BindingFlags.Public | BindingFlags.Instance);
                    if (teleportMethod != null)
                    {
                        teleportMethod.Invoke(selectedEnemy, new object[] { targetPosition });
                        Log1($"Inimigo {enemyNames[selectedEnemyIndex]} teleportado via EnemyTeleported para sincronização multiplayer.");
                    }
                    else
                    {
                        Log1("Método 'EnemyTeleported' não encontrado, sincronização pode não ocorrer.");
                    }
                }

                if (navMeshAgent != null) StartCoroutine(ReEnableNavMeshAgent(navMeshAgent, 2f));

                var enemyGameObject = selectedEnemy.GetComponent<GameObject>();
                if (enemyGameObject == null) enemyGameObject = ((MonoBehaviour)selectedEnemy).gameObject;
                if (enemyGameObject != null)
                {
                    enemyGameObject.SetActive(false);
                    enemyGameObject.SetActive(true);
                    Log1($"Inimigo {enemyNames[selectedEnemyIndex]} reativado para forçar renderização.");
                }
                else
                {
                    Log1($"GameObject do inimigo {enemyNames[selectedEnemyIndex]} não encontrado para re-renderização.");
                }

                UpdateEnemyList();
                Log1($"Teleporte de {enemyNames[selectedEnemyIndex]} concluído.");
            }
            catch (Exception e)
            {
                Log1($"Erro ao teleportar inimigo {enemyNames[selectedEnemyIndex]}: {e.Message}");
            }
        }

        private System.Collections.IEnumerator ReEnableNavMeshAgent(object navMeshAgent, float delay)
        {
            yield return new WaitForSeconds(delay);
            var enabledProperty = navMeshAgent.GetType().GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
            if (enabledProperty != null)
            {
                enabledProperty.SetValue(navMeshAgent, true);
                Log1("NavMeshAgent reativado após teleporte.");
            }
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

        private void ActionSelectorWindow(int windowID)
        {
            if (GUI.Button(new Rect(370, 5, 20, 20), "X"))
            {
                showingActionSelector = false;
            }

            GUI.DragWindow(new Rect(0, 0, 400, 30));

            Rect scrollViewRect = new Rect(10, 35, 380, 355);
            Rect contentRect = new Rect(0, 0, 360, availableActions.Count * 35);
            actionSelectorScroll = GUI.BeginScrollView(scrollViewRect, actionSelectorScroll, contentRect);

            for (int i = 0; i < availableActions.Count; i++)
            {
                Rect actionRect = new Rect(0, i * 35, 340, 30);
                if (GUI.Button(actionRect, availableActions[i].Name))
                {
                    hotkeyBindings[currentHotkeyKey] = availableActions[i].Action;
                    Log1("assigned " + availableActions[i].Name + " to " + currentHotkeyKey);
                    showingActionSelector = false;
                }

                if (actionRect.Contains(Event.current.mousePosition))
                {
                    Rect tooltipRect = new Rect(Event.current.mousePosition.x + 15, Event.current.mousePosition.y, 200, 30);
                    GUI.Label(tooltipRect, availableActions[i].Description);
                }
            }

            GUI.EndScrollView();
        }


        private class HotkeyAction
        {
            public string Name { get; set; }
            public Action Action { get; set; }
            public string Description { get; set; }
            public HotkeyAction(string name, Action action, string description = "")
            {
                Name = name;
                Action = action;
                Description = description;
            }
        }

        private void ShowActionSelector(int slotIndex, KeyCode key)
        {
            showingActionSelector = true;
            currentHotkeySlot = slotIndex;
            currentHotkeyKey = key;
        }

        private void InitializeHotkeyActions()
        {
            availableActions.Add(new HotkeyAction("God Mode", () => {
                bool newGodModeState = !godModeActive;
                PlayerController.GodMode();
                godModeActive = newGodModeState;
                Log1("god mode toggled: " + godModeActive);
            }, "toggles god mode on/off"));

            availableActions.Add(new HotkeyAction("Infinite Health", () => {
                bool newHealState = !infiniteHealthActive;
                infiniteHealthActive = newHealState;
                Health_Player.MaxHealth();
                Log1("infinite health toggled: " + infiniteHealthActive);
            }, "toggles infinite health on/off"));

            availableActions.Add(new HotkeyAction("Infinite Stamina", () => {
                bool newStaminaState = !stamineState;
                stamineState = newStaminaState;
                PlayerController.MaxStamina();
                Log1("infinite stamina toggled: " + stamineState);
            }, "toggles infinite stamina on/off"));

            availableActions.Add(new HotkeyAction("RGB Player", () => {
                playerColor.isRandomizing = !playerColor.isRandomizing;
                Log1("rgb player toggled: " + playerColor.isRandomizing);
            }, "toggles rgb player effect"));

            availableActions.Add(new HotkeyAction("Spawn Money", () => {
                GameObject localPlayer = DebugCheats.GetLocalPlayer();
                if (localPlayer != null)
                {
                    Vector3 targetPosition = localPlayer.transform.position + Vector3.up * 1.5f;
                    transform.position = targetPosition;
                    ItemSpawner.SpawnItem(targetPosition);
                    Log1("money spawned.");
                }
                else
                {
                    Log1("local player not found!");
                }
            }, "spawns money at your position"));

            availableActions.Add(new HotkeyAction("Kill All Enemies", () => {
                DebugCheats.KillAllEnemies();
                Log1("all enemies killed.");
            }, "kills all enemies on the map"));

            availableActions.Add(new HotkeyAction("Enemy ESP Toggle", () => {
                DebugCheats.drawEspBool = !DebugCheats.drawEspBool;
                Log1("enemy esp toggled: " + DebugCheats.drawEspBool);
            }, "toggles enemy esp on/off"));

            availableActions.Add(new HotkeyAction("Item ESP Toggle", () => {
                DebugCheats.drawItemEspBool = !DebugCheats.drawItemEspBool;
                Log1("item esp toggled: " + DebugCheats.drawItemEspBool);
            }, "toggles item esp on/off"));

            availableActions.Add(new HotkeyAction("Player ESP Toggle", () => {
                DebugCheats.drawPlayerEspBool = !DebugCheats.drawPlayerEspBool;
                Log1("player esp toggled: " + DebugCheats.drawPlayerEspBool);
            }, "toggles player esp on/off"));

            availableActions.Add(new HotkeyAction("Heal Self", () => {
                GameObject localPlayer = DebugCheats.GetLocalPlayer();
                if (localPlayer != null)
                {
                    Health_Player.HealPlayer(localPlayer, 100, "Self");
                    Log1("healed self by 100 hp.");
                }
                else
                {
                    Log1("local player not found!");
                }
            }, "heals yourself by 100 hp"));

            availableActions.Add(new HotkeyAction("Max Speed", () => {
                sliderValue = 30f;
                PlayerController.RemoveSpeed(sliderValue);
                Log1("speed set to maximum (30)");
            }, "sets speed to maximum value"));

            availableActions.Add(new HotkeyAction("Normal Speed", () => {
                sliderValue = 5f;
                PlayerController.RemoveSpeed(sliderValue);
                Log1("speed set to normal (5)");
            }, "sets speed to normal value"));

            for (int i = 0; i < defaultHotkeys.Length; i++)
            {
                defaultHotkeys[i] = KeyCode.None;
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
                        else Log1("Método 'Revive' não encontrado!");
                    }
                    else Log1("Instância de playerDeathHead não encontrada.");
                }
                else Log1("Campo 'playerDeathHead' não encontrado.");

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
                            Log1("Campo 'health' não encontrado, tentando HealPlayer.");
                            Health_Player.HealPlayer(selectedPlayer, maxHealth, playerNames[selectedPlayerIndex]);
                        }

                        int currentHealth = healthField != null ? (int)healthField.GetValue(playerHealthInstance) : -1;
                        Log1($"Saúde atual após revive: {currentHealth}");
                }
                    else Log1("Instância de playerHealth é nula, não foi possível restaurar saúde.");
                }
                Log1("Campo 'health' não encontrado, tentando HealPlayer como fallback.");
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
            if (!initialized)
            {
                Start();
                initialized = true;
            }

            if (DebugCheats.drawEspBool || DebugCheats.drawItemEspBool || DebugCheats.drawExtractionPointEspBool ||
                DebugCheats.drawPlayerEspBool || DebugCheats.draw3DPlayerEspBool || DebugCheats.draw3DItemEspBool)
                DebugCheats.DrawESP();

            GUI.Label(new Rect(10, 10, 200, 30), "D.A.R.K CHEAT | DEL - MENU");
            GUI.Label(new Rect(198, 10, 200, 30), "MADE BY Github/D4rkks");

            // handle modal first
            if (showingActionSelector)
            {
                Rect fullOverlay = new Rect(0, 0, Screen.width, Screen.height);
                GUI.Box(fullOverlay, "", overlayDimStyle);

                Rect modalRect = new Rect(actionSelectorX, actionSelectorY, 400, 400);

                // trying to only block events that are outside the modal window
                if (Event.current.type == EventType.MouseDown ||
                    Event.current.type == EventType.MouseUp ||
                    Event.current.type == EventType.MouseDrag)
                {
                    if (!modalRect.Contains(Event.current.mousePosition))
                    {
                        // blocking outer event
                        Event.current.Use();
                    }


                modalRect = GUI.Window(12345, modalRect, ActionSelectorWindow, "", actionSelectorBoxStyle);
                actionSelectorX = modalRect.x;
                actionSelectorY = modalRect.y;
            }

            GUI.depth = 0;

                if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
                {
                    return;
                }
            }

            if (showMenu)
            {
                GUIStyle overlayStyle = new GUIStyle();
                overlayStyle.normal.background = MakeSolidBackground(Color.clear, 0f);
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, overlayStyle);

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                Rect menuRect = new Rect(menuX, menuY, 600, 730);
                Rect titleRect = new Rect(menuX, menuY, 600, titleBarHeight);

                GUI.Box(menuRect, "", menuStyle);
                UIHelper.Begin("D.A.R.K. Menu 1.1.1.1", menuX, menuY, 600, 800, 30, 30, 10);

                if (Event.current.type == EventType.MouseDown && titleRect.Contains(Event.current.mousePosition))
                {
                    isDragging = true;
                    dragOffset = Event.current.mousePosition - new Vector2(menuX, menuY);
                }
                if (Event.current.type == EventType.MouseUp) isDragging = false;
                if (isDragging && Event.current.type == EventType.MouseDrag)
                {
                    Vector2 newPosition = Event.current.mousePosition - dragOffset;
                    menuX = Mathf.Clamp(newPosition.x, 0, Screen.width - 600);
                    menuY = Mathf.Clamp(newPosition.y, 0, Screen.height - 730);
                }

                float tabWidth = 75f;
                float tabHeight = 40f;
                float spacing = 5f;
                float totalWidth = 7 * tabWidth + 6 * spacing;
                float startX = menuX + (600 - totalWidth) / 2f;

                GUIStyle tabStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white, background = MakeSolidBackground(Color.gray, 1f) },
                    hover = { textColor = Color.yellow, background = MakeSolidBackground(new Color(0.2f, 0.2f, 0.2f), 1f) },
                    active = { textColor = Color.green, background = MakeSolidBackground(Color.black, 1f) }
                };
                GUIStyle selectedTabStyle = new GUIStyle(tabStyle)
                {
                    normal = { textColor = Color.white, background = MakeSolidBackground(new Color(0.35f, 0.35f, 0.35f), 1f) }
                };

                if (GUI.Button(new Rect(startX, menuY + 30, tabWidth, tabHeight), "Player", currentCategory == MenuCategory.Player ? selectedTabStyle : tabStyle))
                    currentCategory = MenuCategory.Player;
                if (GUI.Button(new Rect(startX + (tabWidth + spacing), menuY + 30, tabWidth, tabHeight), "ESP", currentCategory == MenuCategory.ESP ? selectedTabStyle : tabStyle))
                    currentCategory = MenuCategory.ESP;
                if (GUI.Button(new Rect(startX + 2 * (tabWidth + spacing), menuY + 30, tabWidth, tabHeight), "Combat", currentCategory == MenuCategory.Combat ? selectedTabStyle : tabStyle))
                    currentCategory = MenuCategory.Combat;
                if (GUI.Button(new Rect(startX + 3 * (tabWidth + spacing), menuY + 30, tabWidth, tabHeight), "Misc", currentCategory == MenuCategory.Misc ? selectedTabStyle : tabStyle))
                    currentCategory = MenuCategory.Misc;
                if (GUI.Button(new Rect(startX + 4 * (tabWidth + spacing), menuY + 30, tabWidth, tabHeight), "Enemies", currentCategory == MenuCategory.Enemies ? selectedTabStyle : tabStyle))
                    currentCategory = MenuCategory.Enemies;
                if (GUI.Button(new Rect(startX + 5 * (tabWidth + spacing), menuY + 30, tabWidth, tabHeight), "Items", currentCategory == MenuCategory.Items ? selectedTabStyle : tabStyle))
                    currentCategory = MenuCategory.Items;
                if (GUI.Button(new Rect(startX + 6 * (tabWidth + spacing), menuY + 30, tabWidth, tabHeight), "Hotkeys", currentCategory == MenuCategory.Hotkeys ? selectedTabStyle : tabStyle))
                    currentCategory = MenuCategory.Hotkeys;

                GUIStyle instructionStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Color.white } };
                GUI.Label(new Rect(menuX + 10, menuY + 75, 580, 20), "Press F5 to reload! Press DEL to close! Press F10 to unload!", instructionStyle);

                switch (currentCategory)
                {
                    case MenuCategory.Player:
                        UpdatePlayerList();
                        UIHelper.Label("Select a player:", menuX + 30, menuY + 95);
                        playerScrollPosition = GUI.BeginScrollView(new Rect(menuX + 30, menuY + 115, 540, 150), playerScrollPosition, new Rect(0, 0, 520, playerNames.Count * 35), false, true);
                        for (int i = 0; i < playerNames.Count; i++)
                        {
                            if (i == selectedPlayerIndex) GUI.color = Color.white;
                            else GUI.color = Color.gray;
                            if (GUI.Button(new Rect(0, i * 35, 520, 30), playerNames[i])) selectedPlayerIndex = i;
                            GUI.color = Color.white;
                        }
                        GUI.EndScrollView();

                        if (UIHelper.Button("Heal Player", menuX + 30, menuY + 275))
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
                        if (UIHelper.Button("Damage Player", menuX + 30, menuY + 315))
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
                        bool newHealState = UIHelper.ButtonBool("Toggle Infinite Health", infiniteHealthActive, menuX + 30, menuY + 355);
                        if (newHealState != infiniteHealthActive) { infiniteHealthActive = newHealState; Health_Player.MaxHealth(); }
                        bool newStaminaState = UIHelper.ButtonBool("Toggle Infinite Stamina", stamineState, menuX + 30, menuY + 395);
                        if (newStaminaState != stamineState) { stamineState = newStaminaState; PlayerController.MaxStamina(); Hax2.Log1("God mode toggled: " + stamineState); }
                        bool newGodModeState = UIHelper.ButtonBool("Toggle God Mode", godModeActive, menuX + 30, menuY + 435);
                        if (newGodModeState != godModeActive) { PlayerController.GodMode(); godModeActive = newGodModeState; Hax2.Log1("God mode toggled: " + godModeActive); }

                        UIHelper.Label("Speed Value " + sliderValue, menuX + 30, menuY + 475);
                        oldSliderValue = sliderValue;
                        sliderValue = UIHelper.Slider(sliderValue, 1f, 30f, menuX + 30, menuY + 495);

                        UIHelper.Label("Strength Value: " + sliderValueStrength, menuX + 30, menuY + 515);
                        oldSliderValueStrength = sliderValueStrength;
                        sliderValueStrength = UIHelper.Slider(sliderValueStrength, 1f, 100f, menuX + 30, menuY + 535);

                        UIHelper.Label("Stamina Recharge Delay: " + Hax2.staminaRechargeDelay, menuX + 30, menuY + 565);
                        Hax2.staminaRechargeDelay = UIHelper.Slider(Hax2.staminaRechargeDelay, 0f, 10f, menuX + 30, menuY + 586);

                        UIHelper.Label("Stamina Recharge Rate: " + Hax2.staminaRechargeRate, menuX + 30, menuY + 605);
                        Hax2.staminaRechargeRate = UIHelper.Slider(Hax2.staminaRechargeRate, 1f, 20f, menuX + 30, menuY + 625);

                        if (Hax2.staminaRechargeDelay != oldStaminaRechargeDelay || Hax2.staminaRechargeRate != oldStaminaRechargeRate)
                        {
                            PlayerController.DecreaseStaminaRechargeDelay(Hax2.staminaRechargeDelay, Hax2.staminaRechargeRate);
                            Hax2.Log1($"Stamina recharge updated: Delay={Hax2.staminaRechargeDelay}x, Rate={Hax2.staminaRechargeRate}x");
                            oldStaminaRechargeDelay = Hax2.staminaRechargeDelay;
                            oldStaminaRechargeRate = Hax2.staminaRechargeRate;
                        }
                        break;

                    case MenuCategory.ESP:
                        DebugCheats.drawEspBool = UIHelper.Checkbox("Enemy ESP", DebugCheats.drawEspBool, menuX + 30, menuY + 105);
                        if (DebugCheats.drawEspBool)
                        {
                            DebugCheats.showEnemyNames = UIHelper.Checkbox("Show Enemy Names", DebugCheats.showEnemyNames, menuX + 50, menuY + 125);
                            DebugCheats.showEnemyDistance = UIHelper.Checkbox("Show Enemy Distance", DebugCheats.showEnemyDistance, menuX + 50, menuY + 155);
                        }

                        DebugCheats.drawItemEspBool = UIHelper.Checkbox("Item ESP", DebugCheats.drawItemEspBool, menuX + 30, menuY + 185);
                        if (DebugCheats.drawItemEspBool)
                        {
                            DebugCheats.showItemNames = UIHelper.Checkbox("Show Item Names", DebugCheats.showItemNames, menuX + 50, menuY + 205);
                            DebugCheats.showItemDistance = UIHelper.Checkbox("Show Item Distance", DebugCheats.showItemDistance, menuX + 50, menuY + 235);
                            DebugCheats.showItemValue = UIHelper.Checkbox("Show Item Value", DebugCheats.showItemValue, menuX + 50, menuY + 265);
                            DebugCheats.draw3DItemEspBool = UIHelper.Checkbox("3D Item ESP", DebugCheats.draw3DItemEspBool, menuX + 50, menuY + 295);
                        }

                        DebugCheats.drawExtractionPointEspBool = UIHelper.Checkbox("Extraction ESP", DebugCheats.drawExtractionPointEspBool, menuX + 30, menuY + 325);
                        if (DebugCheats.drawExtractionPointEspBool)
                        {
                            DebugCheats.showExtractionNames = UIHelper.Checkbox("Show Extraction Names", DebugCheats.showExtractionNames, menuX + 50, menuY + 355);
                            DebugCheats.showExtractionDistance = UIHelper.Checkbox("Show Extraction Distance", DebugCheats.showExtractionDistance, menuX + 50, menuY + 385);
                        }

                        DebugCheats.drawPlayerEspBool = UIHelper.Checkbox("2D Player ESP", DebugCheats.drawPlayerEspBool, menuX + 30, menuY + 420);
                        DebugCheats.draw3DPlayerEspBool = UIHelper.Checkbox("3D Player ESP", DebugCheats.draw3DPlayerEspBool, menuX + 30, menuY + 450);
                        if (DebugCheats.drawPlayerEspBool || DebugCheats.draw3DPlayerEspBool)
                        {
                            DebugCheats.showPlayerNames = UIHelper.Checkbox("Show Player Names", DebugCheats.showPlayerNames, menuX + 50, menuY + 480);
                            DebugCheats.showPlayerDistance = UIHelper.Checkbox("Show Player Distance", DebugCheats.showPlayerDistance, menuX + 50, menuY + 510);
                            DebugCheats.showPlayerHP = UIHelper.Checkbox("Show Player HP", DebugCheats.showPlayerHP, menuX + 50, menuY + 540);
                        }
                        break;

                    case MenuCategory.Combat:
                        UpdatePlayerList();
                        UIHelper.Label("Select a player:", menuX + 30, menuY + 95);
                        playerScrollPosition = GUI.BeginScrollView(new Rect(menuX + 30, menuY + 115, 540, 200), playerScrollPosition, new Rect(0, 0, 520, playerNames.Count * 35), false, true);
                        for (int i = 0; i < playerNames.Count; i++)
                        {
                            if (i == selectedPlayerIndex) GUI.color = Color.white;
                            else GUI.color = Color.gray;
                            if (GUI.Button(new Rect(0, i * 35, 520, 30), playerNames[i])) selectedPlayerIndex = i;
                            GUI.color = Color.white;
                        }
                        GUI.EndScrollView();

                        if (UIHelper.Button("Revive", menuX + 30, menuY + 330)) { ReviveSelectedPlayer(); Hax2.Log1("Player revived: " + playerNames[selectedPlayerIndex]); }
                        if (UIHelper.Button("Kill Selected Player", menuX + 30, menuY + 370)) { KillSelectedPlayer(); Hax2.Log1("Tentativa de matar o jogador selecionado realizada."); }
                        if (UIHelper.Button("Send Player To Void", menuX + 30, menuY + 410)) SendSelectedPlayerToVoid();
                        if (UIHelper.Button("Teleport Player To Me", menuX + 30, menuY + 450)) { Teleport.TeleportPlayerToMe(selectedPlayerIndex, playerList, playerNames); Hax2.Log1($"Teleportado {playerNames[selectedPlayerIndex]} até você."); }
                        if (UIHelper.Button("Teleport Me To Player", menuX + 30, menuY + 490)) { Teleport.TeleportMeToPlayer(selectedPlayerIndex, playerList, playerNames); Hax2.Log1($"Teleportado você até {playerNames[selectedPlayerIndex]}."); }
                        break;

                    case MenuCategory.Misc:
                        if (UIHelper.Button("Spawn Money", menuX + 30, menuY + 105))
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
                        bool newPlayerColorState = UIHelper.ButtonBool("RGB Player", playerColor.isRandomizing, menuX + 30, menuY + 145);
                        if (newPlayerColorState != playerColor.isRandomizing)
                        {
                            playerColor.isRandomizing = newPlayerColorState;
                            Hax2.Log1("Randomize toggled: " + playerColor.isRandomizing);
                        }

                        UIHelper.Label("Flashlight Intensity: " + Hax2.flashlightIntensity, menuX + 30, menuY + 185);
                        Hax2.flashlightIntensity = UIHelper.Slider(Hax2.flashlightIntensity, 1f, 100f, menuX + 30, menuY + 205);

                        UIHelper.Label("Crouch Delay: " + Hax2.crouchDelay, menuX + 30, menuY + 225);
                        Hax2.crouchDelay = UIHelper.Slider(Hax2.crouchDelay, 0f, 5f, menuX + 30, menuY + 245);

                        UIHelper.Label("Set Crouch Speed: " + Hax2.crouchSpeed, menuX + 30, menuY + 265);
                        Hax2.crouchSpeed = UIHelper.Slider(Hax2.crouchSpeed, 1f, 50f, menuX + 30, menuY + 285);

                        UIHelper.Label("Set Jump Force: " + Hax2.jumpForce, menuX + 30, menuY + 305);
                        Hax2.jumpForce = UIHelper.Slider(Hax2.jumpForce, 1f, 50f, menuX + 30, menuY + 326);

                        UIHelper.Label("Set Extra Jumps: " + Hax2.extraJumps, menuX + 30, menuY + 345);
                        Hax2.extraJumps = (int)UIHelper.Slider(Hax2.extraJumps, 1f, 100f, menuX + 30, menuY + 365);

                        UIHelper.Label("Set Custom Gravity: " + Hax2.customGravity, menuX + 30, menuY + 385);
                        Hax2.customGravity = UIHelper.Slider(Hax2.customGravity, -10f, 50f, menuX + 30, menuY + 405);

                        UIHelper.Label("Set Grab Range: " + Hax2.grabRange, menuX + 30, menuY + 425);
                        Hax2.grabRange = UIHelper.Slider(Hax2.grabRange, 0f, 50f, menuX + 30, menuY + 445);

                        UIHelper.Label("Set Throw Strength: " + Hax2.throwStrength, menuX + 30, menuY + 465);
                        Hax2.throwStrength = UIHelper.Slider(Hax2.throwStrength, 0f, 50f, menuX + 30, menuY + 485);

                        UIHelper.Label("Set Slide Decay: " + Hax2.slideDecay, menuX + 30, menuY + 505);
                        Hax2.slideDecay = UIHelper.Slider(Hax2.slideDecay, -10f, 50f, menuX + 30, menuY + 525);


                        if (Hax2.flashlightIntensity != OldflashlightIntensity)
                        {
                            PlayerController.SetFlashlightIntensity(Hax2.flashlightIntensity);
                            OldflashlightIntensity = Hax2.flashlightIntensity;
                        }
                        if (Hax2.crouchDelay != OldcrouchDelay)
                        {
                            PlayerController.SetCrouchDelay(Hax2.crouchDelay);
                            OldcrouchDelay = Hax2.crouchDelay;
                        }
                        if (Hax2.jumpForce != Hax2.OldjumpForce)
                        {
                            PlayerController.SetJumpForce(Hax2.jumpForce);
                            OldjumpForce = Hax2.jumpForce;
                        }
                        if (Hax2.customGravity != Hax2.OldcustomGravity)
                        {
                            PlayerController.SetCustomGravity(Hax2.customGravity);
                            OldcustomGravity = Hax2.customGravity;
                        }
                        if (Hax2.extraJumps != Hax2.OldextraJumps)
                        {
                            PlayerController.SetExtraJumps(Hax2.extraJumps);
                            OldextraJumps = Hax2.extraJumps;
                        }
                        if (Hax2.crouchSpeed != Hax2.OldcrouchSpeed)
                        {
                            PlayerController.SetCrouchSpeed(Hax2.crouchSpeed);
                            OldcrouchSpeed = Hax2.crouchSpeed;
                        }
                        if (Hax2.grabRange != Hax2.OldgrabRange)
                        {
                            PlayerController.SetGrabRange(Hax2.grabRange);
                            OldgrabRange = Hax2.grabRange;
                        }
                        if (Hax2.throwStrength != Hax2.OldthrowStrength)
                        {
                            PlayerController.SetThrowStrength(Hax2.throwStrength);
                            OldthrowStrength = Hax2.throwStrength;
                        }
                        if (Hax2.slideDecay != Hax2.OldslideDecay)
                        {
                            PlayerController.SetSlideDecay(Hax2.slideDecay);
                            OldslideDecay = Hax2.slideDecay;
                        }
                        break;

                    case MenuCategory.Enemies:
                        UpdateEnemyList();
                        UIHelper.Label("Select an enemy:", menuX + 30, menuY + 95);

                        enemyScrollPosition = GUI.BeginScrollView(new Rect(menuX + 30, menuY + 115, 540, 200), enemyScrollPosition, new Rect(0, 0, 520, enemyNames.Count * 35), false, true);
                        for (int i = 0; i < enemyNames.Count; i++)
                        {
                            if (i == selectedEnemyIndex) GUI.color = Color.white;
                            else GUI.color = Color.gray;
                            if (GUI.Button(new Rect(0, i * 35, 520, 30), enemyNames[i])) selectedEnemyIndex = i;
                            GUI.color = Color.white;
                        }
                        GUI.EndScrollView();

                        if (UIHelper.Button("Kill Selected Enemy", menuX + 30, menuY + 330))
                        {
                            KillSelectedEnemy();
                            Hax2.Log1($"Tentativa de matar o inimigo selecionado realizada: {enemyNames[selectedEnemyIndex]}");
                        }
                        if (UIHelper.Button("Kill All Enemies", menuX + 30, menuY + 370))
                        {
                            DebugCheats.KillAllEnemies();
                            Hax2.Log1("Tentativa de matar todos os inimigos realizada.");
                        }
                        if (UIHelper.Button("Teleport Enemy to Me", menuX + 30, menuY + 410))
                        {
                            TeleportEnemyToMe();
                            Hax2.Log1($"Tentativa de teleportar {enemyNames[selectedEnemyIndex]} até você realizada.");
                        }
                        break;

                    case MenuCategory.Items:
                        UIHelper.Label("Select an item:", menuX + 30, menuY + 95);

                        itemScrollPosition = GUI.BeginScrollView(new Rect(menuX + 30, menuY + 115, 540, 200), itemScrollPosition, new Rect(0, 0, 520, itemList.Count * 35), false, true);
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

                        if (UIHelper.Button("Teleport Item to Me", menuX + 30, menuY + 330))
                        {
                            if (selectedItemIndex >= 0 && selectedItemIndex < itemList.Count)
                            {
                                ItemTeleport.TeleportItemToMe(itemList[selectedItemIndex]);
                                Hax2.Log1($"Teleported item: {itemList[selectedItemIndex].Name}");
                            }
                            else
                            {
                                Hax2.Log1("Nenhum item válido selecionado para teleporte!");
                            }
                        }
                        if (UIHelper.Button("Teleport All Items to Me", menuX + 30, menuY + 370))
                        {
                            ItemTeleport.TeleportAllItemsToMe();
                            Hax2.Log1("Teleporting all items initiated.");
                        }
                        if (UIHelper.Button("Change Item Value to 10K", menuX + 30, menuY + 410))
                        {
                            if (selectedItemIndex >= 0 && selectedItemIndex < itemList.Count)
                            {
                                ItemTeleport.SetItemValue(itemList[selectedItemIndex], 10000);
                                Hax2.Log1($"Updated value: {itemList[selectedItemIndex].Value}");
                            }
                            else
                            {
                                Hax2.Log1("Nenhum item válido selecionado para alterar valor!");
                            }
                        }
                        break;

                    case MenuCategory.Hotkeys:
                        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 16,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = Color.white }
                        };
                        GUI.Label(new Rect(menuX + 70, menuY + 95, 540, 25), "Hotkey Configuration", headerStyle);

                        GUI.Label(new Rect(menuX + 70, menuY + 120, 540, 20), "How to set up a hotkey:", instructionStyle);
                        GUI.Label(new Rect(menuX + 90, menuY + 140, 540, 20), "1. Click on a key field → press desired key", instructionStyle);
                        GUI.Label(new Rect(menuX + 90, menuY + 160, 540, 20), "2. Click on action field → select function", instructionStyle);

                        GUIStyle warningStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 12,
                            normal = { textColor = Color.red }
                        };
                        GUI.Label(new Rect(menuX + 70, menuY + 185, 540, 20),
                            "System keys (DEL, F5, F10, F12) cannot be used as hotkeys", warningStyle);

                        float yPos = menuY + 215;

                        for (int i = 0; i < 12; i++)
                        {
                            KeyCode currentKey = defaultHotkeys[i];
                            string keyText = (currentKey == KeyCode.None) ? "Not Set" : currentKey.ToString();
                            string actionName = "Not assigned";

                            if (currentKey != KeyCode.None && hotkeyBindings.ContainsKey(currentKey))
                            {
                                var action = hotkeyBindings[currentKey];
                                if (action != null)
                                {
                                    for (int j = 0; j < availableActions.Count; j++)
                                    {
                                        if (availableActions[j].Action == action)
                                        {
                                            actionName = availableActions[j].Name;
                                            break;
                                        }
                                    }
                                }
                            }

                            Rect slotRect = new Rect(menuX + 30, yPos, 150, 30);
                            bool isSelected = selectedHotkeySlot == i && configuringHotkey;

                            if (GUI.Button(slotRect, isSelected ? "Press any key..." : keyText))
                            {
                                selectedHotkeySlot = i;
                                configuringHotkey = true;
                                pendingHotkey = KeyCode.None;
                                Log1("configuring hotkey for slot " + (i + 1));
                            }

                            Rect actionRect = new Rect(menuX + 190, yPos, 290, 30);
                            if (GUI.Button(actionRect, actionName))
                            {
                                selectedHotkeySlot = i;
                                if (currentKey != KeyCode.None)
                                {
                                    ShowActionSelector(i, currentKey);
                                }
                                else
                                {
                                    Log1("Please assign a key to this slot first");
                                }
                            }

                            Rect clearRect = new Rect(menuX + 490, yPos, 80, 30);
                            if (GUI.Button(clearRect, "Clear") && currentKey != KeyCode.None)
                            {
                                if (hotkeyBindings.ContainsKey(currentKey))
                                {
                                    hotkeyBindings.Remove(currentKey);
                                }
                                defaultHotkeys[i] = KeyCode.None;
                                Log1("cleared hotkey binding for slot " + (i + 1));
                            }

                            yPos += 40;
                        }

                        if (pendingHotkey != KeyCode.None)
                        {
                            KeyCode oldKey = defaultHotkeys[selectedHotkeySlot];
                            Action currentAction = null;

                            if (oldKey != KeyCode.None && hotkeyBindings.ContainsKey(oldKey))
                            {
                                currentAction = hotkeyBindings[oldKey];
                                hotkeyBindings.Remove(oldKey);
                            }

                            defaultHotkeys[selectedHotkeySlot] = pendingHotkey;

                            if (hotkeyBindings.ContainsKey(pendingHotkey))
                            {
                                Log1("Key already assigned. Reassigning.");

                                if (currentAction != null)
                                {
                                    Action existingAction = hotkeyBindings[pendingHotkey];
                                    hotkeyBindings[pendingHotkey] = currentAction;

                                    for (int i = 0; i < defaultHotkeys.Length; i++)
                                    {
                                        if (i != selectedHotkeySlot && defaultHotkeys[i] == pendingHotkey)
                                        {
                                            defaultHotkeys[i] = KeyCode.None;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (currentAction != null)
                            {
                                hotkeyBindings[pendingHotkey] = currentAction;
                            }
                            else
                            {
                                hotkeyBindings[pendingHotkey] = null;
                            }

                            pendingHotkey = KeyCode.None;
                        }
                        break;
                }
            }

            if (showDebugMenu)
            {
                UIHelper.ResetDebugGrid();
                UIHelper.BeginDebugMenu("Debug Log", 800, 50, 500, 500, 30, 30, 10);
                UIHelper.Label("Press F12 to close debug log", 830, 70);
                foreach (var logMessage in debugLogMessages)
                {
                    if (!string.IsNullOrEmpty(logMessage.message)) UIHelper.DebugLabel(logMessage.message);
                }
            }

            if (showingActionSelector)
            {
                // draw full-screen overlay and consume mouse events so main GUI is blocked
                Rect fullOverlay = new Rect(0, 0, Screen.width, Screen.height);
                GUI.Box(fullOverlay, "", overlayDimStyle);
                if (Event.current.type == EventType.MouseDown ||
                    Event.current.type == EventType.MouseUp ||
                    Event.current.type == EventType.MouseDrag)
                {
                    Event.current.Use(); // consume events
                }

                // draw modal window using GUI.Window for natural dragging
                Rect modalRect = new Rect(actionSelectorX, actionSelectorY, 400, 400);
                modalRect = GUI.Window(12345, modalRect, ActionSelectorWindow, "", actionSelectorBoxStyle);
                actionSelectorX = modalRect.x;
                actionSelectorY = modalRect.y;
            }
        }

        private static Texture2D MakeSolidBackground(Color color, float alpha)//fix
        {
            Color key = new Color(color.r, color.g, color.b, alpha);
            if (!solidTextures.ContainsKey(color))
            {
                Texture2D texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, key);
                texture.Apply();
                solidTextures[color] = texture;
            }
            return solidTextures[color];
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
