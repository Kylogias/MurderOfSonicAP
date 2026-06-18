from worlds.AutoWorld import World

from .apshared import apshared
from .apshared import location_name_to_id as lname_to_id
from .apshared import item_name_to_id as iname_to_id

from .locations import LocationState
from .items import ItemState

class TMOSTHWorld(World):
	game = apshared["game"]
	location_name_to_id = lname_to_id
	item_name_to_id = iname_to_id
	
	def generate_early(self):
		self.location_state = LocationState()
		self.item_state = ItemState()

	def create_regions(self):
		self.location_state.setup_regions(self)

	def create_items(self):
		self.item_state.setup_items(self)

	def create_item(self, name):
		return self.item_state.create_item(self, name)

	def get_filler_item_name(self):
		return self.item_state.get_filler_item(self)

	def fill_slot_data(self):
		return {
			"version": apshared["version"]
		}