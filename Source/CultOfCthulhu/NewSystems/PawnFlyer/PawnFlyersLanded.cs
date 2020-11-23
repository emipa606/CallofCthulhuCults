using System;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace CultOfCthulhu
{
    public class PawnFlyersLanded : Thing, IActiveDropPod, IThingHolder
    {
        public int age;

        public PawnFlyer pawnFlyer;

        public PawnFlyerDef PawnFlyerDef
        {
            get
            {
                return pawnFlyer.def as PawnFlyerDef;
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
            if (contents != null)
            {
                outChildren.Add(contents);
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return null;
        }

        private ActiveDropPodInfo contents;

        public ActiveDropPodInfo Contents
        {
            get
            {
                return contents;
            }
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

        public override void ExposeData()
        {
            base.ExposeData();
            //Pawn
            Scribe_References.Look<PawnFlyer>(ref pawnFlyer, "pawnFlyer");

            //Vanilla
            Scribe_Values.Look<int>(ref age, "age", 0, false);
            Scribe_Deep.Look<ActiveDropPodInfo>(ref contents, "contents", new object[]
            {
                this
            });
        }

        public IntVec3 GetPosition()
        {
            return PositionHeld;
        }

        public Map GetMap()
        {
            return MapHeld;
        }

        public override void DrawAt(Vector3 drawLoc, bool flipped)
        {
            if (drawLoc.InBounds(Map))
            {
                pawnFlyer?.Drawer?.DrawAt(drawLoc);
            }
        }

        public override void Tick()
        {
            age++;
            if ((this?.contents?.openDelay ?? -1) > -1 && age > contents.openDelay)
            {
                DismountAll();
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            contents?.innerContainer?.ClearAndDestroyContents(DestroyMode.Vanish);
            Map map = Map;
            base.Destroy(mode);
            if (mode == DestroyMode.KillFinalize)
            {
                for (int i = 0; i < 1; i++)
                {
                    Thing thing = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel, null);
                    GenPlace.TryPlaceThing(thing, Position, map, ThingPlaceMode.Near, null);
                }
            }
        }

        private void DismountAll()
        {
            if (!pawnFlyer.Spawned)
            {
                if (pawnFlyer.Destroyed)
                {
                    GenSpawn.Spawn(pawnFlyer, Position, Map, Rot4.Random);

                    Cthulhu.Utility.DebugReport("Spawned Destroyed PawnFlyer: " + pawnFlyer.Label);
                }
                else
                {
                    GenPlace.TryPlaceThing(pawnFlyer, Position, Map, ThingPlaceMode.Near, out Thing pawnFlyer2, delegate (Thing placedThing, int count)
                     {
                         Cthulhu.Utility.DebugReport("Successfully Spawned: " + pawnFlyer.Label);
                     });
                }
            }

            foreach (Thing thing in contents.innerContainer.InRandomOrder())
            {
                //Log.Message("1");
                if (thing.Spawned) continue; //Avoid errors. We already spawned our pawnFlyer.
                //Log.Message("2");

                //this.contents.innerContainer.TryDrop(thing, ThingPlaceMode.Near, out thing2);

                GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near, out Thing thing2, delegate (Thing placedThing, int count)
                 {
                    //Log.Message("3");

                    if (Find.TickManager.TicksGame < 1200 && TutorSystem.TutorialMode && placedThing.def.category == ThingCategory.Item)
                     {
                         Find.TutorialState.AddStartingItem(placedThing);
                     }
                 });
                //Log.Message("4");

                if (thing2 is Pawn pawn)
                {
                    //Log.Message("5");

                    //if (!pawn.IsPrisoner)
                    //{
                    //    if (pawn.Faction != pawnFlyer.Faction)
                    //        pawn.SetFaction(pawnFlyer.Faction);
                    //}
                    if (pawn.RaceProps.Humanlike)
                    {
                        if (PawnFlyerDef.landedTale != null)
                        {
                            TaleRecorder.RecordTale(PawnFlyerDef.landedTale, new object[]
                            {
                            pawn
                            });
                        }
                    }
                    if (pawn.IsColonist && pawn.Spawned && !Map.IsPlayerHome)
                    {
                        pawn.drafter.Drafted = true;
                    }
                }
            }
            
            if (contents.leaveSlag)
            {
                for (int j = 0; j < 1; j++)
                {
                    Thing thing3 = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel, null);
                    GenPlace.TryPlaceThing(thing3, Position, Map, ThingPlaceMode.Near, null);
                }
            }
            if (PawnFlyerDef.dismountSound != null)
            {
                PawnFlyerDef.dismountSound.PlayOneShot(new TargetInfo(Position, Map, false));
            }
            else
            {
                Log.Warning("PawnFlyersLanded :: Dismount sound not set");
            }
            Destroy(DestroyMode.Vanish);
        }


    }
}
