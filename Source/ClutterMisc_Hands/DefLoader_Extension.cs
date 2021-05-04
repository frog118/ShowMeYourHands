using System.Linq;
using UnityEngine;
using Verse;

namespace WHands
{
    public class DefLoader_Extension : Def
    {
        public override void ResolveReferences()
        {
            base.ResolveReferences();
            CompLoader();
            WeaponComps();
        }


        private void CompLoader()
        {
            //var Tdef = ThingDefOf.Human;
            //if (Tdef == null)
            //{
            //    return;
            //}

            var Compie = new CompProperties {compClass = typeof(HandDrawer)};
            foreach (var thingDef in from race in DefDatabase<ThingDef>.AllDefsListForReading
                where race.race?.Humanlike == true
                select race)
            {
                thingDef.comps?.Add(Compie);
            }
        }

        private bool HandCheck()
        {
            return ModsConfig.ActiveModsInLoadOrder.Any(mod => mod.Name == "Clutter Laser Rifle");
        }

        private void LaserLoad()
        {
            if (!HandCheck())
            {
                return;
            }

            var wepzie = ThingDef.Named("LaserRifle");
            if (wepzie == null)
            {
                return;
            }

            var Compie = new WhandCompProps
            {
                compClass = typeof(WhandComp),
                MainHand = new Vector3(-0.2f, 0.3f, -0.05f),
                SecHand = new Vector3(0.25f, 0f, -0.05f)
            };
            wepzie.comps.Add(Compie);
        }

        private void WeaponComps()
        {
            var def = ThingDef.Named("ClutterHandsSettings");

            if (def is not ClutterHandsTDef clutterHandsTDef)
            {
                return;
            }

            if (clutterHandsTDef.WeaponCompLoader.Count <= 0)
            {
                return;
            }

            foreach (var weaponSets in clutterHandsTDef.WeaponCompLoader)
            {
                if (weaponSets.ThingTargets.Count <= 0)
                {
                    continue;
                }

                foreach (var weaponDefName in weaponSets.ThingTargets)
                {
                    var weapon = ThingDef.Named(weaponDefName);
                    if (weapon == null)
                    {
                        continue;
                    }

                    ClutterMain.doneWeapons.Add(weapon);

                    var Compie = new WhandCompProps {compClass = typeof(WhandComp), MainHand = weaponSets.MainHand};


                    if (weaponSets.MainHand == Vector3.zero)
                    {
                        Compie.MainHand = Vector3.zero;
                    }

                    Compie.SecHand = weaponSets.SecHand;

                    if (weaponSets.SecHand == Vector3.zero)
                    {
                        Compie.SecHand = Vector3.zero;
                    }

                    weapon.comps.Add(Compie);
                }
            }

            LaserLoad();
        }
    }
}