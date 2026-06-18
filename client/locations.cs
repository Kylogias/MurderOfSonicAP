using UnityEngine;
using HarmonyLib;
using MelonLoader;

namespace tmosthap {
	class Locations {
		[HarmonyPatch(typeof(StartDialog), "StartDialogAt")]
		private class DialogPatch {
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

		public static void sendLocation(string sanity, string obj) {
			string env = GameObject.Find("Canvas/EnvironmentFrame").GetComponent<EnvironmentView>().GetEnvironmentKey();
			foreach (APRoom room in APShared.rooms) {
				if (room.env == env) {
					foreach (APRoomCheck check in room.checks) {
						if (check.sanity == sanity && check.obj == obj) {
							ModMain.currentSession.Locations.CompleteLocationChecksAsync(null, check.id);
							return;
						}
					}
				}
			}
			MelonLogger.Msg("Unable to find sanity {0} object {1} in environment {2}", sanity, obj, env);
		}

		public static void sendLocation(string sanity, int index) {
			string env = GameObject.Find("Canvas/EnvironmentFrame").GetComponent<EnvironmentView>().GetEnvironmentKey();
			foreach (APRoom room in APShared.rooms) {
				if (room.env == env) {
					foreach (APRoomCheck check in room.checks) {
						if (check.sanity == sanity && check.index == index) {
							ModMain.currentSession.Locations.CompleteLocationChecksAsync(null, check.id);
							return;
						}
					}
				}
			}
			MelonLogger.Msg("Unable to find sanity {0} index {1} in environment {2}", sanity, index, env);
		}
	}
}