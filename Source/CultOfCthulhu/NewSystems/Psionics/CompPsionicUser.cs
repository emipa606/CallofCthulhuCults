using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using AbilityUser;

namespace CultOfCthulhu
{
    public class CompPsionicUser : AbilityUser.CompAbilityUser
    {
        public bool firstTick = false;

        public override bool TryTransformPawn()
        {
            return IsPsionic;
        }

        public void PostInitializeTick()
        {
            if (AbilityUser != null)
            {
                if (AbilityUser.Spawned)
                {
                    if (AbilityUser.story != null)
                    {
                        firstTick = true;
                        Initialize();
                        AddPawnAbility(CultsDefOf.Cults_PsionicBlast);
                        AddPawnAbility(CultsDefOf.Cults_PsionicShock);
                        AddPawnAbility(CultsDefOf.Cults_PsionicBurn);
                    }
                }
            }
        }

        public override void CompTick()
        {
            if (AbilityUser != null)
            {
                if (AbilityUser.Spawned)
                {
                    if (Find.TickManager.TicksGame > 200)
                    {
                        if (IsPsionic)
                        {
                            if (!firstTick) PostInitializeTick();
                            base.CompTick();
                        }
                    }
                }
            }
        }

        public bool IsPsionic
        {
            get
            {
                if (AbilityUser != null)
                {
                    if (AbilityUser.health != null)
                    {
                        if (AbilityUser.health.hediffSet != null)
                        {
                            if (AbilityUser.health.hediffSet.HasHediff(CultsDefOf.Cults_PsionicBrain)) return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
