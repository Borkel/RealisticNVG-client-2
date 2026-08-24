using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BorkelRNVG.Patches
{
    public static class AmandsGraphicsAmbientPatch
    {
        private const string AmandsGraphicsTypeName = "AmandsGraphics.AmandsGraphicsClass";

        private static readonly HashSet<string> NvgAmbientMembers = new HashSet<string>
        {
            "AmbientContrast",
            "NightVisionSkyColor",
            "NightVisionEquatorColor",
            "NightVisionGroundColor",
            "LightIntensity"
        };

        private static Type _amandsGraphicsType;
        private static MethodInfo _updateAmandsGraphics;
        private static MethodInfo _transpiler;
        private static bool _patched;

        public static void TogglePatch(bool enabled)
        {
            if (!TryResolveTarget())
                return;

            if (enabled && !_patched)
            {
                Plugin.harmony.Patch(
                    _updateAmandsGraphics,
                    transpiler: new HarmonyMethod(_transpiler));
                _patched = true;
                Plugin.Logger.LogInfo("Amands Graphics NVG ambient changes disabled.");
            }
            else if (!enabled && _patched)
            {
                Plugin.harmony.Unpatch(_updateAmandsGraphics, _transpiler);
                _patched = false;
                Plugin.Logger.LogInfo("Amands Graphics NVG ambient changes enabled.");
            }

            RefreshAmandsGraphics();
        }

        private static bool TryResolveTarget()
        {
            if (_updateAmandsGraphics != null)
                return true;

            _amandsGraphicsType = AccessTools.TypeByName(AmandsGraphicsTypeName);
            if (_amandsGraphicsType == null)
                return false;

            _updateAmandsGraphics = AccessTools.Method(
                _amandsGraphicsType,
                "UpdateAmandsGraphics");
            _transpiler = AccessTools.Method(
                typeof(AmandsGraphicsAmbientPatch),
                nameof(Transpiler));

            if (_updateAmandsGraphics == null)
            {
                Plugin.Logger.LogWarning(
                    "Amands Graphics was detected, but UpdateAmandsGraphics could not be found.");
                return false;
            }

            return true;
        }

        private static void RefreshAmandsGraphics()
        {
            UnityEngine.Object instance =
                UnityEngine.Object.FindObjectOfType(_amandsGraphicsType);
            if (instance == null)
                return;

            FieldInfo graphicsMode = AccessTools.Field(
                _amandsGraphicsType,
                "GraphicsMode");
            if (graphicsMode?.GetValue(instance) is bool active && !active)
                return;

            try
            {
                _updateAmandsGraphics.Invoke(instance, null);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    $"Could not refresh Amands Graphics ambient settings: {exception.Message}");
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            bool insideNvgBlock = false;
            int blockedWrites = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldsfld &&
                    instruction.operand is FieldInfo loadedField &&
                    loadedField.DeclaringType == __originalMethod.DeclaringType &&
                    loadedField.Name == "NVG")
                {
                    insideNvgBlock = true;
                }

                if (!insideNvgBlock || !IsNvgAmbientWrite(instruction, out bool isStatic))
                {
                    yield return instruction;
                    continue;
                }

                blockedWrites++;
                CodeInstruction popValue = new CodeInstruction(OpCodes.Pop);
                popValue.labels.AddRange(instruction.labels);
                popValue.blocks.AddRange(instruction.blocks);
                yield return popValue;

                if (!isStatic)
                    yield return new CodeInstruction(OpCodes.Pop);
            }

            if (blockedWrites == 0)
            {
                Plugin.Logger.LogWarning(
                    "Amands Graphics NVG ambient writes were not found; its API may have changed.");
            }
            else
            {
                Plugin.Logger.LogInfo(
                    $"Blocked {blockedWrites} Amands Graphics NVG ambient writes.");
            }
        }

        private static bool IsNvgAmbientWrite(
            CodeInstruction instruction,
            out bool isStatic)
        {
            isStatic = false;

            if (instruction.opcode == OpCodes.Stfld &&
                instruction.operand is FieldInfo instanceField &&
                NvgAmbientMembers.Contains(instanceField.Name))
            {
                return true;
            }

            if (instruction.opcode == OpCodes.Stsfld &&
                instruction.operand is FieldInfo staticField &&
                NvgAmbientMembers.Contains(staticField.Name))
            {
                isStatic = true;
                return true;
            }

            if ((instruction.opcode == OpCodes.Call ||
                 instruction.opcode == OpCodes.Callvirt) &&
                instruction.operand is MethodInfo setter &&
                setter.Name.StartsWith("set_", StringComparison.Ordinal) &&
                NvgAmbientMembers.Contains(setter.Name.Substring(4)))
            {
                isStatic = setter.IsStatic;
                return true;
            }

            return false;
        }
    }
}
