using System.Collections.Generic;

namespace tmosthap {
	class SlotData {
		public static int version;
		public static bool carRando;

		public SlotData(Dictionary<string, object> slot) {
			version = (int)(long)slot["version"];
			carRando = (bool)slot["car_rando"];
		}

		public SlotData() {
			version = -1;
		}
	}
}