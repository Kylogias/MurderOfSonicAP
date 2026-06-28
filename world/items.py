from BaseClasses import Item, ItemClassification
from enum import Enum

class ItemType(Enum):
	progression = 0
	filler = 1
	car = 2

from .apshared import apshared, item_name_to_id

class TMOSTHItem(Item):
	game = apshared["game"]

class ItemState:
	def __init__(self):
		self.item_class = {}
		self.filler_items = []
		self.progress_items = []
		self.car_items = []
		for item in apshared["items"]:
			self.item_class[item["name"]] = item["type"]
			if item["type"] == ItemType.filler:
				self.item_class[item["name"]] = ItemClassification.filler
				self.filler_items.append(item["name"])
			if item["type"] == ItemType.progression:
				self.item_class[item["name"]] = ItemClassification.progression
				self.progress_items.append(item["name"])
			if item["type"] == ItemType.car:
				self.item_class[item["name"]] = ItemClassification.progression
				self.car_items.append(item["name"])
	
	def create_item(self, world, item):
		return TMOSTHItem(item, self.item_class[item], item_name_to_id[item], world.player)

	def get_filler_item(self, world):
		return world.random.choice(self.filler_items)
	
	def setup_items(self, world):
		itempool = []
		precollect = []
		for item in self.progress_items:
			itempool.append(world.create_item(item))
		if world.car_rando:
			starting_car = world.random.choice(self.car_items)
			for item in self.car_items:
				if item == starting_car:
					precollect.append(item)
				else:
					itempool.append(world.create_item(item))
		num_unfilled = len(world.multiworld.get_unfilled_locations(world.player))
		num_filler = num_unfilled - len(itempool)
		for i in range(num_filler):
			itempool.append(world.create_filler())
		world.multiworld.itempool += itempool
		for i in precollect:
			world.push_precollected(world.create_item(i))
