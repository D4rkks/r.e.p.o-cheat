using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace r.e.p.o_cheat
{
	/// <summary>
	/// Cosmetics go through MetaManager / PlayerCosmetics (equip, save, SetupCosmeticsRPC / SetupColorsRPC).
	/// Rainbow cycles body tint via SetupColors — not the removed SetColorRPC / playerColor path.
	/// </summary>
	public static class CosmeticFeatures
	{
		public static bool RainbowMode;

		public static float RainbowSpeed = 0.35f;

		public static bool LiveRandom;

		public static float LiveRandomInterval = 2.5f;

		public static string LastStatus = string.Empty;

		public static bool FeedingMachine;

		private static Coroutine _feedRoutine;

		private static MonoBehaviour _feedHost;

		private static MethodInfo _machineInteract;

		private static FieldInfo _machineState;

		private static float _nextColorSync;

		private static float _nextOutfit;

		private static readonly BindingFlags InstAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private static FieldInfo _unlocksField;

		private static FieldInfo _equippedField;

		private static FieldInfo _colorsField;

		private static FieldInfo _tokensField;

		private static FieldInfo _materialsField;

		private static FieldInfo _deathHeadField;

		private static FieldInfo _menuCosmeticsField;

		private static int _albedoId;

		private static int _emissionId;

		private static int _fresnelId;

		private const byte TokenEvent = 172;

		private static bool _photonEventHooked;

		private static float _lastTokenEvent;

		public static bool UnlockAll()
		{
			try
			{
				MetaManager meta = MetaManager.instance;
				if (meta == null)
				{
					LastStatus = "MetaManager not ready";
					return false;
				}
				List<int> unlocked = GetUnlocked(meta);
				int before = unlocked.Count;
				bool changed = meta.CosmeticUnlockAll();
				int after = GetUnlocked(meta).Count;
				LastStatus = changed
					? "Unlocked " + (after - before) + " cosmetics (" + after + "/" + meta.cosmeticAssets.Count + ")"
					: "Already unlocked (" + before + "/" + meta.cosmeticAssets.Count + ")";
				RefreshTokenUi();
				ApplyLocalCosmetics(sync: true, forced: true);
				return true;
			}
			catch (Exception ex)
			{
				LastStatus = "UnlockAll failed: " + ex.Message;
				return false;
			}
		}

		public static bool RandomizeOutfit(bool randomizeColors, bool persistSave = true)
		{
			try
			{
				MetaManager meta = MetaManager.instance;
				if (meta == null)
				{
					LastStatus = "MetaManager not ready";
					return false;
				}
				if (GetUnlocked(meta).Count == 0)
				{
					meta.CosmeticUnlockAll();
				}
				List<int> unlocked = GetUnlocked(meta);
				if (unlocked.Count == 0)
				{
					LastStatus = "No cosmetics in MetaManager";
					return false;
				}

				foreach (int index in new List<int>(GetEquipped(meta)))
				{
					if (index >= 0 && index < meta.cosmeticAssets.Count && meta.cosmeticAssets[index] != null)
					{
						meta.CosmeticUnequip(meta.cosmeticAssets[index], _isPreview: false);
					}
				}

				var byType = new Dictionary<SemiFunc.CosmeticType, List<CosmeticAsset>>();
				foreach (int index in unlocked)
				{
					if (index < 0 || index >= meta.cosmeticAssets.Count)
					{
						continue;
					}
					CosmeticAsset asset = meta.cosmeticAssets[index];
					if (asset == null)
					{
						continue;
					}
					if (!byType.TryGetValue(asset.type, out var list))
					{
						list = new List<CosmeticAsset>();
						byType[asset.type] = list;
					}
					list.Add(asset);
				}

				int equippedCount = 0;
				foreach (var pair in byType)
				{
					CosmeticTypeAsset typeAsset = (int)pair.Key < meta.cosmeticTypeAssets.Count ? meta.cosmeticTypeAssets[(int)pair.Key] : null;
					if (typeAsset != null && typeAsset.meshSwitch)
					{
						continue;
					}
					bool multi = typeAsset != null && typeAsset.canEquipMultiple;
					int rolls = multi ? 2 : 1;
					for (int r = 0; r < rolls; r++)
					{
						CosmeticAsset pick = pair.Value[UnityEngine.Random.Range(0, pair.Value.Count)];
						if (meta.CosmeticEquip(pick, _isPreview: false))
						{
							equippedCount++;
						}
					}
				}

				int[] colors = EnsureColorArray(meta);
				if (randomizeColors && meta.colors != null && meta.colors.Count > 0)
				{
					for (int i = 0; i < colors.Length; i++)
					{
						colors[i] = UnityEngine.Random.Range(0, meta.colors.Count);
					}
					SetMetaColors(meta, colors);
				}

				ApplyLocalCosmetics(sync: true, forced: true);
				if (persistSave)
				{
					meta.Save();
				}
				LastStatus = "Random outfit: " + equippedCount + " pieces" + (randomizeColors ? " + colors" : string.Empty);
				return equippedCount > 0;
			}
			catch (Exception ex)
			{
				LastStatus = "RandomizeOutfit failed: " + ex.Message;
				return false;
			}
		}

		public static void TickLiveRandom()
		{
			if (!LiveRandom)
			{
				return;
			}
			if (Time.time < _nextOutfit)
			{
				return;
			}
			_nextOutfit = Time.time + Mathf.Max(0.45f, LiveRandomInterval);
			RandomizeOutfit(randomizeColors: !RainbowMode, persistSave: false);
		}

		public static void TickRainbow()
		{
			if (!RainbowMode)
			{
				return;
			}
			MetaManager meta = MetaManager.instance;
			if (meta == null || meta.colors == null || meta.colors.Count == 0)
			{
				return;
			}
			float hue = Mathf.Repeat(Time.time * Mathf.Max(0.05f, RainbowSpeed), 1f);
			Color rgb = Color.HSVToRGB(hue, 1f, 1f);
			int palette = NearestPaletteIndex(meta, rgb);
			if (Time.time >= _nextColorSync)
			{
				_nextColorSync = Time.time + 0.45f;
				ApplyPaletteColor(palette, sync: true);
			}
			foreach (PlayerCosmetics pc in EnumerateLocalCosmetics())
			{
				TintMaterials(pc, rgb);
			}
		}

		public static bool ApplyPaletteColor(int colorId, bool sync)
		{
			try
			{
				MetaManager meta = MetaManager.instance;
				if (meta == null || meta.colors == null || meta.colors.Count == 0)
				{
					LastStatus = "color palette not ready";
					return false;
				}
				colorId = Mathf.Clamp(colorId, 0, meta.colors.Count - 1);
				int[] colors = EnsureColorArray(meta);
				for (int i = 0; i < colors.Length; i++)
				{
					colors[i] = colorId;
				}
				SetMetaColors(meta, colors);
				bool applied = false;
				foreach (PlayerCosmetics pc in EnumerateLocalCosmetics())
				{
					pc.SetupColors(sync, colors);
					applied = true;
				}
				LastStatus = applied ? ("Color " + colorId + " (" + ColorName(meta, colorId) + ")") : "no local PlayerCosmetics";
				return applied;
			}
			catch (Exception ex)
			{
				LastStatus = "color: " + ex.Message;
				return false;
			}
		}

		public static Dictionary<int, string> GetPaletteNames()
		{
			var map = new Dictionary<int, string>();
			try
			{
				MetaManager meta = MetaManager.instance;
				if (meta != null && meta.colors != null && meta.colors.Count > 0)
				{
					for (int i = 0; i < meta.colors.Count; i++)
					{
						map[i] = ColorName(meta, i);
					}
					return map;
				}
			}
			catch
			{
			}
			for (int i = 0; i <= 35; i++)
			{
				map[i] = LanguageManager.GetColorName(i);
			}
			return map;
		}

		public static SemiFunc.Rarity TokenRarity = SemiFunc.Rarity.UltraRare;

		public static string RarityLabel(SemiFunc.Rarity rarity)
		{
			switch (rarity)
			{
				case SemiFunc.Rarity.Uncommon: return L.T("cos.rarity_uncommon");
				case SemiFunc.Rarity.Rare: return L.T("cos.rarity_rare");
				case SemiFunc.Rarity.UltraRare: return L.T("cos.rarity_ultra");
				default: return L.T("cos.rarity_common");
			}
		}

		public static int AddTokens(int count)
		{
			try
			{
				MetaManager meta = MetaManager.instance;
				if (meta == null)
				{
					LastStatus = "MetaManager missing";
					return 0;
				}
				if (_tokensField == null)
				{
					_tokensField = typeof(MetaManager).GetField("cosmeticTokens", InstAll);
				}
				List<int> tokens = _tokensField?.GetValue(meta) as List<int>;
				if (tokens == null)
				{
					LastStatus = "no token list";
					return 0;
				}
				count = Mathf.Clamp(count, 1, 200);
				int rarity = (int)TokenRarity;
				for (int i = 0; i < count; i++)
				{
					tokens.Add(rarity);
				}
				meta.Save();
				RebuildTokenUi();
				LastStatus = "tokens +" + count + " " + RarityLabel(TokenRarity) + " (now " + tokens.Count + ")";
				return count;
			}
			catch (Exception ex)
			{
				LastStatus = "tokens: " + ex.Message;
				return 0;
			}
		}

		public static void SpawnCubeAtSelected()
		{
			PlayerAvatar avatar = GetSelectedAvatar();
			if (avatar == null)
			{
				avatar = SemiFunc.PlayerAvatarLocal();
			}
			if ((UnityEngine.Object)avatar == null)
			{
				LastStatus = L.T("common.no_players");
				return;
			}
			Transform t = avatar.playerTransform != null ? avatar.playerTransform : avatar.transform;
			Vector3 pos = t.position + t.forward * 1.2f + Vector3.up * 0.4f;
			if (NativeGameApi.SpawnCosmeticCube(TokenRarity, pos))
			{
				LastStatus = L.T("cos.cube_spawned");
			}
			else
			{
				LastStatus = NativeGameApi.LastStatus;
			}
		}

		public static void AddTokensToSelected(int count)
		{
			EnsurePhotonEventHook();
			PlayerAvatar avatar = GetSelectedAvatar();
			if (avatar == null || IsLocalAvatar(avatar))
			{
				AddTokens(count);
				return;
			}
			if (!PhotonNetwork.InRoom || avatar.photonView == null || avatar.photonView.Owner == null)
			{
				LastStatus = L.T("cos.tokens_need_room");
				return;
			}
			int actor = avatar.photonView.Owner.ActorNumber;
			Player localPlayer = PhotonNetwork.LocalPlayer;
			if (localPlayer != null && actor == localPlayer.ActorNumber)
			{
				AddTokens(count);
				return;
			}
			try
			{
				RaiseEventOptions options = new RaiseEventOptions
				{
					TargetActors = new[] { actor }
				};
				PhotonNetwork.RaiseEvent(TokenEvent, new object[] { count, (int)TokenRarity }, options, SendOptions.SendReliable);
				PhotonNetwork.SendAllOutgoingCommands();
				string name = SemiFunc.PlayerGetName(avatar) ?? actor.ToString();
				LastStatus = L.T("cos.tokens_sent_fmt", count, name);
			}
			catch (Exception ex)
			{
				LastStatus = "tokens: " + ex.Message;
			}
		}

		internal static void UnhookPhotonEvents()
		{
			if (!_photonEventHooked)
			{
				return;
			}
			try
			{
				if (PhotonNetwork.NetworkingClient != null)
				{
					PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
				}
			}
			catch
			{
			}
			_photonEventHooked = false;
		}

		internal static void EnsurePhotonEventHook()
		{
			if (_photonEventHooked)
			{
				return;
			}
			try
			{
				if (PhotonNetwork.NetworkingClient == null)
				{
					return;
				}
				PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
				_photonEventHooked = true;
			}
			catch
			{
			}
		}

		private static void OnPhotonEvent(EventData photonEvent)
		{
			if (photonEvent == null || photonEvent.Code != TokenEvent || !PhotonNetwork.InRoom)
			{
				return;
			}
			if (photonEvent.Sender <= 0)
			{
				return;
			}
			Player sender = null;
			try
			{
				Room room = PhotonNetwork.CurrentRoom;
				sender = room != null ? room.GetPlayer(photonEvent.Sender) : null;
			}
			catch
			{
			}
			if (sender == null)
			{
				return;
			}
			object[] args = photonEvent.CustomData as object[];
			if (args == null || args.Length < 1)
			{
				return;
			}
			int count;
			int rarity;
			try
			{
				count = Convert.ToInt32(args[0]);
				rarity = args.Length > 1 ? Convert.ToInt32(args[1]) : (int)TokenRarity;
			}
			catch
			{
				return;
			}
			count = Mathf.Clamp(count, 1, 200);
			if (rarity < 0 || rarity > 3)
			{
				return;
			}
			if (Time.unscaledTime - _lastTokenEvent < 0.25f)
			{
				return;
			}
			_lastTokenEvent = Time.unscaledTime;
			SemiFunc.Rarity saved = TokenRarity;
			try
			{
				TokenRarity = (SemiFunc.Rarity)rarity;
				int added = AddTokens(count);
				if (added > 0)
				{
					LastStatus = L.T("cos.tokens_received_fmt", added, RarityLabel(TokenRarity));
				}
			}
			finally
			{
				TokenRarity = saved;
			}
		}

		private static PlayerAvatar GetSelectedAvatar()
		{
			if (Hax2.selectedPlayerIndex < 0 || Hax2.selectedPlayerIndex >= Hax2.playerList.Count)
			{
				return null;
			}
			return Hax2.playerList[Hax2.selectedPlayerIndex] as PlayerAvatar;
		}

		private static bool IsLocalAvatar(PlayerAvatar avatar)
		{
			if ((UnityEngine.Object)avatar == null)
			{
				return true;
			}
			if (avatar.photonView != null && avatar.photonView.IsMine)
			{
				return true;
			}
			PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
			return (UnityEngine.Object)local != null && local == avatar;
		}

		public static List<int> GetTokenList()
		{
			MetaManager meta = MetaManager.instance;
			if (meta == null)
			{
				return null;
			}
			if (_tokensField == null)
			{
				_tokensField = typeof(MetaManager).GetField("cosmeticTokens", InstAll);
			}
			return _tokensField?.GetValue(meta) as List<int>;
		}

		public static int CurrencyForRarity(SemiFunc.Rarity rarity)
		{
			switch (rarity)
			{
				case SemiFunc.Rarity.Common: return 2;
				case SemiFunc.Rarity.Uncommon: return 4;
				case SemiFunc.Rarity.Rare: return 8;
				case SemiFunc.Rarity.UltraRare: return 15;
				default: return 2;
			}
		}

		public static int SumCashoutValue(List<int> tokens)
		{
			if (tokens == null || tokens.Count == 0)
			{
				return 0;
			}
			int sum = 0;
			for (int i = 0; i < tokens.Count; i++)
			{
				sum += CurrencyForRarity((SemiFunc.Rarity)tokens[i]);
			}
			return sum;
		}

		public static string FormatTokenPreview()
		{
			try
			{
				List<int> tokens = GetTokenList();
				if (tokens == null)
				{
					return L.T("cos.token_none");
				}
				int c = 0, u = 0, r = 0, ur = 0;
				for (int i = 0; i < tokens.Count; i++)
				{
					switch ((SemiFunc.Rarity)tokens[i])
					{
						case SemiFunc.Rarity.Uncommon: u++; break;
						case SemiFunc.Rarity.Rare: r++; break;
						case SemiFunc.Rarity.UltraRare: ur++; break;
						default: c++; break;
					}
				}
				int cash = SumCashoutValue(tokens);
				int now = 0;
				try { now = SemiFunc.StatGetRunCurrency(); } catch { }
				return L.T("cos.token_preview_fmt", tokens.Count, c, u, r, ur, cash, now, now + cash);
			}
			catch (Exception ex)
			{
				return "tokens: " + ex.Message;
			}
		}

		/// <summary>
		/// Shop machine duplicate payout: skip remaining cosmetic rolls and convert every
		/// token to run currency (Common 2 / Uncommon 4 / Rare 8 / UltraRare 15). Host-synced.
		/// </summary>
		public static bool CashOutAllTokensAsCurrency()
		{
			try
			{
				List<int> tokens = GetTokenList();
				if (tokens == null)
				{
					LastStatus = "no token list";
					return false;
				}
				if (tokens.Count == 0)
				{
					LastStatus = L.T("cos.token_none");
					return false;
				}
				int gained = SumCashoutValue(tokens);
				int spent = tokens.Count;
				if (!NativeGameApi.IsHost())
				{
					LastStatus = L.T("cos.cashout_need_host") + "  +" + gained + "K";
					return false;
				}
				int now = SemiFunc.StatGetRunCurrency();
				SemiFunc.StatSetRunCurrency(now + gained);
				tokens.Clear();
				MetaManager.instance.Save();
				RebuildTokenUi();
				try
				{
					if (CurrencyUI.instance != null)
					{
						CurrencyUI.instance.FetchCurrency();
					}
				}
				catch { }
				try
				{
					if (ShopIncreaseUI.instance != null)
					{
						ShopIncreaseUI.instance.ShowIncrease(gained, 3f);
					}
				}
				catch { }
				LastStatus = L.T("cos.cashout_done_fmt", spent, gained, now + gained);
				return true;
			}
			catch (Exception ex)
			{
				LastStatus = "cashout: " + ex.Message;
				return false;
			}
		}

		/// <summary>
		/// Guest-legal dump: CosmeticShopMachine.Interact() → InteractClientRPC to the
		/// real master. Currency is still applied by the host in RewardCurrency.
		/// One token per Idle cycle (host animation). Unlocks leftover cosmetics first
		/// so CosmeticLockedGet returns null and the machine pays shop money.
		/// </summary>
		public static void ToggleMachineFeed(MonoBehaviour runner)
		{
			if (FeedingMachine)
			{
				StopMachineFeed();
				LastStatus = L.T("cos.feed_cancelled");
				return;
			}
			if (runner == null)
			{
				LastStatus = "no coroutine host";
				return;
			}
			List<int> tokens = GetTokenList();
			if (tokens == null || tokens.Count == 0)
			{
				LastStatus = L.T("cos.token_none");
				return;
			}
			if (CosmeticShopMachine.instance == null)
			{
				LastStatus = L.T("cos.feed_need_shop");
				return;
			}
			_feedHost = runner;
			FeedingMachine = true;
			_feedRoutine = runner.StartCoroutine(FeedMachineRoutine());
		}

		public static void StopMachineFeed()
		{
			FeedingMachine = false;
			if (_feedHost != null && _feedRoutine != null)
			{
				_feedHost.StopCoroutine(_feedRoutine);
			}
			_feedRoutine = null;
			_feedHost = null;
		}

		private static IEnumerator FeedMachineRoutine()
		{
			int inserted = 0;
			try
			{
				UnlockAll();
				while (FeedingMachine)
				{
					List<int> tokens = GetTokenList();
					if (tokens == null || tokens.Count == 0)
					{
						LastStatus = L.T("cos.feed_done_fmt", inserted);
						yield break;
					}
					CosmeticShopMachine machine = CosmeticShopMachine.instance;
					if (machine == null)
					{
						LastStatus = L.T("cos.feed_need_shop");
						yield break;
					}
					float wait = 0f;
					while (FeedingMachine && machine != null && !IsMachineIdle(machine) && wait < 25f)
					{
						LastStatus = L.T("cos.feed_wait_fmt", inserted, tokens.Count);
						wait += Time.deltaTime;
						yield return null;
						machine = CosmeticShopMachine.instance;
					}
					if (!FeedingMachine)
					{
						yield break;
					}
					if (machine == null)
					{
						LastStatus = L.T("cos.feed_need_shop");
						yield break;
					}
					if (!IsMachineIdle(machine))
					{
						LastStatus = L.T("cos.feed_timeout");
						yield break;
					}
					if (!InvokeMachineInteract(machine))
					{
						LastStatus = "Interact() missing";
						yield break;
					}
					float ack = 0f;
					while (FeedingMachine && machine != null && IsMachineIdle(machine) && ack < 3f)
					{
						ack += Time.deltaTime;
						yield return null;
						machine = CosmeticShopMachine.instance;
					}
					if (!FeedingMachine)
					{
						yield break;
					}
					if (machine == null)
					{
						LastStatus = L.T("cos.feed_need_shop");
						yield break;
					}
					if (IsMachineIdle(machine))
					{
						LastStatus = L.T("cos.feed_timeout");
						yield break;
					}
					inserted++;
					wait = 0f;
					while (FeedingMachine && machine != null && !IsMachineIdle(machine) && wait < 25f)
					{
						int left = GetTokenList()?.Count ?? 0;
						LastStatus = L.T("cos.feed_progress_fmt", inserted, left);
						wait += Time.deltaTime;
						yield return null;
						machine = CosmeticShopMachine.instance;
					}
				}
			}
			finally
			{
				FeedingMachine = false;
				_feedRoutine = null;
				_feedHost = null;
			}
		}

		private static bool IsMachineIdle(CosmeticShopMachine machine)
		{
			try
			{
				if (_machineState == null)
				{
					_machineState = typeof(CosmeticShopMachine).GetField("stateCurrent", InstAll);
				}
				object raw = _machineState?.GetValue(machine);
				return raw != null && (CosmeticShopMachine.State)raw == CosmeticShopMachine.State.Idle;
			}
			catch
			{
				return false;
			}
		}

		private static bool InvokeMachineInteract(CosmeticShopMachine machine)
		{
			try
			{
				if (_machineInteract == null)
				{
					_machineInteract = typeof(CosmeticShopMachine).GetMethod("Interact", InstAll, null, Type.EmptyTypes, null);
				}
				if (_machineInteract == null)
				{
					return false;
				}
				_machineInteract.Invoke(machine, null);
				return true;
			}
			catch (Exception ex)
			{
				LastStatus = "Interact: " + ex.Message;
				return false;
			}
		}

		public static bool SetAllTokensRarity(SemiFunc.Rarity rarity)
		{
			try
			{
				List<int> tokens = GetTokenList();
				if (tokens == null)
				{
					LastStatus = "no token list";
					return false;
				}
				if (tokens.Count == 0)
				{
					LastStatus = L.T("cos.token_none");
					return false;
				}
				int rarityValue = (int)rarity;
				for (int i = 0; i < tokens.Count; i++)
				{
					tokens[i] = rarityValue;
				}
				TokenRarity = rarity;
				MetaManager meta = MetaManager.instance;
				meta?.Save();
				RebuildTokenUi();
				LastStatus = L.T("cos.set_all_done_fmt", tokens.Count, RarityLabel(rarity));
				return true;
			}
			catch (Exception ex)
			{
				LastStatus = "rarity: " + ex.Message;
				return false;
			}
		}

		public static PlayerCosmetics GetLocalPlayerCosmetics()
		{
			foreach (PlayerCosmetics pc in EnumerateLocalCosmetics())
			{
				if (pc != null)
				{
					return pc;
				}
			}
			return null;
		}

		private static IEnumerable<PlayerCosmetics> EnumerateLocalCosmetics()
		{
			PlayerAvatar avatar = null;
			try { avatar = SemiFunc.PlayerAvatarLocal(); } catch { }
			if (avatar == null)
			{
				try { avatar = SemiFunc.PlayerGetLocal(); } catch { }
			}
			if (avatar != null)
			{
				if (avatar.playerCosmetics != null)
				{
					yield return avatar.playerCosmetics;
				}
				if (_deathHeadField == null)
				{
					_deathHeadField = typeof(PlayerAvatar).GetField("playerDeathHead", InstAll);
				}
				PlayerDeathHead head = _deathHeadField?.GetValue(avatar) as PlayerDeathHead;
				if (head != null && head.playerCosmetics != null)
				{
					yield return head.playerCosmetics;
				}
			}
			if (PlayerAvatarMenu.instance != null)
			{
				if (_menuCosmeticsField == null)
				{
					_menuCosmeticsField = typeof(PlayerAvatarMenu).GetField("playerCosmetics", InstAll);
				}
				PlayerCosmetics menuPc = _menuCosmeticsField?.GetValue(PlayerAvatarMenu.instance) as PlayerCosmetics;
				if (menuPc != null)
				{
					yield return menuPc;
				}
			}
		}

		private static void ApplyLocalCosmetics(bool sync, bool forced)
		{
			try
			{
				if (GameplayManager.instance != null)
				{
					SetMember(GameplayManager.instance, "cosmetics", true);
				}
			}
			catch
			{
			}
			List<int> equipped = null;
			try
			{
				equipped = new List<int>(GetEquipped(MetaManager.instance));
			}
			catch
			{
			}
			try
			{
				MetaManager.instance?.CosmeticPlayerUpdateLocal(sync, forced);
			}
			catch
			{
			}
			foreach (PlayerCosmetics pc in EnumerateLocalCosmetics())
			{
				if (equipped != null)
				{
					pc.SetupCosmetics(sync, forced, equipped);
				}
				else
				{
					pc.SetupCosmetics(sync, forced);
				}
				int[] colors = GetColors(MetaManager.instance);
				if (colors != null)
				{
					pc.SetupColors(sync, colors);
				}
			}
		}

		private static void SetMember(object instance, string name, object value)
		{
			if (instance == null)
			{
				return;
			}
			FieldInfo field = instance.GetType().GetField(name, InstAll);
			if (field != null)
			{
				field.SetValue(instance, value);
			}
		}

		private static void TintMaterials(PlayerCosmetics pc, Color rgb)
		{
			if (pc == null)
			{
				return;
			}
			if (_materialsField == null)
			{
				_materialsField = typeof(PlayerCosmetics).GetField("playerMaterials", InstAll);
			}
			List<PlayerMaterial> mats = _materialsField?.GetValue(pc) as List<PlayerMaterial>;
			if (mats == null)
			{
				return;
			}
			if (_albedoId == 0)
			{
				_albedoId = Shader.PropertyToID("_AlbedoColor");
				_emissionId = Shader.PropertyToID("_EmissionColor");
				_fresnelId = Shader.PropertyToID("_FresnelColor");
			}
			foreach (PlayerMaterial mat in mats)
			{
				if (mat == null || !mat.tintable)
				{
					continue;
				}
				try
				{
					mat.Setup();
				}
				catch
				{
				}
				Renderer renderer = mat.GetComponent<Renderer>();
				if (renderer == null)
				{
					continue;
				}
				Material material = renderer.material;
				if (material == null)
				{
					continue;
				}
				Color a = material.GetColor(_albedoId);
				material.SetColor(_albedoId, new Color(rgb.r, rgb.g, rgb.b, a.a));
				Color e = material.GetColor(_emissionId);
				material.SetColor(_emissionId, new Color(rgb.r, rgb.g, rgb.b, e.a));
				if (mat.tintFresnel)
				{
					material.SetColor(_fresnelId, new Color(rgb.r, rgb.g, rgb.b, 1f));
				}
			}
		}

		private static int NearestPaletteIndex(MetaManager meta, Color rgb)
		{
			int best = 0;
			float bestD = float.MaxValue;
			for (int i = 0; i < meta.colors.Count; i++)
			{
				if (meta.colors[i] == null)
				{
					continue;
				}
				Color c = meta.colors[i].color;
				float d = (c.r - rgb.r) * (c.r - rgb.r) + (c.g - rgb.g) * (c.g - rgb.g) + (c.b - rgb.b) * (c.b - rgb.b);
				if (d < bestD)
				{
					bestD = d;
					best = i;
				}
			}
			return best;
		}

		private static string ColorName(MetaManager meta, int index)
		{
			try
			{
				if (index >= 0 && index < meta.colors.Count && meta.colors[index] != null)
				{
					string n = meta.colors[index].colorName;
					if (!string.IsNullOrEmpty(n))
					{
						return n;
					}
				}
			}
			catch
			{
			}
			return LanguageManager.GetColorName(index);
		}

		private static List<int> GetUnlocked(MetaManager meta)
		{
			if (_unlocksField == null)
			{
				_unlocksField = typeof(MetaManager).GetField("cosmeticUnlocks", InstAll);
			}
			return _unlocksField?.GetValue(meta) as List<int> ?? new List<int>();
		}

		private static List<int> GetEquipped(MetaManager meta)
		{
			if (_equippedField == null)
			{
				_equippedField = typeof(MetaManager).GetField("cosmeticEquipped", InstAll);
			}
			return _equippedField?.GetValue(meta) as List<int> ?? new List<int>();
		}

		private static int[] GetColors(MetaManager meta)
		{
			if (_colorsField == null)
			{
				_colorsField = typeof(MetaManager).GetField("colorsEquipped", InstAll);
			}
			return _colorsField?.GetValue(meta) as int[];
		}

		private static int[] EnsureColorArray(MetaManager meta)
		{
			int[] colors = GetColors(meta);
			int needed = Enum.GetValues(typeof(SemiFunc.CosmeticType)).Length;
			if (colors == null || colors.Length < needed)
			{
				int[] next = new int[needed];
				if (colors != null)
				{
					Array.Copy(colors, next, Math.Min(colors.Length, needed));
				}
				colors = next;
				_colorsField?.SetValue(meta, colors);
			}
			return colors;
		}

		private static void SetMetaColors(MetaManager meta, int[] colors)
		{
			for (int i = 0; i < colors.Length; i++)
			{
				meta.CosmeticColorSet(i, colors[i]);
			}
		}

		private static void RefreshTokenUi()
		{
			RebuildTokenUi();
		}

		private static void RebuildTokenUi()
		{
			try
			{
				if (CosmeticTokenUI.instance == null)
				{
					return;
				}
				FieldInfo listField = typeof(CosmeticTokenUI).GetField("tokenObjects", InstAll);
				if (listField?.GetValue(CosmeticTokenUI.instance) is List<CosmeticTokenUIElement> objects)
				{
					for (int i = 0; i < objects.Count; i++)
					{
						CosmeticTokenUIElement el = objects[i];
						if (el != null)
						{
							UnityEngine.Object.Destroy(el.gameObject);
						}
					}
					objects.Clear();
				}
				CosmeticTokenUI.instance.Setup();
			}
			catch
			{
				try
				{
					if (CosmeticTokenUI.instance != null)
					{
						CosmeticTokenUI.instance.Setup();
					}
				}
				catch
				{
				}
			}
		}
	}
}
