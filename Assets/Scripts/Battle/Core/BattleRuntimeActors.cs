using UnityEngine;

namespace WarOfEras.Battle.Core
{
    public enum UnitRole
    {
        Combat,
        Builder
    }

    // 建筑兵任务类型与设施类型一一对应，None 表示普通行军/自动修复状态。
    public enum BuilderTaskKind
    {
        None,
        Tower,
        ResourceWell
    }

    public sealed class UnitDefinition
    {
        public UnitDefinition(
            string displayName,
            string key,
            int cost,
            float maxHealth,
            float damage,
            float speed,
            float attackRange,
            float attackInterval,
            float scale,
            int reward,
            Sprite[] moveFrames,
            Sprite[] attackFrames,
            Color tint,
            UnitRole role = UnitRole.Combat)
        {
            DisplayName = displayName;
            Key = key;
            Cost = cost;
            MaxHealth = maxHealth;
            Damage = damage;
            Speed = speed;
            AttackRange = attackRange;
            AttackInterval = attackInterval;
            Scale = scale;
            Reward = reward;
            MoveFrames = moveFrames;
            AttackFrames = attackFrames;
            Tint = tint;
            Role = role;
        }

        public string DisplayName { get; }
        public string Key { get; }
        public int Cost { get; }
        public float MaxHealth { get; }
        public float Damage { get; }
        public float Speed { get; }
        public float AttackRange { get; }
        public float AttackInterval { get; }
        public float Scale { get; }
        public int Reward { get; }
        public Sprite[] MoveFrames { get; }
        public Sprite[] AttackFrames { get; }
        public Color Tint { get; }
        public UnitRole Role { get; }
    }

    public sealed class BattleTimedDestroy : MonoBehaviour
    {
        private float remaining = 1f;

