using System;
using System.Collections.Generic;
using ElementalBattlegroundsMod.Api;
using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    /// <summary>
    /// EB can equip combinations that ULTRAKILL's terminal normally makes mutually exclusive.
    /// A few vanilla resources therefore live in WeaponCharges singletons only because vanilla
    /// never has two physical owners for them at once. These helpers virtualize only those
    /// mutually-exclusive / duplicate-chassis resources per EB slot while deliberately leaving
    /// true player-wide resources (Rapid ammo/heat, Rail charge, Rocket primary recovery,
    /// Jackhammer yellow-hit streak, etc.) shared.
    /// </summary>
    internal static class SlotResourceOwnerResolver
    {
        internal static T Resolve<T>(T state) where T : Component
        {
            if (state == null)
                return null;

            WeaponIdentifier wid = state.GetComponent<WeaponIdentifier>();
            if (wid == null || !wid.duplicate)
                return state;

            ElementalSlotMarker marker = state.GetComponent<ElementalSlotMarker>();
            GunControl gc = MonoSingleton<GunControl>.Instance;
            int family = marker != null ? (int)marker.family : -1;
            int position = marker != null ? marker.position : -1;

            // ULTRAKILL destroys and recreates every Dual Wield clone on every weapon swap.
            // The old implementation searched Resources.FindObjectsOfTypeAll<T>() the first time
            // each fresh clone asked for its resource owner, causing one scene-wide scan per clone
            // per swap (and a cache full of dead clone instance IDs). EB already stores the exact
            // logical owner on the copied marker, so resolve straight through GunControl's slot
            // table instead. This stays O(1) no matter how many Dual Wield powerups are active.
            if (gc != null && gc.slots != null && family >= 0 && family < gc.slots.Count)
            {
                List<GameObject> slot = gc.slots[family];
                if (slot != null && position >= 0 && position < slot.Count)
                {
                    GameObject ownerObject = slot[position];
                    if (ownerObject != null && ownerObject != state.gameObject)
                    {
                        ElementalSlotMarker ownerMarker = ownerObject.GetComponent<ElementalSlotMarker>();
                        if (ownerMarker != null &&
                            string.Equals(ownerMarker.choiceId, marker.choiceId, StringComparison.Ordinal))
                        {
                            T owner = ownerObject.GetComponent<T>();
                            if (owner != null)
                                return owner;
                        }
                    }
                }
            }

            // If another mod creates an unusual duplicate outside GunControl's EB slot table, do
            // not perform an expensive global search. Falling back to the local state is safer and
            // keeps ordinary weapon swapping deterministic.
            return state;
        }

        // Kept as a no-op compatibility hook for rebuild/respawn callers from older revisions.
        internal static void ClearCache() { }
    }

    internal static class PerSlotVanillaResourceRuntime
    {
        internal static void Attach(GameObject weapon, ElementalSlotMarker marker)
        {
            if (weapon == null || marker == null || marker.IsEmpty)
                return;

            Revolver revolver = weapon.GetComponent<Revolver>();
            if (revolver != null)
            {
                RevolverResourceKind kind = RevolverResourceKind.None;
                if (marker.element == ElementId.Water || IsChoice(marker, "rev_piercer_std", "rev_piercer_alt") ||
                    IsExternalTemplate(marker, VanillaWeaponTemplate.PiercerRevolver, VanillaWeaponTemplate.SlabPiercerRevolver))
                    kind = RevolverResourceKind.Piercer;
                else if (IsChoice(marker, "rev_marksman_std", "rev_marksman_alt") ||
                         IsExternalTemplate(marker, VanillaWeaponTemplate.MarksmanRevolver, VanillaWeaponTemplate.SlabMarksmanRevolver))
                    kind = RevolverResourceKind.Marksman;
                else if (IsChoice(marker, "rev_sharp_std", "rev_sharp_alt") ||
                         IsExternalTemplate(marker, VanillaWeaponTemplate.SharpshooterRevolver, VanillaWeaponTemplate.SlabSharpshooterRevolver))
                    kind = RevolverResourceKind.Sharpshooter;

                if (kind != RevolverResourceKind.None)
                {
                    PerSlotRevolverSecondaryState state = weapon.GetComponent<PerSlotRevolverSecondaryState>();
                    if (state == null)
                        state = weapon.AddComponent<PerSlotRevolverSecondaryState>();
                    state.Configure(kind);
                }
            }

            // The two Sawed-On forms are mutually exclusive in the terminal, but EB can put both
            // in the arsenal at once. Give each physical form its own launch charge and wear reset.
            if (IsChoice(marker, "sho_saw_std", "sho_saw_alt") ||
                IsExternalTemplate(marker, VanillaWeaponTemplate.SawedOnShotgun, VanillaWeaponTemplate.SawedOnJackhammer))
            {
                if (weapon.GetComponent<PerSlotSawedOnState>() == null)
                    weapon.AddComponent<PerSlotSawedOnState>();
            }

            Nailgun nailgun = weapon.GetComponent<Nailgun>();
            if (nailgun != null)
            {
                if (marker.element == ElementId.Fire || IsChoice(marker, "nai_over_std", "nai_over_alt") ||
                    IsExternalTemplate(marker, VanillaWeaponTemplate.OverheatNailgun, VanillaWeaponTemplate.OverheatSawbladeLauncher))
                {
                    if (weapon.GetComponent<PerSlotOverheatHeatsinkState>() == null)
                        weapon.AddComponent<PerSlotOverheatHeatsinkState>();
                }

                if (marker.element == ElementId.Water || IsChoice(marker, "nai_attr_std", "nai_attr_alt") ||
                    IsExternalTemplate(marker, VanillaWeaponTemplate.AttractorNailgun, VanillaWeaponTemplate.AttractorSawbladeLauncher))
                {
                    if (weapon.GetComponent<PerSlotAttractorMagnetState>() == null)
                        weapon.AddComponent<PerSlotAttractorMagnetState>();
                }

                if (marker.element == ElementId.Storm || IsChoice(marker, "nai_jump_std", "nai_jump_alt") ||
                    IsExternalTemplate(marker, VanillaWeaponTemplate.JumpstartNailgun, VanillaWeaponTemplate.JumpstartSawbladeLauncher))
                {
                    if (weapon.GetComponent<PerSlotJumpstartRechargeState>() == null)
                        weapon.AddComponent<PerSlotJumpstartRechargeState>();
                }
            }

            RocketLauncher launcher = weapon.GetComponent<RocketLauncher>();
            if (launcher != null && marker.explosiveMode != ExplosiveIntegrationMode.ForceGrenade)
            {
                RocketSecondaryResourceKind kind = RocketSecondaryResourceKind.None;
                if (marker.element == ElementId.Earth || IsChoice(marker, "rock_srs_std") ||
                    IsExternalTemplate(marker, VanillaWeaponTemplate.SrsRocketLauncher))
                    kind = RocketSecondaryResourceKind.Cannonball;
                else if (marker.element == ElementId.Fire || IsChoice(marker, "rock_fire_std") ||
                         IsExternalTemplate(marker, VanillaWeaponTemplate.FirestarterRocketLauncher))
                    kind = RocketSecondaryResourceKind.Napalm;

                if (kind != RocketSecondaryResourceKind.None)
                {
                    PerSlotRocketSecondaryState state = weapon.GetComponent<PerSlotRocketSecondaryState>();
                    if (state == null)
                        state = weapon.AddComponent<PerSlotRocketSecondaryState>();
                    state.Configure(kind);
                }
            }
        }

        private static bool IsExternalTemplate(ElementalSlotMarker marker, params VanillaWeaponTemplate[] templates)
        {
            // Built-in elements keep the exact ownership rules they had before API v1. Template
            // metadata is broader than ownership: e.g. Grass/Wind borrow Jumpstart chassis but do
            // not own Jumpstart's vanilla cable resource. Only external registrations use their
            // public template descriptor as the ownership declaration.
            if (marker == null || !marker.hasVanillaTemplate || templates == null ||
                string.IsNullOrEmpty(marker.elementId) ||
                !ElementRegistry.TryGet(marker.elementId, out ElementRegistryRecord record) || !record.external)
                return false;
            foreach (VanillaWeaponTemplate template in templates)
            {
                if (marker.vanillaTemplate == template)
                    return true;
            }
            return false;
        }

        private static bool IsChoice(ElementalSlotMarker marker, params string[] ids)
        {
            if (marker == null || string.IsNullOrEmpty(marker.choiceId))
                return false;
            foreach (string id in ids)
            {
                if (string.Equals(marker.choiceId, id, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }

    internal enum RevolverResourceKind
    {
        None,
        Piercer,
        Marksman,
        Sharpshooter
    }

    internal sealed class PerSlotRevolverSecondaryState : MonoBehaviour
    {
        private RevolverResourceKind kind;
        private Revolver revolver;
        private float charge;
        private float lastRefreshAt;
        private int depth;
        private float savedCharge;
        private bool savedAlt;

        internal bool UsesGlobalDuringUpdate => kind == RevolverResourceKind.Marksman || kind == RevolverResourceKind.Sharpshooter;

        internal void Configure(RevolverResourceKind value)
        {
            kind = value;
            revolver = GetComponent<Revolver>();
            charge = MaxCharge;
            lastRefreshAt = Time.time;
        }

        private float MaxCharge => kind == RevolverResourceKind.Marksman ? 400f :
                                   kind == RevolverResourceKind.Sharpshooter ? 300f : 100f;

        private float RechargePerSecond
        {
            get
            {
                if (kind == RevolverResourceKind.Piercer)
                    return revolver != null && revolver.altVersion ? 20f : 40f;
                if (kind == RevolverResourceKind.Marksman)
                    return 25f;
                if (kind == RevolverResourceKind.Sharpshooter)
                    return revolver != null && revolver.altVersion ? 35f : 15f;
                return 0f;
            }
        }

        private void Refresh()
        {
            float now = Time.time;
            if (NoWeaponCooldown.NoCooldown)
                charge = MaxCharge;
            else if (lastRefreshAt > 0f && charge < MaxCharge)
                charge = Mathf.MoveTowards(charge, MaxCharge, Mathf.Max(0f, now - lastRefreshAt) * RechargePerSecond);
            lastRefreshAt = now;
        }

        internal void ResetFull()
        {
            charge = MaxCharge;
            lastRefreshAt = Time.time;
            depth = 0;
        }

        internal void BeginNative()
        {
            if (kind == RevolverResourceKind.None)
                return;
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
                return;
            if (depth == 0)
            {
                Refresh();
                switch (kind)
                {
                    case RevolverResourceKind.Piercer:
                        savedCharge = wc.rev0charge;
                        savedAlt = wc.rev0alt;
                        wc.rev0charge = charge;
                        wc.rev0alt = revolver != null && revolver.altVersion;
                        break;
                    case RevolverResourceKind.Marksman:
                        savedCharge = wc.rev1charge;
                        wc.rev1charge = charge;
                        break;
                    case RevolverResourceKind.Sharpshooter:
                        savedCharge = wc.rev2charge;
                        savedAlt = wc.rev2alt;
                        wc.rev2charge = charge;
                        wc.rev2alt = revolver != null && revolver.altVersion;
                        break;
                }
            }
            depth++;
        }

        internal void EndNative()
        {
            if (kind == RevolverResourceKind.None || depth <= 0)
                return;
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
            {
                depth = 0;
                return;
            }
            depth--;
            if (depth > 0)
                return;

            switch (kind)
            {
                case RevolverResourceKind.Piercer:
                    charge = Mathf.Clamp(wc.rev0charge, 0f, MaxCharge);
                    wc.rev0charge = savedCharge;
                    wc.rev0alt = savedAlt;
                    break;
                case RevolverResourceKind.Marksman:
                    charge = Mathf.Clamp(wc.rev1charge, 0f, MaxCharge);
                    wc.rev1charge = savedCharge;
                    break;
                case RevolverResourceKind.Sharpshooter:
                    charge = Mathf.Clamp(wc.rev2charge, 0f, MaxCharge);
                    wc.rev2charge = savedCharge;
                    wc.rev2alt = savedAlt;
                    break;
            }
            lastRefreshAt = Time.time;
        }
    }

    internal sealed class PerSlotSawedOnState : MonoBehaviour
    {
        private float charge = 1f;
        private float lastRefreshAt;
        private float wearResetUntil;
        private int chargeDepth;
        private float savedSharedCharge;
        private int wearDepth;
        private float savedSharedWear;

        private void Awake()
        {
            lastRefreshAt = Time.time;
        }

        private void RefreshCharge()
        {
            float now = Time.time;
            if (NoWeaponCooldown.NoCooldown)
                charge = 1f;
            else if (charge < 1f)
            {
                WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
                float rate = wc != null && wc.shoSawAmount > 0 ? 0.125f : 0.25f;
                charge = Mathf.MoveTowards(charge, 1f, Mathf.Max(0f, now - lastRefreshAt) * rate);
            }
            lastRefreshAt = now;
        }

        internal void ResetFull()
        {
            charge = 1f;
            lastRefreshAt = Time.time;
            wearResetUntil = 0f;
            chargeDepth = 0;
            wearDepth = 0;
        }

        internal void BeginChargeNative()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
                return;
            if (chargeDepth == 0)
            {
                RefreshCharge();
                savedSharedCharge = wc.shoSawCharge;
                wc.shoSawCharge = charge;
            }
            chargeDepth++;
        }

        internal void EndChargeNative()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null || chargeDepth <= 0)
            {
                chargeDepth = 0;
                return;
            }
            chargeDepth--;
            if (chargeDepth > 0)
                return;
            charge = Mathf.Clamp01(wc.shoSawCharge);
            lastRefreshAt = Time.time;
            wc.shoSawCharge = savedSharedCharge;
        }

        internal void BeginWearNative()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
                return;
            if (wearDepth == 0)
            {
                savedSharedWear = wc.shoSawResetTimer;
                wc.shoSawResetTimer = Mathf.Max(0f, wearResetUntil - Time.time);
            }
            wearDepth++;
        }

        internal void EndWearNative()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null || wearDepth <= 0)
            {
                wearDepth = 0;
                return;
            }
            wearDepth--;
            if (wearDepth > 0)
                return;
            float remaining = Mathf.Max(0f, wc.shoSawResetTimer);
            wearResetUntil = remaining > 0f ? Time.time + remaining : 0f;
            wc.shoSawResetTimer = savedSharedWear;
        }
    }

    internal sealed class PerSlotOverheatHeatsinkState : MonoBehaviour
    {
        private Nailgun nailgun;
        private float heatSinks;
        private float lastRefreshAt;
        private int depth;
        private float savedShared;

        private float Max => nailgun != null && nailgun.altVersion ? 1f : 2f;

        private void Awake()
        {
            nailgun = GetComponent<Nailgun>();
            heatSinks = Max;
            lastRefreshAt = Time.time;
        }

        private void Refresh()
        {
            float now = Time.time;
            if (NoWeaponCooldown.NoCooldown)
                heatSinks = Max;
            else if (heatSinks < Max)
                heatSinks = Mathf.MoveTowards(heatSinks, Max, Mathf.Max(0f, now - lastRefreshAt) * 0.125f);
            lastRefreshAt = now;
        }

        internal void ResetFull()
        {
            heatSinks = Max;
            lastRefreshAt = Time.time;
            depth = 0;
        }

        internal void BeginNative()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
                return;
            if (depth == 0)
            {
                Refresh();
                if (nailgun != null && nailgun.altVersion)
                {
                    savedShared = wc.naiSawHeatsinks;
                    wc.naiSawHeatsinks = heatSinks;
                }
                else
                {
                    savedShared = wc.naiHeatsinks;
                    wc.naiHeatsinks = heatSinks;
                }
            }
            depth++;
        }

        internal void EndNative()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null || depth <= 0)
            {
                depth = 0;
                return;
            }
            depth--;
            if (depth > 0)
                return;
            if (nailgun != null && nailgun.altVersion)
            {
                heatSinks = Mathf.Clamp(wc.naiSawHeatsinks, 0f, Max);
                wc.naiSawHeatsinks = savedShared;
            }
            else
            {
                heatSinks = Mathf.Clamp(wc.naiHeatsinks, 0f, Max);
                wc.naiHeatsinks = savedShared;
            }
            lastRefreshAt = Time.time;
        }
    }

    internal sealed class MagnetSlotOwnership : MonoBehaviour
    {
        internal PerSlotAttractorMagnetState owner;
    }

    internal sealed class PerSlotAttractorMagnetState : MonoBehaviour
    {
        private readonly List<Magnet> ownedMagnets = new List<Magnet>();
        private float charge = 3f;
        private float lastRefreshAt;
        private int depth;
        private float savedShared;

        private void Awake()
        {
            lastRefreshAt = Time.time;
        }

        private int ActiveOwnedMagnets()
        {
            for (int i = ownedMagnets.Count - 1; i >= 0; i--)
            {
                if (ownedMagnets[i] == null)
                    ownedMagnets.RemoveAt(i);
            }
            return ownedMagnets.Count;
        }

        private void Refresh()
        {
            float now = Time.time;
            if (NoWeaponCooldown.NoCooldown)
            {
                charge = 3f;
            }
            else
            {
                float target = Mathf.Max(0f, 3f - ActiveOwnedMagnets());
                if (charge < target)
                    charge = Mathf.MoveTowards(charge, target, Mathf.Max(0f, now - lastRefreshAt) * 3f);
                else if (charge > target)
                    charge = target;
            }
            lastRefreshAt = now;
        }

        internal void ResetFull()
        {
            charge = 3f;
            ownedMagnets.Clear();
            lastRefreshAt = Time.time;
            depth = 0;
        }

        internal void BeginNative()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
                return;
            if (depth == 0)
            {
                Refresh();
                savedShared = wc.naiMagnetCharge;
                wc.naiMagnetCharge = charge;
            }
            depth++;
        }

        internal void EndNative()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null || depth <= 0)
            {
                depth = 0;
                return;
            }
            depth--;
            if (depth > 0)
                return;
            charge = Mathf.Clamp(wc.naiMagnetCharge, 0f, 3f);
            lastRefreshAt = Time.time;
            wc.naiMagnetCharge = savedShared;
        }

        internal void ClaimNewestMagnet()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null || wc.magnets == null)
                return;
            for (int i = wc.magnets.Count - 1; i >= 0; i--)
            {
                Magnet magnet = wc.magnets[i];
                if (magnet == null)
                    continue;
                MagnetSlotOwnership ownership = magnet.GetComponent<MagnetSlotOwnership>();
                if (ownership != null)
                    continue;
                ownership = magnet.gameObject.AddComponent<MagnetSlotOwnership>();
                ownership.owner = this;
                ownedMagnets.Add(magnet);
                return;
            }
        }
    }

    internal sealed class PerSlotJumpstartRechargeState : MonoBehaviour
    {
        private float charge = 5f;
        private float lastRefreshAt;
        private int depth;
        private float savedShared;

        private void Awake()
        {
            lastRefreshAt = Time.time;
        }

        private void Refresh()
        {
            float now = Time.time;
            if (NoWeaponCooldown.NoCooldown)
                charge = 5f;
            else if (charge < 5f)
                charge = Mathf.MoveTowards(charge, 5f, Mathf.Max(0f, now - lastRefreshAt));
            lastRefreshAt = now;
        }

        internal void ResetFull()
        {
            charge = 5f;
            lastRefreshAt = Time.time;
            depth = 0;
        }

        internal void BeginNative()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
                return;
            if (depth == 0)
            {
                Refresh();
                savedShared = wc.naiZapperRecharge;
                wc.naiZapperRecharge = charge;
            }
            depth++;
        }

        internal void EndNative()
        {
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null || depth <= 0)
            {
                depth = 0;
                return;
            }
            depth--;
            if (depth > 0)
                return;
            charge = Mathf.Clamp(wc.naiZapperRecharge, 0f, 5f);
            lastRefreshAt = Time.time;
            wc.naiZapperRecharge = savedShared;
        }
    }

    internal enum RocketSecondaryResourceKind
    {
        None,
        Cannonball,
        Napalm
    }

    internal sealed class PerSlotRocketSecondaryState : MonoBehaviour
    {
        private RocketSecondaryResourceKind kind;
        private float charge = 1f;
        private float lastRefreshAt;
        private int depth;
        private float savedShared;

        internal void Configure(RocketSecondaryResourceKind value)
        {
            kind = value;
            charge = 1f;
            lastRefreshAt = Time.time;
        }

        private void Refresh()
        {
            float now = Time.time;
            if (NoWeaponCooldown.NoCooldown)
                charge = 1f;
            else if (charge < 1f)
                charge = Mathf.MoveTowards(charge, 1f, Mathf.Max(0f, now - lastRefreshAt) * 0.125f);
            lastRefreshAt = now;
        }

        internal void ResetFull()
        {
            charge = 1f;
            lastRefreshAt = Time.time;
            depth = 0;
        }

        internal void BeginNative()
        {
            if (kind == RocketSecondaryResourceKind.None)
                return;
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
                return;
            if (depth == 0)
            {
                Refresh();
                if (kind == RocketSecondaryResourceKind.Cannonball)
                {
                    savedShared = wc.rocketCannonballCharge;
                    wc.rocketCannonballCharge = charge;
                }
                else
                {
                    savedShared = wc.rocketNapalmFuel;
                    wc.rocketNapalmFuel = charge;
                }
            }
            depth++;
        }

        internal void EndNative()
        {
            if (kind == RocketSecondaryResourceKind.None || depth <= 0)
                return;
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
            {
                depth = 0;
                return;
            }
            depth--;
            if (depth > 0)
                return;
            if (kind == RocketSecondaryResourceKind.Cannonball)
            {
                charge = Mathf.Clamp01(wc.rocketCannonballCharge);
                wc.rocketCannonballCharge = savedShared;
            }
            else
            {
                charge = Mathf.Clamp01(wc.rocketNapalmFuel);
                wc.rocketNapalmFuel = savedShared;
            }
            lastRefreshAt = Time.time;
        }
    }

}
