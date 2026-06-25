using UnityEngine;
using UnityEngine.UI;
using MelonLoader;
using HarmonyLib;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using Ink.Runtime;
using Newtonsoft.Json.Linq;

[assembly: MelonInfo(typeof(tmosthap.ModMain), "TMOSTH-AP", "0.1.0", "Kylogias")]
[assembly: MelonGame("Sonic Social", "The Murder of Sonic The Hedgehog")]

namespace tmosthap {
	class ModMain : MelonMod {
		public enum QOLOption {
			Off = 0,
			On = 1,
			AfterFirstComplete = 2
		}
		
		static MelonPreferences_Entry<string> ip;
		static MelonPreferences_Entry<int> port;
		static MelonPreferences_Entry<string> slot;
		static MelonPreferences_Entry<string> password;
		static MelonPreferences_Entry<bool> deathlinkEnabled;
		static MelonPreferences_Entry<QOLOption> autoskip;
		
		public static ArchipelagoSession currentSession;
		public static DeathLinkService deathLink;
		static Dictionary<string, object> slotDataDict;
		static Dictionary<ItemIds, int> itemState;

		class DisplayedMessage {
			public GameObject go;
			public float timeLeft;
		}
		private static DisplayedMessage[] messageText;
		static Queue<string> messages;

		static GameObject inventoryListener;

		static int lastItem;
		static bool invDirty;
		static bool deathlinkPending;
		
