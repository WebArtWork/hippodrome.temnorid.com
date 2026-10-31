# Temnorid Hippodrome

**Ride. Race. Cross the line first.**

Breeding, training and racing horses. A [Temnorid](https://temnorid.com) game for **Windows**, built on the [IT Kamianets 3D Engine](https://3d.itkamianets.com) with Unity 6000.6.

**Status:** version 0.4.0, playable, with multiplayer. Website: https://hippodrome.temnorid.com · Download: [Temnorid-Hippodrome-Setup.exe](https://github.com/WebArtWork/hippodrome.temnorid.com/releases/latest/download/Temnorid-Hippodrome-Setup.exe)

## What it is

Breed and train horses, feed them well and bring them to the track. A horse's speed, stamina and temper, together with your sense of pacing, decide who crosses the line first.

Start at village fairs, then compete in city tournaments with prizes and betting. Wins bring bloodlines, saddles and horse armour that help in Conquest and the Arena.

## The first playable

The first playable is the race itself: you on a horse against computer riders, three laps around one track.

### How a session goes

1. Press Start Race. A countdown runs, then the race begins.
2. Accelerate, brake and steer your horse around the track. The camera follows behind. Keep clear of the other riders and the rails: both slow you down.
3. Pass every checkpoint in order; a lap only counts if you did. Cutting across the field does not.
   Twice a lap the track narrows between hay bales, with a bar across the gap: jump it with Space.
4. Watch your lap, your time and your position among the riders.
5. After the last lap you see the final order and your time. Restart to race again.

### Controls

| Key | Action |
| --- | --- |
| W | Gallop faster |
| S | Slow down |
| A / D | Steer left / right |
| Shift | Burst of speed (needs a short rest between uses) |
| Space | Jump |
| Esc | Pause menu |

### Playing with others

Up to four people can ride the same race. On the first screen choose **Play with others**, then either **Host a game here** (friends on the same network type the address it shows) or **Host online** (friends type a short code; needs the game linked to Unity Cloud, see below). Everybody presses **Ready** and the host starts. Each person rides their own horse, the computer riders fill the empty places, and the catch-up help the computer riders give in a single-player race is switched off.

### Updates

When a newer release is published on GitHub, the Windows game downloads and installs it by itself the next time it starts, and starts again. Turn it off in **Profile** if you prefer. On Android and iOS the update comes from the store or the release page.

### Riding

- **Computer riders:** they race to win. They go round slower horses, use their own bursts of speed, stay on your tail when you lead and chase you down if you slip, but they make mistakes too: one now and then mistimes a jump.
- **Other riders:** horses do not pass through each other. Coming up close behind another horse makes yours ease off, and horses side by side slow down a little and are pushed apart, so you have to steer round.
- **Walls:** touching a rail or a hay bale takes speed away; scraping along it keeps taking it.
- **Boost (Shift):** a burst of speed for about a second. A rested horse can do it again after about three and a half seconds. Pressing it sooner gives no speed and still tires the horse, and a horse that is worn out slows down and ignores the key until it has caught its breath. The bar at the bottom left shows the horse's stamina.
- **Jump (Space):** the horse leaves the ground for about three quarters of a second. Be in the air when the front of the horse reaches the bar; hit it and the horse stumbles and loses most of its speed. A jump costs a little stamina.

### Win and lose

- **Win:** Finish three valid laps ahead of the other riders.
- **Lose:** There is no losing, only a place. Finish first to win; your best lap and best time are saved.

### The scene

One closed racecourse with a start and finish line, checkpoints, rails, two narrow passes between hay bales with a bar to jump, and four horse-and-rider shapes in different colours.

## How it connects to Lords

Horses from the Hippodrome will join the armies of Lords, and bloodlines, saddles and horse armour will help in Conquest and the Arena.

This is for a later phase: [Lords](https://lords.temnorid.com) is the central strategy game and every Temnorid game will be connected to it. **This version is fully offline and does not talk to Lords**; progress is saved locally on the player's PC. Keep identifiers and save data simple and stable so the connection is cheap to add later.

## Scope of this version

- Riding, boost and stamina, jumping, crowding and walls, computer riders, checkpoints, laps, positions, results, best times saved.
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
- **Packages:** `Packages/manifest.json` points at the engine packages by their GitHub URLs (`https://github.com/IT-Kamianets/<package>.git`); Unity downloads them on first open.
- **Level building:** menu **Temnorid > Hippodrome > Build Scene** (or `-executeMethod Temnorid.Hippodrome.EditorTools.HippodromeSceneBuilder.Build` from the command line) rebuilds `Assets/Scenes/Main.unity` from scratch: the track, rails, the two narrow passes with their hurdles, checkpoints, four horses, camera and light, all from primitives. It can be run again at any time; do not edit the scene by hand.
- **Code map (`Assets/Scripts`):**
  - `RaceRules`: the laps, checkpoints and ranking rules, with no Unity in them.
  - `RaceController`: the race itself (grid, countdown, pause, finish, records).
  - `HorseController` with a `HorseDriver`: `PlayerDriver` (keyboard or stick, with Shift and Space kept until the physics step uses them) or `BotDriver` (follows waypoints, boosts now and then, jumps the hurdles with a little timing error, keeps to the gap in the narrow passes).
  - `HorseStamina` (boost, cooldown, strain, exhaustion) and `HorsePhysics` (crowding, following, wall hits): no Unity in them, so they are unit tested. `Hurdle`: the bar across a narrow pass; it checks the front of each horse crossing and never blocks the way.
  - `RacePacing` with `RaceController.PacingAssist`: for races against computer riders only. A rider behind the player gains up to 12% speed, but only until it is about 6 m behind, so it hovers on the player's tail and passes only if the player slips. A rider ahead of the player eases by up to 18% in proportion to its lead, so a player who rides well from behind can win. Changes are gradual so they are not noticed. **It must be switched off (`PacingAssist = false`) for any race against other people.**
  - `BotDriver` also overtakes (moves out to the side with more room when a slower horse is ahead), boosts every time the horse is rested and takes the hurdles with a per-rider timing error.
  - `Checkpoint`, `Racer`, `RaceCamera`, `RaceHud` (all screens, plain uGUI), `HippodromeRecords` (what is saved), `AssetIdTag` (stable ids like `horses/basic` for future models).
- **Tests:** `Assets/Tests/EditMode` covers the race rules, the records file, stamina, crowding and pacing; `Assets/Tests/PlayMode` rides a full race with every horse on autopilot and checks on the real track that horses cannot ride through each other, walls and bars slow them, a jump clears a bar and a boost is faster. `BalanceTests` (explicit, a few minutes) plays whole races with a good, an average and a sloppy stand-in player and writes `Builds/balance.txt`: the good one wins by well under a second with the bots on its tail, the average one loses narrowly, the sloppy one clearly. Use it when tuning difficulty. Run them from Window > General > Test Runner, or with `Unity.exe -batchmode -runTests -testPlatform EditMode` (or `PlayMode`).
- **Save file:** `<persistent data path>/hippodrome/default.json` (on Windows `%USERPROFILE%\AppData\LocalLow\Temnorid\Temnorid Hippodrome\hippodrome\default.json`).
- **Do not add:** networking, Firebase, accounts, or any link to Lords in this version.

## Website and downloads

The `docs/` folder is this game's public website (`docs/index.html`, served at https://hippodrome.temnorid.com through GitHub Pages; `docs/CNAME` holds the domain). It repeats the information in this README: what the game is, how to play, controls and the download button.

The download button points to the latest GitHub Release asset:

`https://github.com/WebArtWork/hippodrome.temnorid.com/releases/latest/download/Temnorid-Hippodrome-Setup.exe`

Release steps:

1. Build the Windows player from Unity (File > Build Profiles > Windows).
2. Wrap the build in a Windows installer named `Temnorid-Hippodrome-Setup.exe`.
3. Create a GitHub Release (tag `v<version>`, for example `v0.4.0`; installed games update to it by themselves) and attach that file and the zip.
4. Open the website, click the button, install, play, save, restart, uninstall.

When you change the game's description, controls or rules, update this README and `docs/index.html` together.

## License

See [LICENSE](LICENSE).
