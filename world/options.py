from dataclasses import dataclass

from Options import PerGameCommonOptions, Toggle, Choice

class CarRando(Toggle):
	"""
	Should the train cars be put into the item pool?
	NOTE :: Press the tilde button (~) to switch train cars
	You will also have to do this on connect
	The tilde button is available without car rando on as well
	"""
	display_name = "Car Rando"

class DialogSanity(Choice):
	"""
	Should the 76 choices in dialogs be checks
	Optionally including the 64 missable choices
	- Will require you to load a save or new game
	"""
	display_name = "Choice Sanity"

	option_off = 0
	option_on = 1
	option_include_missable = 2

	default = option_off

class DeductionSanity(Choice):
	"""
	Should the 31 correct deductions be checks. 
	Optionally including the 54 incorrect deductions.
	- Note that this does not override deathlink if enabled
	"""
	display_name = "Deduction Sanity"
	
	option_off = 0
	option_on = 1
	option_include_incorrect = 2

	default = option_off

@dataclass
class TMOSTHOptions(PerGameCommonOptions):
	car_rando: CarRando
	choice_sanity: DialogSanity
	deduction_sanity: DeductionSanity

