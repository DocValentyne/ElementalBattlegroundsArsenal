using UnityEngine;

namespace ElementalBattlegroundsMod
{
    // Counter's combustion rides ULTRAKILL's real Flammable/EnemyIdentifier fire path so its
    // visuals and gasoline interactions remain native. While combustion is active we normalize
    // the actual native fire tick directly:
    //   * combustion alone: 0.25
    //   * combustion + any stronger/real fire source: 0.75
    // This intentionally does NOT try to layer a second DOT. ULTRAKILL's stronger gasoline fire
    // is the 0.5 tick; combustion simply upgrades that same tick to 0.75 while active.
    internal sealed class WeakBurnStatus : MonoBehaviour
    {
        private EnemyIdentifier eid;
        private GameObject sourceWeapon;
        private float remaining;
        private bool externalFireApplied;
        private bool initializedNativeFire;

        [System.ThreadStatic]
        private static bool applyingOwnFire;

        internal static bool ApplyingOwnFire => applyingOwnFire;
        internal bool CombustionActive => remaining > 0f;
        internal float NativeFireDamage => externalFireApplied ? 0.75f : 0.25f;

        private void Awake()
        {
            eid = GetComponent<EnemyIdentifier>();
        }

        internal void AddFromHit(GameObject source, float actualDamage, string hitter, int attackId)
        {
            if (eid == null || eid.dead)
                return;

            sourceWeapon = source ?? sourceWeapon;
            float baseBonus;
            float damageScale;
            bool isNail = hitter == "nail" || hitter == "nailgun";
            bool isSaw = hitter == "sawblade" || hitter == "chainsawprojectile";
            if (isNail)
            {
                baseBonus = 0f;
                damageScale = 0.5f;
            }
            else if (isSaw)
            {
                baseBonus = 0.5f;
                damageScale = 0.35f;
            }
            else
            {
                baseBonus = 1.5f;
                damageScale = 0.35f;
            }

            // RuntimeRegistry's attack id is shared by all hits from a grouped attack. Keep the
            // fixed bonus once per attack instead of once per pellet/explosion overlap.
            if (baseBonus > 0f && !WeakBurnAttackBonusGate.TryConsume(eid, source, attackId))
                baseBonus = 0f;

            float addition = baseBonus + Mathf.Max(0f, actualDamage) * damageScale;
            remaining = Mathf.Min(5f, remaining + addition);
            EnsureNativeFire();
        }

        private void Update()
        {
            if (eid == null || eid.dead)
            {
                Destroy(this);
                return;
            }

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                // If combustion was the only reason the enemy was burning, clean up the weak
                // native fire. If real gasoline/fire was applied at any point, leave the same
                // Flammable objects alive so vanilla can finish its own duration at 0.5/tick.
                if (!externalFireApplied)
                    ExtinguishOwnedWeakFire();
                Destroy(this);
                return;
            }

            EnsureNativeFire();
            MaintainWeakFuel();
        }

        private void EnsureNativeFire()
        {
            if (eid == null || eid.dead)
                return;
            applyingOwnFire = true;
            try
            {
                if (!initializedNativeFire || eid.flammables == null || eid.flammables.Count == 0)
                {
                    // A tiny amount makes ULTRAKILL construct its normal fuel-only Flammable
                    // components. Our damage patch changes the native 0.5 tick to 0.25 until a
                    // real fire source is added, at which point the same tick becomes 0.75 while
                    // combustion remains active.
                    eid.AddFlammable(0.01f);
                    initializedNativeFire = true;
                }
                MaintainWeakFuel();
                eid.StartBurning(100f);
            }
            finally
            {
                applyingOwnFire = false;
            }
        }

        private void MaintainWeakFuel()
        {
            if (eid.flammables == null)
                return;
            // Vanilla consumes 0.175 fuel every 0.5 s. 0.35 fuel per remaining second therefore
            // makes our capped timer line up closely with the real gasoline burn machinery. Never
            // lower fuel here: a genuine gasoline application is allowed to extend beyond the
            // combustion timer and survive after this component is gone.
            float targetFuel = Mathf.Clamp(remaining * 0.35f, 0.02f, 1.75f);
            foreach (Flammable flammable in eid.flammables)
            {
                if (flammable == null || !flammable.fuelOnly)
                    continue;
                if (flammable.fuel < targetFuel)
                    flammable.fuel = targetFuel;
            }
        }

        private void ExtinguishOwnedWeakFire()
        {
            if (eid == null || externalFireApplied || eid.flammables == null)
                return;
            foreach (Flammable flammable in eid.flammables)
            {
                if (flammable != null && flammable.fuelOnly)
                {
                    flammable.fuel = 0f;
                    flammable.heat = 0f;
                    flammable.PutOut(getWet: false);
                }
            }
        }

        internal void MarkExternalFire()
        {
            externalFireApplied = true;
        }

        internal static void NotifyExternalFire(EnemyIdentifier enemy)
        {
            if (enemy == null || applyingOwnFire)
                return;
            WeakBurnStatus status = enemy.GetComponent<WeakBurnStatus>();
            if (status != null)
                status.MarkExternalFire();
        }

        private static bool HasExistingNativeFire(EnemyIdentifier enemy)
        {
            if (enemy == null)
                return false;
            if (enemy.isGasolined)
                return true;
            if (enemy.burners != null)
            {
                foreach (Flammable burner in enemy.burners)
                    if (burner != null && burner.burning)
                        return true;
            }
            return false;
        }

        internal static void Apply(EnemyIdentifier enemy, GameObject source, float actualDamage, string hitter, int attackId)
        {
            if (enemy == null || enemy.dead)
                return;

            WeakBurnStatus status = enemy.GetComponent<WeakBurnStatus>();
            if (status == null)
            {
                bool alreadyBurning = HasExistingNativeFire(enemy);
                status = enemy.gameObject.AddComponent<WeakBurnStatus>();
                if (alreadyBurning)
                    status.MarkExternalFire();
            }
            status.AddFromHit(source, actualDamage, hitter, attackId);
        }
    }

    internal static class WeakBurnAttackBonusGate
    {
        private static readonly System.Collections.Generic.Dictionary<int, int> lastAttackByEnemy =
            new System.Collections.Generic.Dictionary<int, int>();
        private static readonly System.Collections.Generic.Dictionary<long, float> lastFallbackByEnemySource =
            new System.Collections.Generic.Dictionary<long, float>();

        internal static bool TryConsume(EnemyIdentifier eid, GameObject source, int attackId)
        {
            int enemyId = eid != null ? eid.GetInstanceID() : 0;
            if (attackId != 0)
            {
                int previous;
                if (lastAttackByEnemy.TryGetValue(enemyId, out previous) && previous == attackId)
                    return false;
                lastAttackByEnemy[enemyId] = attackId;
                return true;
            }
            int sourceId = source != null ? source.GetInstanceID() : 0;
            long key = ((long)enemyId << 32) ^ (uint)sourceId;
            float previousTime;
            if (lastFallbackByEnemySource.TryGetValue(key, out previousTime) && Time.time - previousTime < 0.075f)
                return false;
            lastFallbackByEnemySource[key] = Time.time;
            return true;
        }
    }
}
