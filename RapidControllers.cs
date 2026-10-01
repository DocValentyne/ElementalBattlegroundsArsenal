using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBattlegroundsMod
{
    internal sealed class PoisonNailgunController : ActiveWeaponController
    {
        private Nailgun nailgun;
        private bool initialDrawFinished;
        private static readonly AccessTools.FieldRef<Nailgun, bool> NailgunCanShoot =
            AccessTools.FieldRefAccess<Nailgun, bool>("canShoot");

        protected override void Awake()
        {
            base.Awake();
            nailgun = GetComponent<Nailgun>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            initialDrawFinished = false;
        }

        protected override void Update()
        {
            base.Update();
            if (nailgun != null && !initialDrawFinished && NailgunCanShoot(nailgun))
                initialDrawFinished = true;
            if (!CanUseSecondary() || !SecondaryInputTiming.AllowsInstantFreshPress(initialDrawFinished))
                return;

            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (cc == null)
                return;
            Vector3 origin = cc.GetDefaultPos() + cc.transform.forward * 0.75f;
            Vector3 direction = cc.transform.forward.normalized;
            // The Nailgun's initial draw animation owns the CanShoot event. Do not replace that
            // animation when a fresh secondary press is accepted during draw-out, or the primary
            // could remain permanently disabled for this equip.
            if (initialDrawFinished)
                GetComponentInChildren<Animator>()?.SetTrigger("Shoot");
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();
            RuntimeAudio.PlayJumpstartCable(nailgun);
            GameObject host = new GameObject("EB Poison Orb");
            host.transform.position = origin;
            PoisonOrbRuntime orb = host.AddComponent<PoisonOrbRuntime>();
            orb.sourceWeapon = gameObject;
            orb.direction = direction;
            StartCooldown(WeaponTuning.PoisonOrbCooldown);
        }
    }

}
