WIP and Unstable Archipelago integration for The Murder of Sonic the Hedgehog

[Archipelago](https://archipelago.gg/) is "a cross-game modification system which randomizes different games, then uses the result to build a single unified multi-player game. Items from one game may be present in another, and you will need your fellow players to find items you need in their games to help you complete your own," (copied from the website)

Anything interactable inside the train cars as well as the THINK sections are checks

Items consist of the inventory items you normally come across as well as useless filler and optionally train cars

The goal is to beat the game

# Installation (client)
1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader)
2. Drop the latest client into the folder with the TMOSTH executable
3. Drop [MelonPreferencesManager](https://github.com/Bluscream/MelonPreferencesManager) (Mono) into the `Mods` folder
4. Drop [UniverseLib](https://github.com/sinai-dev/UniverseLib/releases/tag/1.5.1) (Mono, dependency of MelonPreferencesManager) into the `UserLibs` folder
5. One launched, press F5 to open the preference manager, go into the "Archipelago" tab, and enter the connection information
6. The game will automatically connect when you hit "New Game"

# Building

The build system is very much "it works on my machine"
Prerequisites:
1. Python 3
2. [Mono](https://mono-project.com), latest should be fine? The build system expects the `mcs` command
3. [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net)
4. [MelonLoader](https://github.com/LavaGang/MelonLoader)

To compile
1. Edit the string on line 8 in `build.py` to the path of your TMOSTH installation
2. Drop Archipelago.MultiClient.Net into the UserLibs directory of the TMOSTH installation
3. Try running the buildsystem.