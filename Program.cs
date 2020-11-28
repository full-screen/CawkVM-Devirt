using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System.Diagnostics;
using System.Reflection;

namespace CawkVM_Devirt

{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "CawkVM Devirt - Made by roberth#0310";
            try
            {
                if (args.Length <= 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"(!) Error: ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Drag and Drop file like de4dot!");
                }
                else
                {
                    string path = args[0];
                    if (File.Exists(path))
                    {
                        ModuleDefMD module = ModuleDefMD.Load(path);
                        Module smodule = Assembly.LoadFrom(path).ManifestModule;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"(!) Loaded: ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"{path}");
                        Console.WriteLine();
                        ConversionBack.Initialize.Initalize("Eddy^CZ_", smodule);
                        foreach (TypeDef type in module.GetTypes())
                        {
                            if (type.IsGlobalModuleType)
                            {
                                type.Methods.Clear();
                                type.Fields.Clear();
                                type.Events.Clear();
                                type.Properties.Clear();
                                continue;
                            }

                            foreach (MethodDef method in type.Methods)
                            {
                                if (!method.HasBody) continue;
                                if (!method.Body.HasInstructions) continue;
                                IList<Instruction> instr = method.Body.Instructions;
                                final.Clear();
                                for (int i = 0; i < instr.Count;i++)
                                {
                                    if (instr[i].OpCode == OpCodes.Call && instr[i].Operand.ToString().Contains("ConvertBack::Runner"))
                                    {
                                        int position = (int)instr[i-4].GetLdcI4Value();
                                        int size = (int)instr[i-3].GetLdcI4Value(); ;
                                        int ID = (int)instr[i-2].GetLdcI4Value();
                                        Console.WriteLine("----------------\nMethod: 0x" + method.MDToken+" ("+method.Name+")");
                                        Console.WriteLine("Position: " + position);
                                        Console.WriteLine("Size: " + size);
                                        Console.WriteLine("Id: " + ID);
                                        object[] parameters = new object[method.Parameters.Count];
                                        int index = 0;
                                        foreach (var par in method.Parameters)
                                        {
                                            parameters[index++] = par.Type.Next;
                                        }
                                        Console.WriteLine("Parameters:" + parameters.Length);
                                        MethodBase methodx;
                                        methodx = GetMethod((int)((MethodDef)method).MDToken.Raw, smodule);
                                        Console.WriteLine("\n(!) Restored Instructions:\n");

                                        ConversionBack.ConvertBack.Runner(position, size, ID, parameters, methodx);
                                        method.Body.Instructions.Clear();
                                        var restored_instruction_ = ConversionBack.ConvertBack.to_return;
                                        uint uindex = 0;
                                        foreach (var new_instr in restored_instruction_)
                                        {
                                            var restored_instruction = OpCodeToOpcodes((System.Reflection.Emit.OpCode)new_instr.Item1, (object)new_instr.Item2, method, uindex++);
                                            Console.WriteLine(restored_instruction);
                                            instr.Add(restored_instruction);

                                        }
                                        foreach (var toedit in final)
                                        {
                                            var item1 = toedit.Item1;
                                            int item2 = (int)toedit.Item2;
                                            item1.Operand = instr[item2];
                                        }
                                        Console.WriteLine("----------------\n");
                                        break;
                                        
                                    }
                                }

                            }
                        }

                        ModuleWriterOptions opts = new ModuleWriterOptions(module);
                        opts.Logger = DummyLogger.NoThrowInstance;
                        opts.MetaDataOptions.Flags = MetaDataFlags.PreserveAll;
                        if (path.Contains(".exe"))
                        {
                            string new_path = path.Replace(".exe", "_Devirted.exe");
                            module.Write(new_path, opts);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write($"(!) Saved: ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"{new_path}");
                        }
                        else if (path.Contains(".dll"))
                        {
                            string new_path = path.Replace(".dll", "_Devirted.dll");
                            module.Write(new_path, opts);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write($"(!) Saved: ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"{new_path}");
                        }

                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($"(!) Error: ");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Invalid File Path!");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"(!) Error: ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{ex}");
            }
            Console.ReadKey();
        }
        private static MethodBase GetMethod(int MethodToken, Module asm)
        {
            MethodBase info = (MethodBase)asm.ResolveMethod(MethodToken);
            return info;
        }

        private static List<Tuple<Instruction, object>> final = new List<Tuple<Instruction, object>>();
        private static Instruction OpCodeToOpcodes(System.Reflection.Emit.OpCode opCode, object operand, MethodDef method, uint index)
        {
            Instruction instr = new Instruction();
            switch (opCode.Name)
            {
                case "add":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Add;
                    break;
                case "ret":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ret;
                    break;
                case "ldarga.s":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ldarga_S;
                    break;
                case "ldc.i4":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ldc_I4;
                    try
                    {
                        instr.Operand = (int)operand;
                    }
                    catch
                    {
                        instr.Operand = (uint)operand;
                    }
                    break;
                case "ldarga":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ldarg;
                    break;
                case "arglist":
                    instr.Offset = index;
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Arglist;
                    break;
                case "ldstr":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ldstr;
                    instr.Operand = operand.ToString();
                    break;
                case "pop":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Pop;
                    break;
                case "ldarg":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ldarg;
                    break;
                case "ldlen":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ldlen;
                    break;
                case "convI4":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Conv_I4;
                    break;
                case "ceq":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ceq;
                    break;
                case "ldc":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ldc_I4;
                    break;
                case "stloc":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Stloc;
                    break;
                case "ldloc":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ldloc;
                    break;
                case "brfalse":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Brfalse;
                    instr.Operand = operand;
                    final.Add(new Tuple<Instruction, object>(instr, operand));
                    break;
                case "ldnull":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ldnull;
                    break;
                case "br":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Br;
                    instr.Operand = operand;
                    final.Add(new Tuple<Instruction, object>(instr, operand));
                    break;
                case "newArr":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Newarr;
                    break;
                case "ldelemU1":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Ldelem_U1;
                    break;
                case "xor":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Xor;
                    break;
                case "convU1":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Conv_U1;
                    break;
                case "stelemI1":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Stelem_I1;
                    break;
                case "clt":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Clt;
                    break;
                case "brtrue":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Brtrue;
                    final.Add(new Tuple<Instruction, object>(instr, operand));
                    break;
                case "rem":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Rem;
                    break;
                case "nop":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Nop;
                    break;
                case "call":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Call;
                    if (operand is MethodInfo)
                    {
                        instr.Operand = method.Module.Import((MethodInfo)operand);
                    }
                    else if (operand is ConstructorInfo)
                    {
                        instr.Operand = method.Module.Import((ConstructorInfo)operand);
                    }

                    break;
                case "newObj":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Newobj;
                    break;
                case "callvirt":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Callvirt;
                    break;
                case "sub":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Sub;
                    break;
                case "conv.u8":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Conv_U8;
                    break;
                case "conv.i8":
                    instr.Offset = index;
                    instr.OpCode = OpCodes.Conv_I8;
                    break;
                default:
                    throw new Exception($"Unknown OpCode {opCode}");
            }
            return instr;
        }

    }
}
