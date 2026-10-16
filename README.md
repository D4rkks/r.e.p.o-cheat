# r.e.p.o-cheat
made by D4rkks (and community) (second repo bc first one i fucked up)

> [!WARNING]
> THIS IS A OPEN-SOURCE PROJECT! ITS NOT INTENDED TO BE SOLD OR TO BE THE ULTIMATE LAST R.E.P.O CHEAT, EVERYONE CAN USE IT AND FEEL FREE TO CONTRIBUTE!

Basic C# Mono open-source cheat for a new lethal like game called R.E.P.O.

<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/45f59200-fed3-4c40-b13b-5d4c86e12ca8" />

---

## 🚀 Features

### 👤 Self
- **God Mode / Infinite Health**: Never die.
- **Infinite Stamina**: Run forever.
- **NoClip**: Fly through walls.
- **Customizable Stats**: Adjust your Speed, Strength, Jump Force, Gravity, Throw Strength, and Grab Range in real-time. Values and toggles are re-applied automatically after each level load.
- **No Weapon Recoil/Cooldown**: Fire weapons without limitations.
- **Grab Through Walls**: Interactive with objects anywhere.
- **RGB Player**: Cycle through colors.
- **No Fog**: Clear visibility in all levels.
- **Custom FOV**: Change your Field of View from 60 to 120.

### 👁️ Visuals (ESP)
- **Modern UI**: Clean and intuitive menu design.
- **Enemy ESP**: See enemies through walls with Boxes, Names, Distance, and current/max Health (kept in sync from network hits when you're not the host).
- **Item ESP**: Track valuables with Box, Value, and Name. Includes a minimum value filter.
- **Player ESP**: Track allies/others with HP and Distance.
- **Extraction ESP**: Find exits easily.
- **Chams**: Customizable 3D Chams for both items and enemies.
- **Map Info**: Displays total value of items on the map and player status list.
- **Player Status Overlay**: Alive count, who is dead, whether their head is in an extraction/truck and what they are spectating.

### ⚔️ Combat & Players
- **Player Interaction**: Damage, heal, kill, or revive any player in the lobby.
- **Teleportation**: Move players to yourself (host only, as in the game), teleport yourself to any player, or send players into "The Void".
- **Social**: Spoof names or use a Rainbow Name effect.

### 👾 Enemies
- **Enemy Control**: Blind all enemies or kill them instantly.
- **Teleport Enemies**: Bring monsters to specific players.
- **Mob Spawner**: Spawn any enemy in the game (Host/MasterClient required).

### 📦 Items
- **Object Spawner** (Spawner tab): spawn any shop item, any valuable (optional custom value) or cosmetic box, the same way the game does (Host/MasterClient required in multiplayer).
- **Money Bag Spawner**: spawn a money bag with a custom value (bag size follows the value).
- **Set Item Value**: set any value on the selected item (synced to everyone when host).
- **Item Teleport**: Pull all items to your position.
- **Pack Items on Extraction**: Teleport every valuable onto the extraction platform, Tetris-style (exact collider sizes, laid flat, heightmap stacking, items that roll out are re-placed automatically).
- **Extraction Control**: Pick any extraction point, teleport to it or activate it remotely.
- **Remote Sell**: Sell items without going back to the ship.

### 🛠️ Misc & Server
- **Server Browser**: Powerful lobby search with filters (Region, Players, Hidden full lobbies).
- **Hotkey Manager**: Bind any cheat feature to your preferred keys.
- **Config System**: Save and load your cheat settings automatically.
- **Trolling Features**: Lobby Crasher, Fake Player Spawner, and NPC Spam.

---

## 🛠️ Requirements
- The game **R.E.P.O** (Steam)
- **Visual Studio 2019+** with the *.NET desktop development* workload (.NET Framework 4.8)
- **SharpMonoInjector** (MIT): download the latest release from [warbler/SharpMonoInjector](https://github.com/warbler/SharpMonoInjector/releases) and extract the `SharpMonoInjector.Console` folder (containing `smi.exe` and `SharpMonoInjector.dll`) at the root of this repo. The binaries are not committed: get them from the official release.

## 🏗️ How to Build
1. **Point the project to your game install.** The project references the game's assemblies in `REPO_Data\Managed` (they are never committed).
   - Default path: `C:\Program Files (x86)\Steam\steamapps\common\REPO\REPO_Data\Managed\`
   - Other Steam library: copy `GamePath.props.example` to `GamePath.props` (git-ignored) and edit `REPOManagedDir`, or build with `/p:REPOManagedDir=...`.
2. Open `r.e.p.o cheat.sln` in **Visual Studio**, select **Release**, then build (Ctrl+Shift+B).
3. The DLL is written to `r.e.p.o cheat/bin/Release/r.e.p.o cheat.dll`.

> [!NOTE]
> The Release build targets **x64**. REPO is a 64-bit process: a 32-bit/AnyCPU-32 DLL is rejected with a *platform mismatch* error.

## 💉 How to Inject
The cheat is a Mono assembly: it must be loaded by a **Mono injector** that calls its entry point. Native DLL injectors (Extreme Injector, etc.) report success but nothing happens, because they never call `Loader.Init`.

Entry point:
- **Namespace**: `r.e.p.o_cheat`
- **Class**: `Loader`
- **Method**: `Init` (unload: `Loader.UnloadCheat`, bound to **F10**)

### Option 1: `start_and_inject.bat` (normal use)
1. Build in **Release** (see above).
2. Run `start_and_inject.bat`, then launch the game (or the other way around).
3. The script waits for `REPO.exe`, waits 3 s for Mono to initialize, then runs:
   ```
   smi.exe inject -p REPO -a "r.e.p.o cheat\bin\Release\r.e.p.o cheat.dll" -n r.e.p.o_cheat -c Loader -m Init
   ```

### Option 2: `rebuild_and_inject.bat` (development, no game restart)
Mono cannot unload an assembly, and re-injecting an assembly with the same name returns the already loaded copy, so normally every code change needs a game restart. This script works around it:
1. Press **F10** in game to unload the running cheat (destroys its GameObject and network hooks).
2. Run `rebuild_and_inject.bat` and press a key when prompted. It will:
   - find MSBuild through `vswhere`;
   - delete the previous hot-reload builds;
   - build with `/p:HotReload=true`, which gives the assembly a **unique timestamped name** (`r.e.p.o cheat_yyyyMMdd_HHmmss.dll`, see the `HotReload` property group in the `.csproj`);
   - inject the newest DLL with `smi.exe`.

Each reload leaves the old assembly in memory (inactive), which is fine for a dev session; restart the game from time to time.

### Troubleshooting
| Symptom | Fix |
| --- | --- |
| `Platform mismatch… 32-bit DLL… 64-bit process` | Build in **Release** (x64). |
| "Injected" but no menu | Use SharpMonoInjector (Mono injector), not a native injector. Press **DEL** to toggle the menu. |
| `MSB3027` file locked on build | The game holds the injected DLL: use `rebuild_and_inject.bat` (unique names) or close the game. |
| Missing references on build | `REPOManagedDir` doesn't point to your `REPO_Data\Managed` folder (see *How to Build*). |
| Need logs | Press **F12** in game to open the debug log. |

---

*Enjoy responsibility! Or not. It's a cheat after all.*
