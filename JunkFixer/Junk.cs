using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JunkFixer
{
    class Junk
    {
        private Dictionary<string, TypeSig> CctorFieldList = new Dictionary<string, TypeSig>();
        private Dictionary<string, Local> RestoredList = new Dictionary<string, Local>();
        private TypeDef CctorType;

        private ModuleDefMD md;

        public Junk(ModuleDefMD Module)
        {
            this.md = Module;
            local2field();
            FixFieldsPointers();
            RemovePointerJunk();
            FixLocalsPointers();
            
            Save(md.Location.Contains(".exe") ? md.Location.Replace(".exe", " - Fixed.exe") : md.Location.Replace(".dll", " - Fixed.dll"));
        }

        private void local2field()
        {
            foreach (var FieldDef in CctorType.Fields.Where(f => f.IsStatic))
                CctorFieldList.Add(FieldDef.Name, FieldDef.FieldSig.GetFieldType());
            foreach (var TypeDef in md.Types.Where(t => t.HasMethods))
            {
                foreach (var MethodDef in TypeDef.Methods.Where(m => m.HasBody))
                {
                    IList<Instruction> IL = MethodDef.Body.Instructions;
                    for (var x = 0; x < IL.Count; x++)
                    {
                        if (IL[x].OpCode == OpCodes.Ldsfld || IL[x].OpCode == OpCodes.Ldsflda || IL[x].OpCode == OpCodes.Stsfld && IL[x].Operand is FieldDef)
                        {
                            var Name = ((FieldDef)IL[x].Operand).Name;
                            if (CctorFieldList.ContainsKey(Name))
                            {
                                TypeSig Sig = null;
                                CctorFieldList.TryGetValue(Name, out Sig);
                                var RestoredLocal = new Local(Sig, Name, 0);
                                MethodDef.Body.Variables.Add(RestoredLocal);
                                CctorType.Fields.Remove((FieldDef)IL[x].Operand);
                                if (!RestoredList.ContainsKey(Name))
                                {
                                    IL[x].OpCode = GetOpType(IL[x].OpCode.Code);
                                    IL[x].Operand = RestoredLocal;
                                    RestoredList.Add(Name, RestoredLocal);
                                    Program.Log($"Restored {Sig.FullName} At {x}", Program.LogType.Done);
                                }
                                else
                                {
                                    IL[x].OpCode = GetOpType(IL[x].OpCode.Code);
                                    IL[x].Operand = RestoredList[Name];
                                    Program.Log($"Restored {Sig.FullName} At {x}", Program.LogType.Done);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void FixFieldsPointers()
        {
            foreach (var TypeDef in md.Types.Where(t => t.HasFields))
            {
                foreach (var FieldDef in TypeDef.Fields.Where(f => f.FieldType.IsPointer))
                {
                    FieldDef.FieldType = md.ImportAsTypeSig(typeof(byte));
                    Program.Log($"[Fix] Field : {FieldDef.Name} On Type : {TypeDef.Name}", Program.LogType.Done);
                }
            }
        }

        private void FixLocalsPointers()
        {
            foreach (var TypeDef in md.Types.Where(t => t.HasMethods))
            {
                foreach (var MethodDef in TypeDef.Methods.Where(m => m.HasBody))
                {
                    foreach (var Local in MethodDef.Body.Variables.Where(l => l.Type.IsPointer && l.Type.FullName.Contains("Byte*")))
                    {
                        Local.Type = md.ImportAsTypeSig(typeof(byte));
                        Program.Log($"[Fixed] Byte Pointer On Method : {MethodDef.Name}", Program.LogType.Done);
                    }
                }
            }
        }

        private void RemovePointerJunk()
        {
            foreach (var TypeDef in md.Types.Where(t => t.HasMethods))
            {
                foreach (var MethodDef in TypeDef.Methods.Where(m => m.HasBody))
                {
                    IList<Instruction> IL = MethodDef.Body.Instructions;
                    for (int x = 0; x < IL.Count; x++)
                    {
                        if (IL[x].OpCode == OpCodes.Ldind_I4)
                        {
                            var H = (FieldDef)IL[x - 1].Operand;
                            var Deter1 = IL[x - 1].OpCode == OpCodes.Ldfld && H.DeclaringType.Layout == TypeAttributes.AutoLayout && H.DeclaringType.IsSealed && H.DeclaringType.IsBeforeFieldInit;
                            var Deter2 = IL[x + 1].OpCode.Name.StartsWith("conv");
                            if (Deter1 || Deter2)
                            {
                                IL[x].OpCode = OpCodes.Nop;
                                Program.Log($"[Removed] Pointer At {IL[x].Offset} At Method {MethodDef.Name}", Program.LogType.Done);
                            }
                        }
                    }
                }
            }
        }

        private void Save(string NewLoc)
        {
            if (md.IsILOnly)
            {
                var Options = new ModuleWriterOptions(md)
                { Logger = DummyLogger.NoThrowInstance };
                Options.MetadataOptions.Flags = MetadataFlags.PreserveAll;
                md.Write(NewLoc, Options);
            }
            else
            {
                var NativeOptions = new NativeModuleWriterOptions(md, false)
                { Logger = DummyLogger.NoThrowInstance };
                NativeOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll;
                md.NativeWrite(NewLoc, NativeOptions);
            }
        }

        static OpCode GetOpType(Code X)
        {
            switch (X)
            {
                case Code.Stsfld:
                    return OpCodes.Stloc;
                case Code.Ldsfld:
                    return OpCodes.Ldloc;
                case Code.Ldsflda:
                    return OpCodes.Ldloca;
                default:
                    return null;
            }
        }
    }
}
