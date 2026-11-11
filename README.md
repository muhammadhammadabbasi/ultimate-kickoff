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
| Pass / switch player | PASS | J |
| Shoot (hold = power) | SHOOT | K |

## Project structure

| Path | Kaam |
|---|---|
| `Assets/FootballPrototype/Scripts/` | Gameplay: ball, players + AI, goalkeepers, match, mobile controls |
| `Assets/FootballPrototype/Editor/FootballPrototypeBuilder.cs` | Pura stadium/scene code se banata hai (Unity mein: Tools → Football Prototype → Build Scene) |
| `Assets/Editor/CIBuild.cs` | GitHub par APK build karne wala script |
| `.github/workflows/build-android.yml` | Automatic APK build + release |

## Agar build fail ho

- **"Missing repository secrets"** → upar wala step 4 dobara check karein.
- **"No sdkmanager found"** ya Android SDK error → Actions → Build Android APK → Run workflow → Unity version mein `6000.0.45f1` likh kar chalayein.

## Roadmap

- [x] Phase 1: 3v3 + keepers, dribble, pass, shoot, tackle, score, 3-min match
- [ ] Phase 2: 5v5 / 11v11, formations, set pieces
- [ ] Phase 3: Real 3D players + animations, better stadium & lighting
- [ ] Phase 4: Menus, teams, player cards, tournaments
- [ ] Phase 5: Online PvP
