from dataclasses import dataclass

from Options import PerGameCommonOptions, Toggle

class CarRando(Toggle):
	"""
	Should the train cars be put into the item pool?
	NOTE :: Press the tilde button (~) to switch train cars
	You will also have to do this on connect
	The tilde button is available without car rando on as well
	"""
	display_name = "Car Rando"

@dataclass
class TMOSTHOptions(PerGameCommonOptions):
	car_rando: CarRando

