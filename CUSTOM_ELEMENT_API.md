# Elemental Battlegrounds custom-element API v1

Elemental Battlegrounds 0.1.0 includes a supported public API for other BepInEx mods to add their own elements to EB's loadout system.

A custom element can provide:

- one stable element ID;
- a name, author, colors, icon, and in-game guide text;
- exactly one weapon for each of EB's five families: Revolver, Close, Rapid, Ultimate, and Explosive;
- a vanilla weapon chassis for each of those five weapons;
- addon-owned `MonoBehaviour` components that implement the element's actual mechanics.

The important rule is simple: **use the public `ElementalBattlegroundsMod.Api` namespace and do not build against EB's internal classes.**

```csharp
using ElementalBattlegroundsMod.Api;
```

Do not reflect into or directly depend on `ElementId`, `ElementRegistry`, `ElementalSlotMarker`, `LoadoutChoice`, `GunSetter`, EB Harmony patches, or EB's resource-virtualization classes. Those are implementation details and are not part of the compatibility contract.


## Fastest way to start

The easiest starting point is the separate `ElementalBattlegroundsExampleElement` developer project. It registers the Prism test element using only API v1. Copy that project, change its plugin GUID, element ID, names, icon, guides, weapon templates, and attached components, then replace the example mechanics with your own.

Prism is developer reference material, not an official EB element and not balanced player content. Do not bundle it with your addon or with Elemental Battlegrounds itself.

If you are starting from an empty BepInEx project instead, your project must reference the compiled `ElementalBattlegrounds.dll` at build time. Do not copy EB source files into your addon. A normal project reference looks like this:

```xml
<Reference Include="ElementalBattlegrounds" HintPath="$(ElementalBattlegroundsDll)" Private="false" />
```

Your plugin must also declare EB as a hard BepInEx dependency so EB loads before your addon:

```csharp
[BepInPlugin(Guid, Name, Version)]
[BepInDependency(ElementalBattlegroundsApi.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    // ...
}
```


## Registering an element

Register from your plugin's `Awake()` after declaring the hard dependency above.

API v1 requires exactly five weapon registrations: one for every family. `Attach` may be null if a weapon should simply behave like its selected vanilla chassis with the element's EB colors.

```csharp
private const int TargetApiVersion = 1;
private const string ElementId = "myname:crystal";

private void Awake()
{
    ElementRegistration element = new ElementRegistration
    {
        // Pin the API contract your addon was written and tested against.
        ApiVersion = TargetApiVersion,

        // This is save/preset identity. Do not casually change it later.
        Id = ElementId,
        DisplayName = "Crystal",
        Author = "My Name",

        // Palette is required. FromAccent is the easy option.
        Palette = ElementColorPalette.FromAccent(new Color(0.65f, 0.85f, 1f)),

        // Icon and guide text are optional, but strongly recommended for a real addon.
        Icon = mySprite,
        SimpleGuide = "Short explanation shown in EB's element guide.",
        AdvancedGuide = "Longer explanation of the element and its five weapons.",

        // Optional. Use this only when an older released ID needs to keep resolving.
        LegacyIds = new[] { "myname:old-crystal" },

        Weapons = new[]
        {
            new ElementWeaponRegistration
            {
                Family = ElementWeaponFamily.Revolver,
                Template = VanillaWeaponTemplate.PiercerRevolver,
                DisplayName = "Crystal Revolver",
                Attach = context => context.Weapon.AddComponent<MyRevolverController>()
            },
            new ElementWeaponRegistration
            {
                Family = ElementWeaponFamily.Close,
                Template = VanillaWeaponTemplate.CoreEjectShotgun,
                DisplayName = "Crystal Shotgun",
                Attach = context => context.Weapon.AddComponent<MyCloseController>()
            },
            new ElementWeaponRegistration
            {
                Family = ElementWeaponFamily.Rapid,
                Template = VanillaWeaponTemplate.AttractorNailgun,
                DisplayName = "Crystal Nailgun",
                Attach = context => context.Weapon.AddComponent<MyRapidController>()
            },
            new ElementWeaponRegistration
            {
                Family = ElementWeaponFamily.Ultimate,
                Template = VanillaWeaponTemplate.ElectricRailcannon,
                DisplayName = "Crystal Railcannon",
                Attach = context => context.Weapon.AddComponent<MyUltimateController>()
            },
            new ElementWeaponRegistration
            {
                Family = ElementWeaponFamily.Explosive,
                Template = VanillaWeaponTemplate.FreezeframeRocketLauncher,
                DisplayName = "Crystal Rocket Launcher",
                Attach = context => context.Weapon.AddComponent<MyExplosiveController>()
            }
        }
    };

    if (!ElementalBattlegroundsApi.RegisterElement(element, out string error))
        Logger.LogError("Could not register Crystal: " + error);
}
```

