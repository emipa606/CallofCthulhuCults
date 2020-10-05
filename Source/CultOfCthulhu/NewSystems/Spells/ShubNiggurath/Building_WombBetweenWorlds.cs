using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;
using System.Text;

namespace CultOfCthulhu
{

    public class Building_WombBetweenWorlds : ThingWithComps
    {
        private const int InitialPawnSpawnDelay = 960;

        private const int PawnSpawnRadius = 5;

        private const float MaxSpawnedPawnsPoints = 500f;

        private const int InitialPawnsPoints = 260;

        public bool active = true;

        public int nextPawnSpawnTick = -1;

        public List<Pawn> spawnedPawns = new List<Pawn>();

        private Lord lord;

        private int ticksToSpawnInitialPawns = -1;

        //private static readonly FloatRange PawnSpawnIntervalDays = new FloatRange(0.85f, 1.1f);
        private static readonly FloatRange PawnSpawnIntervalDays = new FloatRange(3.85f, 3.1f);

        


        private float SpawnedPawnsPoints
        {
            get
            {
                FilterOutUnspawnedPawns();
                float num = 0f;
                for (int i = 0; i < spawnedPawns.Count; i++)
                {
                    num += spawnedPawns[i].kindDef.combatPower;
                }
                return num;
            }
        }

        public override void SpawnSetup(Map map, bool bla)
        {
            base.SpawnSetup(map, bla);
            if (Faction == null)
            {
                SetFaction(Faction.OfInsects, null);
            }
        }

        public void StartInitialPawnSpawnCountdown()
        {
            ticksToSpawnInitialPawns = 960;
        }

        private void SpawnInitialPawnsNow()
        {
            ticksToSpawnInitialPawns = -1;
            while (SpawnedPawnsPoints < 260f)
            {
                if (!TrySpawnPawn(out _, Map))
                {
                    return;
                }
            }
            CalculateNextPawnSpawnTick();
        }

        public override void TickRare()
        {
            base.TickRare();
            FilterOutUnspawnedPawns();
            if (!active && !Position.Fogged(Map))
            {
                Activate();
            }
            if (active)
            {
                if (ticksToSpawnInitialPawns > 0)
                {
                    ticksToSpawnInitialPawns -= 250;
                    if (ticksToSpawnInitialPawns <= 0)
                    {
                        SpawnInitialPawnsNow();
                    }
                }
                if (Find.TickManager.TicksGame >= nextPawnSpawnTick)
                {
                    if (SpawnedPawnsPoints < MaxSpawnedPawnsPoints)
                    {
                        bool flag = TrySpawnPawn(out Pawn pawn, Map);
                        if (flag && pawn.caller != null)
                        {
                            pawn.caller.DoCall();
                        }
                    }
                    CalculateNextPawnSpawnTick();
                }
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            //List<Lord> lords = Find.LordManager.lords;
            //for (int i = 0; i < lords.Count; i++)
            //{
            //    lords[i].ReceiveMemo("HiveDestroyed");
            //}
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (dinfo.Def.ExternalViolenceFor(dinfo.IntendedTarget) && dinfo.Instigator != null)
            {
                if (ticksToSpawnInitialPawns > 0)
                {
                    SpawnInitialPawnsNow();
                }
                //Lord lord = this.Lord;
                //if (lord != null)
                //{
                //    lord.ReceiveMemo("HiveAttacked");
                //}
            }
            base.PostApplyDamage(dinfo, totalDamageDealt);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref active, "active", false, false);
            Scribe_Values.Look<int>(ref nextPawnSpawnTick, "nextPawnSpawnTick", 0, false);
            Scribe_Collections.Look<Pawn>(ref spawnedPawns, "spawnedPawns", LookMode.Reference, new object[0]);
            Scribe_Values.Look<int>(ref ticksToSpawnInitialPawns, "ticksToSpawnInitialPawns", 0, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                spawnedPawns.RemoveAll((Pawn x) => x == null);
            }
        }

        private void Activate()
        {
            active = true;
            nextPawnSpawnTick = Find.TickManager.TicksGame + Rand.Range(200, 400);
            //CompSpawnerHives comp = base.GetComp<CompSpawnerHives>();
            //if (comp != null)
            //{
            //    comp.CalculateNextHiveSpawnTick();
            //}
        }

        public override string GetInspectString()
        {
            StringBuilder s = new StringBuilder();
            s.Append(base.GetInspectString());
            string text = String.Empty;

            if (CanSpawnPawns())
            {
                text = text + "DarkYoungSpawnsIn".Translate() + ": " + (nextPawnSpawnTick - Find.TickManager.TicksGame).ToStringTicksToPeriodVague();
            }
            else
            {

            }
            s.Append(text);
            return s.ToString();
        }

        public bool CanSpawnPawns()
        {
            return SpawnedPawnsPoints < MaxSpawnedPawnsPoints;
        }

        private void CalculateNextPawnSpawnTick()
        {
            float num = GenMath.LerpDouble(0f, 5f, 1f, 0.5f, (float)spawnedPawns.Count);
            nextPawnSpawnTick = Find.TickManager.TicksGame + (int)(PawnSpawnIntervalDays.RandomInRange * 60000f / (num * Find.Storyteller.difficulty.enemyReproductionRateFactor));
            //this.nextPawnSpawnTick = Find.TickManager.TicksGame + (int)(Building_WombBetweenWorlds.PawnSpawnIntervalDays.RandomInRange * 60000f);
        }

        private void FilterOutUnspawnedPawns()
        {
            spawnedPawns.RemoveAll((Pawn x) => !x.Spawned);
        }

        private bool TrySpawnPawn(out Pawn pawn, Map map)
        {
            var kindDef = Cthulhu.Utility.IsCosmicHorrorsLoaded() ? PawnKindDef.Named("ROM_DarkYoung") : PawnKindDefOf.Megaspider;
            pawn = PawnGenerator.GeneratePawn(kindDef, Faction);
            try
            {
                IntVec3 pos = Position;
                for (int i = 0; i < 3; i++)
                {
                    pos += GenAdj.CardinalDirections[2];
                }
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(pos, map, 1), map); //
                spawnedPawns.Add(pawn);
                if (Faction != Faction.OfPlayer)
                {
                    if (lord == null)
                    {
                        lord = CreateNewLord();
                    }
                    lord.AddPawn(pawn);
                }

                Messages.Message("Cults_NewDarkYoung".Translate(), pawn, MessageTypeDefOf.PositiveEvent);
                return true;
            }
            catch
            {
                return true;
            }
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerator<Gizmo> enumerator = base.GetGizmos().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Spawn pawn",
                    icon = TexCommand.ReleaseAnimals,
                    action = delegate
                    {
                        TrySpawnPawn(out Pawn pawn, Map);
                    }
                };
            }
            yield break;
        }

        public override bool PreventPlayerSellingThingsNearby(out string reason)
        {
            if (spawnedPawns.Count > 0)
            {
                if (spawnedPawns.Any((Pawn p) => !p.Downed))
                {
                    reason = def.label;
                    return true;
                }
            }
            reason = null;
            return false;
        }

        private Lord CreateNewLord()
        {
            //Log.Message("Building_WombBetweenWorlds LordJob_DefendPoint");
            return LordMaker.MakeNewLord(Faction, new LordJob_DefendPoint(Position), null);
        }
    }
}
