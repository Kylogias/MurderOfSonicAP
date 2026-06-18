from BaseClasses import Item, ItemClassification

from .apshared import apshared, item_name_to_id

class TMOSTHItem(Item):
	game = apshared["game"]

class ItemState:
	def __init__(self):
		self.item_class = {}
		self.filler_items = []
		self.progress_items = []
		for item in apshared["items"]:
			self.item_class[item["name"]] = item["type"]
			if item["type"] == ItemClassification.filler:
				self.filler_items.append(item["name"])
			if item["type"] == ItemClassification.progression:
				self.progress_items.append(item["name"])
	
	def create_item(self, world, item):
		return TMOSTHItem(item, self.item_class[item], item_name_to_id[item], world.player)

	def get_filler_item(self, world):
		print(self.filler_items)
		return world.random.choice(self.filler_items)
	
	def setup_items(self, world):
		itempool = []
		for item in self.progress_items:
			itempool.append(world.create_item(item))
		num_unfilled = len(world.multiworld.get_unfilled_locations(world.player))
		num_filler = num_unfilled - len(itempool)
		for i in range(num_filler):
			itempool.append(world.create_filler())
		world.multiworld.itempool += itempool
