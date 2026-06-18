namespace tmosthap {
	class APItem {
		public long id;
		public string inventory;
		public string env;
		public APItem(long i, string inv, string e) {
			id = i;
			inventory = inv;
			env = e;
		}
	}
	
	class APRoomCheck {
		public string sanity;
		public string obj;
		public int index;
		public long id;
		public APRoomCheck(string t, string o, int idx, long i) {
			sanity = t;
			obj = o;
			index = idx;
			id = i;
		}
	}

	class APRoom {
		public string env;
		public APRoomCheck[] checks;
		public APRoom(string e, APRoomCheck[] c) {
			env = e;
			checks = c;
		}
	}
}