﻿using HarmonyLib;
using System;

namespace dmaTrainABS.Patching
{
    public static class Patcher
    {
        private const string HarmonyId = "dmaMods.TrainABS";
        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) return;

            patched = true;
            var harmony = new Harmony(HarmonyId);

            var originalMethods = Harmony.GetAllPatchedMethods();
            foreach (var method in originalMethods)
            {
                if (method.DeclaringType == typeof(TrainAI))
                {
                    harmony.Unpatch(method, HarmonyPatchType.All, "me.tmpe");
                }
            }

            harmony.PatchAll(typeof(Patcher).Assembly); // you can also do manual patching here!
        }

        public static void UnpatchAll()
        {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            patched = false;
        }

        internal static void ForcePatch()
        {
            patched = false; PatchAll();
        }

    }
}