The code above does not make the example controllers for you. They are ordinary addon-owned Unity/BepInEx components. Put your weapon mechanic in those components and use normal ULTRAKILL/Harmony techniques when the mechanic genuinely needs them.


## What is required and what is optional

`ElementRegistration` requires:

- `ApiVersion` — use `1` for API v1;
- `Id` — the element's stable namespaced identity;
- `DisplayName`;
- `Palette`;
- `Weapons` — exactly five entries, one per family.

Each `ElementWeaponRegistration` requires:

- `Family`;
- a `Template` belonging to that family;
- `DisplayName`.

These are optional:

- `Author`;
- `Icon` — a null icon is accepted, but the element has no selector/guide icon;
- `SimpleGuide` — if absent, EB shows a generic "No guide text was provided" message;
- `AdvancedGuide` — if absent, the Advanced button is disabled;
- `LegacyIds`;
- `Attach`;
- `OwnsSecondary` — leave false unless you really replace the vanilla secondary action.

Registration is all-or-nothing. API v1 rejects duplicate IDs, the reserved `elementalbattlegrounds:*` namespace, invalid family/template combinations, duplicate or missing families, missing required display names/palette, and unsupported API versions without partially registering the element.


## Stable IDs and saves

`Id` is the element's persistence identity. EB loadouts and named presets store this ID, not the display name.

Use a namespaced ID such as:

```text
myname:crystal
```

Public IDs are canonicalized to lowercase. Other than the single colon separating namespace and name, the supported characters are letters, digits, `.`, `_`, and `-`.

Changing `DisplayName` is safe. Changing `Id` normally breaks old saved references. If you must rename an already released ID, put the previous ID in `LegacyIds` so EB can resolve old presets/loadouts to the new registration.

The namespace `elementalbattlegrounds` is reserved for official built-in elements.

If an addon is removed, EB does not erase its saved ID. The loadout displays `MISSING: author:id`. Reinstalling an addon with the same stable ID resolves that saved slot again.


## API versioning

The gameplay mod version and API version are separate things.

```csharp
ElementalBattlegroundsApi.ApiVersion == 1
```

A v1 addon should explicitly use:

```csharp
ApiVersion = 1
```

or a pinned constant such as `TargetApiVersion = 1`.

Do **not** write this in an addon that was specifically written for v1:

```csharp
ApiVersion = ElementalBattlegroundsApi.ApiVersion
```

That constant means "the newest API contract supported by the installed host." Using it as your target would silently claim compatibility with a future API contract your addon has never tested.

For API v1, existing public enum numeric values, public type/member names, validation behavior, stable-ID persistence, and attach/context behavior are compatibility commitments. New compatible values or members may be added, but existing v1 enum values will not be reordered or renumbered. A genuinely breaking change belongs in a future API version.


## Weapon templates

`VanillaWeaponTemplate` is the supported way to choose the vanilla chassis your custom weapon starts from. Do not select `GunSetter` arrays or prefab indices yourself.

API v1 exposes:

