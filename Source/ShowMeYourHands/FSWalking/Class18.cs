using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands.FSWalking
{
    internal class Class18
    {
        public static IEnumerable<CodeInstruction> DrawCarriedThing_Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            MethodInfo drawAtMethod = AccessTools.Method(typeof(Thing), nameof(Thing.DrawAt));

            int indexDrawAt = instructionList.FindIndex(x => x.opcode == OpCodes.Callvirt && x.operand == drawAtMethod);

            instructionList.RemoveAt(indexDrawAt);
            instructionList.InsertRange(indexDrawAt, new List<CodeInstruction>
            {
                // carriedThing.DrawAt(drawPos, flip);
                // IL_00c4: ldloc.0 this Thing
                // IL_00c5: ldloc.1 drawPos
                // IL_00c6: ldloc.3 flip

                new CodeInstruction(OpCodes.Ldarg_0), // this.PawnRenderer
                new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(PawnRenderer),
                        "pawn")), // pawn
                new CodeInstruction(OpCodes.Ldloc_2),           // thingBehind
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(HandDrawer),
                        nameof(HandDrawer.CheckAndDrawHands)))
            });
            return instructionList;
        }

    }
}