        public void Configure(float duration)
        {
            remaining = Mathf.Max(0.05f, duration);
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class BattleVfxFade : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private LineRenderer lineRenderer;
        private Vector3 initialScale;
        private Color initialSpriteColor;
        private Color initialLineStartColor;
        private Color initialLineEndColor;
        private float initialLineWidth;
        private float duration = 1f;
        private float elapsed;
        private float expandRate;
        private float rotationSpeed;

        public void Configure(float effectDuration, float scaleExpansion, float degreesPerSecond)
        {
            duration = Mathf.Max(0.05f, effectDuration);
            expandRate = scaleExpansion;
            rotationSpeed = degreesPerSecond;
            spriteRenderer = GetComponent<SpriteRenderer>();
            lineRenderer = GetComponent<LineRenderer>();
            initialScale = transform.localScale;

            if (spriteRenderer != null)
            {
                initialSpriteColor = spriteRenderer.color;
            }

            if (lineRenderer != null)
            {
                initialLineStartColor = lineRenderer.startColor;
                initialLineEndColor = lineRenderer.endColor;
                initialLineWidth = lineRenderer.widthMultiplier;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var alpha = 1f - t;

            if (expandRate != 0f)
            {
                transform.localScale = initialScale * (1f + expandRate * t);
            }

            if (rotationSpeed != 0f)
            {
                transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            }

            if (spriteRenderer != null)
            {
                var color = initialSpriteColor;
                color.a *= alpha;
                spriteRenderer.color = color;
            }

            if (lineRenderer != null)
            {
                var start = initialLineStartColor;
                var end = initialLineEndColor;
                start.a *= alpha;
                end.a *= alpha;
                lineRenderer.startColor = start;
                lineRenderer.endColor = end;
                lineRenderer.widthMultiplier = initialLineWidth * Mathf.Lerp(1f, 0.45f, t);
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class BattleBuildPromptPulse : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private Color baseColor;
        private float minScale = 0.85f;
        private float maxScale = 1.15f;
        private float pulseSpeed = 2.4f;

        public void Configure(float minimumScale, float maximumScale, float speed)
        {
            minScale = minimumScale;
            maxScale = maximumScale;
            pulseSpeed = speed;
            baseScale = transform.localScale;
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        }

        private void Update()
        {
            if (baseScale == Vector3.zero)
            {
                baseScale = transform.localScale;
            }

            var wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            transform.localScale = baseScale * Mathf.Lerp(minScale, maxScale, wave);

            if (spriteRenderer != null)
            {
                var color = baseColor;
                color.a = baseColor.a * Mathf.Lerp(0.68f, 1f, wave);
                spriteRenderer.color = color;
            }
        }
    }

    public sealed class BattleClickMarkerPulse : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private Color baseColor;
        private float duration = 0.7f;
        private float elapsed;
        private float minScale = 0.8f;
        private float maxScale = 1.2f;
        private float pulseSpeed = 12f;

        public void Configure(float effectDuration, float minimumScale, float maximumScale, float speed)
        {
            duration = Mathf.Max(0.05f, effectDuration);
            minScale = minimumScale;
            maxScale = maximumScale;
            pulseSpeed = speed;
            baseScale = transform.localScale;
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        }

        private void Update()
        {
            if (baseScale == Vector3.zero)
            {
                baseScale = transform.localScale;
            }

            elapsed += Time.deltaTime;
            var life = Mathf.Clamp01(elapsed / duration);
            var wave = (Mathf.Sin(elapsed * pulseSpeed) + 1f) * 0.5f;
            transform.localScale = baseScale * Mathf.Lerp(minScale, maxScale, wave);

            if (spriteRenderer != null)
            {
                var color = baseColor;
                color.a = baseColor.a * (1f - life) * Mathf.Lerp(0.35f, 1f, wave);
                spriteRenderer.color = color;
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class TowerDefinition
    {
        public TowerDefinition(string displayName, int cost, float damage, float attackInterval, float range, Color tint)
        {
            DisplayName = displayName;
            Cost = cost;
            Damage = damage;
            AttackInterval = attackInterval;
            Range = range;
            Tint = tint;
        }

        public string DisplayName { get; }
        public int Cost { get; }
        public float Damage { get; }
        public float AttackInterval { get; }
        public float Range { get; }
        public Color Tint { get; }
    }

    public abstract class BattleFacility : MonoBehaviour
    {
        // 防御塔和资源点共享这组接口，让单位攻击、建筑兵修复和控制器销毁通知能统一处理。
        public abstract int Team { get; }
        public abstract bool IsAlive { get; }
        public abstract float CurrentHealth { get; }
        public abstract float MaxHealth { get; }
        public bool NeedsRepair => IsAlive && CurrentHealth < MaxHealth - 0.5f;
        public virtual Vector3 CenterPosition => transform.position;
        public abstract void TakeDamage(float amount, int attackerTeam);
        public abstract float Repair(float amount);
    }

    public sealed class AgePowerDefinition
    {
        public AgePowerDefinition(string displayName, float cooldown, float damage, bool isGlobal, float statusDuration, float speedMultiplier, float attackIntervalMultiplier)
        {
            DisplayName = displayName;
            Cooldown = cooldown;
            Damage = damage;
            IsGlobal = isGlobal;
            StatusDuration = statusDuration;
            SpeedMultiplier = speedMultiplier;
            AttackIntervalMultiplier = attackIntervalMultiplier;
        }

        public string DisplayName { get; }
        public float Cooldown { get; }
        public float Damage { get; }
        public bool IsGlobal { get; }
        public float StatusDuration { get; }
        public float SpeedMultiplier { get; }
        public float AttackIntervalMultiplier { get; }
    }

    public sealed class BattleUnit : MonoBehaviour
    {
        private const float AttackImpulseDuration = 0.18f;
        private const float HitReactionDuration = 0.22f;
        private const float BuilderActionRange = 2.9f;
        private const float BuilderRepairPerSecond = 55f;
        private const float BuilderWorkEffectInterval = 0.45f;

        private BattleGameController controller;
        private Transform visualRoot;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer selectionRenderer;
        private Vector3[] routePoints;
        private Sprite holdSprite;
        private float health;
        private float damage;
        private float speed;
        private float attackTimer;
        private float frameTimer;
        private float hitFlash;
        private float attackImpulseTimer;
        private float hitReactionTimer;
        private float attackImpulseDirection;
        private float hitReactionDirection;
        private float statusTimer;
        private float statusSpeedMultiplier = 1f;
        private float statusAttackIntervalMultiplier = 1f;
        private float builderWorkEffectTimer;
        private int frameIndex;
        private int routeTargetIndex;
        private int builderTaskSlotIndex = -1;
        private int builderTaskTowerTypeIndex = -1;
        private BuilderTaskKind builderTaskKind = BuilderTaskKind.None;
        private bool attacking;
        private bool stopAtRouteEnd;
        private bool reachedHoldPoint;

        public UnitDefinition Definition { get; private set; }
        public int Team { get; private set; }
        public int LaneIndex { get; private set; }
        public bool IsAlive => health > 0f;
        public bool IsBuilder => Definition != null && Definition.Role == UnitRole.Builder;
        public bool HasAssignedBuilderTask => builderTaskKind != BuilderTaskKind.None;
        public BuilderTaskKind AssignedBuilderTaskKind => builderTaskKind;
        public int AssignedBuilderTaskSlotIndex => builderTaskSlotIndex;
        public int AssignedBuilderTaskTowerTypeIndex => builderTaskTowerTypeIndex;

        public void Configure(
            BattleGameController owner,
            UnitDefinition definition,
            int team,
            int laneIndex,
            Vector3 position,
            float healthMultiplier,
            float damageMultiplier,
            float speedMultiplier,
            Vector3[] customRoute = null,
            bool stopWhenRouteEnds = false)
        {
            controller = owner;
            Definition = definition;
            Team = team;
            LaneIndex = laneIndex;
            health = definition.MaxHealth * healthMultiplier;
            damage = definition.Damage * damageMultiplier;
            speed = definition.Speed * speedMultiplier;
            var usesCustomRoute = customRoute != null && customRoute.Length > 0;
            routePoints = usesCustomRoute ? customRoute : owner.GetLaneRoute(laneIndex);
            routeTargetIndex = team == 0 ? 1 : routePoints.Length - 2;
            stopAtRouteEnd = stopWhenRouteEnds;

            transform.position = usesCustomRoute ? routePoints[0] : position;
            transform.localScale = Vector3.one * definition.Scale * owner.UnitVisualScale;

            CreateGroundShadow();
            CreateSelectionIndicator();

            var visualObject = new GameObject("Unit Visual", typeof(SpriteRenderer));
            visualObject.transform.SetParent(transform, false);
            visualRoot = visualObject.transform;

            spriteRenderer = visualObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = definition.MoveFrames[0];
            holdSprite = spriteRenderer.sprite;
            spriteRenderer.flipX = team == 1;
            spriteRenderer.color = GetBaseTint();

            UpdateGroundShadow();
            UpdateSorting();
        }

        private void Update()
        {
            if (controller == null || Definition == null || !IsAlive || controller.IsGameOver)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            hitFlash = Mathf.Max(0f, hitFlash - Time.deltaTime);
            attackImpulseTimer = Mathf.Max(0f, attackImpulseTimer - Time.deltaTime);
            hitReactionTimer = Mathf.Max(0f, hitReactionTimer - Time.deltaTime);
            builderWorkEffectTimer = Mathf.Max(0f, builderWorkEffectTimer - Time.deltaTime);
            UpdateStatusEffect();
            attacking = false;

            if (IsBuilder && TryDoBuilderWork())
            {
                attacking = true;
            }
            else
            {
                var target = controller.FindNearestEnemy(this);
                if (target != null && Vector2.Distance(target.transform.position, transform.position) <= BattleGameController.GetUnitEngageDistance(Definition))
                {
                    attacking = true;
                    TryAttackUnit(target);
                }
                else
                {
                    var facilityTarget = controller.FindNearestEnemyFacility(this);
                    if (facilityTarget != null
                        && Vector2.Distance(facilityTarget.CenterPosition, transform.position) <= Mathf.Max(BattleGameController.GetUnitEngageDistance(Definition), 1.55f))
                    {
                        attacking = true;
                        TryAttackFacility(facilityTarget);
                    }
                    else if (reachedHoldPoint)
                    {
                        attacking = false;
                        HoldPosition();
                    }
                    else if (IsAtEnemyBase())
                    {
                        attacking = true;
                        TryAttackBase();
                    }
                    else
                    {
                        MoveForward();
                    }
                }
            }

            UpdateAnimation();
            UpdateTint();
            UpdateVisualPose();
            UpdateSorting();
        }

        public void AssignBuilderTask(BuilderTaskKind taskKind, int slotIndex, int towerTypeIndex)
        {
            if (!IsBuilder || taskKind == BuilderTaskKind.None)
            {
                return;
            }

            builderTaskKind = taskKind;
            builderTaskSlotIndex = slotIndex;
            builderTaskTowerTypeIndex = towerTypeIndex;
        }

        public void TakeDamage(float amount, int attackerTeam)
        {
            if (!IsAlive)
            {
                return;
            }

            health -= amount;
            hitFlash = 0.22f;
            hitReactionTimer = HitReactionDuration;
            hitReactionDirection = attackerTeam == 0 ? 1f : -1f;

            if (health <= 0f)
            {
                controller.NotifyUnitKilled(this, attackerTeam);
                Destroy(gameObject);
            }
        }

        public void ApplyStatusEffect(float duration, float speedMultiplier, float attackIntervalMultiplier)
        {
            if (!IsAlive || duration <= 0f)
            {
                return;
            }

            statusTimer = Mathf.Max(statusTimer, duration);
            statusSpeedMultiplier = Mathf.Min(statusSpeedMultiplier, speedMultiplier);
            statusAttackIntervalMultiplier = Mathf.Max(statusAttackIntervalMultiplier, attackIntervalMultiplier);
            hitFlash = Mathf.Max(hitFlash, 0.18f);
        }

        public void RedirectToRoute(int laneIndex, Vector3[] customRoute, bool stopWhenRouteEnds)
        {
            if (!IsAlive || customRoute == null || customRoute.Length == 0)
            {
                return;
            }

            var redirectedRoute = new Vector3[customRoute.Length + 1];
            redirectedRoute[0] = transform.position;
            for (var i = 0; i < customRoute.Length; i++)
            {
                redirectedRoute[i + 1] = customRoute[i];
            }

            LaneIndex = laneIndex;
            routePoints = redirectedRoute;
            routeTargetIndex = 1;
            stopAtRouteEnd = stopWhenRouteEnds;
            reachedHoldPoint = false;
        }

        public void SetSelectionVisible(bool visible)
        {
            if (selectionRenderer == null)
            {
                if (!visible || Team != 0)
                {
                    return;
                }

                CreateSelectionIndicator();
            }

            selectionRenderer.enabled = visible && Team == 0 && IsAlive;
            if (selectionRenderer.enabled)
            {
                UpdateSelectionIndicator();
            }
        }

        private void UpdateStatusEffect()
        {
            if (statusTimer <= 0f)
            {
                return;
            }

            statusTimer = Mathf.Max(0f, statusTimer - Time.deltaTime);
            if (statusTimer <= 0f)
            {
                statusSpeedMultiplier = 1f;
                statusAttackIntervalMultiplier = 1f;
            }
        }

        private bool TryDoBuilderWork()
        {
            // 建筑兵优先完成已派发的修建设施任务；没有待建任务时才自动修复附近友方设施。
            if (HasAssignedBuilderTask)
            {
                if (!controller.IsBuilderTaskPending(builderTaskKind, builderTaskSlotIndex, builderTaskTowerTypeIndex))
                {
                    ClearBuilderTask();
                    return false;
                }

                var targetPosition = controller.GetBuilderTaskPosition(builderTaskKind, builderTaskSlotIndex);
                if (Vector2.Distance(transform.position, targetPosition) <= BuilderActionRange)
                {
                    if (controller.TryCompleteBuilderTask(this, builderTaskKind, builderTaskSlotIndex, builderTaskTowerTypeIndex))
                    {
                        ClearBuilderTask();
                        reachedHoldPoint = true;
                        holdSprite = spriteRenderer != null ? spriteRenderer.sprite : holdSprite;
                        PlayBuilderWorkEffect(targetPosition);
                        return true;
                    }
                }
            }

            var damagedFacility = controller.FindNearestDamagedFriendlyFacility(this, BuilderActionRange);
            if (damagedFacility == null)
            {
                return false;
            }

            var repaired = damagedFacility.Repair(BuilderRepairPerSecond * Time.deltaTime);
            if (repaired <= 0f)
            {
                return false;
            }

            PlayBuilderWorkEffect(damagedFacility.CenterPosition);
            return true;
        }

        private void ClearBuilderTask()
        {
            builderTaskKind = BuilderTaskKind.None;
            builderTaskSlotIndex = -1;
            builderTaskTowerTypeIndex = -1;
        }

        private void PlayBuilderWorkEffect(Vector3 position)
        {
            if (builderWorkEffectTimer > 0f)
            {
                return;
            }

            controller.SpawnBuilderWorkEffect(position, Team);
            builderWorkEffectTimer = BuilderWorkEffectInterval;
        }

        private void TryAttackUnit(BattleUnit target)
        {
            if (attackTimer > 0f)
            {
                return;
            }

            attackImpulseTimer = AttackImpulseDuration;
            attackImpulseDirection = Team == 0 ? 1f : -1f;
            var hitPosition = Vector3.Lerp(transform.position, target.transform.position, 0.58f);
            controller.SpawnCombatHitEffect(hitPosition, Team, Definition.AttackRange > 1.3f);
            target.TakeDamage(damage, Team);
            attackTimer = Definition.AttackInterval * statusAttackIntervalMultiplier;
        }

        private void TryAttackFacility(BattleFacility target)
        {
            if (attackTimer > 0f || target == null || !target.IsAlive)
            {
                return;
            }

            attackImpulseTimer = AttackImpulseDuration;
            attackImpulseDirection = Team == 0 ? 1f : -1f;
            var hitPosition = Vector3.Lerp(transform.position, target.CenterPosition, 0.58f);
            controller.SpawnCombatHitEffect(hitPosition, Team, Definition.AttackRange > 1.3f);
            target.TakeDamage(damage, Team);
            attackTimer = Definition.AttackInterval * statusAttackIntervalMultiplier;
        }

        private void TryAttackBase()
        {
            if (attackTimer > 0f)
            {
                return;
            }

            attackImpulseTimer = AttackImpulseDuration;
            attackImpulseDirection = Team == 0 ? 1f : -1f;
            var basePosition = controller.GetBasePosition(Team == 0 ? 1 : 0);
            controller.SpawnCombatHitEffect(new Vector3(basePosition.x, transform.position.y, 0f), Team, Definition.AttackRange > 1.3f);
            controller.DamageBase(Team == 0 ? 1 : 0, damage);
            attackTimer = Definition.AttackInterval * statusAttackIntervalMultiplier;
        }

        private void MoveForward()
        {
            if (routePoints == null || routePoints.Length == 0 || routeTargetIndex < 0 || routeTargetIndex >= routePoints.Length)
            {
                var direction = Team == 0 ? 1f : -1f;
                transform.position += new Vector3(direction * speed * statusSpeedMultiplier * Time.deltaTime, 0f, 0f);
                return;
            }

            var target = routePoints[routeTargetIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * statusSpeedMultiplier * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) <= 0.025f)
            {
                routeTargetIndex += Team == 0 ? 1 : -1;
                if (stopAtRouteEnd && (routeTargetIndex >= routePoints.Length || routeTargetIndex < 0))
                {
                    reachedHoldPoint = true;
                    holdSprite = spriteRenderer != null ? spriteRenderer.sprite : holdSprite;
                }
            }
        }

        private void HoldPosition()
        {
            if (spriteRenderer != null && holdSprite != null)
            {
                spriteRenderer.sprite = holdSprite;
            }
        }

        private bool IsAtEnemyBase()
        {
            if (stopAtRouteEnd)
            {
                return false;
            }

            if (routePoints != null && routePoints.Length > 0)
            {
                return Team == 0 ? routeTargetIndex >= routePoints.Length : routeTargetIndex < 0;
            }

            var targetBase = controller.GetBasePosition(Team == 0 ? 1 : 0);
            return Team == 0
                ? transform.position.x >= targetBase.x - 0.55f
                : transform.position.x <= targetBase.x + 0.55f;
        }

        private void UpdateAnimation()
        {
            var frames = attacking && Definition.AttackFrames.Length > 0 ? Definition.AttackFrames : Definition.MoveFrames;
            if (reachedHoldPoint && !attacking)
            {
                HoldPosition();
                return;
            }

            if (frames.Length == 0)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            var frameDuration = attacking ? 0.11f : 0.16f;
            if (frameTimer < frameDuration)
            {
                return;
            }

            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
            UpdateGroundShadow();
        }

        private void UpdateTint()
        {
            var baseColor = GetBaseTint();
            spriteRenderer.color = hitFlash > 0f ? Color.Lerp(baseColor, Color.red, 0.55f) : baseColor;
        }

        private void UpdateVisualPose()
        {
            if (visualRoot == null)
            {
                return;
            }

            var parentScale = Mathf.Max(0.01f, transform.localScale.x);
            var localOffset = Vector3.zero;
            var rotation = 0f;

            if (attackImpulseTimer > 0f)
            {
                var pulse = Mathf.Sin((attackImpulseTimer / AttackImpulseDuration) * Mathf.PI);
                localOffset.x += attackImpulseDirection * pulse * 0.12f / parentScale;
                rotation += attackImpulseDirection * pulse * -3.5f;
            }

            if (hitReactionTimer > 0f)
            {
                var pulse = Mathf.Sin((hitReactionTimer / HitReactionDuration) * Mathf.PI);
                localOffset.x += hitReactionDirection * pulse * 0.08f / parentScale;
                localOffset.y += pulse * 0.03f / parentScale;
                rotation += hitReactionDirection * pulse * 4.5f;
            }

            visualRoot.localPosition = localOffset;
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private Color GetBaseTint()
        {
            var tint = Definition != null ? Definition.Tint : Color.white;
            if (IsBuilder)
            {
                tint = Color.Lerp(tint, new Color(1f, 0.84f, 0.36f, 1f), 0.3f);
            }

            if (Team == 1)
            {
                tint = Color.Lerp(tint, new Color(1f, 0.5f, 0.42f, 1f), 0.35f);
            }

            if (statusTimer > 0f)
            {
                tint = Color.Lerp(tint, new Color(0.55f, 0.9f, 1f, 1f), 0.25f);
            }

            return tint;
        }

        private void CreateBuilderToolBadge()
        {
            var parentScale = Mathf.Max(0.01f, transform.localScale.x);
            var badgeRoot = new GameObject("Builder Tool Badge").transform;
            badgeRoot.SetParent(transform, false);
            badgeRoot.localPosition = new Vector3(0.28f / parentScale, 0.48f / parentScale, 0f);
            badgeRoot.localRotation = Quaternion.Euler(0f, 0f, -34f);

            var handleObject = new GameObject("Tool Handle", typeof(SpriteRenderer));
            handleObject.transform.SetParent(badgeRoot, false);
            handleObject.transform.localScale = new Vector3(0.055f / parentScale, 0.34f / parentScale, 1f);
            var handle = handleObject.GetComponent<SpriteRenderer>();
            handle.sprite = BattleGameController.SharedWhiteSprite;
            handle.color = new Color(0.42f, 0.25f, 0.12f, 0.95f);

            var headObject = new GameObject("Tool Head", typeof(SpriteRenderer));
            headObject.transform.SetParent(badgeRoot, false);
            headObject.transform.localPosition = new Vector3(0f, 0.15f / parentScale, 0f);
            headObject.transform.localScale = new Vector3(0.22f / parentScale, 0.065f / parentScale, 1f);
            var head = headObject.GetComponent<SpriteRenderer>();
            head.sprite = BattleGameController.SharedWhiteSprite;
            head.color = new Color(1f, 0.78f, 0.28f, 0.98f);
        }

        private void CreateGroundShadow()
        {
            var shadowObject = new GameObject("Ground Shadow", typeof(SpriteRenderer));
            shadowObject.transform.SetParent(transform, false);

            shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
            shadowRenderer.sprite = BattleGameController.SharedWhiteSprite;
            shadowRenderer.color = new Color(0.03f, 0.025f, 0.018f, 0.32f);
        }

        private void CreateSelectionIndicator()
        {
            if (selectionRenderer != null || Team != 0)
            {
                return;
            }

            var selectionObject = new GameObject("Selection Ring", typeof(SpriteRenderer));
            selectionObject.transform.SetParent(transform, false);

            selectionRenderer = selectionObject.GetComponent<SpriteRenderer>();
            selectionRenderer.sprite = BattleGameController.SharedVfxCircleSprite;
            selectionRenderer.color = new Color(0.18f, 1f, 0.22f, 0.58f);
            selectionRenderer.enabled = false;
        }

        private void UpdateGroundShadow()
        {
            if (shadowRenderer == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            var parentScale = Mathf.Max(0.01f, transform.localScale.x);
            var spriteBounds = spriteRenderer.sprite.bounds;
            var worldWidth = Mathf.Clamp(spriteBounds.size.x * transform.localScale.x * 0.68f, 0.36f, 0.95f);
            shadowRenderer.transform.localScale = new Vector3(worldWidth / parentScale, 0.1f / parentScale, 1f);
            shadowRenderer.transform.localPosition = new Vector3(0f, -spriteBounds.extents.y + 0.08f / parentScale, 0f);
            UpdateSelectionIndicator();
        }

        private void UpdateSelectionIndicator()
        {
            if (selectionRenderer == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            var parentScale = Mathf.Max(0.01f, transform.localScale.x);
            var spriteBounds = spriteRenderer.sprite.bounds;
            var worldWidth = Mathf.Clamp(spriteBounds.size.x * transform.localScale.x * 0.9f, 0.48f, 1.12f);
            selectionRenderer.transform.localScale = new Vector3(worldWidth / parentScale, 0.24f / parentScale, 1f);
            selectionRenderer.transform.localPosition = new Vector3(0f, -spriteBounds.extents.y + 0.07f / parentScale, 0f);
        }

        private void UpdateSorting()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            var order = 30 + Mathf.RoundToInt((4.5f - transform.position.y) * 10f);
            spriteRenderer.sortingOrder = order + Team;
            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = order - 2;
            }

            if (selectionRenderer != null)
            {
                selectionRenderer.sortingOrder = order - 1;
            }

            if (IsBuilder)
            {
                var badgeRoot = transform.Find("Builder Tool Badge");
                if (badgeRoot != null)
                {
                    var renderers = badgeRoot.GetComponentsInChildren<SpriteRenderer>();
                    for (var i = 0; i < renderers.Length; i++)
                    {
                        renderers[i].sortingOrder = order + 4;
                    }
                }
            }
        }
    }

    public sealed class BattleTower : BattleFacility
    {
        private BattleGameController controller;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer shadowRenderer;
        private Sprite[] frames;
        private TowerDefinition definition;
        private float health;
        private float maxHealth;
        private float attackTimer;
        private float frameTimer;
        private int laneIndex;
        private int team;
        private int slotIndex;
        private int towerTypeIndex;
        private int frameIndex;

        public override int Team => team;
        public int SlotIndex => slotIndex;
        public int TowerTypeIndex => towerTypeIndex;
        public override bool IsAlive => health > 0f;
        public override float CurrentHealth => health;
        public override float MaxHealth => maxHealth;
        public override Vector3 CenterPosition => transform.position + new Vector3(0f, 0.18f, 0f);

        public void Configure(BattleGameController owner, int lane, int towerTeam, int facilitySlotIndex, int typeIndex, TowerDefinition towerDefinition, Sprite[] towerFrames)
        {
            controller = owner;
            laneIndex = lane;
            team = towerTeam;
            slotIndex = facilitySlotIndex;
            towerTypeIndex = Mathf.Max(0, typeIndex);
            definition = towerDefinition;
            frames = towerFrames;
            maxHealth = GetTowerMaxHealth(towerDefinition);
            health = maxHealth;

            transform.localScale = Vector3.one * owner.TowerVisualScale;
            CreateGroundShadow();

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
            spriteRenderer.color = GetBaseTint();
            spriteRenderer.flipX = team == 1;

            UpdateGroundShadow();
            UpdateSorting();
        }

        public override void TakeDamage(float amount, int attackerTeam)
        {
            if (!IsAlive)
            {
                return;
            }

            health -= Mathf.Max(0f, amount);
            RefreshDamageTint();

            if (health > 0f)
            {
                return;
            }

            controller.NotifyTowerDestroyed(this, attackerTeam);
            Destroy(gameObject);
        }

        public override float Repair(float amount)
        {
            // 修复保留原有建筑，不会重建已被摧毁的塔。
            if (!IsAlive || amount <= 0f || health >= maxHealth)
            {
                return 0f;
            }

            var previousHealth = health;
            health = Mathf.Min(maxHealth, health + amount);
            RefreshDamageTint();
            return health - previousHealth;
        }

        public void RefreshVisuals(TowerDefinition towerDefinition, Sprite[] towerFrames)
        {
            definition = towerDefinition;
            frames = towerFrames;
            frameIndex = 0;
            frameTimer = 0f;
            var healthRatio = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 1f;
            maxHealth = GetTowerMaxHealth(towerDefinition);
            health = Mathf.Clamp(maxHealth * healthRatio, 1f, maxHealth);

            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
            spriteRenderer.flipX = team == 1;
            RefreshDamageTint();

            UpdateGroundShadow();
            UpdateSorting();
        }

        private void Update()
        {
            if (controller == null || controller.IsGameOver || !IsAlive)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            Animate();

            if (attackTimer > 0f)
            {
                return;
            }

            var range = definition != null ? definition.Range : 3.4f;
            var target = controller.FindTowerTarget(team, laneIndex, transform.position, range);
            if (target == null)
            {
                return;
            }

            var damage = definition != null ? definition.Damage : 34f;
            var interval = definition != null ? definition.AttackInterval : 1.05f;
            var multiplier = team == 0 ? controller.TowerDamageMultiplier : 1f;
            controller.SpawnCombatHitEffect(target.transform.position, team, true);
            target.TakeDamage(damage * multiplier, team);
            attackTimer = interval;
        }

        private void Animate()
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            if (frameTimer < 0.16f)
            {
                return;
            }

            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
            UpdateGroundShadow();
        }

        private void CreateGroundShadow()
        {
            var shadowObject = new GameObject("Tower Shadow", typeof(SpriteRenderer));
            shadowObject.transform.SetParent(transform, false);

            shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
            shadowRenderer.sprite = BattleGameController.SharedWhiteSprite;
            shadowRenderer.color = new Color(0.025f, 0.02f, 0.015f, 0.28f);
        }

        private void UpdateGroundShadow()
        {
            if (shadowRenderer == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            var parentScale = Mathf.Max(0.01f, transform.localScale.x);
            var spriteBounds = spriteRenderer.sprite.bounds;
            var worldWidth = Mathf.Clamp(spriteBounds.size.x * transform.localScale.x * 0.72f, 0.65f, 1.35f);
            shadowRenderer.transform.localScale = new Vector3(worldWidth / parentScale, 0.16f / parentScale, 1f);
            shadowRenderer.transform.localPosition = new Vector3(0f, -spriteBounds.extents.y + 0.12f / parentScale, 0f);
        }

        private Color GetBaseTint()
        {
            var tint = definition != null ? definition.Tint : new Color(1f, 0.94f, 0.78f, 1f);
            return team == 1 ? Color.Lerp(tint, new Color(1f, 0.42f, 0.32f, 1f), 0.38f) : tint;
        }

        private void RefreshDamageTint()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            var healthRatio = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 1f;
            spriteRenderer.color = Color.Lerp(Color.red, GetBaseTint(), healthRatio);
        }

        private void UpdateSorting()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            var order = 24 + Mathf.RoundToInt((4.5f - transform.position.y) * 10f);
            spriteRenderer.sortingOrder = order;
            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = order - 1;
            }
        }

        private static float GetTowerMaxHealth(TowerDefinition towerDefinition)
        {
            if (towerDefinition == null)
            {
                return 260f;
            }

            return 220f + towerDefinition.Damage * 6f + towerDefinition.Range * 20f;
        }
    }

    public sealed class BattleResourceWell : BattleFacility
    {
        private const float ResourceWellMaxHealth = 360f;

        private BattleGameController controller;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer shadowRenderer;
        private float health;
        private int slotIndex;
        private int team;

        public override int Team => team;
        public int SlotIndex => slotIndex;
        public override bool IsAlive => health > 0f;
        public override float CurrentHealth => health;
        public override float MaxHealth => ResourceWellMaxHealth;
        public override Vector3 CenterPosition => transform.position + new Vector3(0f, 0.12f, 0f);

        public void Configure(BattleGameController owner, int facilitySlotIndex, int facilityTeam, Sprite sprite, float visualScale)
        {
            controller = owner;
            slotIndex = facilitySlotIndex;
            team = facilityTeam;
            health = ResourceWellMaxHealth;
            transform.localScale = Vector3.one * visualScale;

            CreateGroundShadow();
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.flipX = team == 1;
            spriteRenderer.color = GetBaseTint();
            UpdateGroundShadow();
            UpdateSorting();
        }

        public override void TakeDamage(float amount, int attackerTeam)
        {
            if (!IsAlive)
            {
                return;
            }

            health -= Mathf.Max(0f, amount);
            RefreshDamageTint();

            if (health > 0f)
            {
                return;
            }

            controller.NotifyResourceWellDestroyed(this, attackerTeam);
            Destroy(gameObject);
        }

        public override float Repair(float amount)
        {
            // 资源点被修复后会恢复颜色，但收益逻辑仍由控制器根据是否存在资源点实例计算。
            if (!IsAlive || amount <= 0f || health >= ResourceWellMaxHealth)
            {
                return 0f;
            }

            var previousHealth = health;
            health = Mathf.Min(ResourceWellMaxHealth, health + amount);
            RefreshDamageTint();
            return health - previousHealth;
        }

        private void CreateGroundShadow()
        {
            var shadowObject = new GameObject("Resource Point Shadow", typeof(SpriteRenderer));
            shadowObject.transform.SetParent(transform, false);

            shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
            shadowRenderer.sprite = BattleGameController.SharedWhiteSprite;
            shadowRenderer.color = new Color(0.025f, 0.02f, 0.015f, 0.24f);
        }

        private void UpdateGroundShadow()
        {
            if (shadowRenderer == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            var parentScale = Mathf.Max(0.01f, transform.localScale.x);
            var spriteBounds = spriteRenderer.sprite.bounds;
            var worldWidth = Mathf.Clamp(spriteBounds.size.x * transform.localScale.x * 0.7f, 0.42f, 0.95f);
            shadowRenderer.transform.localScale = new Vector3(worldWidth / parentScale, 0.12f / parentScale, 1f);
            shadowRenderer.transform.localPosition = new Vector3(0f, -spriteBounds.extents.y + 0.1f / parentScale, 0f);
        }

        private Color GetBaseTint()
        {
            return team == 0 ? Color.white : new Color(1f, 0.72f, 0.62f, 1f);
        }

        private void RefreshDamageTint()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            var healthRatio = Mathf.Clamp01(health / ResourceWellMaxHealth);
            spriteRenderer.color = Color.Lerp(Color.red, GetBaseTint(), healthRatio);
        }

        private void UpdateSorting()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            var order = 22 + Mathf.RoundToInt((4.5f - transform.position.y) * 10f);
            spriteRenderer.sortingOrder = order;
            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = order - 1;
            }
        }
    }
}
