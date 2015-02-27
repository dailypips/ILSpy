using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public class InfoUtil {
        public static FieldInfo Info(FieldDefinition def)
        {
            if (def == null) return null;
            if (FieldInfoDict.ContainsKey(def))
                return FieldInfoDict[def];
            return null;
        }

        public static MethodInfo Info(MethodDefinition def)
        {
            if (def == null) return null;
            if (MethodInfoDict.ContainsKey(def))
                return MethodInfoDict[def];
            return null;
        }
        public static PropertyInfo Info(PropertyDefinition def)
        {
            if (def == null) return null;
            if (PropertyInfoDict.ContainsKey(def))
                return PropertyInfoDict[def];
            return null;
        }
        public static ClassInfo Info(TypeDefinition def)
        {
            if (def == null) return null;
            if (ClassInfoDict.ContainsKey(def))
                return ClassInfoDict[def];
            return null;
        }
        public static ClassInfo Info(string name)
        {
            foreach (var d in ClassInfoDict.Keys)
            {
                if (d.Name == name)
                    return ClassInfoDict[d];
            }
            return null;
        }
        public static ModuleInfo Info(ModuleDefinition def)
        {
            if (def == null) return null;
            if (ModuleInfoDict.ContainsKey(def))
                return ModuleInfoDict[def];
            return null;
        }
        public static EventInfo Info(EventDefinition def)
        {
            if (def == null) return null;
            if (EventInfoDict.ContainsKey(def))
                return EventInfoDict[def];
            return null;
        }
        #region field reference

        static Dictionary<FieldDefinition, FieldInfo> FieldInfoDict = new Dictionary<FieldDefinition, FieldInfo>();
        static Dictionary<PropertyDefinition, PropertyInfo> PropertyInfoDict = new Dictionary<PropertyDefinition, PropertyInfo>();
        static Dictionary<MethodDefinition, MethodInfo> MethodInfoDict = new Dictionary<MethodDefinition, MethodInfo>();
        static Dictionary<EventDefinition, EventInfo> EventInfoDict = new Dictionary<EventDefinition, EventInfo>();
        static Dictionary<TypeDefinition, ClassInfo> ClassInfoDict = new Dictionary<TypeDefinition, ClassInfo>();
        public static Dictionary<ModuleDefinition, ModuleInfo> ModuleInfoDict = new Dictionary<ModuleDefinition, ModuleInfo>();

        public static void BuildModuleDict(ModuleDefinition module)
        {

            if (ModuleInfoDict.ContainsKey(module))
                return;

            var info = new ModuleInfo(module);
            ModuleInfoDict.Add(module, info);

            foreach (var t in module.Types)
            {
                ClassInfo cinfo = new ClassInfo(t);
                ClassInfoDict.Add(t, cinfo);

                foreach (var f in t.Fields)
                {
                    FieldInfo finfo = new FieldInfo(f);
                    FieldInfoDict.Add(f, finfo);
                }

                foreach (var p in t.Properties)
                {
                    PropertyInfo pinfo = new PropertyInfo(p);
                    PropertyInfoDict.Add(p, pinfo);
                }

                foreach (var m in t.Methods)
                {
                    MethodInfo minfo = new MethodInfo(m);
                    MethodInfoDict.Add(m, minfo);
                }

                foreach (var e in t.Events)
                {
                    EventInfo einfo = new EventInfo(e);
                    EventInfoDict.Add(e, einfo);
                }
            }
            info.post();
            info.inValidCache();
        }
        #endregion

        #region code 
        public static void ScanCode(ModuleDefinition module)
        {
            foreach (var t in module.Types)
            {
                foreach (var m in t.Methods)
                {
                    ScanCode(m);
                }
            }
        }
        public static PropertyDefinition FindPropertyWithMethod(MethodDefinition m)
        {
            if (m.IsGetter)
            {
                foreach (var p in m.DeclaringType.Properties)
                {
                    if (p.GetMethod == m)
                        return p;
                }
            }
            else
            {
                foreach (var p in m.DeclaringType.Properties)
                {
                    if (p.SetMethod == m)
                        return p;
                }
            }
            return null;
        }

        public static void ScanCode(MethodDefinition method)
        {
            if (!method.HasBody)
                return;
            
            foreach (Instruction instr in method.Body.Instructions)
            {
                MethodReference mr = instr.Operand as MethodReference;
                if (mr != null)
                {
                    var def = mr.Resolve();
                    if (def != null && def.Module == method.Module)
                    {
                        var info = InfoUtil.Info(def);
                        if (info != null)
                        {
                            if (method.DeclaringType != def.DeclaringType)
                                info.UsedByOther.Add(method);
                            else
                                info.UsedByThis.Add(method);
                        }
                    }
                }

                /* Find ReadBy */
                // TODO error rename in field reference 
                if (isReadReference(instr.OpCode.Code))
                {
                    FieldReference fr = instr.Operand as FieldReference;
                    if (fr != null)
                    {
                        var def = fr.Resolve();
                        if (def != null && def.Module == method.Module)
                        {
                            var info = Info(def);
                            if (info != null)
                            {
                                if (method.IsGetter || method.IsSetter)
                                {
                                    var minfo = InfoUtil.Info(method);
                                    if (minfo != null)
                                    {
                                       if (minfo.MethodBody.Count() == 1)
                                        {
                                            var p = FindPropertyWithMethod(method);
                                            info.DeclareProperty = p;
                                        }
                                    }
                                }

                                if (method.DeclaringType != def.DeclaringType)
                                    info.ReadByOther.Add(method);
                                else
                                    info.ReadByThis.Add(method);
                            }
                            else
                                Console.Write("Error");
                        }
                    }
                }
            }
            /* Find AssignBy */
            foreach (Instruction instr in method.Body.Instructions)
            {
                if (isAssignReference(instr.OpCode.Code))
                {
                    FieldReference fr = instr.Operand as FieldReference;
                    if (fr != null)
                    {
                        var def = fr.Resolve();
                        if (def != null && def.Module == method.Module)
                        {
                            var info = Info(def);
                            if (info != null)
                            {
                                if (method.IsGetter || method.IsSetter)
                                {
                                    var p = FindPropertyWithMethod(method);
                                    if (p == null)
                                    {
                                        p = FindPropertyWithMethod(method);
                                        Console.Write("Error");
                                    }
                                    else
                                        info.DeclareProperty = p;
                                }

                                if (method.DeclaringType != def.DeclaringType)
                                    info.AssignByOther.Add(method);
                                else
                                    info.ReadByThis.Add(method);
                            }
                            else
                                Console.Write("Error");
                        }
                    }
                }
            }
        }

        public static bool isReadReference(Code code)
        {
            switch (code)
            {
                case Code.Ldfld:
                case Code.Ldsfld:
                    return true;
                case Code.Stfld:
                case Code.Stsfld:
                    return false;
                case Code.Ldflda:
                case Code.Ldsflda:
                    return true; // always show address-loading
                default:
                    return false;
            }
        }
        public static bool isAssignReference(Code code)
        {
            switch (code)
            {
                case Code.Ldfld:
                case Code.Ldsfld:
                    return false;
                case Code.Stfld:
                case Code.Stsfld:
                    return true;
                case Code.Ldflda:
                case Code.Ldsflda:
                    return true; // always show address-loading
                default:
                    return false;
            }
        }
        #endregion
    }
}
