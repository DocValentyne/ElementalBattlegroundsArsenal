using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    internal static class RuntimeVisuals
    {
        internal static GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color, float placeholderAlpha = 0.5f)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CreatePlaceholderMaterial(color, placeholderAlpha);
            return obj;
        }

        internal static LineRenderer Circle(string name, Vector3 center, float radius, Color color, int points = 64)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = center;
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = false;
            line.positionCount = Mathf.Max(12, points);
            line.widthMultiplier = 0.09f;
            line.material = CreatePlaceholderMaterial(color, 0.5f);
            color.a = 0.5f;
            line.startColor = color;
            line.endColor = color;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = (float)i / line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius));
            }
            return line;
        }


        internal static Material CreatePlaceholderMaterial(Color color, float alpha)
        {
            // ULTRAKILL's blank material uses an opaque shader. Setting its _Color alpha and
            // blend properties is not sufficient on every render path, which is why the 0.0.17
            // transparency pass still looked solid in-game. Use a shader that is actually authored
            // for alpha blending, then fall back to the game's blank material only if none exist.
            Shader shader = Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Legacy Shaders/Transparent/Diffuse")
                ?? Shader.Find("Sprites/Default")
                ?? (MonoSingleton<DefaultReferenceManager>.Instance != null &&
                    MonoSingleton<DefaultReferenceManager>.Instance.blankMaterial != null
                    ? MonoSingleton<DefaultReferenceManager>.Instance.blankMaterial.shader
                    : Shader.Find("Standard"));

            Material material = new Material(shader);
            color.a = Mathf.Clamp01(alpha);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", Texture2D.whiteTexture);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissiveColor"))
                material.SetColor("_EmissiveColor", color);
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color);

            // Fallback support for Standard-like shaders if one of the explicit transparent
            // shaders is unavailable on a particular ULTRAKILL build.
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", 5);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", 10);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            return material;
        }

        internal static void SetCircleRadius(LineRenderer line, float radius)
        {
            if (line == null) return;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = (float)i / line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius));
            }
        }

        internal static HashSet<EnemyIdentifier> EnemiesInSphere(Vector3 center, float radius)
        {
            HashSet<EnemyIdentifier> enemies = new HashSet<EnemyIdentifier>();
            Collider[] colliders = Physics.OverlapSphere(center, radius, LayerMaskDefaults.Get(LMD.Enemies), QueryTriggerInteraction.Collide);
            foreach (Collider collider in colliders)
            {
                EnemyIdentifierIdentifier eidid = collider.GetComponentInParent<EnemyIdentifierIdentifier>();
                EnemyIdentifier eid = eidid != null ? eidid.eid : collider.GetComponentInParent<EnemyIdentifier>();
                if (eid != null && !eid.dead)
                    enemies.Add(eid);
            }
            return enemies;
        }

        internal static Rigidbody EnemyBody(EnemyIdentifier eid)
        {
            if (eid == null) return null;
            Rigidbody body = eid.GetComponent<Rigidbody>();
            if (body != null) return body;
            return eid.GetComponentInChildren<Rigidbody>();
        }

        internal static GameObject SpawnEnemyOnlyExplosion(Vector3 position, float sizeMultiplier, int damage, GameObject sourceWeapon, bool super = false)
        {
            DefaultReferenceManager drm = MonoSingleton<DefaultReferenceManager>.Instance;
            if (drm == null)
                return null;
            GameObject prefab = super ? drm.superExplosion : drm.explosion;
            if (prefab == null)
                return null;
            GameObject obj = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            foreach (Explosion explosion in obj.GetComponentsInChildren<Explosion>())
            {
                explosion.sourceWeapon = sourceWeapon;
                explosion.hitterWeapon = "ebelemental";
                explosion.canHit = AffectedSubjects.EnemiesOnly;
                explosion.damage = damage;
                explosion.maxSize *= sizeMultiplier;
                explosion.speed *= sizeMultiplier;
            }
            obj.transform.localScale *= sizeMultiplier;
            return obj;
        }

        internal static LineRenderer Lightning(string name, Vector3 start, Vector3 end, Color color,
            float width = 0.1f, int segments = 8, float jitter = 0.4f)
        {
            GameObject obj = new GameObject(name);
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = Mathf.Max(2, segments + 1);
            line.widthMultiplier = width;
            line.material = CreatePlaceholderMaterial(color, color.a);
            line.startColor = color;
            Color endColor = color;
            endColor.a *= 0.45f;
            line.endColor = endColor;

            Vector3 delta = end - start;
            Vector3 direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.down;
            Vector3 side = Vector3.Cross(direction, Vector3.up);
            if (side.sqrMagnitude < 0.01f)
                side = Vector3.Cross(direction, Vector3.right);
            side.Normalize();
            Vector3 secondSide = Vector3.Cross(direction, side).normalized;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = (float)i / (line.positionCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                if (i > 0 && i < line.positionCount - 1)
                {
                    float edgeFade = Mathf.Sin(t * Mathf.PI);
                    point += (side * UnityEngine.Random.Range(-jitter, jitter) +
                              secondSide * UnityEngine.Random.Range(-jitter, jitter)) * edgeFade;
                }
                line.SetPosition(i, point);
            }
            return line;
        }
    }

    internal sealed class FireColumnRuntime : MonoBehaviour
    {
        public GameObject sourceWeapon;
        public float damageMultiplier = 1f;
        private float age;
        private float tick;
        private GameObject visual;
        private int attackId;
        private readonly HashSet<EnemyIdentifier> launched = new HashSet<EnemyIdentifier>();

        private void Start()
        {
            attackId = RuntimeRegistry.NewAttackId();
            visual = RuntimeVisuals.Primitive(PrimitiveType.Cylinder, "Fire Column Visual",
                transform.position + Vector3.up * (WeaponTuning.FireColumnHeight * 0.5f),
                new Vector3(WeaponTuning.FireColumnRadius * 2f, WeaponTuning.FireColumnHeight * 0.5f, WeaponTuning.FireColumnRadius * 2f),
                new Color(1f, 0.18f, 0.02f, 0.55f));
            RuntimeRegistry.IgniteSporeClouds(transform.position + Vector3.up * 3f, 6f, sourceWeapon);
        }

        private void Update()
        {
            age += Time.deltaTime;
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = WeaponTuning.FireColumnTick;
                using (RuntimeRegistry.BeginAttack(attackId))
                {
                    // Use the actual tall cylinder instead of a single sphere around its middle.
                    // The previous sphere could miss the enemy that was directly clicked if the
                    // floor snap placed the column base far below its body/weak point.
                    Vector3 bottom = transform.position + Vector3.up * 0.1f;
                    Vector3 top = transform.position + Vector3.up * WeaponTuning.FireColumnHeight;
                    Collider[] colliders = Physics.OverlapCapsule(bottom, top, WeaponTuning.FireColumnRadius,
                        LayerMaskDefaults.Get(LMD.Enemies), QueryTriggerInteraction.Collide);
                    HashSet<EnemyIdentifier> victims = new HashSet<EnemyIdentifier>();
                    foreach (Collider collider in colliders)
                    {
                        EnemyIdentifierIdentifier eidid = collider.GetComponentInParent<EnemyIdentifierIdentifier>();
                        EnemyIdentifier eid = eidid != null ? eidid.eid : collider.GetComponentInParent<EnemyIdentifier>();
                        if (eid != null && !eid.dead)
                            victims.Add(eid);
                    }
                    foreach (EnemyIdentifier eid in victims)
                    {
                        eid.hitter = "ebfirecolumn";
                        eid.DeliverDamage(eid.gameObject, Vector3.up * 1600f, eid.bodyTransform.position,
                            WeaponTuning.FireColumnDamage * damageMultiplier, false, 0f, sourceWeapon);
                        if (!eid.bigEnemy && launched.Add(eid))
                        {
                            Rigidbody body = RuntimeVisuals.EnemyBody(eid);
                            if (body != null)
                                body.AddForce(Vector3.up * 8f, ForceMode.VelocityChange);
                        }
                    }
                }
            }
            if (age >= WeaponTuning.FireColumnLifetime)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
        }
    }

    internal sealed class VineAnchorRuntime : MonoBehaviour
    {
        public GameObject sourceWeapon;
        public EnemyIdentifier anchorEnemy;
        private float age;
        private GameObject visual;
        private LineRenderer ring;
        private readonly HashSet<EnemyIdentifier> captured = new HashSet<EnemyIdentifier>();

        private void Start()
        {
            visual = RuntimeVisuals.Primitive(PrimitiveType.Sphere, "Vine Anchor Visual", transform.position,
                Vector3.one * 0.7f, new Color(0.1f, 0.85f, 0.15f, 0.8f));
            if (visual != null)
                visual.transform.SetParent(transform, true);
            ring = RuntimeVisuals.Circle("Vine Closing Ring", transform.position, WeaponTuning.VineStartRadius, new Color(0.15f, 1f, 0.2f, 0.9f));

            Vector3 center = anchorEnemy != null ? anchorEnemy.bodyTransform.position : transform.position;
            foreach (EnemyIdentifier eid in RuntimeVisuals.EnemiesInSphere(center, WeaponTuning.VineStartRadius))
                if (eid != anchorEnemy && !IsRopePullExcluded(eid))
                    captured.Add(eid);
        }

        private void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (anchorEnemy != null && anchorEnemy.dead)
                anchorEnemy = null;
            Vector3 center = anchorEnemy != null ? anchorEnemy.bodyTransform.position : transform.position;
            if (ring != null)
                ring.transform.position = center;

            bool pulling = age < WeaponTuning.VinePullDuration;
            float t = Mathf.Clamp01(age / WeaponTuning.VinePullDuration);
            float radius = Mathf.Lerp(WeaponTuning.VineStartRadius, WeaponTuning.VineEndRadius, Mathf.SmoothStep(0f, 1f, t));
            RuntimeVisuals.SetCircleRadius(ring, radius);

            captured.RemoveWhere(eid => eid == null || eid.dead || IsRopePullExcluded(eid));
            foreach (EnemyIdentifier eid in captured)
            {
                Rigidbody body = RuntimeVisuals.EnemyBody(eid);
                if (body == null)
                    continue;
                Vector3 delta = center - eid.bodyTransform.position;
                float distance = delta.magnitude;
                if (distance <= 0.35f)
                {
                    body.velocity = Vector3.Lerp(body.velocity, Vector3.zero, 0.7f);
                    continue;
                }

                body.WakeUp();
                Vector3 toward = delta / distance;
                if (pulling)
                {
                    float desiredSpeed = Mathf.Lerp(WeaponTuning.VineMinPullSpeed, WeaponTuning.VineMaxPullSpeed, Mathf.Clamp01(distance / WeaponTuning.VineStartRadius));

                    // A rope should win against an enemy simply walking the opposite direction.
                    // Preserve tangential motion, but remove any outward radial velocity and force
                    // at least the configured inward component every physics frame.
                    Vector3 velocity = body.velocity;
                    float inwardSpeed = Vector3.Dot(velocity, toward);
                    if (inwardSpeed < desiredSpeed)
                        velocity += toward * (desiredSpeed - inwardSpeed);
                    body.velocity = velocity;

                    // The visible rope ring is also a hard catch boundary. Once it contracts past a
                    // captured enemy, pull that enemy back to the ring rather than letting AI
                    // acceleration outrun the rope and escape the original capture set.
                    float excess = distance - radius;
                    if (excess > 0f)
                    {
                        Vector3 correction = toward * excess;
                        if (body.isKinematic)
                            body.position += correction;
                        else
                            body.MovePosition(body.position + correction);
                    }
                }
                else
                {
                    if (distance > WeaponTuning.VineEndRadius)
                    {
                        float holdSpeed = Mathf.Min(24f, Mathf.Max(WeaponTuning.VineMinPullSpeed, distance * 8f));
                        float inwardSpeed = Vector3.Dot(body.velocity, toward);
                        if (inwardSpeed < holdSpeed)
                            body.velocity += toward * (holdSpeed - inwardSpeed);
                        float excess = distance - WeaponTuning.VineEndRadius;
                        if (excess > 0f)
                        {
                            Vector3 correction = toward * excess;
                            if (body.isKinematic) body.position += correction;
                            else body.MovePosition(body.position + correction);
                        }
                    }
                    else
                    {
                        body.velocity = Vector3.Lerp(body.velocity, Vector3.zero, 0.82f);
                    }
                }
            }

            if (age >= WeaponTuning.VinePullDuration + WeaponTuning.VineHoldDuration)
                Destroy(gameObject);
        }

        private static bool IsRopePullExcluded(EnemyIdentifier enemy)
        {
            if (enemy == null || enemy.stationary || enemy.bigEnemy)
                return true;
            switch (enemy.enemyType)
            {
                // Unlike the old gravity-orb pull, the vine is a physical rope. These enemies
                // are intentionally too heavy / anchored to be dragged.
                case EnemyType.Centaur:
                case EnemyType.Cerberus:
                case EnemyType.Gutterman:
                case EnemyType.Guttertank:
                case EnemyType.HideousMass:
                case EnemyType.Leviathan:
                case EnemyType.Minotaur:
                case EnemyType.FleshPrison:
                case EnemyType.FleshPanopticon:
                case EnemyType.Gabriel:
                case EnemyType.GabrielSecond:
                case EnemyType.Minos:
                case EnemyType.MinosPrime:
                case EnemyType.Sisyphus:
                case EnemyType.SisyphusPrime:
                case EnemyType.Geryon:
                case EnemyType.Power:
                case EnemyType.Idol:
                case EnemyType.Deathcatcher:
                case EnemyType.Providence:
                    return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
            if (ring != null) Destroy(ring.gameObject);
        }
    }

    internal sealed class CycloneRuntime : MonoBehaviour
    {
        internal static readonly HashSet<CycloneRuntime> Active = new HashSet<CycloneRuntime>();
        public Vector3 velocity;
        public GameObject sourceWeapon;
        private float age;
        private float tick;
        private int attackId;
        private GameObject visual;

        private void Start()
        {
            attackId = RuntimeRegistry.NewAttackId();
            Active.Add(this);
            visual = RuntimeVisuals.Primitive(PrimitiveType.Cylinder, "Cyclone Visual", transform.position,
                new Vector3(WeaponTuning.CycloneRadius * 1.1f, 4f, WeaponTuning.CycloneRadius * 1.1f),
                new Color(0.35f, 0.95f, 0.25f, 1f), 1f);
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            if (visual != null)
            {
                visual.transform.position = transform.position;
                visual.transform.Rotate(Vector3.up, 420f * Time.deltaTime, Space.World);
            }

            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = 0.2f;
                using (RuntimeRegistry.BeginAttack(attackId))
                {
                    foreach (EnemyIdentifier eid in RuntimeVisuals.EnemiesInSphere(transform.position, WeaponTuning.CycloneRadius))
                    {
                        eid.hitter = "ebcyclone";
                        float hitDamage = WeaponTuning.CycloneDamage * (eid.enemyType == EnemyType.Filth ? 2f : 1f);
                        eid.DeliverDamage(eid.gameObject, velocity.normalized * 1500f + Vector3.up * 2200f,
                            eid.bodyTransform.position, hitDamage, false, 0f, sourceWeapon);
                        if (!eid.bigEnemy)
                        {
                            Rigidbody body = RuntimeVisuals.EnemyBody(eid);
                            if (body != null)
                                body.AddForce(velocity.normalized * 6f + Vector3.up * 10f, ForceMode.VelocityChange);
                        }
                    }
                }
                Collider[] projectiles = Physics.OverlapSphere(transform.position, 3f, 1 << 14, QueryTriggerInteraction.Collide);
                foreach (Collider collider in projectiles)
                {
                    Projectile projectile = collider.GetComponentInParent<Projectile>();
                    if (projectile != null && !projectile.playerBullet && projectile.breakable)
                        projectile.Break();
                }
            }

            if (age >= WeaponTuning.CycloneLifetime)
                Destroy(gameObject);
        }

        internal static void PushFromShot(Vector3 origin, Vector3 direction)
        {
            CycloneRuntime[] copy = new CycloneRuntime[Active.Count];
            Active.CopyTo(copy);
            foreach (CycloneRuntime cyclone in copy)
            {
                if (cyclone == null) continue;
                Vector3 to = cyclone.transform.position - origin;
                float along = Vector3.Dot(to, direction);
                if (along < 0f || along > 45f) continue;
                float offAxis = Vector3.Cross(direction.normalized, to).magnitude;
                if (offAxis > 5f) continue;
                cyclone.velocity = Vector3.Lerp(cyclone.velocity, direction.normalized * WeaponTuning.CycloneSpeed, 0.65f);
            }
        }

        private void OnDestroy()
        {
            Active.Remove(this);
            if (visual != null) Destroy(visual);
        }
    }

    internal sealed class PoisonSeedStatus : MonoBehaviour
    {
        public float remaining = 8f;
        private GameObject visual;
        private EnemyIdentifier eid;
        private bool counted;
        private static int liveCount;

        internal bool IsLive => eid != null && !eid.dead && remaining > 0f;
        internal static bool AnyLive => liveCount > 0;

        private void OnEnable()
        {
            if (!counted)
            {
                counted = true;
                liveCount++;
            }
        }

        private void Start()
        {
            eid = GetComponent<EnemyIdentifier>();
            visual = RuntimeVisuals.Primitive(PrimitiveType.Sphere, "Poison Seed Attached", transform.position,
                Vector3.one * 0.65f, new Color(0.35f, 1f, 0.2f, 0.8f));
            if (visual != null)
            {
                visual.transform.SetParent(transform, true);
                visual.transform.localPosition = Vector3.up * 0.8f;
            }
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
                Destroy(this);
        }

        private void OnDisable()
        {
            if (counted)
            {
                counted = false;
                liveCount = Mathf.Max(0, liveCount - 1);
            }
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
        }
    }

    internal sealed class PoisonOrbRuntime : MonoBehaviour
    {
        public GameObject sourceWeapon;
        public Vector3 direction;
        private GameObject visual;
        private float age;
        private Vector3 previousPosition;
        private const float Radius = 0.35f;
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[24];

        private void Start()
        {
            if (direction.sqrMagnitude < 0.001f)
                direction = MonoSingleton<CameraController>.Instance != null ? MonoSingleton<CameraController>.Instance.transform.forward : Vector3.forward;
            direction.Normalize();
            previousPosition = transform.position;
            visual = RuntimeVisuals.Primitive(PrimitiveType.Sphere, "Poison Orb Visual", transform.position,
                Vector3.one * 0.45f, new Color(0.4f, 0.95f, 0.15f, 0.85f));
        }

        private void Update()
        {
            age += Time.deltaTime;
            Vector3 next = transform.position + direction * WeaponTuning.PoisonOrbSpeed * Time.deltaTime;
            Vector3 delta = next - previousPosition;
            float distance = delta.magnitude;
            if (distance > 0.001f)
            {
                int count = Physics.SphereCastNonAlloc(previousPosition, Radius, delta.normalized, HitBuffer, distance,
                    LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment), QueryTriggerInteraction.Collide);

                float nearestDistance = float.PositiveInfinity;
                EnemyIdentifier nearestEnemy = null;
                bool found = false;

                for (int i = 0; i < count; i++)
                {
                    RaycastHit hit = HitBuffer[i];
                    if (hit.collider == null || hit.distance >= nearestDistance)
                        continue;

                    EnemyIdentifierIdentifier eidid = hit.collider.GetComponentInParent<EnemyIdentifierIdentifier>();
                    EnemyIdentifier eid = eidid != null ? eidid.eid : hit.collider.GetComponentInParent<EnemyIdentifier>();
                    bool environment = LayerMaskDefaults.IsMatchingLayer(hit.collider.gameObject.layer, LMD.Environment);
                    if ((eid == null || eid.dead) && !environment)
                        continue;

                    nearestDistance = hit.distance;
                    nearestEnemy = eid != null && !eid.dead ? eid : null;
                    found = true;
                }

                if (found)
                {
                    if (nearestEnemy != null)
                        Attach(nearestEnemy);
                    else
                        Destroy(gameObject);
                    return;
                }
            }

            transform.position = next;
            previousPosition = next;
            if (visual != null) visual.transform.position = transform.position;
            if (age >= WeaponTuning.PoisonOrbLifetime)
                Destroy(gameObject);
        }

        private void Attach(EnemyIdentifier target)
        {
            PoisonSeedStatus seed = target.GetComponent<PoisonSeedStatus>();
            if (seed == null) seed = target.gameObject.AddComponent<PoisonSeedStatus>();
            seed.remaining = WeaponTuning.PoisonSeedLifetime;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
        }
    }

    internal sealed class PoisonStatus : MonoBehaviour
    {
        private EnemyIdentifier eid;
        private GameObject sourceWeapon;
        private float remaining;
        private float potency;
        private float tick;

        private void Awake()
        {
            eid = GetComponent<EnemyIdentifier>();
        }

        internal void Add(GameObject source, float duration, float potencyAdd)
        {
            sourceWeapon = source ?? sourceWeapon;
            remaining = Mathf.Min(10f, remaining + duration);
            potency = Mathf.Clamp(potency + potencyAdd, 0.25f, Mathf.Max(0.25f, WeaponTuning.PoisonMaxPotency));
        }

        private void Update()
        {
            if (eid == null || eid.dead)
            {
                Destroy(this);
                return;
            }
            remaining -= Time.deltaTime;
            tick -= Time.deltaTime;
            // Weapon values are editable live. Re-apply the current cap before every tick so
            // lowering maximum potency immediately affects poison that is already active.
            potency = Mathf.Clamp(potency, 0.25f, Mathf.Max(0.25f, WeaponTuning.PoisonMaxPotency));
            if (tick <= 0f)
            {
                tick = 0.5f;
                eid.hitter = "ebpoison";
                eid.DeliverDamage(eid.gameObject, Vector3.zero, eid.bodyTransform.position, potency, false, 0f, sourceWeapon);
            }
            if (remaining <= 0f)
                Destroy(this);
        }

        internal static void Apply(EnemyIdentifier eid, GameObject source, float duration = 2f, float potencyAdd = 0.08f)
        {
            if (eid == null || eid.dead) return;
            PoisonStatus poison = eid.GetComponent<PoisonStatus>();
            if (poison == null) poison = eid.gameObject.AddComponent<PoisonStatus>();
            poison.Add(source, duration, potencyAdd);
        }
    }

    internal sealed class WaterDashRuntime : MonoBehaviour
    {
        public NewMovement movement;
        public Vector3 direction;
        public float minimumSpeed = 40f;
        public float maximumSpeed = 55f;
        private Vector3 dashVelocity;
        private int fixedFrames;

        private void Start()
        {
            if (movement == null || movement.rb == null)
            {
                Destroy(gameObject);
                return;
            }
            Vector3 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : movement.transform.forward;
            float preservedSpeed = movement.rb.velocity.magnitude;
            float target = Mathf.Clamp(Mathf.Max(minimumSpeed, preservedSpeed), minimumSpeed, Mathf.Max(minimumSpeed, maximumSpeed));
            dashVelocity = dir * target;

            // Launch marks the player airborne / cancels Source states that would erase a grounded
            // velocity assignment. The real desired velocity is then enforced through the next
            // couple physics ticks so a dash-jump or high-speed run cannot flatten an upward aim.
            movement.Launch(dir, 0.01f, true);
            movement.rb.velocity = dashVelocity;
            movement.transform.position += -movement.rb.GetGravityDirection().normalized * 0.06f;
            movement.windState = Mathf.Max(movement.windState, 0.18f);
        }

        private void FixedUpdate()
        {
            if (movement == null || movement.rb == null)
            {
                Destroy(gameObject);
                return;
            }
            if (fixedFrames < 2)
            {
                movement.rb.velocity = dashVelocity;
                fixedFrames++;
                return;
            }
            Destroy(gameObject);
        }
    }

    internal sealed class GeyserRuntime : MonoBehaviour
    {
        public GameObject sourceWeapon;
        public Vector3 up = Vector3.up;
        private float age;
        private GameObject visual;
        private bool launchedPlayer;
        public bool playerAlreadyLaunched;
        public bool allowPlayerLaunch = true;
        private readonly HashSet<EnemyIdentifier> launchedEnemies = new HashSet<EnemyIdentifier>();

        private void Start()
        {
            visual = RuntimeVisuals.Primitive(PrimitiveType.Cylinder, "Geyser Visual", transform.position + up * 4f,
                new Vector3(5f, 4f, 5f), new Color(0.15f, 0.65f, 1f, 0.4f));
        }

        private void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (playerAlreadyLaunched)
                launchedPlayer = true;
            if (allowPlayerLaunch && !launchedPlayer && movement != null)
            {
                Vector3 delta = movement.transform.position - transform.position;
                float vertical = Vector3.Dot(delta, up);
                Vector3 horizontal = delta - up * vertical;
                if (vertical >= -1f && vertical <= 6f && horizontal.magnitude <= 3.25f)
                {
                    Vector3 velocity = movement.rb.velocity;
                    float upward = Vector3.Dot(velocity, up);
                    movement.rb.velocity = Vector3.ProjectOnPlane(velocity, up) + up * Mathf.Max(WeaponTuning.GeyserPlayerSpeed, upward);
                    movement.transform.position += up * 0.08f;
                    RuntimeAudio.PlayBoostPadLaunch(movement);
                    launchedPlayer = true;
                }
            }

            foreach (EnemyIdentifier eid in RuntimeVisuals.EnemiesInSphere(transform.position + up * 4f, 6f))
            {
                if (eid.bigEnemy || !launchedEnemies.Add(eid)) continue;
                Rigidbody body = RuntimeVisuals.EnemyBody(eid);
                if (body != null)
                    body.AddForce(up * WeaponTuning.GeyserEnemySpeed, ForceMode.VelocityChange);
            }
            if (age >= WeaponTuning.GeyserLifetime)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
        }
    }
}
