import os
import glob
import shutil
import json

os.system("clear")

SPARKDIR = "tmosthdir"
UNITYBASE = f"{SPARKDIR}/\"The Murder of Sonic The Hedgehog_Data\"/Managed/"
MODNAME = "APTMOSTH.Mono.dll"

COMMANDBASE = [
	"mcs",
	"-target:library",
	"-sdk:4.7.2",
	f"-out:mod/Mods/{MODNAME}",
	f"-reference:{SPARKDIR}/MelonLoader/net35/MelonLoader.dll",
	f"-reference:{SPARKDIR}/MelonLoader/net35/0Harmony.dll",
	f"-reference:{SPARKDIR}/UserLibs/Archipelago.MultiClient.Net.dll",
	f"-reference:{SPARKDIR}/UserLibs/Newtonsoft.Json.dll"
]

INCLUDE_MANAGED = [
	"Assembly-CSharp.dll", "UnityEngine.dll", "UnityEngine.CoreModule.dll", "netstandard",
	"Unity.TextMeshPro.dll", "UnityEngine.UI.dll", "Ink-Libraries.dll", "UnityEngine.UIModule.dll",
	"UnityEngine.InputLegacyModule", "UnityEngine.TextRenderingModule.dll"
]

ITEM_BASE = 2324655000
LOCATION_BASE = 2324650000

with open("apshared.json") as apfile:
	shared = json.load(apfile)

curID = ITEM_BASE
item_name_to_id = {}
for item in shared["items"]:
	curID += 1
	item["id"] = curID
	if not "inventory" in item:
		item["inventory"] = ""
	if not "env" in item:
		item["env"] = ""
	item_name_to_id[item["name"]] = curID

curID = LOCATION_BASE
location_name_to_id = {}
sanity_priority = ["obj", "scr", "run", "dialog", "deduct"]
sanities = {}
for sanity in sanity_priority:
	sanities[sanity] = []
for room in shared["rooms"]:
	for check in room["checks"].copy():
		if not "object" in check:
			check["object"] = ""
		if not "index" in check:
			check["index"] = -1
		if not "death" in check:
			check["death"] = "false"
		if not "dialog" in check:
			check["dialog"] = []
		if check["sanity"] in sanity_priority:
			sanities[check["sanity"]].append([room["name"], check])
		else:
			room["checks"].remove(check)
		pass

for sanity in sanity_priority:
	for pair in sanities[sanity]:
		room_name = pair[0]
		check = pair[1]
		check["id"] = curID
		location_name_to_id[f"{room_name} {check['name']}"] = curID
		curID += 1
		

with open("world/apshared.py", "w") as appy:
	appy.write("from .items import ItemType\n")
	appy.write("apshared = ")
	shared_str = json.dumps(shared)
	index = shared_str.find("itemtype")
	while True:
		index = shared_str.find("itemtype")
		if index == -1: break
		part = shared_str.partition('"itemtype": "')
		shared_str = "".join([part[0], '"type": ItemType.'])
		part = part[2].partition('"')
		shared_str = "".join([shared_str, part[0], part[2]])
	appy.write(shared_str)
	appy.write("\n")
	appy.write(f"location_name_to_id = {json.dumps(location_name_to_id)}\n")
	appy.write(f"item_name_to_id = {json.dumps(item_name_to_id)}\n")

with open("client/apshared.cs", "w") as apcs:
	apcs.write("namespace tmosthap {\n")
	apcs.write("public enum ItemIds : long {\n")
	for item in shared["items"]:
		apcs.write(f"\t\t{item['name'].replace(' ', '_').replace("'", '').upper()} = {item['id']},\n")
	apcs.write(f"\t\tBASE = {ITEM_BASE}\n")
	apcs.write("}\n")
	apcs.write("\tclass APShared {\n")
	apcs.write(f"\t\tpublic static int version = {shared['version']};\n")
	apcs.write(f"\t\tpublic static APItem[] items = ")
	apcs.write("{\n")
	for item in shared["items"]:
		apcs.write(f"\t\t\tnew APItem({item['id']}, \"{item['inventory']}\", \"{item['env']}\")")
		if item != shared["items"][-1]:
			apcs.write(",")
		apcs.write("\n");
	apcs.write("\t\t};\n")
	apcs.write(f"\t\tpublic static APRoom[] rooms = ")
	apcs.write("{\n")
	for room in shared["rooms"]:
		apcs.write(f"\t\t\tnew APRoom(\"{room['env']}\", new APRoomCheck[]")
		apcs.write("{\n")
		for check in room["checks"]:
			apcs.write(f"\t\t\t\tnew APRoomCheck(\"{check['sanity']}\", \"{check['object']}\", {check['index']}, {check['id']}, \"{check['death']}\", new string[]")
			apcs.write("{")
			for dialog in check["dialog"]:
				apcs.write(f"\"{dialog}\"")
				if dialog != check["dialog"][-1]:
					apcs.write(", ")
			apcs.write("})")
			if check != room["checks"][-1]:
				apcs.write(",")
			apcs.write("\n")
		apcs.write("\t\t\t})")
		if room != shared["rooms"][-1]:
			apcs.write(",")
		apcs.write("\n")
	apcs.write("\t\t};\n")
	apcs.write("\t}\n")
	apcs.write("}")

command = COMMANDBASE[0]

for i in COMMANDBASE[1:]:
	command = f"{command} {i}"

for i in INCLUDE_MANAGED:
	command = f"{command} -reference:{UNITYBASE}{i}"

for file in glob.iglob("client/**/*.cs", recursive=True):
	command = f"{command} {file}"

os.system(command)
shutil.copyfile(f"mod/Mods/{MODNAME}", f"{SPARKDIR}/Mods/{MODNAME}")

