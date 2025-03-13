using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime; // Necessário para ReceiverGroup e RaiseEventOptions
using ExitGames.Client.Photon; // Necessário para SendOptions
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
namespace r.e.p.o_cheat
{
    static class DebugCheats
    {
        private static int frameCounter = 0;
        public static List<Enemy> enemyList = new List<Enemy>();
        public static List<object> valuableObjects = new List<object>();
        private static List<object> playerList = new List<object>();
        private static Camera cachedCamera;
        private static float scaleX, scaleY;
        public static Texture2D texture2;
        private static float lastUpdateTime = 0f;
        private static float lastExtractionUpdateTime = 0f;
        private const float updateInterval = 1f;
        private const float extractionUpdateInterval = 5f;
        private static GameObject localPlayer;
        private static List<ExtractionPointData> extractionPointList = new List<ExtractionPointData>();

        public static bool drawEspBool = false;
        public static bool drawItemEspBool = false;
        public static bool drawPlayerEspBool = false;
        public static bool draw3DItemEspBool = false;
        public static bool draw3DPlayerEspBool = false;
        public static bool drawExtractionPointEspBool = false;
        public static GUIStyle nameStyle;
        public static GUIStyle valueStyle;
        public static GUIStyle enemyStyle;
        public static GUIStyle healthStyle;
        public static GUIStyle distanceStyle;

        public static bool showEnemyNames = true;
        public static bool showEnemyDistance = true;
        public static bool showItemNames = true;
        public static bool showItemValue = true;
        public static bool showItemDistance = false;
        public static bool showPlayerDeathHeads = true;
        public static bool showExtractionNames = true;
        public static bool showExtractionDistance = true;
        public static bool showPlayerNames = true;
        public static bool showPlayerDistance = true;
        public static bool showPlayerHP = true;

        private static List<PlayerData> playerDataList = new List<PlayerData>();
        private static float lastPlayerUpdateTime = 0f;
        private static float playerUpdateInterval = 1f;
        private static Dictionary<int, int> playerHealthCache = new Dictionary<int, int>();
        private const float maxEspDistance = 100f;

        public class PlayerData
        {
            public object PlayerObject { get; }
            public PhotonView PhotonView { get; }
            public Transform Transform { get; }
            public bool IsAlive { get; set; }
            public string Name { get; set; }

            public PlayerData(object player)
            {
                PlayerObject = player;
                PhotonView = player.GetType().GetField("photonView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(player) as PhotonView;
                Transform = player.GetType().GetProperty("transform", BindingFlags.Public | BindingFlags.Instance)?.GetValue(player) as Transform;
                Name = (player as PlayerAvatar) != null ? (SemiFunc.PlayerGetName(player as PlayerAvatar) ?? "Unknown Player") : "Unknown Player";
                IsAlive = true;
            }
        }
        static DebugCheats()
        {
            cachedCamera = Camera.main;
            if (cachedCamera != null)
            {
                scaleX = (float)Screen.width / cachedCamera.pixelWidth;
                scaleY = (float)Screen.height / cachedCamera.pixelHeight;
            }
            UpdateLists();
            UpdateLocalPlayer();
            UpdateExtractionPointList();
            UpdatePlayerDataList();
        }
        private static void UpdatePlayerDataList()
        {
            playerDataList.Clear();
            playerHealthCache.Clear();
            var players = SemiFunc.PlayerGetList();
            if (players != null)
            {
                foreach (var player in players)
                {
                    if (player != null)
                    {
                        var data = new PlayerData(player);
                        if (data.PhotonView != null && data.Transform != null)
                        {
                            playerDataList.Add(data);
                            int health = GetPlayerHealth(player);
                            playerHealthCache[data.PhotonView.ViewID] = health;
                        }
                    }
                }
            }
            lastPlayerUpdateTime = Time.time;
            Hax2.Log1($"Lista de dados de jogadores atualizada: {playerDataList.Count} jogadores.");
        }
        private static void UpdateExtractionPointList()
        {
            extractionPointList.Clear();
            var extractionPoints = UnityEngine.Object.FindObjectsOfType(Type.GetType("ExtractionPoint, Assembly-CSharp"));
            if (extractionPoints != null)
            {
                foreach (var ep in extractionPoints)
                {
                    var extractionPoint = ep as ExtractionPoint;
                    if (extractionPoint != null && extractionPoint.gameObject.activeInHierarchy)
                    {
                        var currentStateField = extractionPoint.GetType().GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance);
                        string cachedState = "Unknown";
                        if (currentStateField != null)
                        {
                            var stateValue = currentStateField.GetValue(extractionPoint);
                            cachedState = stateValue?.ToString() ?? "Unknown";
                        }
                        Vector3 cachedPosition = extractionPoint.transform.position;
                        extractionPointList.Add(new ExtractionPointData(extractionPoint, cachedState, cachedPosition));
                        Hax2.Log1($"Extraction Point cacheado na posição: {cachedPosition}");
                    }
                }
                Hax2.Log1($"Lista de Extraction Points atualizada: {extractionPointList.Count} pontos encontrados.");
            }
        }

