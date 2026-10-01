using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBattlegroundsMod
{
    internal sealed class GeyserLauncherController : ActiveWeaponController
    {
        private RocketLauncher launcher;
        private static readonly AccessTools.FieldRef<RocketLauncher, TimeSince> SinceEquipped =
            AccessTools.FieldRefAccess<RocketLauncher, TimeSince>("sinceEquipped");
        private const float NativeDrawDelay = 0.25f;

        protected override void Awake()
        {
            base.Awake();
            launcher = GetComponent<RocketLauncher>();
        }

        protected override void Update()
        {
            base.Update();
            bool drawFinished = launcher != null && (float)SinceEquipped(launcher) >= NativeDrawDelay;
            if (!CanUseSecondary() || !SecondaryInputTiming.AllowsInstantFreshPress(drawFinished))
                return;

            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            Vector3 up = movement.transform.up;
            Vector3 origin = movement.transform.position + up;
            // The geyser is a floor object, so let the alt create it even when the player is far
            // above the floor. Being too high is still punished: the initial player is only
            // eligible for the launch if they were actually within the geyser's launch height.
            if (!Physics.Raycast(origin, -up, out RaycastHit hit, 1000f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
                return;

            GetComponent<Animator>()?.SetTrigger("Fire");
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();

            float verticalDistance = Mathf.Max(0f, Vector3.Dot(movement.transform.position - hit.point, up));
            bool playerInLaunchRange = verticalDistance <= 6f;
            if (playerInLaunchRange)
            {
                Vector3 velocity = movement.rb.velocity;
                Vector3 planar = Vector3.ProjectOnPlane(velocity, up);
                float upward = Vector3.Dot(velocity, up);
                float launchSpeed = Mathf.Max(WeaponTuning.GeyserPlayerSpeed, upward);
                movement.Launch(up, 0.01f, true);
                movement.rb.velocity = planar + up * launchSpeed;
                movement.transform.position += up * 0.08f;
                RuntimeAudio.PlayBoostPadLaunch(movement);
            }

            GameObject host = new GameObject("EB Geyser");
            host.transform.position = hit.point;
            GeyserRuntime geyser = host.AddComponent<GeyserRuntime>();
            geyser.sourceWeapon = gameObject;
            geyser.up = up;
            geyser.playerAlreadyLaunched = playerInLaunchRange;
            geyser.allowPlayerLaunch = playerInLaunchRange;
            StartCooldown(WeaponTuning.GeyserCooldown);
        }
    }

    internal sealed class GrassBurstLauncherController : ActiveWeaponController
    {
        private RocketLauncher launcher;
        private bool firing;
        private Coroutine burstRoutine;
        private float drawReadyAt;
        private const float RequiredDrawDelay = 0.25f;
        private static readonly AccessTools.FieldRef<RocketLauncher, float> RocketCooldown =
            AccessTools.FieldRefAccess<RocketLauncher, float>("cooldown");

        protected override void Awake()
        {
            base.Awake();
            launcher = GetComponent<RocketLauncher>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Keep a real draw timer instead of trusting RocketLauncher.sinceEquipped, because a
            // fresh Alt-Fire press can fast-forward the native timer. We still allow that fresh
            // press immediately; an Alt-Fire that was already held before swapping waits here.
            drawReadyAt = Time.time + RequiredDrawDelay;
        }

        protected override void Update()
        {
            base.Update();
            bool drawFinished = Time.time >= drawReadyAt;
            // Share the launcher's native primary readiness exactly like Grenade Launcher's
            // green alternate: if a normal rocket is still on fire cooldown, Burst is not
            // allowed to bypass that recovery and effectively create a fourth instant rocket.
            if (launcher == null || firing || !CanUseSecondary() ||
                (!NoWeaponCooldown.NoCooldown && RocketCooldown(launcher) > 0.0001f) ||
                !SecondaryInputTiming.AllowsInstantFreshPress(drawFinished))
                return;
            burstRoutine = StartCoroutine(Burst());
        }

        private void OnDisable()
        {
            // Deactivating the weapon stops Unity coroutines before their finally/last-line cleanup
            // can run. Explicitly clear the burst state so cancelling by weapon swap cannot jam the
            // alt for the rest of the level.
            if (burstRoutine != null)
                StopCoroutine(burstRoutine);
            burstRoutine = null;
            firing = false;
            GrassBurstContext.Reset();
        }

        private IEnumerator Burst()
        {
            firing = true;
            StartCooldown(WeaponTuning.GrassRocketCooldown);
            for (int i = 0; i < 3; i++)
            {
                GrassBurstContext.Enter();
                try
                {
                    launcher.Shoot();
                }
                finally
                {
                    GrassBurstContext.Exit();
                }
                yield return new WaitForSeconds(WeaponTuning.GrassRocketInterval);
            }
            firing = false;
            burstRoutine = null;
        }
    }

    internal static class GrassBurstContext
    {
        private static int depth;
        internal static bool Active => depth > 0;
        internal static void Enter() => depth++;
        internal static void Exit() => depth = Mathf.Max(0, depth - 1);
        internal static void Reset() => depth = 0;
    }
}
