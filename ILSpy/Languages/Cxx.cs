using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    class Cxx
    {
        /*static void WriteNamespaceStart(string name, ITextOutput output)
        {
            string nspace = name;
            if (nspace != null)
                nspace = nspace.Replace("SmartQuant", "QuantKit");
            bool hasNamespace = nspace != null && nspace != "";
            if (hasNamespace)
            {
                output.WriteLine();
                output.WriteLine("namespace " + nspace + " {");
                output.WriteLine();
            }
        }

        static void WriteNamespaceEnd(string name, ITextOutput output)
        {
            string nspace = name;
            if (nspace != null)
                nspace = nspace.Replace("SmartQuant", "QuantKit");
            bool hasNamespace = nspace != null && nspace != "";
            if (hasNamespace)
            {
                output.WriteLine();
                output.WriteLine("} // " + nspace);
                output.WriteLine();
            }
        }*/

        static string cancialParameterName(MethodDefinition def, ParameterDefinition p)
        {
            string result = p.Name;

            return result;
        }
        static void WriteParameters(MethodDefinition def, ITextOutput output)
        {
            bool isValueType;
            bool isFirst = true;
            var info = InfoUtil.Info(def);
            bool isCopyContructor = info.isCopyConstructor;
            if (isCopyContructor)
            {
                var cinfo = info.DeclaringType;
                output.Write("const " + cinfo.PrivateName + " &"+def.Parameters[0].Name);
                return;
            }
            foreach (var p in def.Parameters)
            {
                if (isFirst)
                    isFirst = false;
                else
                    output.Write(", ");
                string ptype = Util.TypeString(p.ParameterType, out isValueType);
                if (!isValueType && !(p.HasConstant && p.Constant == null))
                    ptype = "const " + ptype + "&";
                output.Write(ptype);
                output.Write(" ");
                output.Write(p.Name);
            }
        }

        static void WriteMethodHead(MethodDefinition def, ITextOutput output, bool isDeclaration)
        {
            if (def.IsConstructor && def.IsStatic)
                return;

            var info = InfoUtil.Info(def);

            if (def.IsConstructor)
            {
                output.Write(def.DeclaringType.Name + "Private::" + def.DeclaringType.Name + "Private");
            }
            else
            {
                output.Write(info.ReturnType.TypeName);
                output.Write(" ");
                output.Write(def.DeclaringType.Name + "Private::"+info.Name);
            }
            output.Write("(");
            WriteParameters(def, output);
            output.Write(")");
        }

        /*static void BuildIncludeList(TypeDefinition def, out List<string> externalList, out List<string> moduleClassList, out List<string> moduleEnumOrInterfaceList)
        {
            List<string> elist = new List<string>();
            List<string> mlist = new List<string>();
            List<string> clist = new List<string>();
            bool hasBaseType = def.BaseType != null && def.BaseType.Name != "Object";
            if (hasBaseType)
                mlist.Add(def.BaseType.Name); // basetype must be include file
            if (def.HasInterfaces)
            {
                foreach (var i in def.Interfaces)
                {
                    if (i.Module == def.Module)
                        mlist.Add(i.Name);
                    else
                        elist.Add(i.Name);
                }
            }
            foreach (var m in def.Methods)
            {
                Util.AddInclude(def.Namespace, m.ReturnType, elist, clist, mlist);
                foreach (var p in m.Parameters)
                    Util.AddInclude(def.Namespace, p.ParameterType, elist, clist, mlist);
                //special for currencyId
                foreach (var p in m.Parameters)
                {
                    if (p.Name == "currencyId" && p.HasConstant && p.Constant != null)
                        mlist.Add("CurrencyId");
                }
            }
            foreach (var f in def.Fields)
            {
                Util.AddInclude(def.Namespace, f.FieldType, elist, clist, mlist);
            }

            externalList = elist.Distinct<string>().ToList();
            moduleClassList = clist.Distinct<string>().ToList();
            moduleEnumOrInterfaceList = mlist.Distinct<string>().ToList();
        }

        static void WriteIncludeBody(TypeDefinition def, TypeDeclaration decl, ITextOutput output)
        {
            List<string> externalList;
            List<string> moduleEnumOrInterfaceList;
            List<string> classList;

            BuildIncludeList(def, out externalList, out classList, out moduleEnumOrInterfaceList);

            foreach (var m in classList)
            {
                if (m != def.Name)
                {
                    output.WriteLine("#include <QuantKit/" + m + ".h>");
                }
            }

            var tevent = Util.GetTypeDefinition(def.Module, "Event");
            if (tevent == def || Util.isInhertFrom(def, tevent))
            {
                if (classList.Count() > 0)
                    output.WriteLine();
                output.WriteLine("#include <QuantKit/EventType.h>");
            }
        }*/

        /*static void process_getTypeId(MethodDefinition m, ITextOutput output)
        {
            var decl = Util.getMethodDeclaration(m);
            var rlist = decl.Descendants.OfType<ReturnStatement>().ToList();
            if (rlist.Count() > 0)
            {
                var pexpr = rlist[0].Descendants.OfType<PrimitiveExpression>().ToList();
                if (pexpr.Count() > 0)
                {
                    var v = pexpr[0].Value;
                    output.Indent();
                    
                    output.Unindent();
                }

            }

        }*/

        static void WriteMethod(MethodDefinition m, ITextOutput output)
        {
            if (m.IsConstructor && m.IsStatic)
                return;
            if (m.DeclaringType != null && m.DeclaringType.IsInterface)
                return;

            var info = InfoUtil.Info(m);

            WriteMethodHead(m, output, true);
            if (m.IsGetter)
                output.Write(" const");
            output.WriteLine();
            output.WriteLine("{");
            // special process getTypeId

            if (info.Name == "getTypeId")
            {
                output.Indent();
                if (m.Name == "get_TypeId")
                    output.WriteLine(" return EventType::" + m.DeclaringType.Name +";");
                output.Unindent();
            }
            else
            {
                //output.Indent();
                foreach (var s in info.MethodBody)
                    output.WriteLine(s);
                //output.Unindent();
            }
            /*if (m.ReturnType.MetadataType != MetadataType.Void)
            {
                output.Indent();
                string rvalue;
                if (m.Name == "get_TypeId")
                    rvalue = "EventType::" + m.DeclaringType.Name;
                else
                    rvalue = Util.GetDefaultValue(m.ReturnType);
                output.WriteLine("return " + rvalue + ";");
                output.Unindent();
            }*/
            output.WriteLine("}");
        }

        static bool isSpecial_DataObject(MethodDefinition m)
        {
            var info = InfoUtil.Info(m);
            if (info.DeclaringType.Name == "DataObject" && info.Parameters.Count() == 1 && info.Parameters[0].Name == "dateTime")
                return true;
            return false;
        }

        static void WriteCtorBody(MethodDefinition m , ITextOutput output)
        {
            if (isSpecial_DataObject(m))
            {
                return;
            }
            var info = InfoUtil.Info(m);
            output.Indent();
            foreach (var s in info.MethodBody)
                output.WriteLine(s);
            output.Unindent();
        }
        static void WriteCtorInitBody(MethodDefinition m , ITextOutput output)
        {
            var info = InfoUtil.Info(m);
            if (isSpecial_DataObject(m))
            {
                output.Indent();
                output.WriteLine(": EventPrivate(dataTime)");
                output.Unindent();
                return;
            }
            var clist = info.CtorInitBody;
            if (clist.Count() > 0)
            {
                output.Indent();
                foreach (var s in clist)
                    output.WriteLine(s);
                output.Unindent();
            }
        }
        static void WriteCtor(MethodDefinition m, ITextOutput output)
        {
            if (m.IsConstructor && m.IsStatic)
                return;
            if (m.DeclaringType != null && m.DeclaringType.IsInterface)
                return;

            var info = InfoUtil.Info(m);
            if (info.isCopyConstructor)
                return;
            WriteMethodHead(m, output, true);
            output.WriteLine();
            WriteCtorInitBody(m, output);
            output.WriteLine("{");
            // special process getTypeId

            if (info.Name == "getTypeId")
            {
                output.Indent();
                string rvalue;
                if (m.Name == "get_TypeId")
                    rvalue = "EventType::" + m.DeclaringType.Name;
                output.Unindent();
            }
            else
            {
                WriteCtorBody(m, output);
            }
            output.WriteLine("}");
        }
        static void WriteFieldAsMethod(FieldDefinition field, ITextOutput output, bool isGetter)
        {
            var name = field.Name;
            if (field.Name.StartsWith("m_"))
                name = field.Name.Remove(0, 2);

            if (isGetter)
            {
                bool isValueType;
                string type = Util.TypeString(field.FieldType, out isValueType);
                output.Write(type);
                output.Write(" ");
                output.Write("get");
                output.Write(Util.upperFirstChar(name));
                output.WriteLine("() const");
            }
            else
            {
                bool isValueType;
                string type = Util.TypeString(field.FieldType, out isValueType);
                output.Write("void set");
                output.Write(Util.upperFirstChar(name));
                output.Write("(");
                output.Write(type);
                output.WriteLine(" value)");
            }
        }
        static void WriteAddCppMethod(TypeDefinition def, ITextOutput output)
        {
            if (def.IsInterface)
                return;
            var info = InfoUtil.Info(def);
            output.WriteLine(def.Name + "Private::~" + def.Name + "Private ()");
            output.WriteLine("{");
            output.WriteLine("}");
            output.WriteLine();
            if (def.Fields.Count() > 0)
            {
                output.WriteLine("bool " + def.Name + "Private::operator==(const " + def.Name + " &other) const");
                output.WriteLine("{");
                output.Indent();
                if (info.IsBaseClassInModule)
                    output.Write("return ");
                else
                    output.WriteLine("return base::operator==(other) &&");
                    
                for (int i = 0; i < def.Fields.Count(); ++i )
                {
                    var f = def.Fields[i];
                     output.Write( f.Name + " == other." + f.Name);
                     if (i == def.Fields.Count() - 1)
                         output.WriteLine(";");
                     else
                         output.WriteLine();
                    if (i < def.Fields.Count() - 1)
                        output.Write("&& ");
                    
                }
                output.Unindent();
                output.WriteLine("}");
            }
        }

        static bool hasChildClass(TypeDefinition def)
        {
            foreach (var t in def.Module.Types)
            {
                if (t == def)
                    continue;
                if (Util.isInhertFrom(t, def))
                    return true;
            }
            return false;
        }

        static void ConvertToSection(TypeDefinition def, List<MethodDefinition> ctorSections, List<MethodDefinition> pulicSections, List<MethodDefinition> protectedSection, List<MethodDefinition> privateSection)
        {
            List<MethodDefinition> ctors = new List<MethodDefinition>();
            List<MethodDefinition> props = new List<MethodDefinition>();
            List<MethodDefinition> others = new List<MethodDefinition>();
            foreach (var m in def.Methods)
            {
                if (m.IsConstructor)
                    ctors.Add(m);
                else if (m.IsGetter || m.IsSetter)
                    props.Add(m);
                else others.Add(m);
            }

            foreach (var m in ctors.OrderBy(x => x.Parameters.Count()).ToList())
            {
                ctorSections.Add(m);
            }

            foreach (var m in props)
            {
                var modifiers = Util.ConvertModifiers(m);
                if (modifiers.HasFlag(Modifiers.Public))
                    pulicSections.Add(m);
                else
                {
                    if (modifiers.HasFlag(Modifiers.Internal) || modifiers.HasFlag(Modifiers.Protected))
                        protectedSection.Add(m);
                    else
                        privateSection.Add(m);
                }
            }

            foreach (var m in others.OrderBy(x => x.Name).ToList())
            {
                var modifiers = Util.ConvertModifiers(m);
                if (modifiers.HasFlag(Modifiers.Public))
                    pulicSections.Add(m);
                else
                {
                    if (modifiers.HasFlag(Modifiers.Internal) || modifiers.HasFlag(Modifiers.Protected))
                        protectedSection.Add(m);
                    else
                        privateSection.Add(m);
                }
            }
        }

        static void FindFieldAccess(TypeDefinition def, List<FieldDefinition> GetAndSet, List<FieldDefinition> onlyGet, List<FieldDefinition> onlySet)
        {
            List<FieldDefinition> gets = new List<FieldDefinition>();
            List<FieldDefinition> sets = new List<FieldDefinition>();

            foreach (var f in def.Fields)
            {
                var info = InfoUtil.Info(f);
                if (info != null)
                {
                    if (info.ReadByOther.Count() > 0 || info.AssignByOther.Count() > 0)
                        if (info.DeclareProperty == null)
                            gets.Add(f);

                    if (info.AssignByOther.Count() > 0)
                        if (info.DeclareProperty == null)
                            sets.Add(f);
                }
            }

            foreach (var f in gets)
            {
                if (sets.Contains(f))
                {
                    GetAndSet.Add(f);
                    sets.Remove(f);
                }
                else
                    onlyGet.Add(f);
            }

            foreach (var f in sets)
                onlySet.Add(f);
        }

        static void WriteFieldMethodBody(FieldDefinition def, ITextOutput output, bool isRead)
        {
            if (!isRead)
            {
                output.WriteLine("{");
                output.WriteLine("}");
            }
            else
            {
                output.WriteLine("{");
                output.Indent();
                string value = Util.GetDefaultValue(def.FieldType);
                output.WriteLine("return " + value + ";");
                output.Unindent();
                output.WriteLine("}");
            }
        }
        
        public static void WritePrivateClassBody(TypeDefinition def, ITextOutput output)
        {
            List<MethodDefinition> ctorSections = new List<MethodDefinition>();
            List<MethodDefinition> publicSections = new List<MethodDefinition>();
            List<MethodDefinition> protectedSection = new List<MethodDefinition>();
            List<MethodDefinition> privateSection = new List<MethodDefinition>();

            ConvertToSection(def, ctorSections, publicSections, protectedSection, privateSection);

            List<FieldDefinition> GetAndSet = new List<FieldDefinition>();
            List<FieldDefinition> OnlyGet = new List<FieldDefinition>();
            List<FieldDefinition> OnlySet = new List<FieldDefinition>();
            FindFieldAccess(def, GetAndSet, OnlyGet, OnlySet);

            //output.Write("class " + def.Name + "Private : public QSharedData");
            //output.WriteLine();
            //output.WriteLine("{");

            /* write ctor section */
            if (ctorSections.Count() > 0)
            {
                //output.WriteLine("public:");
                //output.Indent();

                foreach (var m in ctorSections)
                {
                    WriteCtor(m, output);
                }
                if (ctorSections.Count() > 0)
                    output.WriteLine();
                WriteAddCppMethod(def, output);

                if (publicSections.Count() > 0 || protectedSection.Count() > 0 || privateSection.Count() > 0)
                    output.WriteLine();
            }

            /* write public method */
            if (publicSections.Count() > 0)
            {
                //output.WriteLine("public:");
                //output.Indent();
                /* write filed as method */
                for (int i = 0; i < GetAndSet.Count(); ++i)
                {
                    var f = GetAndSet[i];
                    WriteFieldAsMethod(f, output, true);
                    WriteFieldMethodBody(f, output, true);
                    WriteFieldAsMethod(f, output, false);
                    WriteFieldMethodBody(f, output, false);
                    if (i < GetAndSet.Count() - 1)
                        output.WriteLine();
                }
                if (GetAndSet.Count() > 0 && OnlySet.Count() > 0)
                    output.WriteLine();

                for (int i = 0; i < OnlySet.Count(); ++i)
                {
                    var f = OnlySet[i];
                    WriteFieldAsMethod(f, output, true);
                    WriteFieldMethodBody(f, output, true);
                    WriteFieldAsMethod(f, output, false);
                    WriteFieldMethodBody(f, output, false);
                    if (i < OnlySet.Count() - 1)
                        output.WriteLine();
                }

                if (OnlyGet.Count() > 0 && (GetAndSet.Count() > 0 || OnlySet.Count() > 0))
                    output.WriteLine();
                for (int i = 0; i < OnlyGet.Count(); ++i)
                {
                    var f = OnlyGet[i];
                    WriteFieldAsMethod(f, output, true);
                    WriteFieldMethodBody(f, output, true);
                    if (i < OnlyGet.Count() - 1)
                        output.WriteLine();
                }

                if (publicSections.Count() > 0 && (GetAndSet.Count() > 0 || OnlySet.Count() > 0 || OnlyGet.Count() > 0))
                    output.WriteLine();

                foreach (var m in publicSections)
                {
                    WriteMethod(m, output);
                    output.WriteLine();
                }
                //output.Unindent();
                if (protectedSection.Count() > 0 || privateSection.Count() > 0)
                    output.WriteLine();
            }

            /* write protected section */
            if (!def.IsInterface && !Helper.isClassAsEnum(def) && hasChildClass(def))
            {
                if (protectedSection.Count() > 0)
                {
                    output.WriteLine("//protected");
                    //output.Indent();
                    foreach (var m in protectedSection)
                    {
                        WriteMethod(m, output);
                        output.WriteLine();
                    }
                    //output.Unindent();
                    if (privateSection.Count() > 0)
                        output.WriteLine();
                }
            }

            if (!def.IsInterface && !Helper.isClassAsEnum(def))
            {
                if (privateSection.Count() > 0)
                {
                    output.WriteLine("//private:");
                    //output.Indent();
                    foreach (var m in privateSection)
                    {
                        WriteMethod(m, output);
                        output.WriteLine();
                    }
                    //output.Unindent();
                }
            }
            //output.WriteLine("};");
        }

        /*static void WriteClass(TypeDefinition def, TypeDeclaration decl, ITextOutput output)
        {
            if (def.IsInterface || Helper.isClassAsEnum(def))
                return;
            output.WriteLine("#include <QuantKit/" + def.Name + "_p.h>");
            output.WriteLine();
            WriteIncludeBody(def, decl, output);
            WriteNamespaceStart(def.Namespace, output);

            output.WriteLine("namespace Internal {");
            output.WriteLine();

            WritePrivateClassBody(def, decl, output);

            output.WriteLine("} // namespace Internal");
            output.WriteLine();
            WriteNamespaceEnd(def.Namespace, output);
        }*/
    }
}