        public class ExtractionPointData
        {
            public ExtractionPoint ExtractionPoint { get; }
            public string CachedState { get; }
            public Vector3 CachedPosition { get; }

            public ExtractionPointData(ExtractionPoint ep, string state, Vector3 position)
            {
                ExtractionPoint = ep;
                CachedState = state;
                CachedPosition = position;
            }
        }
        private static void UpdateLists()
        {
            UpdateExtractionPointList();
            enemyList.Clear();
            var enemyDirectorType = Type.GetType("EnemyDirector, Assembly-CSharp");
            if (enemyDirectorType != null)
            {
                var enemyDirectorInstance = enemyDirectorType.GetField("instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (enemyDirectorInstance != null)
                {
                    var enemiesSpawnedField = enemyDirectorType.GetField("enemiesSpawned", BindingFlags.Public | BindingFlags.Instance);
                    if (enemiesSpawnedField != null)
                    {
                        var enemies = enemiesSpawnedField.GetValue(enemyDirectorInstance) as IEnumerable<object>;
                        if (enemies != null)
                        {
                            foreach (var enemy in enemies)
                            {
                                if (enemy != null)
                                {
                                    var enemyInstanceField = enemy.GetType().GetField("enemyInstance", BindingFlags.NonPublic | BindingFlags.Instance)
                                                          ?? enemy.GetType().GetField("Enemy", BindingFlags.NonPublic | BindingFlags.Instance)
                                                          ?? enemy.GetType().GetField("childEnemy", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (enemyInstanceField != null)
                                    {
                                        var enemyInstance = enemyInstanceField.GetValue(enemy) as Enemy;
                                        if (enemyInstance != null && enemyInstance.gameObject != null && enemyInstance.gameObject.activeInHierarchy)
                                        {
                                            enemyList.Add(enemyInstance);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            playerList.Clear();
            var players = SemiFunc.PlayerGetList();
            if (players != null)
            {
                foreach (var player in players)
                {
                    if (player != null)
                    {
                        playerList.Add(player);
                    }
                }
            }

            lastUpdateTime = Time.time;
            Hax2.Log1($"Listas atualizadas: {enemyList.Count} inimigos, {valuableObjects.Count} itens, {playerList.Count} jogadores.");
        }

        private static void UpdateLocalPlayer()
        {
            localPlayer = GetLocalPlayer();
            if (localPlayer != null)
            {
                Hax2.Log1("Jogador local atualizado com sucesso: " + localPlayer.name);
            }
            else
            {
                Hax2.Log1("Falha ao atualizar jogador local!");
            }
        }



        public static GameObject GetLocalPlayer()
        {
            if (PhotonNetwork.IsConnected)
            {
                var players = SemiFunc.PlayerGetList();
                if (players != null)
                {
                    foreach (var player in players)
                    {
                        var photonViewField = player.GetType().GetField("photonView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (photonViewField != null)
                        {
                            var photonView = photonViewField.GetValue(player) as PhotonView;
                            if (photonView != null && photonView.IsMine)
                            {
                                var gameObjectProperty = player.GetType().GetProperty("gameObject", BindingFlags.Public | BindingFlags.Instance);
                                if (gameObjectProperty != null)
                                {
                                    GameObject foundPlayer = gameObjectProperty.GetValue(player) as GameObject;
                                    Hax2.Log1("Local player encontrado via Photon: " + foundPlayer.name);
                                    return foundPlayer;
                                }
                                Hax2.Log1("Local player encontrado via PhotonView: " + photonView.gameObject.name);
                                return photonView.gameObject;
                            }
                        }
                    }
                }

                if (PhotonNetwork.LocalPlayer != null)
                {
                    foreach (var photonView in UnityEngine.Object.FindObjectsOfType<PhotonView>())
                    {
                        if (photonView.Owner == PhotonNetwork.LocalPlayer && photonView.IsMine)
                        {
                            Hax2.Log1("Local player encontrado via Photon fallback: " + photonView.gameObject.name);
                            return photonView.gameObject;
                        }
                    }
                }
            }
            else
            {
                var players = SemiFunc.PlayerGetList();
                if (players != null && players.Count > 0)
                {
                    var player = players[0];
                    var gameObjectProperty = player.GetType().GetProperty("gameObject", BindingFlags.Public | BindingFlags.Instance);
                    if (gameObjectProperty != null)
                    {
                        GameObject foundPlayer = gameObjectProperty.GetValue(player) as GameObject;
                        Hax2.Log1("Local player encontrado em singleplayer via PlayerGetList: " + foundPlayer.name);
                        return foundPlayer;
                    }
                }

                var playerAvatarType = Type.GetType("PlayerAvatar, Assembly-CSharp");
                if (playerAvatarType != null)
                {
                    var playerAvatar = UnityEngine.Object.FindObjectOfType(playerAvatarType) as MonoBehaviour;
                    if (playerAvatar != null)
                    {
                        Hax2.Log1("Local player encontrado em singleplayer via PlayerAvatar: " + playerAvatar.gameObject.name);
                        return playerAvatar.gameObject;
                    }
                }

                var playerByTag = GameObject.FindWithTag("Player");
                if (playerByTag != null)
                {
                    Hax2.Log1("Local player encontrado em singleplayer via tag 'Player': " + playerByTag.name);
                    return playerByTag;
                }

                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains("Player") && obj.activeInHierarchy)
                    {
                        Hax2.Log1("Local player encontrado em singleplayer via nome genérico: " + obj.name);
                        return obj;
                    }
                }

                Hax2.Log1("Nenhum jogador local encontrado no singleplayer após todas as tentativas!");
                return null;
            }

            Hax2.Log1("Nenhum jogador local encontrado!");
            return null;
        }

        public static void UpdateEnemyList()
        {
            enemyList.Clear();

            var enemyDirectorType = Type.GetType("EnemyDirector, Assembly-CSharp");
            if (enemyDirectorType != null)
            {
                var enemyDirectorInstance = enemyDirectorType.GetField("instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (enemyDirectorInstance != null)
                {
                    var enemiesSpawnedField = enemyDirectorType.GetField("enemiesSpawned", BindingFlags.Public | BindingFlags.Instance);
                    if (enemiesSpawnedField != null)
                    {
                        var enemies = enemiesSpawnedField.GetValue(enemyDirectorInstance) as IEnumerable<object>;
                        if (enemies != null)
                        {
                            foreach (var enemy in enemies)
                            {
                                if (enemy != null)
                                {
                                    var enemyInstanceField = enemy.GetType().GetField("enemyInstance", BindingFlags.NonPublic | BindingFlags.Instance)
                                                          ?? enemy.GetType().GetField("Enemy", BindingFlags.NonPublic | BindingFlags.Instance)
                                                          ?? enemy.GetType().GetField("childEnemy", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (enemyInstanceField != null)
                                    {
                                        var enemyInstance = enemyInstanceField.GetValue(enemy) as Enemy;
                                        if (enemyInstance != null && enemyInstance.gameObject != null && enemyInstance.gameObject.activeInHierarchy)
                                        {
                                            enemyList.Add(enemyInstance);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Hax2.Log1("Nenhum inimigo encontrado em enemiesSpawned");
                        }
                    }
                    else
                    {
                        Hax2.Log1("Campo 'enemiesSpawned' não encontrado");
                    }
                }
                else
                {
                    Hax2.Log1("Instância de EnemyDirector é nula");
                }
            }
            else
            {
                Hax2.Log1("EnemyDirector não encontrado");
            }
        }

        public static void RectFilled(float x, float y, float width, float height, Texture2D text)
        {
            GUI.DrawTexture(new Rect(x, y, width, height), text);
        }

        public static void RectOutlined(float x, float y, float width, float height, Texture2D text, float thickness = 1f)
        {
            RectFilled(x, y, thickness, height, text);
            RectFilled(x + width - thickness, y, thickness, height, text);
            RectFilled(x + thickness, y, width - thickness * 2f, thickness, text);
            RectFilled(x + thickness, y + height - thickness, width - thickness * 2f, thickness, text);
        }

        public static void Box(float x, float y, float width, float height, Texture2D text, float thickness = 2f)
        {
            RectOutlined(x - width / 2f, y - height, width, height, text, thickness);
        }

        public static void InitializeStyles()
        {
            if (nameStyle == null)
            {
                nameStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.yellow },
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true,
                    border = new RectOffset(1, 1, 1, 1)
                };
            }

            if (valueStyle == null)
            {
                valueStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.green },
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
            }

            if (enemyStyle == null)
            {
                enemyStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
            }

            if (healthStyle == null)
            {
                healthStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.green },
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
            }

            if (distanceStyle == null)
            {
                distanceStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.yellow },
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
            }
        }

        private static void CreateBoundsEdges(Bounds bounds, Color color)
        {
            Vector3[] vertices = new Vector3[8];
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            vertices[0] = new Vector3(min.x, min.y, min.z);
            vertices[1] = new Vector3(max.x, min.y, min.z);
            vertices[2] = new Vector3(max.x, min.y, max.z);
            vertices[3] = new Vector3(min.x, min.y, max.z);
            vertices[4] = new Vector3(min.x, max.y, min.z);
            vertices[5] = new Vector3(max.x, max.y, min.z);
            vertices[6] = new Vector3(max.x, max.y, max.z);
            vertices[7] = new Vector3(min.x, max.y, max.z);

            Vector2[] screenVertices = new Vector2[8];
            bool isVisible = false;

            for (int i = 0; i < 8; i++)
            {
                Vector3 screenPos = cachedCamera.WorldToScreenPoint(vertices[i]);
                if (screenPos.z > 0) isVisible = true;
                screenVertices[i] = new Vector2(screenPos.x * scaleX, Screen.height - (screenPos.y * scaleY));
            }

            if (!isVisible) return;

            DrawLine(screenVertices[0], screenVertices[1], color);
            DrawLine(screenVertices[1], screenVertices[2], color);
            DrawLine(screenVertices[2], screenVertices[3], color);
            DrawLine(screenVertices[3], screenVertices[0], color);

            DrawLine(screenVertices[4], screenVertices[5], color);
            DrawLine(screenVertices[5], screenVertices[6], color);
            DrawLine(screenVertices[6], screenVertices[7], color);
            DrawLine(screenVertices[7], screenVertices[4], color);

            DrawLine(screenVertices[0], screenVertices[4], color);
            DrawLine(screenVertices[1], screenVertices[5], color);
            DrawLine(screenVertices[2], screenVertices[6], color);
            DrawLine(screenVertices[3], screenVertices[7], color);
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            if (texture2 == null) return;

            float distance = Vector2.Distance(start, end);
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;

            GUI.color = color;
            Matrix4x4 originalMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y, distance, 1f), texture2);
            GUI.matrix = originalMatrix;
            GUI.color = Color.white;
        }

        private static Bounds GetActiveColliderBounds(GameObject obj)
        {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
            List<Collider> activeColliders = new List<Collider>();

            foreach (Collider col in colliders)
            {
                if (col.enabled && col.gameObject.activeInHierarchy)
                    activeColliders.Add(col);
            }

            if (activeColliders.Count == 0)
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        if (renderers[i].enabled && renderers[i].gameObject.activeInHierarchy)
                            bounds.Encapsulate(renderers[i].bounds);
                    }
                    return bounds;
                }
                return new Bounds(obj.transform.position, Vector3.one * 0.5f);
            }

            Bounds resultBounds = activeColliders[0].bounds;
            for (int i = 1; i < activeColliders.Count; i++)
            {
                resultBounds.Encapsulate(activeColliders[i].bounds);
            }

            resultBounds.Expand(0.1f);
            return resultBounds;
        }
        public static void DrawESP()
        {
            InitializeStyles();
            if (!drawEspBool && !drawItemEspBool && !drawExtractionPointEspBool && !drawPlayerEspBool && !draw3DPlayerEspBool && !draw3DItemEspBool) return;
            if (localPlayer == null)
            {
                UpdateLocalPlayer();
            }
            if (Time.time - lastUpdateTime > updateInterval)
            {
                UpdatePlayerDataList();
                if (drawEspBool || drawItemEspBool || drawExtractionPointEspBool || drawPlayerEspBool || draw3DPlayerEspBool || draw3DItemEspBool)
                {
                    UpdateLists();
                }
                UpdateLocalPlayer();
            }

            frameCounter++;
            if (frameCounter % 2 != 0) return;

            if (cachedCamera == null || cachedCamera != Camera.main)
            {
                cachedCamera = Camera.main;
                if (cachedCamera == null)
                {
                    Hax2.Log1("Camera.main não encontrada!");
                    return;
                }
            }

            scaleX = (float)Screen.width / cachedCamera.pixelWidth;
            scaleY = (float)Screen.height / cachedCamera.pixelHeight;

            if (drawEspBool)
            {
                foreach (var enemyInstance in enemyList)
                {
                    if (enemyInstance == null || !enemyInstance.gameObject.activeInHierarchy || enemyInstance.CenterTransform == null) continue;

                    Vector3 footPosition = enemyInstance.transform.position;
                    float enemyHeightEstimate = 2f;
                    Vector3 headPosition = enemyInstance.transform.position + Vector3.up * enemyHeightEstimate;

                    Vector3 screenFootPos = cachedCamera.WorldToScreenPoint(footPosition);
                    Vector3 screenHeadPos = cachedCamera.WorldToScreenPoint(headPosition);

                    if (screenFootPos.z > 0 && screenHeadPos.z > 0)
                    {
                        float footX = screenFootPos.x * scaleX;
                        float footY = Screen.height - (screenFootPos.y * scaleY);
                        float headY = Screen.height - (screenHeadPos.y * scaleY);

                        float height = Mathf.Abs(footY - headY);
                        float enemyScale = enemyInstance.transform.localScale.y;
                        float baseWidth = enemyScale * 200f;
                        float distance = screenFootPos.z;
                        float width = (baseWidth / distance) * scaleX;

                        width = Mathf.Clamp(width, 30f, height * 1.2f);
                        height = Mathf.Clamp(height, 40f, 400f);

                        float x = footX;
                        float y = footY;

                        Box(x, y, width, height, texture2, 1f);

                        float labelWidth = 100f;
                        float labelX = x - labelWidth / 2f;

                        var enemyParent = enemyInstance.GetComponentInParent(Type.GetType("EnemyParent, Assembly-CSharp"));
                        string enemyName = "Enemy";
                        if (enemyParent != null)
                        {
                            var nameField = enemyParent.GetType().GetField("enemyName", BindingFlags.Public | BindingFlags.Instance);
                            enemyName = nameField?.GetValue(enemyParent) as string ?? "Enemy";
                        }

                        string distanceText = "";
                        if (showEnemyDistance && localPlayer != null)
                        {
                            float distance2 = Vector3.Distance(localPlayer.transform.position, enemyInstance.transform.position);
                            distanceText = $" [{distance2:F1}m]";
                        }

                        string fullText = "";
                        if (showEnemyNames) fullText = enemyName;
                        if (showEnemyDistance) fullText += distanceText;

                        float labelHeight = enemyStyle.CalcHeight(new GUIContent(fullText), labelWidth);
                        float labelY = y - height - labelHeight;

                        GUI.Label(new Rect(labelX, labelY, labelWidth, labelHeight), fullText, enemyStyle);
                    }
                }
            }

            if (drawItemEspBool)
            {
                foreach (var valuableObject in valuableObjects)
                {
                    if (valuableObject == null) continue;

                    bool isPlayerDeathHead = valuableObject.GetType().Name == "PlayerDeathHead";
                    if (!DebugCheats.showPlayerDeathHeads && isPlayerDeathHead) continue;

                    var transform = valuableObject.GetType().GetProperty("transform", BindingFlags.Public | BindingFlags.Instance)?.GetValue(valuableObject) as Transform;
                    if (transform == null || !transform.gameObject.activeInHierarchy) continue;

                    Vector3 itemPosition = transform.position;
                    Vector3 screenPos = cachedCamera.WorldToScreenPoint(itemPosition);

                    if (screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width && screenPos.y > 0 && screenPos.y < Screen.height)
                    {
                        float x = screenPos.x * scaleX;
                        float y = Screen.height - (screenPos.y * scaleY);

                        string itemName;
                        Color originalColor = nameStyle.normal.textColor;

                        if (isPlayerDeathHead)
                        {
                            itemName = "Dead Player Head";
                            nameStyle.normal.textColor = Color.red;
                        }
                        else
                        {
                            nameStyle.normal.textColor = Color.yellow;

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
                                Hax2.Log1($"Erro ao acessar 'name' do item: {e.Message}. Usando nome do GameObject: {itemName}");
                            }

                            if (itemName.StartsWith("Valuable", StringComparison.OrdinalIgnoreCase))
                            {
                                itemName = itemName.Substring("Valuable".Length).Trim();
                            }
                            if (itemName.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
                            {
                                itemName = itemName.Substring(0, itemName.Length - "(Clone)".Length).Trim();
                            }
                        }

                        int itemValue = 0;
                        if (!isPlayerDeathHead)
                        {
                            var valueField = valuableObject.GetType().GetField("dollarValueCurrent", BindingFlags.Public | BindingFlags.Instance);
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
                        }

                        // Set distance color
                        Color distanceColor = isPlayerDeathHead ? Color.red : Color.yellow;
                        nameStyle.normal.textColor = distanceColor;

                        string distanceText = "";
                        if (showItemDistance && localPlayer != null)
                        {
                            float distance = Vector3.Distance(localPlayer.transform.position, itemPosition);
                            distanceText = $" [{distance:F1}m]";
                        }

                        string nameText = showItemNames ? itemName : "";
                        if (showItemDistance) nameText += distanceText;

                        float labelWidth = 150f;
                        float valueLabelHeight = valueStyle.CalcHeight(new GUIContent(itemValue.ToString() + "$"), labelWidth);
                        float nameLabelHeight = nameStyle.CalcHeight(new GUIContent(nameText), labelWidth);
                        float totalHeight = nameLabelHeight + valueLabelHeight + 5f;
                        float labelX = x - labelWidth / 2f;
                        float labelY = y - totalHeight - 5f;

                        // Draw Name (with distance included in the same label)
                        if (!string.IsNullOrEmpty(nameText))
                        {
                            GUI.Label(new Rect(labelX, labelY, labelWidth, nameLabelHeight), nameText, nameStyle);
                        }

                        // Draw Item Value (if not DeadPlayerHead)
                        if (showItemValue && !isPlayerDeathHead)
                        {
                            GUI.Label(new Rect(labelX, labelY + nameLabelHeight + 2f, labelWidth, valueLabelHeight), itemValue.ToString() + "$", valueStyle);
                        }

                        // Draw 3D Item ESP if enabled
                        if (draw3DItemEspBool)
                        {
                            Bounds bounds = GetActiveColliderBounds(transform.gameObject);
                            CreateBoundsEdges(bounds, Color.yellow);
                        }

                        nameStyle.normal.textColor = originalColor;
                    }
                }
            }

            if (drawExtractionPointEspBool)
            {
                foreach (var epData in extractionPointList)
                {
                    if (epData.ExtractionPoint == null || !epData.ExtractionPoint.gameObject.activeInHierarchy) continue;

                    Vector3 screenPos = cachedCamera.WorldToScreenPoint(epData.CachedPosition);

                    if (screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width && screenPos.y > 0 && screenPos.y < Screen.height)
                    {
                        float x = screenPos.x * scaleX;
                        float y = Screen.height - (screenPos.y * scaleY);

                        string pointName = "Extraction Point";
                        string stateText = $" ({epData.CachedState})";
                        string distanceText = showExtractionDistance && localPlayer != null ? $"{Vector3.Distance(localPlayer.transform.position, epData.CachedPosition):F1}m" : "";

                        Color originalColor = nameStyle.normal.textColor;

                        nameStyle.normal.textColor = epData.CachedState == "Active" ? Color.green : (epData.CachedState == "Idle" ? Color.red : Color.cyan);

                        string nameFullText = showExtractionNames ? pointName + stateText : "";
                        if (showExtractionDistance) nameFullText += " " + distanceText;

                        float labelWidth = 150f;
                        float nameLabelHeight = nameStyle.CalcHeight(new GUIContent(nameFullText), labelWidth);
                        float totalHeight = nameLabelHeight;
                        float labelX = x - labelWidth / 2f;
                        float labelY = y - totalHeight - 5f;

                        if (!string.IsNullOrEmpty(nameFullText))
                        {
                            GUI.Label(new Rect(labelX, labelY, labelWidth, nameLabelHeight), nameFullText, nameStyle);
                        }

                        nameStyle.normal.textColor = originalColor;

                    }
                }
            }

            if (drawPlayerEspBool || draw3DPlayerEspBool)
            {

                foreach (var playerData in playerDataList)
                {
                    bool isLocalPlayer = false;
                    if (!PhotonNetwork.IsConnected && localPlayer != null && playerData.Transform.gameObject == localPlayer)
                    {
                        isLocalPlayer = true;
                    }
                    if (playerData.PhotonView == null || (playerData.PhotonView.IsMine && PhotonNetwork.IsConnected) || isLocalPlayer || !playerData.Transform.gameObject.activeInHierarchy) continue;

                    Vector3 playerPos = playerData.Transform.position;
                    float distanceToPlayer = localPlayer != null ? Vector3.Distance(localPlayer.transform.position, playerPos) : float.MaxValue;
                    if (distanceToPlayer > maxEspDistance) continue;

                    Vector3 footPosition = playerPos;
                    float playerHeightEstimate = 2f;
                    Vector3 headPosition = playerPos + Vector3.up * playerHeightEstimate;

                    Vector3 screenFootPos = cachedCamera.WorldToScreenPoint(footPosition);
                    Vector3 screenHeadPos = cachedCamera.WorldToScreenPoint(headPosition);
                    bool isInFront = screenFootPos.z > 0 && screenHeadPos.z > 0;

                    if (!isInFront) continue;

                    float footX = screenFootPos.x * scaleX;
                    float footY = Screen.height - (screenFootPos.y * scaleY);
                    float headY = Screen.height - (screenHeadPos.y * scaleY);

                    float height = Mathf.Abs(footY - headY);
                    float playerScale = playerData.Transform.localScale.y;
                    float baseWidth = playerScale * 200f;
                    float width = (baseWidth / (distanceToPlayer + 1f)) * scaleX;
                    width = Mathf.Clamp(width, 30f, height * 1.2f);
                    height = Mathf.Clamp(height, 40f, 400f);

                    float x = footX;
                    float y = footY;

                    if (draw3DPlayerEspBool)
                    {
                        Bounds bounds = GetActiveColliderBounds(playerData.Transform.gameObject);
                        CreateBoundsEdges(bounds, Color.red);
                    }
                    if (drawPlayerEspBool)
                    {
                        Box(x, y, width, height, texture2, 2f);
                    }

                    Color originalNameColor = nameStyle.normal.textColor;
                    nameStyle.normal.textColor = Color.white;

                    int health = playerHealthCache.ContainsKey(playerData.PhotonView.ViewID) ? playerHealthCache[playerData.PhotonView.ViewID] : 100;
                    string healthText = $"HP: {health}";
                    string distanceText = showPlayerDistance && localPlayer != null ? $"{distanceToPlayer:F1}m" : "";

                    string nameFullText = showPlayerNames ? playerData.Name : "";
                    if (showPlayerDistance) nameFullText += " " + distanceText;

                    float labelWidth = 150f;
                    float nameHeight = nameStyle.CalcHeight(new GUIContent(nameFullText), labelWidth);
                    float healthHeight = healthStyle.CalcHeight(new GUIContent(healthText), labelWidth);
                    float totalHeight = nameHeight + (showPlayerHP ? healthHeight + 2f : 0f);
                    float labelX = footX - labelWidth / 2f;
                    float labelY = footY - height - totalHeight - 10f;

                    if (!string.IsNullOrEmpty(nameFullText))
                    {
                        GUI.Label(new Rect(labelX, labelY, labelWidth, nameHeight), nameFullText, nameStyle);
                    }
                    if (showPlayerHP)
                    {
                        GUI.Label(new Rect(labelX, labelY + nameHeight + 2f, labelWidth, healthHeight), healthText, healthStyle);
                    }

                    nameStyle.normal.textColor = originalNameColor;

                }
            }
        }
        private static int GetPlayerHealth(object player)
            {
                try
                {
                    var playerHealthField = player.GetType().GetField("playerHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (playerHealthField == null) return 100;

                    var playerHealthInstance = playerHealthField.GetValue(player);
                    if (playerHealthInstance == null) return 100;

                    var healthField = playerHealthInstance.GetType().GetField("health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (healthField == null) return 100;

                    return (int)healthField.GetValue(playerHealthInstance);
                }
                catch (Exception e)
                {
                    Hax2.Log1($"Erro ao obter vida do jogador: {e.Message}");
                    return 100;
                }
            }


            public static void KillAllEnemies()
            {
                Hax2.Log1("Tentando matar todos os inimigos");

                foreach (var enemyInstance in enemyList)
                {
                    if (enemyInstance == null) continue;

                    try
                    {
                        var healthField = enemyInstance.GetType().GetField("Health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (healthField != null)
                        {
                            var healthComponent = healthField.GetValue(enemyInstance);
                            if (healthComponent != null)
                            {
                                var healthType = healthComponent.GetType();
                                var hurtMethod = healthType.GetMethod("Hurt", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (hurtMethod != null)
                                {
                                    hurtMethod.Invoke(healthComponent, new object[] { 9999, Vector3.zero });
                                    Hax2.Log1($"Inimigo ferido com 9999 de dano via Hurt");
                                }
                                else
                                    Hax2.Log1("Método 'Hurt' não encontrado em EnemyHealth");
                            }
                            else
                                Hax2.Log1("Componente EnemyHealth é nulo");
                        }
                        else
                            Hax2.Log1("Campo 'Health' não encontrado em Enemy");
                    }
                    catch (Exception e)
                    {
                        Hax2.Log1($"Erro ao matar inimigo: {e.Message}");
                    }
                }
                UpdateEnemyList();
            }
        }
    }
