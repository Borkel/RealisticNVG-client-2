using BSG.CameraEffects;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BorkelRNVG.Patches
{
    public class AmbientPatch
    {
        private static MethodInfo ambientField = AccessTools.PropertySetter(typeof(LevelSettings), nameof(LevelSettings.AmbientType));

        public static void TogglePatch(bool enabled)
        {
            var preCullMethod = AccessTools.Method(typeof(NightVision), nameof(NightVision.OnPreCull));
            var transpiler = AccessTools.Method(typeof(AmbientPatch), nameof(Transpiler));

            if (enabled)
            {
                Plugin.harmony.Patch(preCullMethod, transpiler: new HarmonyMethod(transpiler));
            }
            else
            {
                Plugin.harmony.Unpatch(preCullMethod, transpiler);
            }
        }

        [HarmonyPatch(typeof(NightVision), nameof(NightVision.OnPreCull))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(ambientField))
                {
                    codes.RemoveRange(i - 2, 3);
                    break;
                }
            }

            return codes.AsEnumerable();
        }
    }
}
