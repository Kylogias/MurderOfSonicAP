using UnityEngine;
using HarmonyLib;
using MelonLoader;
using Ink.Runtime;
using System;
using System.Linq;
using System.Reflection;

namespace tmosthap {
	class Locations {
		[HarmonyPatch(typeof(StartDialog), "StartDialogAt")]
		private class StartDialogPatch {
			private static void Prefix(StartDialog __instance) {
				MelonLogger.Msg("Sending Check for Object {0}", __instance.gameObject.name);
				sendLocation("obj", __instance.gameObject.name);
			}
		}
	
		[HarmonyPatch(typeof(TrackManager), "WinCoroutine")]
		private class RunnerPatch {
			private static void Prefix(RunnerGameManager __instance, bool ___endCoroutineRunning, RunnerLevelData ____levelData) {
				if (!___endCoroutineRunning) {
					MelonLogger.Msg("Sending Check for Runner {0}", ____levelData.databaseID);
					sendLocation("run", ____levelData.databaseID);
					if (____levelData.databaseID == 31) {
						MelonLogger.Msg("Goal Completed!");
						ModMain.currentSession.SetGoalAchieved();
					}
				}
			}
		}
	
		[HarmonyPatch(typeof(CycleScreenAssets), "UpdateImage")]
		private class ScreenPatch {
			private static void Prefix(CycleScreenAssets __instance) {
				MelonLogger.Msg("Sending Check for Screen {0}", __instance.gameObject.name);
				sendLocation("scr", __instance.gameObject.name);
			}
		}

		[HarmonyPatch(typeof(DialogView), "OnClickChoiceButton")]
		private class DialogPatch {
			private static void Prefix(DialogView __instance, Choice choice) {
				MelonLogger.Msg("Sending check for Dialog {0}", choice.pathStringOnChoice);
				sendDialogLocation("dialog", choice.pathStringOnChoice);
				if (sendDialogLocation("deduct", choice.pathStringOnChoice)) {
					ModMain.sendDeathLink("failed to deduce properly");
				}
			}
		}

		[HarmonyPatch(typeof(TooManyChoices), "OnClickChoiceButton")]
		private class TooManyChoicesPatch {
			private static void Prefix(Choice choice) {
				MelonLogger.Msg("Sending check for Dialog {0}", choice.pathStringOnChoice);
				if (sendDialogLocation("deduct", choice.pathStringOnChoice)) {
					ModMain.sendDeathLink("is too panicked about the ticking");
				}
			}
		}

		[HarmonyPatch(typeof(InventoryScreen), "Open")]
		private class InventoryScreenPatch {
			private static void Prefix(InventoryScreen __instance, ref Action action) {
				if (action != null) {
					Action temp = action;
					action = (() => {
						var selectedItem = (InventoryItemThumbnailView)__instance.GetType().GetField("selectedItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
						MelonLogger.Msg("Sending Check for Inventory {0} (if correct)", StoryManager.Instance.story.state.currentPathString);
						if (sendDialogLocation("deduct", StoryManager.Instance.story.state.currentPathString, selectedItem.ItemKey)) {
							ModMain.sendDeathLink("couldn't find the correct item");
						}
						temp();
					});
				}
			}
		}
		
		[HarmonyPatch(typeof(LibraryMapScreen), "OnLineItemClicked")]
		private class LibraryMapPatch {
			private static void Prefix(Transform itemTransform) {
				int idx = itemTransform.GetSiblingIndex();
				MelonLogger.Msg("Sending Check for Library Map {0}", idx);
				if (sendLocation("deduct", idx)) {
					ModMain.sendDeathLink("couldn't find Espio's reading nook");
				}
			}
		}

		[HarmonyPatch(typeof(ArcadeScreen), "OnLineItemClicked")]
		private class ArcadePatch {
			private static void Prefix(Transform itemTransform) {
				int idx = itemTransform.GetSiblingIndex();
				MelonLogger.Msg("Sending Check for Arcade Screen {0}", idx);
				if (sendLocation("deduct", idx)) {
					ModMain.sendDeathLink("couldn't read Tails' mind");
				}
			}
		}
		
		[HarmonyPatch(typeof(WhoDunnitScreen), "OnLineItemClicked")]
		private class WhoDunnitPatch {
			private static void Prefix(Transform itemTransform) {
				int idx = itemTransform.GetSiblingIndex();
				MelonLogger.Msg("Sending Check for Who Dunnit {0}", idx);
				if (sendLocation("deduct", idx)) {
					ModMain.sendDeathLink("was not the Imposter. 1 imposter remains");
				}
			}
		}
		
		public static bool sendLocation(string sanity, string obj) {
			string env = GameObject.Find("Canvas/EnvironmentFrame").GetComponent<EnvironmentView>().GetEnvironmentKey();
			foreach (APRoom room in APShared.rooms) {
				if (room.env == env) {
					foreach (APRoomCheck check in room.checks) {
						if (check.sanity == sanity && check.obj == obj) {
							ModMain.currentSession.Locations.CompleteLocationChecksAsync(null, check.id);
							if (check.death == "true") return true;
							return false;
						}
					}
				}
			}
			MelonLogger.Msg("Unable to find check!\n\tenvironment {2}\n\tsanity {0}\n\tobject {1}", sanity, obj, env);
			return false;
		}

		public static bool sendDialogLocation(string sanity, string dialog, string item = null) {
			string env = GameObject.Find("Canvas/EnvironmentFrame").GetComponent<EnvironmentView>().GetEnvironmentKey();
			foreach (APRoom room in APShared.rooms) {
				if (room.env == env) {
					foreach (APRoomCheck check in room.checks) {
						if (check.sanity == sanity && check.dialog.Contains(dialog)) {
							if (item == null || item == check.death) {
								ModMain.currentSession.Locations.CompleteLocationChecksAsync(null, check.id);
								if (check.death == "true") return true;
								return false;
							}
							return true;
						}
					}
				}
			}
			MelonLogger.Msg("Unable to find check!\n\tenvironment {2}\n\tsanity {0}\n\tdialog {1}", sanity, dialog, env);
			return false;
		}
		
		public static bool sendLocation(string sanity, int index) {
			string env = ModMain.getEnvironment();
			foreach (APRoom room in APShared.rooms) {
				if (room.env == env) {
					foreach (APRoomCheck check in room.checks) {
						if (check.sanity == sanity && check.index == index) {
							ModMain.currentSession.Locations.CompleteLocationChecksAsync(null, check.id);
							if (check.death == "true") return true;
							return false;
						}
					}
				}
			}
			MelonLogger.Msg("Unable to find check!\n\tenvironment {2}\n\tsanity {0}\n\tindex {1}", sanity, index, env);
			return false;
		}
	}
}