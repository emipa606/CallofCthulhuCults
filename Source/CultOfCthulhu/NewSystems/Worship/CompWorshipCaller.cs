using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace CultOfCthulhu
{
    public class CompWorshipCaller : ThingComp
    {
        public CompProperties_WorshipCaller Props => props as CompProperties_WorshipCaller;
        public Building_SacrificialAltar Altar
        {
            get
            {
                return (Building_SacrificialAltar)GenClosest.ClosestThingReachable(parent.PositionHeld, parent.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), Verse.AI.PathEndMode.ClosestTouch,
                    TraverseMode.ByPawn, 9999, x => x is Building_SacrificialAltar, null, 0, -1, false, RegionType.Set_Passable, false);
            }
        }
        public IEnumerable<IntVec3> CellsInRange
        {
            get
            {
                return GenRadial.RadialCellsAround(parent.Position, Props.rangeRadius, true);
            }
        }


        public virtual void Use(bool forced)
        {
            Props.hitSound.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
            Building_SacrificialAltar.GetWorshipGroup(Altar, CellsInRange, forced);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            GenDraw.DrawRadiusRing(parent.Position, Props.rangeRadius);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
                yield return g;
        }
    }
}