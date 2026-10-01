using System;
using System.Collections;
using System.Collections.Generic;
using ElementalBattlegroundsMod.Api;
using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBattlegroundsMod
{
    internal sealed class ElementalSecondaryOwner : MonoBehaviour
    {
        [SerializeField]
        internal bool replicateOnDualWield;
    }

    internal static class ElementWeaponRuntime
    {
        private static void AddSecondary<T>(GameObject weapon, bool replicateOnDualWield = false) where T : Component
        {
            weapon.AddComponent<T>();
            ElementalSecondaryOwner owner = weapon.GetComponent<ElementalSecondaryOwner>();
            if (owner == null)
                owner = weapon.AddComponent<ElementalSecondaryOwner>();
            owner.replicateOnDualWield = replicateOnDualWield;
        }

        internal static void Attach(GameObject weapon, ElementalSlotMarker marker)
        {
            if (weapon == null || marker == null)
                return;

            weapon.AddComponent<ElementalColorDriver>();
            weapon.AddComponent<CounterBuffWeaponVisual>();
            PerSlotVanillaResourceRuntime.Attach(weapon, marker);

            if (!marker.IsCustom)
                return;

            Revolver revolver = marker.family == WeaponFamily.Revolver ? weapon.GetComponent<Revolver>() : null;
            if (revolver != null && revolver.altVersion && weapon.GetComponent<ElementalAltRevolverPrimaryCooldownState>() == null)
                weapon.AddComponent<ElementalAltRevolverPrimaryCooldownState>();

            if (marker.family == WeaponFamily.Shotgun && weapon.GetComponent<ShotgunHammer>() != null &&
                weapon.GetComponent<ElementalJackhammerPrimaryCooldownState>() == null)
                weapon.AddComponent<ElementalJackhammerPrimaryCooldownState>();

            // External elements are configured entirely through the public API registration.
            // Do not let the built-in ElementId switch accidentally attach official mechanics to
            // an addon element whose enum bridge is intentionally ElementId.None.
            if (!string.IsNullOrEmpty(marker.elementId) &&
                ElementRegistry.TryGet(marker.elementId, out ElementRegistryRecord registered) &&
                registered.external)
            {
                if (registered.TryGetWeapon(marker.family, out ExternalElementWeaponRecord externalWeapon))
                {
                    if (externalWeapon.ownsSecondary && weapon.GetComponent<ElementalSecondaryOwner>() == null)
                        weapon.AddComponent<ElementalSecondaryOwner>();

                    if (externalWeapon.attach != null)
                    {
                        try
                        {
                            externalWeapon.attach(CustomElementApiBridge.CreateContext(weapon, marker, registered, externalWeapon));
                        }
                        catch (Exception exception)
                        {
                            Plugin.LogSource?.LogError("Custom element attach callback failed for " + registered.stableId +
                                                       " / " + marker.family + ": " + exception);
                        }
                    }
                }
                return;
            }

            switch (marker.family)
            {
                case WeaponFamily.Revolver:
                    if (marker.element == ElementId.Fire || marker.element == ElementId.Grass)
                        AddSecondary<CustomRevolverController>(weapon);
                    else if (marker.element == ElementId.Wind)
                        AddSecondary<PressureRevolverController>(weapon);
                    else if (marker.element == ElementId.Storm)
                        weapon.AddComponent<StormRevolverChargeState>();
                    else if (marker.element == ElementId.Earth)
                        AddSecondary<FaultlineRevolverController>(weapon);
                    break;
                case WeaponFamily.Shotgun:
                    if (marker.element == ElementId.Fire)
                        AddSecondary<CounterShotgunController>(weapon);
                    else if (marker.element == ElementId.Water)
                        AddSecondary<WaterDashController>(weapon);
                    else if (marker.element == ElementId.Grass)
                        AddSecondary<CycloneShotgunController>(weapon, true);
                    else if (marker.element == ElementId.Wind)
                        AddSecondary<UpdraftShotgunController>(weapon);
                    else if (marker.element == ElementId.Storm)
                        AddSecondary<FlashstepJackhammerController>(weapon);
                    else if (marker.element == ElementId.Earth)
                        AddSecondary<StoneguardJackhammerController>(weapon);
                    break;
                case WeaponFamily.Rapid:
                    if (marker.element == ElementId.Grass)
                        AddSecondary<PoisonNailgunController>(weapon, true);
                    else if (marker.element == ElementId.Wind)
                        AddSecondary<TempestSawbladeController>(weapon, true);
                    else if (marker.element == ElementId.Storm)
                        weapon.AddComponent<StormJumpstartScreenColorController>();
                    else if (marker.element == ElementId.Earth)
                        AddSecondary<RockSawbladeController>(weapon, true);
                    break;
                case WeaponFamily.Ultimate:
                    // Storm is the mechanically vanilla Electric Railcannon home. Do not attach
                    // EB balance/input machinery to it; ULTRAKILL owns its charge and firing rules.
                    if (marker.element != ElementId.Storm)
                        weapon.AddComponent<UltimateController>();
                    break;
                case WeaponFamily.Explosive:
                    if (marker.element == ElementId.Water)
                        AddSecondary<GeyserLauncherController>(weapon);
                    else if (marker.element == ElementId.Grass)
                        AddSecondary<GrassBurstLauncherController>(weapon, true);
                    else if (marker.element == ElementId.Wind)
                        AddSecondary<SlipstreamLauncherController>(weapon);
                    else if (marker.element == ElementId.Storm)
                        AddSecondary<ThunderlineLauncherController>(weapon);
                    break;
            }
        }
    }


    /// <summary>
    /// Slab Revolvers persist their post-shot redraw lock through the global
    /// WeaponCharges.revaltpickupcharges array. EB can equip Fire and Earth slabs at the same time,
    /// so each custom slab keeps its own timer and only lends that value to native Revolver code
    /// while the specific weapon is being enabled/fired.
    /// </summary>
    internal sealed class ElementalAltRevolverPrimaryCooldownState : MonoBehaviour
    {
        private float readyAt;
        private int isolationDepth;
        private float savedSharedValue;

        internal float Remaining
        {
            get
            {
                ElementalAltRevolverPrimaryCooldownState owner = SlotResourceOwnerResolver.Resolve(this);
                if (owner != null && owner != this)
                    return owner.Remaining;
                return NoWeaponCooldown.NoCooldown ? 0f : Mathf.Max(0f, readyAt - Time.time);
            }
        }

        internal void Clear()
        {
            ElementalAltRevolverPrimaryCooldownState owner = SlotResourceOwnerResolver.Resolve(this);
            if (owner != null && owner != this)
            {
                owner.Clear();
                return;
            }
            readyAt = 0f;
            isolationDepth = 0;
        }

        internal void BeginNative(Revolver revolver)
        {
            ElementalAltRevolverPrimaryCooldownState owner = SlotResourceOwnerResolver.Resolve(this);
            if (owner != null && owner != this)
            {
                owner.BeginNative(owner.GetComponent<Revolver>());
                return;
            }
            if (revolver == null || !revolver.altVersion)
                return;
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (charges == null || charges.revaltpickupcharges == null || charges.revaltpickupcharges.Length == 0)
                return;
            int variation = Mathf.Clamp(revolver.gunVariation, 0, charges.revaltpickupcharges.Length - 1);
            if (isolationDepth == 0)
            {
                savedSharedValue = charges.revaltpickupcharges[variation];
                charges.revaltpickupcharges[variation] = Remaining;
            }
            isolationDepth++;
        }

        internal void EndNative(Revolver revolver, bool captureNativeWrite)
        {
            ElementalAltRevolverPrimaryCooldownState owner = SlotResourceOwnerResolver.Resolve(this);
            if (owner != null && owner != this)
            {
                owner.EndNative(owner.GetComponent<Revolver>(), captureNativeWrite);
                return;
            }
            if (revolver == null || isolationDepth <= 0)
                return;
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (charges == null || charges.revaltpickupcharges == null || charges.revaltpickupcharges.Length == 0)
            {
                isolationDepth = 0;
                return;
            }
            int variation = Mathf.Clamp(revolver.gunVariation, 0, charges.revaltpickupcharges.Length - 1);
            isolationDepth--;
            if (isolationDepth > 0)
                return;

            float nativeValue = Mathf.Max(0f, charges.revaltpickupcharges[variation]);
            if (captureNativeWrite)
            {
                if (NoWeaponCooldown.NoCooldown || nativeValue <= 0.0001f)
                    readyAt = 0f;
                else
                    readyAt = Time.time + nativeValue;
            }
            charges.revaltpickupcharges[variation] = savedSharedValue;
        }
    }

    /// <summary>
    /// Vanilla Jackhammers store their red-hit overheat in WeaponCharges.shoaltcooldowns[variation].
    /// That is correct for vanilla's three variants, but EB can have multiple custom Jackhammers
    /// backed by the same variation at once (Storm and Earth both use Core Eject Jackhammer).
    /// Keep the native ShotgunHammer implementation, but swap a per-weapon cooldown into the shared
    /// slot only while that specific custom hammer is executing native code.
    /// </summary>
    internal sealed class ElementalJackhammerPrimaryCooldownState : MonoBehaviour
    {
        private float readyAt;
        private float duration;
        private int isolationDepth;
        private float savedSharedValue;
        private float localValueAtBegin;

        internal float Remaining
        {
            get
            {
                ElementalJackhammerPrimaryCooldownState owner = SlotResourceOwnerResolver.Resolve(this);
                if (owner != null && owner != this)
                    return owner.Remaining;
                if (NoWeaponCooldown.NoCooldown)
                    return 0f;
                return Mathf.Max(0f, readyAt - Time.time);
            }
        }

        internal float ReadyFraction
        {
            get
            {
                ElementalJackhammerPrimaryCooldownState owner = SlotResourceOwnerResolver.Resolve(this);
                if (owner != null && owner != this)
                    return owner.ReadyFraction;
                float remaining = Remaining;
                if (duration <= 0f || remaining <= 0f)
                    return 1f;
                return 1f - Mathf.Clamp01(remaining / duration);
            }
        }

        internal void Clear()
        {
            ElementalJackhammerPrimaryCooldownState owner = SlotResourceOwnerResolver.Resolve(this);
            if (owner != null && owner != this)
            {
                owner.Clear();
                return;
            }
            readyAt = 0f;
            duration = 0f;
            isolationDepth = 0;
        }

        internal void BeginNative(ShotgunHammer hammer)
        {
            ElementalJackhammerPrimaryCooldownState owner = SlotResourceOwnerResolver.Resolve(this);
            if (owner != null && owner != this)
            {
                owner.BeginNative(owner.GetComponent<ShotgunHammer>());
                return;
            }
            if (hammer == null)
                return;
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (charges == null || charges.shoaltcooldowns == null || charges.shoaltcooldowns.Length == 0)
                return;
            int variation = Mathf.Clamp(hammer.variation, 0, charges.shoaltcooldowns.Length - 1);

            if (isolationDepth == 0)
            {
                savedSharedValue = charges.shoaltcooldowns[variation];
                localValueAtBegin = Remaining;
                charges.shoaltcooldowns[variation] = localValueAtBegin;
            }
            isolationDepth++;
        }

        internal void EndNative(ShotgunHammer hammer)
        {
            ElementalJackhammerPrimaryCooldownState owner = SlotResourceOwnerResolver.Resolve(this);
            if (owner != null && owner != this)
            {
                owner.EndNative(owner.GetComponent<ShotgunHammer>());
                return;
            }
            if (hammer == null || isolationDepth <= 0)
                return;
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (charges == null || charges.shoaltcooldowns == null || charges.shoaltcooldowns.Length == 0)
            {
                isolationDepth = 0;
                return;
            }
            int variation = Mathf.Clamp(hammer.variation, 0, charges.shoaltcooldowns.Length - 1);

            isolationDepth--;
            if (isolationDepth > 0)
                return;

            float nativeValue = charges.shoaltcooldowns[variation];
            if (NoWeaponCooldown.NoCooldown || nativeValue <= 0.0001f)
            {
                readyAt = 0f;
                duration = 0f;
            }
            else
            {
                // A larger value means native code just started/restarted the cooldown. Preserve
                // that authored duration (currently 7s) rather than hard-coding it in EB.
                if (duration <= 0f || nativeValue > localValueAtBegin + 0.01f)
                    duration = nativeValue;
                readyAt = Time.time + nativeValue;
            }

            charges.shoaltcooldowns[variation] = savedSharedValue;
        }
    }

    internal static class SecondaryInputTiming
    {
        internal static bool IsPressed
        {
            get
            {
                InputManager manager = MonoSingleton<InputManager>.Instance;
                return manager != null && manager.InputSource != null && manager.InputSource.Fire2 != null &&
                       manager.InputSource.Fire2.IsPressed;
            }
        }

        internal static bool WasPressedThisFrame
        {
            get
            {
                InputManager manager = MonoSingleton<InputManager>.Instance;
                return manager != null && manager.InputSource != null && manager.InputSource.Fire2 != null &&
                       manager.InputSource.Fire2.WasPerformedThisFrame;
            }
        }

        // Custom secondaries that do not mechanically depend on the draw animation follow the same
        // input rule as the Grenade Launcher mod: a fresh press during draw-out may act immediately,
        // while an input that was already held before the swap waits until the native weapon has
        // completed its draw. Once a weapon has completed its initial draw, native primary recovery
        // must not re-arm this gate.
        internal static bool AllowsInstantFreshPress(bool initialDrawFinished)
        {
            return IsPressed && (initialDrawFinished || WasPressedThisFrame);
        }
    }

    internal abstract class ActiveWeaponController : MonoBehaviour
    {
        protected ElementalSlotMarker Marker { get; private set; }
        protected float cooldownUntil;
        protected float cooldownDuration;
        private bool cooldownReadyPending;

        internal float CooldownRemaining => Mathf.Max(0f, cooldownUntil - Time.time);
        internal float CooldownDuration => cooldownDuration;
        internal float CooldownReadyFraction => cooldownDuration <= 0f ? 1f : 1f - Mathf.Clamp01(CooldownRemaining / cooldownDuration);

        protected void StartCooldown(float seconds)
        {
            cooldownDuration = Mathf.Max(0f, seconds);
            cooldownUntil = Time.time + cooldownDuration;
            cooldownReadyPending = cooldownDuration > 0f;
        }

        internal void ResetCooldown()
        {
            cooldownUntil = 0f;
            cooldownDuration = 0f;
            cooldownReadyPending = false;
        }

        protected virtual void Awake()
        {
            Marker = GetComponent<ElementalSlotMarker>();
        }

        protected virtual void OnEnable()
        {
            // Cooldowns keep running while holstered, but a cooldown that completed while the
            // weapon was away should stay silent when it is pulled back out later.
            if (cooldownReadyPending && CooldownRemaining <= 0f)
                cooldownReadyPending = false;
        }

        protected virtual void Update()
        {
            if (!cooldownReadyPending || CooldownRemaining > 0f)
                return;

            cooldownReadyPending = false;
            GunControl gc = MonoSingleton<GunControl>.Instance;
            if (gc != null && gc.currentWeapon == gameObject && gameObject.activeInHierarchy)
                CooldownReadyAudio.Play(gameObject);
        }

        protected bool CanUseSecondary()
        {
            GunControl gc = MonoSingleton<GunControl>.Instance;
            if (CooldownRemaining > 0f || MonoSingleton<InputManager>.Instance == null ||
                MonoSingleton<InputManager>.Instance.PerformingCheatMenuCombo() || gc == null || !gc.activated ||
                GameStateManager.Instance.PlayerInputLocked)
                return false;

            if (gc.currentWeapon == gameObject)
                return true;

            // Some projectile-style secondaries make physical sense with Dual Wield and should
            // be emitted by every copied gun. Movement/defensive/target-placement abilities stay
            // main-hand-only. The cloned GameObject carries the same marker and owner component.
            WeaponIdentifier wid = GetComponent<WeaponIdentifier>();
            ElementalSecondaryOwner owner = GetComponent<ElementalSecondaryOwner>();
            if (wid == null || !wid.duplicate || owner == null || !owner.replicateOnDualWield || !gameObject.activeInHierarchy)
                return false;

            ElementalSlotMarker held = gc.currentWeapon != null ? gc.currentWeapon.GetComponent<ElementalSlotMarker>() : null;
            return held != null && Marker != null && held.family == Marker.family && held.element == Marker.element;
        }
    }

}
