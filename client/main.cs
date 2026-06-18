using UnityEngine;
using MelonLoader;
using HarmonyLib;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using System.Collections.Generic;
using System.Reflection;
using Ink.Runtime;
using Newtonsoft.Json.Linq;

[assembly: MelonInfo(typeof(tmosthap.ModMain), "TMOSTH-AP", "0.1.0", "Kylogias")]
[assembly: MelonGame("Sonic Social", "The Murder of Sonic The Hedgehog")]

namespace tmosthap {
	class ModMain : MelonMod {
		static MelonPreferences_Entry<string> ip;
		static MelonPreferences_Entry<int> port;
		static MelonPreferences_Entry<string> slot;
		static MelonPreferences_Entry<string> password;
		static MelonPreferences_Entry<bool> deathlinkEnabled;
		
		public static ArchipelagoSession currentSession;
		public static DeathLinkService deathLink;
		static Dictionary<string, object> slotDataDict;
		static Dictionary<ItemIds, int> itemState;
		static Queue<string> messages;

		static GameObject inventoryListener;

		static int lastItem;
		
		public override void OnInitializeMelon() {
			MelonPreferences.CreateCategory("Archipelago");
			ip = MelonPreferences.CreateEntry<string>("Archipelago", "IP", "localhost");
			port = MelonPreferences.CreateEntry<int>("Archipelago", "Port", 38281);
			slot = MelonPreferences.CreateEntry<string>("Archipelago", "Slot", "Player1");
			password = MelonPreferences.CreateEntry<string>("Archipelago", "Password", "");
			deathlinkEnabled = MelonPreferences.CreateEntry<bool>("Archipelago", "Deathlink Enabled", false);
			
			new SlotData();
			messages = new Queue<string>();
			itemState = new Dictionary<ItemIds, int>();
		}
		
		public override void OnSceneWasInitialized(int buildIndex, string sceneName) {
			/*
			GameObject tabs = GameObject.Find("Canvas/ScreenRootLayer").transform.GetChild(4).Find("OverlayScreenBase/WindowRoot/Tabs").gameObject;
			GameObject tabTemplate = tabs.transform.GetChild(1).gameObject;

			GameObject apTab = GameObject.Instantiate(tabTemplate, tabs.transform);
			apTab.transform.SetSiblingIndex(1);
			apTab.transform.Find("AnimAnchor/Backing/Text (TMP)").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Archipelago";
			*/
			GameObject ap = new GameObject("Archipelago");
			inventoryListener = new GameObject("Inventory Listener", typeof(OnInventoryChange));
			inventoryListener.transform.SetParent(ap.transform);
			inventoryListener.SetActive(false);
		}

		static bool envButton = false;
		[HarmonyPatch(typeof(ChangeEnvironmentButton), "OnClick")]
		private class EnvButtonPatch {
			private static void Prefix() {
				envButton = true;
			}
		}
		
		[HarmonyPatch(typeof(EnvironmentView), "OnMoveToEnvironment")]
		private class EnvironmentPatch {
			private static bool Prefix(Dictionary<string, object> message) {
				if (currentSession == null) return false;
				if (SlotData.carRando && !envButton) return false;
				envButton = false;
				JArray envs = (JArray)currentSession.DataStorage["seen_envs"];
				MelonLogger.Msg("{0}", envs);
				bool hasString = false;
				foreach (string env in envs) {
					if (env == (string)message["DestinationKey"]) hasString = true;
				}
				
				if (!hasString) {
					currentSession.DataStorage["seen_envs"] += new []{(string)message["DestinationKey"]};
				}
				return true;
			}
		}
		
		public override void OnUpdate() {
			if (currentSession == null) return;
			if (Input.GetKeyDown(KeyCode.BackQuote)) {
				Transform rooms = GameObject.Find("Canvas/DebugMapScreen/WindowRoot").transform;
				JArray envs = (JArray)currentSession.DataStorage["seen_envs"];
				List<string> hasCars = new List<string>();
				foreach (APItem item in APShared.items) {
					if (itemState[(ItemIds)item.id] > 0) hasCars.Add(item.env);
				}
				MelonLogger.Msg("{0}", envs);
				for (int i = 0; i < rooms.childCount; i++) {
					GameObject room = rooms.GetChild(i).gameObject;
					ChangeEnvironmentButton ceb = room.GetComponent<ChangeEnvironmentButton>();
					if (ceb == null) {
						room.SetActive(false);
						continue;
					}
					FieldInfo fi = ceb.GetType().GetField("Destination", BindingFlags.NonPublic | BindingFlags.Instance);
					string dest = (string)fi.GetValue(ceb);
					bool hasString = hasCars.Contains(dest);
					foreach (string env in envs) {
						if (env == dest) hasString = true;
					}
					room.SetActive(hasString);
				}
				EventManager.TriggerEvent("ToggleMap", null);
			}
		}
		
