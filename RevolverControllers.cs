using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBattlegroundsMod
{
    internal sealed class FireColumnCoinBeamTag : MonoBehaviour
    {
        internal GameObject sourceWeapon;
        internal bool columnSpawned;
        // Explicitly track how many coins this Fire Column shot has travelled through.
        // Coin's native power field is still used for routing/audio, but keeping our own count
        // makes the custom damage reward deterministic and easy to tune.
        internal int coinCount;
    }

    internal sealed class CustomRevolverController : ActiveWeaponController
    {
        private Revolver revolver;
        private Animator animator;
        private static readonly AccessTools.FieldRef<Revolver, bool> ShootReady = AccessTools.FieldRefAccess<Revolver, bool>("shootReady");
        private static readonly AccessTools.FieldRef<Revolver, bool> GunReady = AccessTools.FieldRefAccess<Revolver, bool>("gunReady");
        private static readonly AccessTools.FieldRef<Revolver, float> ShootCharge = AccessTools.FieldRefAccess<Revolver, float>("shootCharge");

        protected override void Awake()
        {
            base.Awake();
            revolver = GetComponent<Revolver>();
            animator = GetComponentInChildren<Animator>();
        }

        internal bool TryFireCustomAlt()
        {
            if (revolver == null || !CanUseSecondary() || !MonoSingleton<InputManager>.Instance.InputSource.Fire2.IsPressed ||
                !ShootReady(revolver) || !GunReady(revolver) || !SecondaryReady)
                return false;

            ShootReady(revolver) = false;
            GunReady(revolver) = false;
            ShootCharge(revolver) = 0f;
            MonoSingleton<PlayerAnimations>.Instance?.Shoot(revolver.altVersion ? 0.5f : 1f);
            animator?.SetTrigger("ChargeShoot");
            PlayShotFeedback();
            if (Marker.element == ElementId.Fire)
            {
                FireColumn();
                StartCooldown(revolver.altVersion ? Mathf.Max(2f, WeaponTuning.FireColumnCooldown) : WeaponTuning.FireColumnCooldown);
            }
            else if (Marker.element == ElementId.Grass)
            {
                FireVine();
                StartCooldown(WeaponTuning.VineCooldown);
            }
            return true;
        }

        internal bool SecondaryReady => CooldownRemaining <= 0f;

        internal float SecondaryReadyFraction => CooldownReadyFraction;

        private void PlayShotFeedback()
        {
            AudioSource audio = GetComponent<AudioSource>();
            if (audio != null && revolver.gunShots != null && revolver.gunShots.Length > 0)
            {
                audio.clip = revolver.gunShots[UnityEngine.Random.Range(0, revolver.gunShots.Length)];
                audio.volume = 0.55f;
                audio.SetPitch(UnityEngine.Random.Range(0.92f, 1.08f));
                audio.Play(tracked: true);
            }
            MonoSingleton<CameraController>.Instance?.CameraShake(0.35f);
            MonoSingleton<RumbleManager>.Instance?.SetVibrationTracked(RumbleProperties.GunFire, gameObject);
        }

        private void FireColumn()
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            Vector3 origin = cc.GetDefaultPos();
            Vector3 direction = cc.transform.forward;
            Vector3 point = origin + direction * 1000f;
            RaycastHit hit;
            bool gotHit = TryCustomRevolverRay(origin, direction, true, out hit);
            if (gotHit)
                point = hit.point;

            Vector3 muzzle = GetMuzzlePosition(origin);
            Coin coin = gotHit ? hit.collider.GetComponentInParent<Coin>() : null;
            if (coin != null && !coin.shot)
            {
                // Let a real zero-damage RevolverBeam hit the coin. The tagged ExecuteHits patch
                // only swaps the continuation beam so the rest of Coin's native chain logic stays
                // intact, including multi-coin power accumulation.
                GameObject coinBeam = CreateFireCoinBeam();
                if (coinBeam != null)
                {
                    coinBeam.transform.SetPositionAndRotation(cc.transform.position, cc.transform.rotation);
                    RevolverBeam taggedBeam = coinBeam.GetComponent<RevolverBeam>();
                    if (taggedBeam != null)
                        taggedBeam.alternateStartPoint = muzzle;
                    coinBeam.SetActive(true);
                    return;
                }
            }

            SpawnNativeTracer(muzzle, point, ElementPalette.Accent(ElementId.Fire), gotHit ? (RaycastHit?)hit : null);
            if (gotHit)
                ApplyTerrainFeedback(hit);

            SpawnFireColumn(point, 1f, gameObject);
        }

        private void FireVine()
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            Vector3 origin = cc.GetDefaultPos();
            Vector3 direction = cc.transform.forward;
            Vector3 point = origin + direction * 1000f;
            EnemyIdentifier anchorEnemy = null;
            RaycastHit hit;
            bool gotHit = TryCustomRevolverRay(origin, direction, false, out hit);
            if (gotHit)
            {
                point = hit.point;
                EnemyIdentifierIdentifier eidid = hit.collider.GetComponentInParent<EnemyIdentifierIdentifier>();
                if (eidid != null)
                    anchorEnemy = eidid.eid;
                ApplyTerrainFeedback(hit);
            }

            SpawnNativeTracer(GetMuzzlePosition(origin), point, ElementPalette.Accent(ElementId.Grass), gotHit ? (RaycastHit?)hit : null);
            GameObject host = new GameObject("EB Vine Anchor");
            host.transform.position = point;
            VineAnchorRuntime vine = host.AddComponent<VineAnchorRuntime>();
            vine.sourceWeapon = gameObject;
            vine.anchorEnemy = anchorEnemy;
            if (anchorEnemy != null)
                host.transform.SetParent(anchorEnemy.bodyTransform, true);
        }

        private Vector3 GetMuzzlePosition(Vector3 fallback)
        {
            return revolver != null && revolver.gunBarrel != null ? revolver.gunBarrel.transform.position : fallback;
        }

        private void SpawnNativeTracer(Vector3 start, Vector3 end, Color color, RaycastHit? impact)
        {
            if (revolver == null || revolver.revolverBeam == null)
                return;
            CameraController cc = MonoSingleton<CameraController>.Instance;
            GameObject obj = UnityEngine.Object.Instantiate(revolver.revolverBeam, cc.transform.position, cc.transform.rotation);
            obj.transform.position = start;
            RevolverBeam beam = obj.GetComponent<RevolverBeam>();
            if (beam != null)
            {
                // Keep the real RevolverBeam prefab (light, muzzle flash, materials and fade), but
                // suppress its damage/raycast. The actual custom effect owns the hit semantics.
                beam.fake = true;
                beam.sourceWeapon = gameObject;
                beam.alternateStartPoint = start;
                beam.damage = 0f;
            }
            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line != null)
            {
                line.useWorldSpace = true;
                line.SetPosition(0, start);
                line.SetPosition(1, end);
                line.startColor = color;
                line.endColor = new Color(color.r, color.g, color.b, 0.15f);
            }
            foreach (Light light in obj.GetComponentsInChildren<Light>(true))
                if (light != null) light.color = color;
            foreach (ParticleSystem particle in obj.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particle == null) continue;
                ParticleSystem.MainModule main = particle.main;
                main.startColor = color;
            }
            if (obj.transform.childCount > 0)
            {
                Transform muzzle = obj.transform.GetChild(0);
                muzzle.position = start;
                muzzle.rotation = cc.transform.rotation;
            }
            if (impact.HasValue && beam != null && beam.hitParticle != null)
            {
                RaycastHit hit = impact.Value;
                GameObject effect = UnityEngine.Object.Instantiate(beam.hitParticle, hit.point,
                    hit.normal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(hit.normal) : Quaternion.identity);
                foreach (Light light in effect.GetComponentsInChildren<Light>(true))
                    if (light != null) light.color = color;
            }
        }

        private GameObject CreateFireCoinBeam()
        {
            if (revolver == null || revolver.revolverBeam == null)
                return null;
            CameraController cc = MonoSingleton<CameraController>.Instance;
            GameObject obj = UnityEngine.Object.Instantiate(revolver.revolverBeam, cc.transform.position, cc.transform.rotation);
            obj.SetActive(false);
            RevolverBeam beam = obj.GetComponent<RevolverBeam>();
            if (beam == null)
            {
                UnityEngine.Object.Destroy(obj);
                return null;
            }
            beam.fake = false;
            beam.sourceWeapon = gameObject;
            beam.alternateStartPoint = Vector3.zero;
            beam.damage = 0f;
            beam.addedDamage = 0f;
            beam.coinDamageBonusMultiplier = 0f; // coin power is converted into column damage by the tag patch
            beam.hitAmount = 1;
            beam.maxHitsPerTarget = 1;
            beam.splitcoinable = false;
            beam.strongAlt = false;
            Color color = ElementPalette.Accent(ElementId.Fire);
            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line != null)
            {
                line.startColor = color;
                line.endColor = new Color(color.r, color.g, color.b, 0.15f);
            }
            foreach (Light light in obj.GetComponentsInChildren<Light>(true))
                if (light != null) light.color = color;
            FireColumnCoinBeamTag tag = obj.GetComponent<FireColumnCoinBeamTag>();
            if (tag == null) tag = obj.AddComponent<FireColumnCoinBeamTag>();
            tag.sourceWeapon = gameObject;
            tag.columnSpawned = false;
            tag.coinCount = 0;
            return obj;
        }

        internal static void SpawnFireColumn(Vector3 point, float damageMultiplier, GameObject sourceWeapon)
        {
            Vector3 effectPoint = point;
            if (Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out RaycastHit floorHit, 50f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
                effectPoint = floorHit.point;
            GameObject host = new GameObject("EB Fire Column");
            host.transform.position = effectPoint;
            FireColumnRuntime column = host.AddComponent<FireColumnRuntime>();
            column.sourceWeapon = sourceWeapon;
            column.damageMultiplier = damageMultiplier;
        }

        private static void ApplyTerrainFeedback(RaycastHit hit)
        {
            if (hit.collider == null) return;
            if (SceneHelper.IsStaticEnvironment(hit))
                MonoSingleton<SceneHelper>.Instance?.CreateEnviroGibs(hit, 3, 1f);
            Glass glass = hit.collider.GetComponent<Glass>() ?? hit.collider.GetComponentInParent<Glass>();
            if (glass != null && !glass.broken)
                glass.Shatter();
            Breakable breakable = hit.collider.GetComponent<Breakable>() ?? hit.collider.GetComponentInParent<Breakable>();
            if (breakable != null && !breakable.unbreakable && !breakable.specialCaseOnly && breakable.weak)
                breakable.Break(1f);
        }

        private static bool TryCustomRevolverRay(Vector3 origin, Vector3 direction, bool allowCoin, out RaycastHit selected)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, 10000f,
                LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment), QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                Coin coin = hit.collider.GetComponentInParent<Coin>();
                if (coin != null && !allowCoin)
                    continue;
                selected = hit;
                return true;
            }
            selected = default(RaycastHit);
            return false;
        }
    }

}
