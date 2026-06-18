from BaseClasses import Region, Location
from rule_builder.rules import True_, Has
from .apshared import apshared, location_name_to_id
from .items import TMOSTHItem

class TMOSTHLocation(Location):
	game = apshared["game"]

class LocationState:
	def __init__(self):
		self.car_regions = []
		self.sanities = ["obj", "scr", "run"]

	def setup_regions(self, world):
		all_regions = []
		world.multiworld.regions.append(Region("Menu", world.player, world.multiworld))
		for room in apshared["rooms"]:
			region = Region(room["name"], world.player, world.multiworld)
			world.multiworld.regions += [region]
			all_regions.append(region)
			if room["type"] == "car":
				self.car_regions.append(region)
			rule = True_()
			for check in room["checks"]:
				check_name = f"{room['name']} {check['name']}"
				if check_name == "Final Push Runner #11":
					region.add_event("Flicky Saved", "Retirement", location_type=TMOSTHLocation, item_type=TMOSTHItem)
				else:
					if check["sanity"] in self.sanities:
						region.add_locations({check_name: location_name_to_id[check_name]}, TMOSTHLocation)
		for i in range(len(all_regions)):
			region = all_regions[i]
			room = apshared["rooms"][i]
			for entrance in room["entrances"].keys():
				from_region = world.get_region(entrance)
				rule = True_()
				for item in room["entrances"][entrance]:
					rule = rule & Has(item)
				from_region.connect(region, f"{entrance} to {room['name']}")
				world.set_rule(world.get_entrance(f"{entrance} to {room['name']}"), rule)
		world.set_completion_rule(Has("Retirement"))