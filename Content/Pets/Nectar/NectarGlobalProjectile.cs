using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace TutorialMod.Content.Pets.Nectar
{
    public class NectarGlobalProjectile : GlobalProjectile
    {
        // 关键：每只宠物一份 Global 实例，计时器/计数才不会互相干扰。
        public override bool InstancePerEntity => true;

        // 参数见第 9 节；先写成常量，后面统一调。
        private const int StingerCooldown = 30;
        private const int ShotsPerHive = 20;
        private const int StingerDamage = 12;
        private const float StingerKnockback = 2f;
        private const float StingerSpeed = 12f;
        private const float DetectRange = 700f;
        private const int HiveDamage = 18;
        private const float HiveKnockback = 3f;
        private const float HiveGravity = NectarHive.Gravity;
        private const int HiveFlightTime = 45;
        private const float StingerSpawnOffset = 20f;

        // 只在物主客户端使用，不需要额外网络同步。
        private int stingerTimer;
        private int stingerCount;

        public override void SetStaticDefaults()
        {
            // 让这只宠物可以被鞭子/右键标记，索敌里的 OwnerMinionAttackTargetNPC 才会生效
            ProjectileID.Sets.MinionTargettingFeature[ProjectileID.BabyHornet] = true;
        }

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type == ProjectileID.BabyHornet;
        }

        public override void PostAI(Projectile projectile)
        {
            // 1) 主人检查
            Player owner = Main.player[projectile.owner];
            // 2) 无增益/死亡直接返回
            if (!owner.active || owner.dead)
            {
                return;
            }
            if(!owner.HasBuff(BuffID.BabyHornet))
            {
                return;
            }
            // 3) 索敌（OwnerMinionAttackTargetNPC 优先，然后 Main.ActiveNPCs）
            NPC target = FindNearestTarget(projectile, DetectRange);
            // 4) 没目标：计时归零并返回
            if(target==null)
            {
                stingerTimer = 0;
                return;
            }
            // 5) 非物主客户端：直接返回（避免多人重复生成）
            if (Main.myPlayer != projectile.owner)
            {
                return;
            }
            // 6) 冷却到：发射毒刺
            if (++stingerTimer >= StingerCooldown)
            {
                FireStinger(projectile, owner, target);
                stingerTimer = 0;
                stingerCount++;
            }
            // 7) 计数到 ShotsPerHive：发射蜂巢并把计数归零
            if(stingerCount==ShotsPerHive)
            {
                FireHive(projectile, owner, target);
                stingerCount = 0;
            }
        }

        private static NPC FindNearestTarget(Projectile projectile, float range)
        {
            // 见下方“关键片段”
            NPC marked = projectile.OwnerMinionAttackTargetNPC;
            if (marked != null&&marked.CanBeChasedBy())
            {
                return marked;
            }

            NPC result = null;
            float closestDistance = range;

            foreach (NPC npc in Main.npc)
            {
                if (!npc.CanBeChasedBy())
                {
                    continue;
                }

                float distance = Vector2.Distance(npc.Center, projectile.Center);
                if (distance >= closestDistance)
                {
                    continue;
                }

                bool lineOfSight = Collision.CanHitLine(
                    projectile.position, projectile.width, projectile.height, 
                    npc.position, npc.width, npc.height);

                if(!lineOfSight && distance > 100f)
                {
                    continue;
                }

                closestDistance = distance;
                result = npc;
            }

            return result;
        }

        private static void FireStinger(Projectile pet, Player owner, NPC target)
        {
            // 见下方“关键片段”
            Vector2 direction = (target.Center - pet.Center).SafeNormalize(-Vector2.UnitY);
            Vector2 spawnPosition = pet.Center + direction * StingerSpawnOffset;

            int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(StingerDamage);
            float knockback = owner.GetKnockback(DamageClass.Summon).ApplyTo(StingerKnockback);

            Projectile.NewProjectile(
                pet.GetSource_FromAI(),
                spawnPosition,
                direction * StingerSpeed,
                ProjectileID.HornetStinger,
                damage,
                knockback,
                pet.owner);

        }

        private static void FireHive(Projectile pet, Player owner, NPC target)
        {
            // 见下方“关键片段”
            int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(HiveDamage);
            float knockback = owner.GetKnockback(DamageClass.Summon).ApplyTo(HiveKnockback);

            Vector2 start = pet.Center;
            Vector2 velocity = CalculateLob(start, target.Center, HiveGravity, HiveFlightTime);

            Projectile.NewProjectile(pet.GetSource_FromAI(), start, velocity,
                ModContent.ProjectileType<NectarHive>(), damage, knockback, pet.owner);
        }

        private static Vector2 CalculateLob(Vector2 start, Vector2 end, float gravity, int flightTime)
        {
            // 见下方“关键片段”
            Vector2 delta = end - start;
            float vx = delta.X / flightTime;
            float vy = (delta.Y - 0.5f * gravity * flightTime * flightTime) / flightTime;
            return new Vector2(vx, vy);
        }
    }
}