- Revolver: Piercer / Slab Piercer, Marksman / Slab Marksman, Sharpshooter / Slab Sharpshooter;
- Close: Core Eject Shotgun / Jackhammer, Pump Charge Shotgun / Jackhammer, Sawed-On Shotgun / Jackhammer;
- Rapid: Attractor Nailgun / Sawblade Launcher, Overheat Nailgun / Sawblade Launcher, Jumpstart Nailgun / Sawblade Launcher;
- Ultimate: Electric, Screwdriver, and Malicious Railcannon;
- Explosive: Freezeframe, S.R.S., and Firestarter Rocket Launcher.

EB translates these stable descriptors to the current ULTRAKILL weapon prefabs internally.


## `Attach` and `ElementWeaponContext`

`ElementWeaponRegistration.Attach` runs when EB constructs that arsenal-slot weapon. It can run again later if EB rebuilds the arsenal, so treat it as per-weapon-instance setup, not a once-per-game event.

The callback receives an immutable `ElementWeaponContext` containing:

- `Weapon` — the cloned vanilla chassis `GameObject`;
- `ElementId` and `ElementDisplayName`;
- `Family`;
- `SlotIndex` — zero-based EB position inside that family (`0`, `1`, or `2`);
- `Template`;
- `WeaponDisplayName`;
- `IsDualWieldClone`.

Normally `IsDualWieldClone` is false in the original `Attach` callback. ULTRAKILL creates Dual Wield copies afterward, and components attached to the original weapon are cloned with them.

A component running on either the original weapon or one of those clones can ask EB for its public identity:

```csharp
if (ElementalBattlegroundsApi.TryGetWeaponContext(gameObject, out ElementWeaponContext context))
{
    if (!context.IsDualWieldClone)
        Logger.LogInfo(context.ElementId + " / " + context.WeaponDisplayName);
}
```

ULTRAKILL destroys and recreates Dual Wield weapon copies on weapon swaps. Avoid expensive initialization, scene-wide searches, or synchronous log spam every time one of those short-lived clones starts. Use `IsDualWieldClone` when work only belongs on the persistent slot owner.

If an `Attach` callback throws, EB catches the exception, logs that weapon's failure, and continues constructing the rest of the arsenal. The vanilla chassis remains usable when possible.


## Vanilla resource ownership

Because EB can equip combinations that vanilla ULTRAKILL normally makes mutually exclusive, several vanilla weapon resources would otherwise leak between different EB slots.

For addon-registered templates, EB automatically virtualizes the resources it already knows must belong to the individual arsenal slot. This includes Revolver secondary resources, Sawed-On charge/wear, Overheat heatsinks, Attractor magnet allowance, Jumpstart recharge, S.R.S. cannonball charge, and Firestarter fuel. Slab/Jackhammer per-weapon cooldown state is also kept on the individual EB weapon where appropriate.

You do **not** need to patch `WeaponCharges` just because two registered weapons use chassis that vanilla normally prevents from coexisting.

Some vanilla resources are intentionally player-wide and remain shared, including Rapid ammo/heat, Railcannon charge, Rocket Launcher primary recovery, and the Jackhammer yellow-hit streak.

If your own mechanic introduces a new resource, your addon owns that resource and must decide whether it is per weapon, per EB slot, or player-wide.


## Replacing the vanilla secondary action

Set:

```csharp
OwnsSecondary = true
```

only when your addon replaces that chassis's normal secondary action.

This flag does not implement input, cooldowns, or your mechanic. It tells EB's compatibility plumbing that the secondary belongs to the addon. EB currently uses that information for integrations such as Weapon Variant Binds and Grenade Launcher compatibility.

Leave it false when the weapon keeps the vanilla secondary.


## Icons, colors, and guides

`Palette` is required. `ElementColorPalette.FromAccent(...)` is the easiest way to generate a reasonable three-color weapon palette, or you can construct an explicit `ElementColorPalette` yourself.

`Icon` is a Unity `Sprite`. It is optional, but a real released element should normally provide one. Keep the sprite and its backing texture alive for as long as the element remains registered. The Prism developer example shows one way to embed a PNG in the addon DLL and create the sprite at runtime.

