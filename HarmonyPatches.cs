using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Network.NetEvents;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using xTile.Dimensions;
using SObject = StardewValley.Object;

namespace ExtractHayFromSilo
{
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction), new Type[] { typeof(string[]), typeof(Farmer), typeof(Location) })]
    public static class GameLocation_performAction_Patch
    {
        public static bool Prefix(GameLocation __instance, string[] action, Farmer who, ref bool __result)
        {
            if (action.Length >= 1 && action[0] == "BuildingSilo")
            {
                if (who.ActiveItem == null)
                {
                    int piecesOfHayToRemove = Math.Min(ItemRegistry.Create<SObject>("(O)178").maximumStackSize(), __instance.piecesOfHay.Value);
                    SObject hayStack = ItemRegistry.Create<SObject>("(O)178", piecesOfHayToRemove);
                    if (piecesOfHayToRemove > 0 && Game1.player.couldInventoryAcceptThisItem(hayStack))
                    {
                        hayStack = (SObject)who.addItemToInventory(hayStack);
                        if (hayStack == null || hayStack.Stack < piecesOfHayToRemove)
                        {
                            __instance.piecesOfHay.Value -= piecesOfHayToRemove - (hayStack?.Stack ?? 0);
                            Game1.playSound("shwip");
                            __result = true;
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Grass), nameof(Grass.TryDropItemsOnCut))]
    public static class Grass_TryDropItemsOnCut_Patch
    {
        private static readonly MethodInfo storeHayMethod = AccessTools.Method(typeof(GameLocation), nameof(GameLocation.StoreHayInAnySilo));
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> il = instructions.ToList();
            for (int i = 0; i < il.Count; i++)
            {
                if (i >= 1 && il[i-1].Calls(storeHayMethod))
                {
                    Label label = generator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Brfalse, label);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(Grass_TryDropItemsOnCut_Patch), nameof(DropHay));
                    il[i].opcode = OpCodes.Br;
                    yield return il[i];
                    yield return new CodeInstruction(OpCodes.Pop).WithLabels(label);
                }
                else
                {
                    yield return il[i];
                }
            }
        }
        public static void DropHay(int amount, Grass grass)
        {
            Game1.createItemDebris(ItemRegistry.Create("(O)178", amount), new Vector2(grass.Tile.X * 64f + 32f, grass.Tile.Y * 64f + 32f), -1, null, -1, false);
        }
    }
}