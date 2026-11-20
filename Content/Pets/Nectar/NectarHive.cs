using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.Nectar
{
    public class NectarHive : ModProjectile
    {
        // 复用原版蜂巢陷阱弹幕的贴图，无需新 png。
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BeeHive;

        // 爆炸参数（具体数值见第 9 节）
        public const float Gravity = 0.15f;
        private const int Lifetime = 300;
        private const int BeeMin = 3;
        private const int BeeMax = 5;
        private const int BeeDamage = 4;
        private const float BeeKnockback = 0f;

        // 防止 OnKill / OnTileCollide / OnHitNPC 重复爆炸。
        private bool exploded;

        public override void SetStaticDefaults()
        {
            // 让蜂巢吃到鞭子的召唤标记加成。
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults()
        {
            // 复用原版蜂巢陷阱的尺寸（31×31）等基础值。
            Projectile.CloneDefaults(ProjectileID.BeeHive);

            // 原版 655 是“陷阱”，会把 hostile/trap/penetrate 一起复制过来，必须逐项改掉。
            Projectile.aiStyle = 0;          // 0 = 不跑原版 AI，全部自己写
            Projectile.friendly = true;      // 只伤害敌人
            Projectile.hostile = false;      // 不伤害玩家
            Projectile.trap = false;
            Projectile.penetrate = 1;        // 命中一个目标就结束
            Projectile.tileCollide = true;   // 碰方块爆炸
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;  // 5 秒兜底（见参数表）
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void AI()
        {
            // 抛物线：每帧给 Y 加速；把 Gravity 设为 0 就变成悬浮直线飞行。
            Projectile.velocity.Y += Gravity;

            // 蜂巢是方块状贴图，做一点翻滚即可；不需要像箭一样对准速度。
            Projectile.rotation += Projectile.velocity.X * 0.02f;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Explode();
            return true; // true = 让引擎销毁弹幕；false 会反弹/停住
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Explode();
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            // 寿命到 0、被 OnHitNPC 杀死、OnTileCollide 返回 false，最终都会走到这里。
            // exploded 守卫保证只爆一次。
            Explode();
        }

        public override bool? CanCutTiles()
        {
            return false; // 不割草、不砸罐
        }

        private void Explode()
        {
            if (exploded)
            {
                return;
            }

            exploded = true;

            // 表现层：所有客户端都播放
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    i % 2 == 0 ? DustID.Honey : DustID.Bee, //用户：这里是会释放蜂蜜液体或者蜜蜂吗?
                    velocity,
                    100,
                    default,
                    1.2f);
                dust.noGravity = true;
            }

            // 生成弹幕：只在物主客户端
            if (Main.myPlayer != Projectile.owner)
            {
                return;
            }

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                return;
            }

            int count = Main.rand.Next(BeeMin, BeeMax + 1);
            int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(BeeDamage);
            float knockback = owner.GetKnockback(DamageClass.Summon).ApplyTo(BeeKnockback);

            for (int i = 0; i < count; i++)
            {
                Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Circular(12f, 12f);
                Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f) - Vector2.UnitY * 1.5f;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromAI(),
                    spawnPosition,
                    velocity,
                    ProjectileID.Bee,
                    damage,
                    knockback,
                    Projectile.owner);
            }
        }
    }
}