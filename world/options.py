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

class DialogSanity(Toggle):
	"""
	Should the 142 choices in dialogs be checks? You may be required to restart the game multiple times
	"""
	display_name = "Choice Sanity"

class DeductionSanity(Toggle):
	"""
	Should the 85 correct/incorrect deductions be checks. Note that this does not override deathlink if enabled
	"""
	display_name = "Deduction Sanity"

@dataclass
class TMOSTHOptions(PerGameCommonOptions):
	car_rando: CarRando
	choice_sanity: DialogSanity
	deduction_sanity: DeductionSanity