		[HarmonyPatch(typeof(TitleScreen), "OnClickNewGame")]
		private static class NewGamePatch {
			private static void Prefix() {
				ConnectToArchipelago(true);
				inventoryListener.SetActive(true);
			}
		}
		
		public static void ConnectToArchipelago(bool newServer) {
			if (newServer) {
				lastItem = -1;
				foreach (APItem item in APShared.items) {
					itemState[(ItemIds)item.id] = 0;
				}
				if (currentSession != null) {
					currentSession.Items.ItemReceived -= HandleItem;
					currentSession.MessageLog.OnMessageReceived -= OnMessageReceived;
					deathLink.DisableDeathLink();
					currentSession = null;
					deathLink = null;
				}
				new SlotData();
			}
			
			currentSession = ArchipelagoSessionFactory.CreateSession(ip.Value, port.Value);
			LoginResult result;
			if (password.Value.Length != 0) result = currentSession.TryConnectAndLogin("The Murder of Sonic the Hedgehog", slot.Value, ItemsHandlingFlags.AllItems, password: password.Value);
			else result = currentSession.TryConnectAndLogin("The Murder of Sonic the Hedgehog", slot.Value, ItemsHandlingFlags.AllItems);
			
			if (result.Successful) {
				slotDataDict = ((LoginSuccessful)result).SlotData;
				string curRoom = currentSession.RoomState.Seed;
				
				if ((long)slotDataDict["version"] != APShared.version) {
					string msg = string.Format("Client Version does not match APWorld (expected {0}, got {1})! Refusing connection", APShared.version, slotDataDict["version"]);
					MelonLogger.Error(msg);
					messages.Enqueue(msg);
					currentSession = null;
					return;
				}

				new SlotData(slotDataDict);
				currentSession.Items.ItemReceived += HandleItem;
				currentSession.MessageLog.OnMessageReceived += OnMessageReceived;
				deathLink = DeathLinkProvider.CreateDeathLinkService(currentSession);
				deathLink.OnDeathLinkReceived += HandleDeathLink;
				if (deathlinkEnabled.Value) deathLink.EnableDeathLink();
				messages.Enqueue("Successful Connection to Spark 3! You may now collect checks");
				int i = 0;
				foreach (ItemInfo item in currentSession.Items.AllItemsReceived) {
					onItem(item, true);
					i++;
				}
				lastItem = i;
				while (currentSession.Items.Any()) currentSession.Items.DequeueItem();

				currentSession.DataStorage["seen_envs"].Initialize(new JArray());
			} else {
				MelonLogger.Error("Error while connecting to Archipelago");
				messages.Enqueue("Error while connecting to Archipelago");
				foreach(string e in ((LoginFailure)result).Errors) {
					MelonLogger.Error(e);
					messages.Enqueue(e);
				}
				currentSession = null;
			}
		}

		public static void OnMessageReceived(LogMessage message) {
			
		}

		[HarmonyPatch(typeof(InventoryListener), "Evaluate")]
		private class InventoryListenerPatch {
			private static bool Prefix() {
				return false;
			}
		}
		
		class OnInventoryChange : InkVariableListener {
			public OnInventoryChange() {
				InkVariable = "Inventory";
			}
			
			protected override void Evaluate(string varName, object value) {
				rebuildInventory();
			}
		}
		
		private static void rebuildInventory() {
			if (!StoryManager.Instance) return;
			if (!StoryManager.Instance.story) return;
			if (StoryManager.Instance.story.variablesState == null) return;
			if (StoryManager.Instance.story.variablesState["Inventory"] == null) return;
			InkList inventory = (InkList)StoryManager.Instance.story.variablesState["Inventory"];
			inventory.Clear();
			foreach (APItem item in APShared.items) {
				if (item.inventory == "") continue;
				if (!itemState.ContainsKey((ItemIds)item.id)) continue;
				if (itemState[(ItemIds)item.id] > 0) inventory.AddItem(item.inventory);
			}
		}
		
		public static void HandleItem(ReceivedItemsHelper itemHandler) {
			while (itemHandler.Any()) {
				int oldIndex = itemHandler.Index;
				if (oldIndex <= lastItem) continue;
				ItemInfo item = itemHandler.DequeueItem();
				lastItem = oldIndex;
				onItem(item, false);
			}
		}

		private static void onItem(ItemInfo item, bool catchup)  {
			MelonLogger.Msg("Receiving {0} with ID {1}", item.ItemDisplayName, item.ItemId);
			itemState[(ItemIds)item.ItemId] += 1;
			rebuildInventory();
		}

		public static void HandleDeathLink(DeathLink deathLink) {
			
		}
	}
}