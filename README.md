# Teleport Pads

A 7 Days to Die (V2.6) mod that adds craftable teleportation pads. Place pads around the world, name them, and teleport instantly between any pad from a GUI destination picker.

## Features

- **Craftable** at a workbench (requires Advanced Engineering)
- **Blue-tinted** control panel model for easy identification
- **Naming** - give each pad a custom name
- **Networked** - every pad can teleport to every other pad
- **Search & filter** - type to filter the destination list
- **Sortable** - sort destinations by name (A-Z / Z-A) or distance (nearest / farthest)
- **Paginated** - handles large numbers of pads with prev/next navigation
- **Multiplayer** - pad data syncs between server and clients

## Recipe

Crafted at a **Workbench** with the **Advanced Engineering** perk:

| Ingredient | Count |
|---|---|
| Forged Iron | 20 |
| Mechanical Parts | 4 |
| Electric Parts | 8 |
| Scrap Polymers | 5 |

## Installation

1. Build the mod (see below) or use a pre-built `TeleportPads.dll`
2. Copy the entire `TeleportPads` folder into your game's `Mods` directory:
   ```
   7 Days to Die/Mods/TeleportPads/
       ModInfo.xml
       TeleportPads.dll
       Config/
           blocks.xml
           recipes.xml
           Localization.txt
           XUi/
               xui.xml
               windows.xml
               controls.xml
   ```
3. Launch the game. The mod loads automatically.

You do **not** need to copy the `Scripts/`, `Harmony/`, or `build.ps1` files - only the DLL and Config folder are needed at runtime.

## Building from Source

### Requirements

- .NET SDK 8.0 or 10.0 (for the Roslyn compiler)
- The mod folder must be located at `<GameDir>/Mods/TeleportPads/` so the build script can find game assemblies

### Build

```powershell
cd "7 Days to Die/Mods/TeleportPads"
powershell -ExecutionPolicy Bypass -File build.ps1
```

The script:
1. Locates Roslyn (`csc.dll`) from your installed .NET SDK
2. References game assemblies from `7DaysToDie_Data/Managed/` and `0Harmony.dll` from `Mods/0_TFP_Harmony/`
3. Compiles all `.cs` files in `Scripts/` and `Harmony/` into `TeleportPads.dll`

### Output

`TeleportPads.dll` in the mod root. This targets CLR v4.0 (Unity Mono compatible) via `/nostdlib` against the game's own `mscorlib.dll`.

## Usage

1. Craft a Teleport Pad at a workbench
2. Place it in the world
3. Press **E** to interact - choose **Edit Name** to name the pad
4. Place more pads elsewhere and name them
5. Press **E** on any pad and choose **Teleport** to open the destination picker
6. Use the search bar to filter, click **Name** or **Dist** to sort, and click **GO** to teleport

## File Structure

```
TeleportPads/
    ModInfo.xml              # Mod metadata
    TeleportPads.dll         # Compiled mod assembly
    README.md                # This file
    build.ps1                # Build script (Roslyn)
    .gitignore               # Excludes build artifacts
    Config/
        blocks.xml           # Block definition
        recipes.xml          # Crafting recipe
        Localization.txt     # UI text strings
        XUi/
            xui.xml          # Window group registration
            windows.xml      # Teleport picker + naming dialog
            controls.xml     # Pad entry row template
    Scripts/
        BlockTeleportPad.cs          # Block class with activation commands
        TileEntityTeleportPad.cs     # Tile entity (stores pad name, owner)
        TeleportPadManager.cs        # Singleton registry, file persistence
        XUiC_TeleportPadWindow.cs    # Destination picker UI controller
        XUiC_TeleportPadEntry.cs     # Entry row controller
        XUiC_TeleportPadNaming.cs    # Naming dialog controller
        NetPackageTeleportPadSync.cs # Full pad list sync packet
        NetPackageTeleportPadAdd.cs  # Single pad added packet
        NetPackageTeleportPadRemove.cs # Pad removed packet
        NetPackageTeleportPadRename.cs # Pad renamed packet
    Harmony/
        TeleportPadsInit.cs          # IModApi entry point
        TileEntityPatches.cs         # TileEntity.Instantiate patch (type 210)
```
