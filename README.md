# Temnorid Hippodrome

**Ride. Race. Cross the line first.**

Breeding, training and racing horses. A [Temnorid](https://temnorid.com) game for **Windows**, built on the [IT Kamianets 3D Engine](https://3d.itkamianets.com) with Unity 6000.6.

**Status:** in development, first playable not released yet. Website: https://hippodrome.temnorid.com · Download: [Temnorid-Hippodrome-Setup.exe](https://github.com/WebArtWork/hippodrome.temnorid.com/releases/latest/download/Temnorid-Hippodrome-Setup.exe)

## What it is

Breed and train horses, feed them well and bring them to the track. A horse's speed, stamina and temper, together with your sense of pacing, decide who crosses the line first.

Start at village fairs, then compete in city tournaments with prizes and betting. Wins bring bloodlines, saddles and horse armour that help in Conquest and the Arena.

## The first playable

The first playable is the race itself: you on a horse against computer riders, three laps around one track.

### How a session goes

1. Press Start Race. A countdown runs, then the race begins.
2. Accelerate, brake and steer your horse around the track. The camera follows behind.
3. Pass every checkpoint in order; a lap only counts if you did. Cutting across the field does not.
4. Watch your lap, your time and your position among the riders.
5. After the last lap you see the final order and your time. Restart to race again.

### Controls

| Key | Action |
| --- | --- |
| W | Gallop faster |
| S | Slow down |
| A / D | Steer left / right |
| Esc | Pause menu |

### Win and lose

- **Win:** Finish three valid laps ahead of the other riders.
- **Lose:** There is no losing, only a place. Finish first to win; your best lap and best time are saved.

### The scene

One closed racecourse with a start and finish line, checkpoints, barriers and four horse-and-rider shapes in different colours.

## How it connects to Lords

Horses from the Hippodrome will join the armies of Lords, and bloodlines, saddles and horse armour will help in Conquest and the Arena.

This is for a later phase: [Lords](https://lords.temnorid.com) is the central strategy game and every Temnorid game will be connected to it. **This version is fully offline and does not talk to Lords**; progress is saved locally on the player's PC. Keep identifiers and save data simple and stable so the connection is cheap to add later.

## Scope of this version

- Riding, computer riders, checkpoints, laps, positions, results, best times saved.
- No breeding, training, feeding, betting or owned horses yet.

Everything is drawn with Unity primitives (blocks, capsules, spheres), simple colours and text. No models, no paid assets. Every visible object has a stable asset id (for example `resources/wood-node`) so real models can replace the placeholders without touching gameplay.

## For developers and agents

- **Unity:** 6000.6, Universal 3D (URP). Open the folder in Unity Hub. `Library/` is rebuilt locally and not committed.
- **Platform:** Windows only. Keyboard and mouse. One map, offline, local saves.
- **Starts from:** the IT Kamianets 3D Engine Character View starter (empty Unity project). [AGENTS.md](AGENTS.md) is still the starter's generic guide; this README is the game's own.
- **Engine pieces this game uses:** `3d-unity-core` (logging), `3d-unity-input` (all input, through `EngineInput`) and `3d-unity-save` (best times, new in this workspace, with the envelope described by `3d-save-schema`). The horse, the bot riders, the race rules, the camera and the screens are written in this game (`Assets/Scripts`), because no other game needs them yet. It does not use the Character View camera and controls: a horse is not a walking character.
- **Where code lives:**
  - `3d-*` packages: generic, engine-level behaviour with no Temnorid content (combat, AI, inventory, save, ...).
  - `temnorid-*` packages: Temnorid content shared by **several games** (for example `temnorid-character`). Never `temnorid-<game>-*`.
  - `Assets/Scripts` in this project: this game's own rules, scene builders, HUD and menus.
- **Packages while developing:** the packages sit in the shared workspace folder next to the games (`../3d-unity-core`, `../3d-unity-input`, `../3d-unity-save`) and `Packages/manifest.json` points at them with `file:../../<package>`. When they get their own repositories, only these paths change.
- **Level building:** menu **Temnorid > Hippodrome > Build Scene** (or `-executeMethod Temnorid.Hippodrome.EditorTools.HippodromeSceneBuilder.Build` from the command line) rebuilds `Assets/Scenes/Main.unity` from scratch: the track, rails, checkpoints, four horses, camera and light, all from primitives. It can be run again at any time; do not edit the scene by hand.
- **Code map (`Assets/Scripts`):**
  - `RaceRules`: the laps, checkpoints and ranking rules, with no Unity in them.
  - `RaceController`: the race itself (grid, countdown, pause, finish, records).
  - `HorseController` with a `HorseDriver`: `PlayerDriver` (keyboard or stick) or `BotDriver` (follows waypoints).
  - `Checkpoint`, `Racer`, `RaceCamera`, `RaceHud` (all screens, plain uGUI), `HippodromeRecords` (what is saved), `AssetIdTag` (stable ids like `horses/basic` for future models).
- **Tests:** `Assets/Tests/EditMode` covers the race rules and the records file; `Assets/Tests/PlayMode` rides a full race with every horse on autopilot. Run them from Window > General > Test Runner, or with `Unity.exe -batchmode -runTests -testPlatform EditMode` (or `PlayMode`).
- **Save file:** `<persistent data path>/hippodrome/default.json` (on Windows `%USERPROFILE%\AppData\LocalLow\Temnorid\Temnorid Hippodrome\hippodrome\default.json`).
- **Do not add:** networking, Firebase, accounts, or any link to Lords in this version.

## Website and downloads

The `docs/` folder is this game's public website (`docs/index.html`, served at https://hippodrome.temnorid.com through GitHub Pages; `docs/CNAME` holds the domain). It repeats the information in this README: what the game is, how to play, controls and the download button.

The download button points to the latest GitHub Release asset:

`https://github.com/WebArtWork/hippodrome.temnorid.com/releases/latest/download/Temnorid-Hippodrome-Setup.exe`

Release steps (when the first playable is ready):

1. Build the Windows player from Unity (File > Build Profiles > Windows).
2. Wrap the build in a Windows installer named `Temnorid-Hippodrome-Setup.exe`.
3. Create a GitHub Release (tag `v0.1.0`) and attach that file.
4. Open the website, click the button, install, play, save, restart, uninstall.

When you change the game's description, controls or rules, update this README and `docs/index.html` together.

## License

See [LICENSE](LICENSE).