`SimpleGuide` and `AdvancedGuide` are shown in EB's existing right-click element guide. Both are optional. If `SimpleGuide` is absent, EB shows a fallback message. If `AdvancedGuide` is absent, the Advanced button is disabled.

Guide text should explain the element to players. Do not put build instructions, development history, or configuration documentation in the in-game guide.


## Registration lifecycle

Because your addon has a hard BepInEx dependency on EB, normal registration belongs in your plugin's `Awake()`.

EB can also accept a registration after its own `Awake()`. The selector reacts to registry changes, and saved namespaced IDs are resolved when the arsenal is rebuilt.

You can unregister with:

```csharp
ElementalBattlegroundsApi.UnregisterElement(ElementId, out string error);
```

Unregistering removes the element from the registry/selector. Existing live weapon objects are not destructively rewritten in the middle of the frame; the next arsenal rebuild treats saved slots for that ID as missing until the addon returns.

`ElementalBattlegroundsApi.RegistryChanged` is available if your own UI needs to react to elements being registered or unregistered. EB invokes listeners defensively, so one failing listener is logged instead of aborting the registry mutation.

`ElementalBattlegroundsApi.IsElementRegistered(id)` can be used when you only need to check whether EB currently knows an ID (or one of its registered legacy aliases).


## Grenade Launcher behavior

Elemental Battlegrounds 0.1.0 installs Grenade Launcher 2.0.2 as a normal Thunderstore dependency, but custom-element API v1 exposes **native Rocket Launcher chassis only** for the Explosive family.

An external API element using Freezeframe, S.R.S., or Firestarter therefore stays in native Rocket Launcher mode even if the player's normal terminal selection for that variation is Grenade Launcher mode. API v1 does not expose Grenade Launcher chassis descriptors.

If explicit Grenade Launcher templates are added for third-party elements later, that will be a separate supported API capability rather than an addon depending on EB's private Grenade Launcher bridge.


## Publishing an addon

At runtime, keep the hard BepInEx dependency on:

```text
docvalentyne.ultrakill.elementalbattlegrounds
```

For Thunderstore, also list the published Elemental Battlegrounds package as a manifest dependency. If EB is published under the current package name and `DocValentyne` namespace, the 0.1.0 dependency string is:

```text
DocValentyne-Elemental_Battlegrounds_Arsenal-0.1.0
```

Use the EB package version your addon is actually built/tested against when publishing a release of your addon.

Do not ship `ElementalBattlegrounds.dll` inside your addon package. Reference it at compile time and let BepInEx/Thunderstore provide the installed host mod.


## Public API surface at a glance

The main entry points are:

```csharp
ElementalBattlegroundsApi.RegisterElement(...)
ElementalBattlegroundsApi.UnregisterElement(...)
ElementalBattlegroundsApi.IsElementRegistered(...)
ElementalBattlegroundsApi.TryGetWeaponContext(...)
ElementalBattlegroundsApi.RegistryChanged
```

The public data types are:

```text
ElementRegistration
ElementWeaponRegistration
ElementWeaponContext
ElementColorPalette
ElementWeaponFamily
VanillaWeaponTemplate
```

If an addon needs an EB type that is not in `ElementalBattlegroundsMod.Api`, treat that as a sign that API v1 does not currently expose that capability. Do not work around it by binding to EB's private classes and then expect compatibility across releases.


## What API v1 has been tested against

Before the v1 contract was frozen, the separate Prism developer example was used to verify all five chassis, metadata/palette/icon/guide integration, save/preset persistence, missing-addon restoration, Dual Wield cloning, and independent vanilla resource ownership (including a Prism Slab Marksman coexisting with the vanilla Slab Marksman with a separate coin pool).

A separate hostile conformance addon verified duplicate-ID rejection, same-display-name/different-ID registration, unregister behavior, and attach-callback exception containment. Its Close callback intentionally throws; EB contains that weapon's failure and continues constructing the other families.

Those tests are why API v1 is documented as a supported compatibility contract rather than an experimental reflection surface.