		public override void OnInitializeMelon() {
			MelonPreferences.CreateCategory("Archipelago");
			ip = MelonPreferences.CreateEntry<string>("Archipelago", "IP", "localhost");
			port = MelonPreferences.CreateEntry<int>("Archipelago", "Port", 38281);
			slot = MelonPreferences.CreateEntry<string>("Archipelago", "Slot", "Player1");
			password = MelonPreferences.CreateEntry<string>("Archipelago", "Password", "");
			deathlinkEnabled = MelonPreferences.CreateEntry<bool>("Archipelago", "Deathlink Enabled", false);
			autoskip = MelonPreferences.CreateEntry<QOLOption>("Archipelago", "Autoskip Dialog", QOLOption.Off);
			
			new SlotData();
			messages = new Queue<string>();
			itemState = new Dictionary<ItemIds, int>();
			messageText = new DisplayedMessage[5];
			for (int i = 0; i < messageText.Length; i++) {
				messageText[i] = new DisplayedMessage();
			}
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
			GameObject canvasGO = new GameObject("APCanvas", typeof(Canvas), typeof(CanvasScaler));
			canvasGO.transform.SetParent(ap.transform);
			Canvas canvas = canvasGO.GetComponent<Canvas>();
			canvas.sortingOrder = 10;
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			for (int i = 0; i < messageText.Count(); i++) {
				GameObject textGO = new GameObject(string.Format("APText {0}", i), typeof(Text));
				messageText[i].go = textGO;
				messageText[i].timeLeft = 0;
				textGO.SetActive(false);
				textGO.transform.SetParent(canvasGO.transform);
				Text text = textGO.GetComponent<Text>();
				text.GetType().GetMethod("AssignDefaultFont", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(text, new object[]{});
				text.text = "chat looks a bit dead";
				text.alignment = TextAnchor.MiddleCenter;
				text.resizeTextForBestFit = true;
				text.fontSize = 1000;
				text.supportRichText = true;
				RectTransform textRect = textGO.GetComponent<RectTransform>();
				textRect.localPosition = new Vector3(0, -((Screen.height/2)-(Screen.height/16)), 0);
				textRect.sizeDelta = new Vector2(Screen.width, Screen.height/8);
			}

			RectTransform invXfrm = InventoryScreen.Instance.gameObject.GetComponent<RectTransform>();
			RectTransform invWndXfrm = (RectTransform)invXfrm.Find("WindowRoot");
			invWndXfrm.sizeDelta = invWndXfrm.sizeDelta * new Vector2(1.6f, 1);
			RectTransform invLayout = (RectTransform)invWndXfrm.Find("InventoryLayout");
			float oldX = invLayout.rect.x;
			invLayout.sizeDelta = invLayout.sizeDelta * new Vector2(2, 1);
			invLayout.anchoredPosition = invLayout.anchoredPosition * new Vector2(1.5f, 1);
			RectTransform itemDetails = (RectTransform)invWndXfrm.Find("ItemDetails");
			itemDetails.anchoredPosition = itemDetails.anchoredPosition * new Vector2(1.5f, 1);

			EventManager.StartListening("MinigameFinished", playerDiedListener);
			EventManager.StartListening("RunnerPlayerDied", playerDiedListener);
		}
		
		[HarmonyPatch(typeof(InventoryScreen), "Open")]
		private class InventoryScreenPatch {
			private static void Postfix (InventoryScreen __instance, List<InventoryItemThumbnailView> ___currentItems) {
				for (int i = ___currentItems.Count; i < 16; i++) {
					MethodInfo mi = __instance.GetType().GetMethod("CreateThumbnail", BindingFlags.NonPublic | BindingFlags.Instance);
					___currentItems.Add((InventoryItemThumbnailView)mi.Invoke(__instance, new object[]{""}));
				}
			}
		}
		
		static bool envButton = false;
		[HarmonyPatch(typeof(ChangeEnvironmentButton), "OnClick")]
		private class EnvButtonPatch {
			private static void Prefix() {
				envButton = true;
			}
		}
		
		public static string getEnvironment() {
			GameObject go = GameObject.Find("Canvas/EnvironmentFrame");
			if (!go) return "";
			return go.GetComponent<EnvironmentView>().GetEnvironmentKey();
		}
		
		[HarmonyPatch(typeof(EnvironmentView), "OnMoveToEnvironment")]
		private class EnvironmentPatch {
			private static bool Prefix(Dictionary<string, object> message) {
				if (currentSession == null) return false;
				bool canContinue = !SlotData.carRando || envButton;
				string fromEnv = getEnvironment();
				if (!envButton) {
					JArray complete_envs = (JArray)currentSession.DataStorage["complete_envs"];
					bool completeEnv = false;
					foreach (string env in complete_envs) {
						if (env == fromEnv) completeEnv = true;
					}
					if (!completeEnv) currentSession.DataStorage["complete_envs"] += new[]{fromEnv};
				}
				if (fromEnv == "Conductor_Car" || fromEnv == "LockdownDiningCar") canContinue = true;
				if (!canContinue) return false;
				envButton = false;
				JArray seen_envs = (JArray)currentSession.DataStorage["seen_envs"];
				bool seenEnv = false;
				foreach (string env in seen_envs) {
					if (env == (string)message["DestinationKey"]) seenEnv = true;
				}
				
				if (!seenEnv) {
					currentSession.DataStorage["seen_envs"] += new []{(string)message["DestinationKey"]};
				}
				return true;
			}
		}
		
		public static bool isQOLEnabled(QOLOption qol) {
			switch (qol) {
				case QOLOption.Off: return false;
				case QOLOption.On: return true;
				case QOLOption.AfterFirstComplete:
					if (currentSession == null) return false;
					string curEnv = getEnvironment();
					JArray envs = (JArray)currentSession.DataStorage["complete_envs"];
					bool hasString = false;
					foreach (string env in envs) {
						if (env == curEnv) hasString = true;
					}
					return hasString;
			}
			return false;
		}
		
		public override void OnUpdate() {
			if (currentSession == null) return;

			if (invDirty) {if (rebuildInventory()) invDirty = false; else if (messages.Count() == 0) messages.Enqueue("<color=#FF0000>Unable to Rebuild Inventory! Quit to Menu and Reconnect!</color>");}
			if (deathlinkPending) {
				if (RunnerGameManager.Instance.gameObject.activeSelf) {
					isDeathLink = true;
					RunnerGameManager.Instance.OnLose();
				} else if (DialogView.Instance.gameObject.activeSelf) {
					GameManager.Instance.EndConversation();
				}
				deathlinkPending = false;
			}
			
			foreach (DisplayedMessage dm in messageText) {
				dm.timeLeft -= Time.unscaledDeltaTime;
				if (dm.go) {
					if (dm.timeLeft < 0) dm.go.SetActive(false);
					else dm.go.SetActive(true);
				}
			}
			if (messages.Count() > 0 && messageText[0].go != null) {
				int index = -1;
				bool found = false;
				for (index = 0; index < messageText.Count(); index++) {
					if (!messageText[index].go.activeSelf) {
						found = true;
						break;
					}
				}
				if (!found) {
					float maxY = -100000;
					index = -1;
					for (int i = 0; i < messageText.Count(); i++) {
						RectTransform xfrm = messageText[i].go.GetComponent<RectTransform>();
						if (xfrm.localPosition.y > maxY) {
							maxY = xfrm.localPosition.y;
							index = i;
						}
					}
					messageText[index].timeLeft = -1;
				} else {
					RectTransform textRect = messageText[index].go.GetComponent<RectTransform>();
					textRect.localPosition = new Vector3(0, -((Screen.height/2)-(Screen.height/16)), 0);
					textRect.sizeDelta = new Vector2(Screen.width, Screen.height/8);
					for (int i = 0; i < messageText.Count(); i++) {
						if (!messageText[i].go.activeSelf) continue;
						RectTransform moveup = messageText[i].go.GetComponent<RectTransform>();
						moveup.localPosition = new Vector3(0, moveup.localPosition.y + moveup.sizeDelta.y/2, 0);
					}

					messageText[index].timeLeft = 5;
					Text text = messageText[index].go.GetComponent<Text>();
					text.text = string.Format("<b>{0}</b>", messages.Dequeue());
				}
			}
			
			if (isQOLEnabled(autoskip.Value)) {
				DialogView dv = DialogView.Instance;
				Button btn = (Button)dv.GetType().GetField("fullscreenSkipButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dv);
				if (dv.gameObject.activeSelf && btn.interactable && StoryManager.Instance.story.canContinue) {
					dv.GetType().GetMethod("OnClickContinue", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(dv, new object[]{});
				}
			}
			if (Input.GetKeyDown(KeyCode.BackQuote)) {
				Transform rooms = GameObject.Find("Canvas/DebugMapScreen/WindowRoot").transform;
				JArray envs = (JArray)currentSession.DataStorage["seen_envs"];
				List<string> hasCars = new List<string>();
				if (itemState[ItemIds.CASINO] > 0) hasCars.Add("SafeRoom");
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

		[HarmonyPatch(typeof(TitleScreen), "OnLoadingSave")]
		private static class ContinuePatch {
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
			LoginResult result = currentSession.TryConnectAndLogin("The Murder of Sonic the Hedgehog", slot.Value, ItemsHandlingFlags.AllItems, password: password.Value);
			
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
				messages.Enqueue("Successful Connection to TMOSTH! You may now collect checks");
				int i = 0;
				foreach (ItemInfo item in currentSession.Items.AllItemsReceived) {
					onItem(item, true);
					i++;
				}
				lastItem = i;
				while (currentSession.Items.Any()) currentSession.Items.DequeueItem();

				currentSession.DataStorage["seen_envs"].Initialize(new JArray());
				currentSession.DataStorage["complete_envs"].Initialize(new JArray());
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
			StringBuilder sb = new StringBuilder("", 65536);
			MelonLogger.Msg("Message Part Count: {0}", message.Parts.Count());
			foreach (MessagePart part in message.Parts) {
				if (!part.IsBackgroundColor) sb.AppendFormat("<color=#{0:X2}{1:X2}{2:X2}>", part.Color.R, part.Color.G, part.Color.B);
				sb.Append(part.Text);
				if (!part.IsBackgroundColor) sb.Append("</color>");
			}
			messages.Enqueue(sb.ToString());
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
		
		private static bool rebuildInventory() {
			if (!StoryManager.Instance) return false;
			if (!StoryManager.Instance.story) return false;
			if (StoryManager.Instance.story.variablesState == null) return false;
			if (StoryManager.Instance.story.variablesState["Inventory"] == null) return false;
			InkList inventory = (InkList)StoryManager.Instance.story.variablesState["Inventory"];
			if (inventory.origins == null) return false;
			inventory.Clear();
			foreach (APItem item in APShared.items) {
				if (item.inventory == "") continue;
				if (!itemState.ContainsKey((ItemIds)item.id)) continue;
				if (itemState[(ItemIds)item.id] > 0) inventory.AddItem(item.inventory);
			}
			return true;
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
			invDirty = true;
		}
		
		private static void playerDiedListener(Dictionary<string, object> message) {
			if (message != null && message.ContainsKey("PlayerWon") && (bool)message["PlayerWon"]) return;
			sendDeathLink("was unable to THINK!");
		}
		
		public static bool isDeathLink = false;
		public static void sendDeathLink(string source) {
			string cause = string.Format("{0} {1}", slot.Value, source);
			if (deathLink != null && deathlinkEnabled.Value && !isDeathLink) {
				deathLink.SendDeathLink(new DeathLink(slot.Value, cause));
			}
			isDeathLink = false;
		}

		public static void HandleDeathLink(DeathLink deathLink) {
			StringBuilder sb = new StringBuilder("", 65536);
			sb.Append("<color=#E02010>Death Link: ");
			if (deathLink.Cause != null) sb.Append(deathLink.Cause);
			else sb.Append(deathLink.Source);
			sb.Append("</color>");
			messages.Enqueue(sb.ToString());
			deathlinkPending = true;
		}
	}
}