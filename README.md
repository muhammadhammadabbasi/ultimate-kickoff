# Ultimate Kickoff ⚽

FC Mobile–style football game for Android, built with Unity. Every push to `main` builds a fresh APK automatically on GitHub.

## APK download (phone se)

1. Repo ke **Releases** section mein **"Ultimate Kickoff - latest build"** kholein.
2. **UltimateKickoff.apk** download karein.
3. Install karein (pehli dafa phone "Install unknown apps" ki permission maangega, allow kar dein).

## One-time setup: Unity license (sirf aik dafa)

GitHub ko Unity chalane ke liye aapka free Unity Personal license chahiye.

1. Laptop par **Unity Hub** install karein aur apne Unity account se login karein (Editor install karne ki zaroorat nahi).
2. Unity Hub → **Preferences** (Settings) → **Licenses** → **Add** → **Get a free personal license**.
3. License file yahan ban jayegi (Linux): `~/.local/share/unity3d/Unity/Unity_lic.ulf`
   Terminal mein `cat ~/.local/share/unity3d/Unity/Unity_lic.ulf` chala kar pura text copy karein.
4. GitHub par is repo mein: **Settings → Secrets and variables → Actions → New repository secret**, aur ye 3 secrets banayein:

   | Name | Value |
   |---|---|
   | `UNITY_LICENSE` | `.ulf` file ka pura text |
   | `UNITY_EMAIL` | Unity account ka email |
   | `UNITY_PASSWORD` | Unity account ka password |

   Tip: password mein special characters (`@ # $ %` wagera) build mein masla karte hain. Ho sake to sirf letters + numbers wala password rakhein.
5. **Actions** tab → **Build Android APK** → **Run workflow**. Pehla build ~30–60 minute leta hai, baad wale tez hote hain.

## Controls

| Action | Phone | PC (Unity Editor) |
|---|---|---|
| Move | Left joystick | WASD / Arrows |
| Sprint | SPRINT | Left Shift |
| Pass (attack) / Switch player (defence) | PASS / SWITCH | J |
| Through ball (attack) / Press (defence, hold) | THROUGH / PRESS | L |
| Shoot, hold = power (attack) / Tackle (defence) | SHOOT / TACKLE | K |

**Power control:** PASS, THROUGH aur SHOOT ko **dabaye rakhein** to power bar bharta hai (halka/chhota → zor se/lamba). Sirf tap karein to game khud distance dekh kar power set karta hai (qareeb wale ko halka pass, door wale ko lamba). Bar bharte waqt jis player ko pass jayega uska naam dikhta hai.

Set pieces: THROW / SHORT, LOB, CROSS / LONG buttons appear automatically.

## Project structure

| Path | Kaam |
|---|---|
| `Assets/UltimateKickoff/Scripts/Data/` | ~170 players (badle hue naam) + stats, clubs, formations, aapki lineup (save hoti hai) |
| `Assets/UltimateKickoff/Scripts/Match/` | 11v11 match: stadium, 3D players + animations, ball physics, team AI, goalkeepers, set pieces, broadcast camera |
| `Assets/UltimateKickoff/Scripts/UI/` | Home, My Team (cards + player picker), opponent select, match HUD (minimap, buttons), result |
| `Assets/UltimateKickoff/Editor/UltimateKickoffSceneBuilder.cs` | Scene banata hai (Unity mein: Tools → Ultimate Kickoff → Build Scene) |
| `Assets/Editor/CIBuild.cs` | GitHub par APK build karne wala script |
| `.github/workflows/build-android.yml` | Automatic APK build + release |

## Agar build fail ho

- **"Missing repository secrets"** → upar wala step 4 dobara check karein.
- **"No sdkmanager found"** ya Android SDK error → Actions → Build Android APK → Run workflow → Unity version mein `6000.0.45f1` likh kar chalayein.

## Roadmap

- [x] Phase 1: 3v3 prototype
- [x] Phase 2: 11v11, formations, throw-ins, corners, goal kicks, keepers
- [x] Phase 3 (basic): 3D players with running/kick/dive animations, stadium, crowd, broadcast camera
- [x] Phase 4 (basic): Home, My Team lineup with player cards & stats, opponent clubs, results
- [ ] Next: fouls & free kicks, skill moves, commentary/sound, tournaments, packs & coins
- [ ] Later: Online PvP

Note: player names are deliberately altered. Before publishing on the Play Store, change them to fully fictional names.
