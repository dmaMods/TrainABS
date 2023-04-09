using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace dmaTrainABS.Patching
{

    public static class TranspilerUtil
    {
        public delegate bool Comperator(int idx);

        internal static string IL2STR(this IEnumerable<CodeInstruction> instructions)
        {
            string ret = "";
            foreach (var code in instructions)
            {
                ret += code + "\n";
            }
            return ret;
        }

        internal static Type[] GetParameterTypes<TDelegate>(bool instance = false) where TDelegate : Delegate
        {
            IEnumerable<ParameterInfo> parameters = typeof(TDelegate).GetMethod("Invoke").GetParameters();
            if (instance)
            {
                parameters = parameters.Skip(1);
            }
            return parameters.Select(p => p.ParameterType).ToArray();
        }

        internal static MethodInfo DeclaredMethod<TDelegate>(Type type, string name, bool instance = false)
            where TDelegate : Delegate
        {
            var args = GetParameterTypes<TDelegate>(instance);
            var ret = AccessTools.DeclaredMethod(type, name, args);
            return ret;
        }

        public static TDelegate CreateDelegate<TDelegate>(Type type, string name, bool instance) where TDelegate : Delegate
        {
            var types = GetParameterTypes<TDelegate>(instance);
            var ret = type.GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                types,
                new ParameterModifier[0]);
            return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), ret);
        }

        public static List<CodeInstruction> ToCodeList(IEnumerable<CodeInstruction> instructions)
        {
            var originalCodes = new List<CodeInstruction>(instructions);
            var codes = new List<CodeInstruction>(originalCodes);
            return codes;
        }

        public static bool IsSameInstruction(CodeInstruction a, CodeInstruction b, bool debug = false)
        {
            if (a.opcode == b.opcode)
            {
                if (a.operand == b.operand) { return true; }
                return (a.operand is byte aByte && b.operand is byte bByte && aByte == bByte) || (a.operand is int aInt && b.operand is int bInt && aInt == bInt);
            }
            else { return false; }
        }

        public static int SearchInstruction(List<CodeInstruction> codes, CodeInstruction instruction, int index, int dir = +1, int counter = 1)
        {
            try
            {
                return SearchGeneric(codes, idx => IsSameInstruction(codes[idx], instruction), index, dir, counter);
            }
            catch (InstructionNotFoundException)
            {
                throw new InstructionNotFoundException(" Did not found instruction: " + instruction);
            }
        }

        public static int SearchGeneric(List<CodeInstruction> codes, Comperator comperator, int index, int dir = +1, int counter = 1)
        {
            int count = 0;
            for (; 0 <= index && index < codes.Count; index += dir)
            {
                if (comperator(index))
                {
                    if (++count == counter) break;
                }
            }
            if (count != counter)
            {
                throw new InstructionNotFoundException(" Did not found instruction[s]. Comperator =  " + comperator);
            }
            return index;
        }

        public static void MoveLabels(CodeInstruction source, CodeInstruction target)
        {
            var labels = source.labels;
            target.labels.AddRange((IEnumerable<Label>)labels);
            labels.Clear();
        }

        public static void InsertInstructions(List<CodeInstruction> codes, CodeInstruction[] insertion, int index, bool moveLabels = true)
        {
            foreach (var code in insertion)
            {
                if (code == null) throw new Exception("Bad Instructions:\n" + insertion.IL2STR());
            }
            MoveLabels(codes[index], insertion[0]);
            codes.InsertRange(index, insertion);
        }

        public class InstructionNotFoundException : Exception
        {
            public InstructionNotFoundException() : base() { }
            public InstructionNotFoundException(string m) : base(m) { }
        }

    }
}
