using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;

namespace CultOfCthulhu
{
    public class PawnFlyersLeaving : Thing, IActiveDropPod, IThingHolder
    {
        public PawnFlyer pawnFlyer;

        private const int MinTicksSinceStart = -40;

        private const int MaxTicksSinceStart = -15;

        private const int TicksSinceStartToPlaySound = -10;

        private const int LeaveMapAfterTicks = 220;

        private ActiveDropPodInfo contents;

        public int groupID = -1;

        public int destinationTile = -1;

        public IntVec3 destinationCell = IntVec3.Invalid;

        public PawnsArrivalModeDef arriveMode;

        public bool attackOnArrival;

        private int ticksSinceStart;

        private bool alreadyLeft;

        private bool soundPlayed;

        private static readonly List<Thing> tmpActiveDropPods = new List<Thing>();

        private PawnFlyerDef PawnFlyerDef => pawnFlyer.def as PawnFlyerDef;

        public override Vector3 DrawPos => SkyfallerDrawPosUtility.DrawPos_Accelerate(base.DrawPos, ticksSinceStart, -33f, def.skyfaller.speed);//return DropPodAnimationUtility.DrawPosAt(this.ticksSinceStart, base.Position);

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return contents.innerContainer;
        }

        public ActiveDropPodInfo Contents
        {
            get => contents;
            set
            {
                if (contents != null)
                {
                    contents.parent = null;
                }
                if (value != null)
                {
                    value.parent = this;
                }
                contents = value;
            }
        }

        public IntVec3 GetPosition()
        {
            return PositionHeld;
        }

        public Map GetMap()
        {
            return MapHeld;
        }

        public override void PostMake()
        {
            base.PostMake();
            ticksSinceStart = Rand.RangeInclusive(-40, -15);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            //PawnFlyer
            Scribe_References.Look<PawnFlyer>(ref pawnFlyer, "pawnFlyer");

            //Vanilla
            Scribe_Values.Look<int>(ref groupID, "groupID", 0, false);
            Scribe_Values.Look<int>(ref destinationTile, "destinationTile", 0, false);
            Scribe_Values.Look<IntVec3>(ref destinationCell, "destinationCell", default, false);
            Scribe_Values.Look<PawnsArrivalModeDef>(ref arriveMode, "arriveMode", PawnsArrivalModeDefOf.EdgeDrop, false);
            Scribe_Values.Look<bool>(ref attackOnArrival, "attackOnArrival", false, false);
            Scribe_Values.Look<int>(ref ticksSinceStart, "ticksSinceStart", 0, false);
            Scribe_Deep.Look<ActiveDropPodInfo>(ref contents, "contents", new object[]
            {
                this
            });
            Scribe_Values.Look<bool>(ref alreadyLeft, "alreadyLeft", false, false);
            Scribe_Values.Look<bool>(ref soundPlayed, "soundPlayed", false, false);
        }

        public override void Tick()
        {
            if (!soundPlayed && ticksSinceStart >= -10)
            {

                if (PawnFlyerDef.takeOffSound != null)
                {
                    PawnFlyerDef.takeOffSound.PlayOneShot(new TargetInfo(Position, Map, false));
                }
                else
                {
                    Log.Warning("PawnFlyersLeaving :: Take off sound not set");
                }
                soundPlayed = true;
            }
            ticksSinceStart++;
            if (!alreadyLeft && ticksSinceStart >= 220)
            {
                GroupLeftMap();
            }
        }

        // RimWorld.Skyfaller
        private Material cachedShadowMaterial;

        // RimWorld.Skyfaller
        private Material ShadowMaterial
        {
            get
            {
                if (cachedShadowMaterial == null && !def.skyfaller.shadow.NullOrEmpty())
                {
                    cachedShadowMaterial = MaterialPool.MatFrom(def.skyfaller.shadow, ShaderDatabase.Transparent);
                }
                return cachedShadowMaterial;
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flip)
        {
            if (drawLoc.InBounds(Map))
            {
                pawnFlyer.Drawer.DrawAt(drawLoc);
                Material shadowMaterial = ShadowMaterial;
                if (!(shadowMaterial == null))
                {
                    Skyfaller.DrawDropSpotShadow(base.DrawPos, Rotation, shadowMaterial, def.skyfaller.shadowSize, ticksSinceStart);
                }
                //DropPodAnimationUtility.DrawDropSpotShadow(this, this.ticksSinceStart);
            }
        }

        private void GroupLeftMap()
        {

            if (groupID < 0)
            {
                Log.Error("Drop pod left the map, but its group ID is " + groupID);
                Destroy(DestroyMode.Vanish);
                return;
            }

            if (destinationTile < 0)
            {
                Log.Error("Drop pod left the map, but its destination tile is " + destinationTile);
                Destroy(DestroyMode.Vanish);
                return;
            }

            Lord lord = FindLord(groupID, Map);
            if (lord != null)
            {
                Map.lordManager.RemoveLord(lord);
            }

            var PawnFlyersTraveling = (PawnFlyersTraveling)WorldObjectMaker.MakeWorldObject(PawnFlyerDef.travelingDef);
            PawnFlyersTraveling.pawnFlyer = pawnFlyer;
            PawnFlyersTraveling.Tile = Map.Tile;
            PawnFlyersTraveling.destinationTile = destinationTile;
            PawnFlyersTraveling.destinationCell = destinationCell;
            PawnFlyersTraveling.arriveMode = arriveMode;
            PawnFlyersTraveling.attackOnArrival = attackOnArrival;
            Find.WorldObjects.Add(PawnFlyersTraveling);
            tmpActiveDropPods.Clear();
            tmpActiveDropPods.AddRange(Map.listerThings.ThingsInGroup(ThingRequestGroup.ActiveDropPod));

            for (var i = 0; i < tmpActiveDropPods.Count; i++)
            {
                if (tmpActiveDropPods[i] is PawnFlyersLeaving pawnFlyerLeaving && pawnFlyerLeaving.groupID == groupID)
                {
                    Cthulhu.Utility.DebugReport("Transport Already Left");
                    pawnFlyerLeaving.alreadyLeft = true;
                    PawnFlyersTraveling.AddPod(pawnFlyerLeaving.contents, true);
                    pawnFlyerLeaving.contents = null;
                    pawnFlyerLeaving.Destroy(DestroyMode.Vanish);
                }
            }

        }

        // RimWorld.TransporterUtility
        public static Lord FindLord(int transportersGroup, Map map)
        {
            List<Lord> lords = map.lordManager.lords;
            for (var i = 0; i < lords.Count; i++)
            {
                if (lords[i].LordJob is LordJob_LoadAndEnterTransportersPawn lordJob_LoadAndEnterTransporters && lordJob_LoadAndEnterTransporters.transportersGroup == transportersGroup)
                {
                    return lords[i];
                }
            }
            return null;
        }
    }
}
