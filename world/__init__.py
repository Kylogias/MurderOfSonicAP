from worlds.AutoWorld import World

from .items import ItemState
from .locations import LocationState
from .options import TMOSTHOptions

from .apshared import apshared
from .apshared import location_name_to_id as lname_to_id
from .apshared import item_name_to_id as iname_to_id

class TMOSTHWorld(World):
	ut_can_gen_without_yaml = True
	
	game = apshared["game"]
	location_name_to_id = lname_to_id
	item_name_to_id = iname_to_id

	options_dataclass = TMOSTHOptions
	options: TMOSTHOptions
	
	def generate_early(self):
		self.location_state = LocationState()
		self.item_state = ItemState()

		if self.options.car_rando:
			self.car_rando = True
		else:
			self.car_rando = False

		re_gen_passthrough = getattr(self.multiworld, "re_gen_passthrough", {})
		if re_gen_passthrough and self.game in re_gen_passthrough:
			slot_data = re_gen_passthrough[self.game]
			self.car_rando = slot_data["car_rando"]

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
			"version": apshared["version"],
			"car_rando": self.car_rando
		}

	@staticmethod
	def interpret_slot_data(slot_data):
		return slot_data