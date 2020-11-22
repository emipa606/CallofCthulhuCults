using RimWorld;
using System;
using UnityEngine;
using Verse.AI;
using Verse;

namespace CultOfCthulhu
{
    /// <summary>
    /// Originally ZombePawn from JustinC
    /// </summary>
    public class ReanimatedPawn : Pawn
    {
        public bool setZombie = false;
    
        public bool isRaiding = true;

        public bool wasColonist;

        public float notRaidingAttackRange = 15f;

        public ReanimatedPawn()
        {
            Init();
        }
        

        private void Init()
        {
            pather = new Pawn_PathFollower(this);
            stances = new Pawn_StanceTracker(this);
            health = new Pawn_HealthTracker(this);
            jobs = new Pawn_JobTracker(this);
            filth = new Pawn_FilthTracker(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref wasColonist, "wasColonist", false, false);
            //if (Scribe.mode == LoadSaveMode.LoadingVars)
            //{
            //    Cthulhu.Utility.GiveZombieSkinEffect(this);
            //}
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            health.PreApplyDamage(dinfo, out absorbed);
            if (!Destroyed && (dinfo.Def == DamageDefOf.Cut || dinfo.Def == DamageDefOf.Stab))
            {
                var num = 0f;
                var num2 = 0f;
                if (dinfo.Instigator != null && dinfo.Instigator is Pawn)
                {
                    var pawn = dinfo.Instigator as Pawn;
                    if (pawn.skills != null)
                    {
                        SkillRecord expr_9B = pawn.skills.GetSkill(SkillDefOf.Melee);
                        num = expr_9B.Level * 2;
                        num2 = expr_9B.Level / 20f * 3f;
                    }
                    if (UnityEngine.Random.Range(0f, 100f) < 20f + num)
                    {
                        dinfo.SetAmount(999);
                        dinfo.SetHitPart(health.hediffSet.GetBrain());
                        dinfo.Def.Worker.Apply(dinfo, this);
                        return;
                    }
                    dinfo.SetAmount((int)((float)dinfo.Amount * (1f + num2)));
                }
            }
        }

        public override void Tick()
        {
            try
            {
                if (DebugSettings.noAnimals && RaceProps.Animal)
                {
                    Destroy(0);
                }
                else if (!Downed)
                {
                    if (Find.TickManager.TicksGame % 250 == 0)
                    {
                        TickRare();
                    }
                    if (Spawned)
                    {
                        pather.PatherTick();
                    }
                    Drawer.DrawTrackerTick();
                    health.HealthTick();
                    records.RecordsTick();
                    if (Spawned)
                    {
                        stances.StanceTrackerTick();
                    }
                    if (Spawned)
                    {
                        verbTracker.VerbsTick();
                    }
                    if (Spawned)
                    {
                        natives.NativeVerbsTick();
                    }
                    if (equipment != null)
                    {
                        equipment.EquipmentTrackerTick();
                    }
                    if (apparel != null)
                    {
                        apparel.ApparelTrackerTick();
                    }
                    if (Spawned)
                    {
                        jobs.JobTrackerTick();
                    }
                    if (!Dead)
                    {
                        carryTracker.CarryHandsTick();
                    }
                    if (skills != null)
                    {
                        skills.SkillsTick();
                    }
                    if (inventory != null)
                    {
                        inventory.InventoryTrackerTick();
                    }
                }
                if (needs != null && needs.food != null && needs.food.CurLevel <= 0.95f)
                {
                    needs.food.CurLevel = 1f;
                }
                if (needs != null && needs.joy != null && needs.joy.CurLevel <= 0.95f)
                {
                    needs.joy.CurLevel = 1f;
                }
                if (needs != null && needs.beauty != null && needs.beauty.CurLevel <= 0.95f)
                {
                    needs.beauty.CurLevel = 1f;
                }
                if (needs != null && needs.comfort != null && needs.comfort.CurLevel <= 0.95f)
                {
                    needs.comfort.CurLevel = 1f;
                }
                if (needs != null && needs.rest != null && needs.rest.CurLevel <= 0.95f)
                {
                    needs.rest.CurLevel = 1f;
                }
                if (needs != null && needs.mood != null && needs.mood.CurLevel <= 0.45f)
                {
                    needs.mood.CurLevel = 0.5f;
                }
                if (!setZombie)
                {
                    mindState.mentalStateHandler.neverFleeIndividual = true;
                    setZombie = ReanimatedPawnUtility.Zombify(this);
                    //ZombieMod_Utility.SetZombieName(this);
                }
                if (Downed || health.Downed || health.InPainShock)
                {
                    var damageInfo = new DamageInfo(DamageDefOf.Blunt, 9999, 1f, -1f, this, null, null);
                    damageInfo.SetHitPart(health.hediffSet.GetBrain());
                    //damageInfo.SetPart(new BodyPartDamageInfo(this.health.hediffSet.GetBrain(), false, HediffDefOf.Cut));
                    TakeDamage(damageInfo);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
