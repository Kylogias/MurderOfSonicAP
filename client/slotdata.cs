using System.Collections.Generic;

namespace tmosthap {
	class SlotData {
		public static int version;

		public SlotData(Dictionary<string, object> slot) {
			version = (int)(long)slot["version"];
		}

		public SlotData() {
			version = -1;
		}
	}
}