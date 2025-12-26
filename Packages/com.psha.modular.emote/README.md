# Modular Emote

**Modular Emote** is a VRChat SDK3 Avatar tool that lets creators package emote animations as modular prefabs, and lets users install them onto their avatars with minimal setup.

- Platform: Unity + VRChat SDK3 Avatar
- Pipeline: **NDMF (Non-Destructive Modular Framework)**
  - Original AnimatorController assets are never modified
  - All graph edits happen on NDMF virtual controllers during build

## Documentation

- **EN** → https://psha1220.github.io/modular-emote-docs/
- **JP** → https://psha1220.github.io/modular-emote-docs/ja/
- **KR** → https://psha1220.github.io/modular-emote-docs/ko/

---

## What does it do?

### For users (installing distributed content)
- Drop a distributed **ME prefab** under your avatar.
- Choose a **Slot Index (1–8)**.
- Build the avatar.
- Open the VRChat radial menu → **Emote** to test.

### For creators (making distributable content)
- Provide an ME prefab that includes:
  - **ME Psha VRC Emote Installer** component
  - Optional template controllers:
    - **ME Action Layer**
    - **ME FX Layer**
  - Optional behaviour:
    - **Modular Emote Transition Settings** (transition overrides)

---

## Requirements

### Avatar requirements (important)
This tool assumes the avatar uses the **VRCEmote** parameter-driven emote system.

Your avatar should have:
- An Action layer structure that reacts to **`VRCEmote` (int)** values **1–8**, and
- An Expressions Menu where Emotes are triggered via the **Emote radial menu**.

> If your avatar does not use VRCEmote-based emotes, you may need manual adjustments and the setup may not be supported.

---

## Quick install (users)

1. Place your **avatar** in the Unity scene.
2. Place the distributed **ME prefab** in the scene.
3. Drag the ME prefab **under the avatar hierarchy**.
4. In the Installer, set **Slot Index (1–8)**.
5. Build the avatar using **VRChat SDK**.
6. In-game: open the radial menu (default: hold **R**) → **Emote** and verify.

If something looks wrong, start with:

- **Setup VRC Emote** button  
  This attempts to auto-fill settings as much as possible:
  1) Find the VRCAvatarDescriptor  
  2) Detect Action/FX layers (when needed)  
  3) Detect the Emote menu (when needed)  
  4) Guess start/end action states (when possible)  
  5) Set values required for advanced Action-root tracking (when applicable)

---

## How it works (high-level)

### Expressions Menu patch (non-destructive)
- Clones the avatar’s Expressions Menu tree.
- Applies emote entries only to the cloned menu.
- Assigns:
  - Name / Icon / Control type
  - Value = Slot Index (1–8)
  - Parameter = `VRCEmote`

### Action layer merge (non-destructive)
- Merges an ME Action template into the avatar’s **Action** layer in NDMF virtual space.
- Each slot creates an independent branch (e.g. `PshaEmote_1 ... PshaEmote_8`).
- The same template can be reused across multiple slots.

> Internally, template clones are isolated per slot to prevent shared-state issues.

---

## Templates (for creators)

### ME Action Layer
- A template AnimatorController intended to be merged into the avatar’s Action layer.
- All transitions inside the template that use **`VRCEmote`** conditions are automatically rewritten to the selected **Slot Index** during build.

**Custom entry transition settings**
- Add a dedicated state named:
  - `[ME] StartState Transition Settings`
- Add Behaviour:
  - `Modular Emote Transition Settings`
- Configure your desired transition parameters there.

**Common issue: emote feels delayed**
If the avatar’s Action layer path (often the start state) has **VRC Playable Layer Control** with a non-zero **Blend Duration**, the transition into the ME subtree can feel delayed.

Because this behaviour may exist on the **avatar’s own states (outside the template)**, the recommended workaround is:
- Add **VRC Playable Layer Control** to the **first state inside the ME template** and set an appropriate Blend Duration to counteract/standardize the timing.

> Always validate timing in-game after build. Some preview tools may not reproduce loop/timing exactly.

### ME FX Layer
- Optional FX template merged into the avatar’s FX layer (if used by the prefab).
- Intended for expression-driven visuals, toggles, materials, etc.

---

## Credits
© 2025–present P*Connect / P*sha
