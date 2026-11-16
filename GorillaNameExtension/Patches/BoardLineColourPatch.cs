using GorillaLibrary.Utilities;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GorillaNameExtension.Patches;

[HarmonyPatch(typeof(GorillaScoreBoard), nameof(GorillaScoreBoard.RedrawPlayerLines)), HarmonyPriority(650), HarmonyWrapSafe]
internal class BoardLineColourPatch
{
    public static string GetPlayerColor(NetPlayer player)
    {
        if (RigUtility.TryGetRig(player, out RigContainer rigContainer))
        {
            // see TextPropertyOverridePatch for how this works out
            Color colour = rigContainer.Rig.playerText1.color;
            string hex = ColorUtility.ToHtmlStringRGB(colour);
            return $"<color=#{hex.TrimStart('#')}>";
        }

        // i don't care enough to pull the original colour string
        // maybe i'm trusting this code too much. that's a thought
        return "<color=#ffffff>";
    }

    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = [.. instructions];

        for (int i = 0; i < codes.Count; i++)
        {
            CodeInstruction instruction = codes[i];

            if (instruction.opcode != OpCodes.Ldstr || instruction.operand is not string value || !Regex.IsMatch(value, @"^<color=#[0-9a-fA-F]{6}>$"))
                continue;

            CodeInstruction first = new(OpCodes.Ldarg_0);
            first.MoveLabelsFrom(instruction);
            first.MoveBlocksFrom(instruction);

            codes[i] = first;

            CodeInstruction[] newInstructions =
            [
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GorillaScoreBoard), nameof(GorillaScoreBoard.lines))),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Callvirt,  typeof(List<GorillaPlayerScoreboardLine>).GetProperty("Item").GetGetMethod()),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GorillaPlayerScoreboardLine), nameof(GorillaPlayerScoreboardLine.linePlayer))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BoardLineColourPatch), nameof(GetPlayerColor)))
            ];

            codes.InsertRange(i + 1, newInstructions);
            i += newInstructions.Length;
        }

        return codes;
    }
}
