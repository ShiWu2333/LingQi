# Project Directory & Naming Guide

This repository now follows a domain-based layout to keep code, assets, and third-party content organized for Unity.

## Top-level layout under `Assets`
- `Scripts/` — Game code split into `Player`, `Enemy`, `UI`, and `Shared` domains. Assembly definitions are provided per domain to limit cross references.
- `ScriptableObjects/` — Runtime data grouped by domain, e.g., `Combat/WeaponConfigs`, `Combat/ImpactProfiles`, `Combat/Balance`, and `Inventory`.
- `Art/` — Art-facing content such as models, textures, animations, and prefabs (organized into domain subfolders like `Prefabs/Inventory` and `Prefabs/UI`).
- `Scenes/` — Unity scene assets.
- `ThirdParty/` — Imported packages and vendor assets (e.g., TextMesh Pro, visual effects, character packs).
- `Settings/` — Project- or renderer-level settings.

## Naming and placement guidelines
- Place shared or cross-domain utilities/configs in `Scripts/Shared`; Player- and Enemy-specific logic stays in their respective folders.
- UI behaviours live in `Scripts/UI`, and should only reference `Player` or `Shared` assemblies when necessary.
- Put ScriptableObjects in the matching domain folder under `ScriptableObjects/<Domain>` rather than near scripts.
- Store prefabs with other art assets under `Art/Prefabs/<Domain>` so scene references stay consistent.
- Third-party imports belong in `ThirdParty/` and should be referenced via the `ThirdParty` assembly definition when custom scripts are added.

These conventions should be used for future additions to keep dependencies clear and cross-domain coupling minimal.
