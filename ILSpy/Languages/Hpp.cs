using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    class Hpp
    {
        static void WriteHeadStart(string name, ITextOutput output)
        {
            string hname = "__QUANTKIT_" + name.ToUpper() + "_H__";
            output.WriteLine("#ifndef " + hname);
            output.WriteLine("#define " + hname);
            //output.WriteLine("#include <QuantKit/" +name + ".h>");
        }

        static void WriteHeadEnd(string name, ITextOutput output)
        {
            string hname = "__QUANTKIT_" + name.ToUpper() + "_H__";
            output.WriteLine("#endif // " + hname);
        }

        static void WriteNamespaceStart(string name, ITextOutput output)
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
                output.WriteLine("} // namespace " + nspace);
                output.WriteLine();
            }
        }

        static string cancialParameterName(MethodDefinition def, ParameterDefinition p)
        {
            string result = p.Name;

            return result;
        }
        static void WriteParameters(MethodDefinition def, ITextOutput output, bool outputOptionValue)
        {
            bool isValueType;
            bool isFirst = true;
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
                if (outputOptionValue)
                {
                    if (p.HasConstant && p.Constant != null)
                    {
                        if (p.Name == "currencyId" && p.Constant.ToString() == "148")
                            output.Write(" = CurrencyId::USD");
                        else
                        {
                            var c = p.Constant as string;
                            if (c != null)
                                output.Write(" = \"" + p.Constant.ToString().ToLower()+"\"");
                            else {
                                var pdef = p.ParameterType as TypeDefinition;
                                if (pdef != null && pdef.IsEnum)
                                {
                                    foreach (var field in pdef.Fields)
                                    {
                                        if (field.HasConstant && field.Constant.ToString() == p.Constant.ToString())
                                        {
                                            output.Write(" = " + pdef.Name + "::"+ field.Name);
                                            break;
                                        }
                                    }
                                }
                                else
                                    output.Write(" = " + p.Constant.ToString().ToLower());
                            }
                        }
                    }
                    else if (p.HasConstant && p.Constant == null)
                    {
                        output.Write(" = 0");
                    }
                }
            }
        }

        static void WriteMethodHead(MethodDefinition def, ITextOutput output, bool isDeclaration)
        {
            if (def.IsConstructor && def.IsStatic)
                return;

            var info = InfoUtil.Info(def);
            bool isCopyConstructor = info!=null && info.isCopyConstructor;
            if (def.IsConstructor)
            {
                if (def.Parameters.Count() == 1 && !isCopyConstructor)
                    output.Write("explicit ");
                output.Write(def.DeclaringType.Name);
            }
            else
            {
                output.Write(info.ReturnTypeName);
                output.Write(" ");
                output.Write(info.Name);
            }
            output.Write("(");
            WriteParameters(def, output, isDeclaration);
            output.Write(")");
            /*if (modifiers.HasFlag(Modifiers.Override))
            {
                output.Write(" Q_DECL_OVERRIDE");
            }*/
        }

        static void BuildIncludeList(TypeDefinition def, out List<string> externalList, out List<string> moduleClassList, out List<string> moduleEnumOrInterfaceList)
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

        static void WriteIncludeBody(TypeDefinition def, ITextOutput output)
        {
            List<string> externalList;
            List<string> moduleEnumOrInterfaceList;
            List<string> classList;

            BuildIncludeList(def, out externalList, out classList, out moduleEnumOrInterfaceList);
            //TODO IEnumable and etc.. ICollection...
            output.WriteLine("#include <QuantKit/quantkit_global.h>");
            foreach (var e in externalList)
            {
                if (!e.StartsWith("Class"))
                    output.WriteLine("#include <" + e + ">");
            }
            if (!def.IsInterface && !Helper.isClassAsEnum(def))
                output.WriteLine("#include <QSharedDataPointer>");

            if (moduleEnumOrInterfaceList.Count() > 0)
                output.WriteLine();

            foreach (var m in moduleEnumOrInterfaceList)
            {
                if (m != def.Name)
                {
                    output.WriteLine("#include <QuantKit/" + m + ".h>");
                }
            }
            /*foreach (var m in nodupmlist)
            {
                if (m != def.Name)
                    output.WriteLine("#include <QuantKit/" + m + ".h>");
            }*/
        }

        static void WriteMethod(MethodDefinition m, ITextOutput output)
        {
            if (m.IsConstructor && m.IsStatic)
                return;
            if (m.DeclaringType.IsInterface)
                output.Write("virtual ");

            WriteMethodHead(m, output, true);
            
            if (m.IsGetter)
                output.Write(" const");
            
            if (m.DeclaringType != null && m.DeclaringType.IsInterface)
                output.WriteLine(" = 0;");
            else
                output.WriteLine(";");
        }

        static void WriteClassHead(TypeDefinition def, ITextOutput output)
        {
            bool isValueType;
            output.Write("class QUANTKIT_EXPORT " + Util.TypeString(def, out isValueType));
            bool hasBaseType = def.BaseType != null && def.BaseType.Name != "Object";
            if (hasBaseType || def.HasInterfaces)
            {
                bool isFirst = true;
                output.Write(" : public ");
                if (hasBaseType)
                {
                    output.Write(def.BaseType.Name);
                    isFirst = false;
                }

                foreach (var item in def.Interfaces)
                {
                    if (!isFirst)
                        output.Write(" , public ");
                    else
                        isFirst = false;
                    output.Write(Util.TypeString(item, out isValueType));
                }
            }
        }

        static void WriteFieldAsMethod(FieldDefinition field, ITextOutput output, bool isGetter)
        {
            var info = InfoUtil.Info(field);
            if (info == null)
            {
                Console.WriteLine("Error");
                return;
            }
            /*var name = field.Name;
            if (field.Name.StartsWith("m_"))
                name = field.Name.Remove(0, 2);*/

            if (isGetter)
            {
                bool isValueType;
                string type = Util.TypeString(field.FieldType, out isValueType);
                output.Write(type);
                output.Write(" ");
                //output.Write("get");
                //output.Write(Util.upperFirstChar(name));
                output.Write(info.GetterMethodName);
                output.WriteLine("() const;");
            }
            else
            {
                bool isValueType;
                string type = Util.TypeString(field.FieldType, out isValueType);
                output.Write("void ");
                //output.Write(Util.upperFirstChar(name));
                //output.Write("(");
                output.Write(info.SetterMethodName);
                output.Write("(");
                output.Write(type);
                output.WriteLine(" value);");
            }
        }
        static void WriteAddCppMethod(TypeDefinition def, ITextOutput output)
        {
            if (def.IsInterface)
                return;
            output.WriteLine("~" + def.Name + "();");
            output.WriteLine();
            output.WriteLine(def.Name +" &operator=(const "+def.Name+" &other);");
            output.Unindent();
            output.WriteLine("#ifdef Q_COMPILER_RVALUE_REFS");
            output.Indent();
            output.WriteLine("inline "+def.Name+" &operator=("+def.Name+" &&other) { qSwap(d_ptr, other.d_ptr); return *this; }");
            //output.WriteLine(def.Name + " &operator=(" + def.Name + " &&other) { swap(other); return *this; }");
            output.Unindent();
            output.WriteLine("#endif");
            output.Indent();
            //output.WriteLine("void swap(" + def.Name + " &other) { d_ptr.swap(other.d_ptr); }");
            output.WriteLine("inline void swap(" +def.Name+" &other)  { qSwap(d_ptr, other.d_ptr); }");
            output.WriteLine("inline bool operator==(const " + def.Name + " &other) const { return d_ptr == other.d_ptr; }");
            output.WriteLine("inline bool operator!=(const " + def.Name + " &other) const { return !(this->operator==(other));  }");
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
        static void WriteDefaultNullConstructor(TypeDefinition def, ITextOutput output)
        {
            output.WriteLine(def.Name + "();");
        }
        static void WriteDefaultCopyConstructor(TypeDefinition def, ITextOutput output)
        {
            output.WriteLine(def.Name + "(const " + def.Name + " &other);");
        }
        static void WriteClassBody(TypeDefinition def, ITextOutput output)
        {
            if (def.Name == "CurrencyId")
            {
                Helper.WriteCurrencyIdInclude(def, output);
                return;
            }
            if (def.Name == "EventType")
            {
                Helper.WriteEventTypeInclude(def, output);
                return;
            }

            List<MethodDefinition> ctorSections = new List<MethodDefinition>();
            List<MethodDefinition> publicSections = new List<MethodDefinition>();
            List<MethodDefinition> protectedSection = new List<MethodDefinition>();
            List<MethodDefinition> privateSection = new List<MethodDefinition>();

            ConvertToSection(def, ctorSections, publicSections, protectedSection, privateSection);

            List<FieldDefinition> GetAndSet = new List<FieldDefinition>();
            List<FieldDefinition> OnlyGet = new List<FieldDefinition>();
            List<FieldDefinition> OnlySet = new List<FieldDefinition>();
            FindFieldAccess(def, GetAndSet, OnlyGet, OnlySet);

            WriteClassHead(def, output);
            output.WriteLine();
            output.WriteLine("{");

            /* write ctor section */
            if (ctorSections.Count() > 0)
            {
                output.WriteLine("public:");
                output.Indent();

                var tinfo = InfoUtil.Info(def);
                //if (tinfo != null && tinfo.NullConstructor == null)
                //    WriteDefaultNullConstructor(def, output);
                if (tinfo != null && tinfo.CopyConstructor == null)
                    WriteDefaultCopyConstructor(def, output);
                foreach (var m in ctorSections)
                {
                    WriteMethod(m, output);
                }
                WriteAddCppMethod(def, output);
                output.Unindent();
                if (publicSections.Count() > 0)// || protectedSection.Count() > 0 || privateSection.Count() > 0)
                    output.WriteLine();
            }

            /* write public method */
            if (publicSections.Count() > 0)
            {
                output.WriteLine("public:");
                output.Indent();
                /* write filed as method */
                for (int i = 0; i < GetAndSet.Count(); ++i )
                {
                    var f = GetAndSet[i];
                    WriteFieldAsMethod(f, output, true);
                    WriteFieldAsMethod(f, output, false);
                    if (i < GetAndSet.Count() - 1)
                        output.WriteLine();
                }
                if (GetAndSet.Count()>0 && OnlySet.Count() > 0)
                    output.WriteLine();

                for (int i = 0; i < OnlySet.Count(); ++i)
                {
                    var f = OnlySet[i];
                    WriteFieldAsMethod(f, output, true);
                    WriteFieldAsMethod(f, output, false);
                    if (i < OnlySet.Count() - 1)
                        output.WriteLine();
                }

                if (OnlyGet.Count() > 0 && (GetAndSet.Count()>0 || OnlySet.Count()>0))
                    output.WriteLine();
                for (int i = 0; i < OnlyGet.Count(); ++i)
                {
                    var f = OnlyGet[i];
                    WriteFieldAsMethod(f, output, true);
                    if (i < OnlyGet.Count() - 1)
                        output.WriteLine();
                }

                if (publicSections.Count() > 0 && (GetAndSet.Count() > 0 || OnlySet.Count() > 0 || OnlyGet.Count()>0))
                    output.WriteLine();

                foreach (var m in publicSections)
                {
                    WriteMethod(m, output);
                }
                output.Unindent();
                //if (protectedSection.Count() > 0 || privateSection.Count() > 0)
                //    output.WriteLine();
            }

            /* write protected section */
            var cinfo = InfoUtil.Info(def);
            bool hasChildClass = cinfo != null && cinfo.HasDerivedClass;
            bool isDerivedClass = cinfo!=null && cinfo.isDerivedClass;
            if(!def.IsInterface && !Helper.isClassAsEnum(def) &&  hasChildClass)//hasChildClass(def))
            {
                output.WriteLine();
                output.WriteLine("protected:");
                output.Indent();
                output.WriteLine(def.Name + "(" + def.Name + "Private& dd);");
                if (!isDerivedClass)
                    output.WriteLine("QSharedDataPointer<" + def.Name + "Private> d_ptr;");
                output.Unindent();
            }

            if (!def.IsInterface && !Helper.isClassAsEnum(def))
            {
                output.WriteLine();
                output.WriteLine("private:");
                output.Indent();
                //if (!hasChildClass(def))
                if(!hasChildClass && !isDerivedClass)
                {
                    output.WriteLine("QSharedDataPointer<" + def.Name + "Private> d_ptr;");
                    output.WriteLine();
                }
                output.WriteLine("friend QUANTKIT_EXPORT QDataStream &operator<<(QDataStream & stream, const " + def.Name + " &"+ def.Name.ToLower()+");");
                output.Unindent();
            }
            /*if (protectedSection.Count() > 0)
            {
                output.WriteLine("protected:");
                output.Indent();
                foreach (var m in protectedSection)
                {
                    WriteMethod(m, output);
                }
                output.Unindent();
                if (privateSection.Count() > 0)
                    output.WriteLine();
            }*/

            /* write private section */
            /*if (privateSection.Count() > 0)
            {
                output.WriteLine("private:");
                output.Indent();
                foreach (var m in privateSection)
                {
                    WriteMethod(m, output);
                }
                output.Unindent();
            }*/

            output.WriteLine("};");
        }

        static void WriteForward(TypeDefinition def, ITextOutput output)
        {
            List<string> elist;
            List<string> mlist;
            List<string> clist;
            BuildIncludeList(def, out elist, out clist, out mlist);
            var info = InfoUtil.Info(def);
            if (info != null)
            {
                foreach (var m in clist)
                {
                    if (m != def.Name)
                    {
                        output.WriteLine("class " + m + ";");
                        //output.WriteLine("#include <QuantKit/" + m + ".h>");
                    }
                }
                if (clist.Count() > 0)
                    output.WriteLine();
            }
        }
        public static void WriteClass(TypeDefinition def, ITextOutput output)
        {

            WriteHeadStart(def.Name, output);
            output.WriteLine();
            WriteIncludeBody(def, output);
            WriteNamespaceStart(def.Namespace, output);
            if (!def.IsInterface && !Helper.isClassAsEnum(def))
            {
                output.WriteLine("class " + def.Name + "Private;");
                output.WriteLine();
            }
            WriteForward(def, output);
            WriteClassBody(def, output);
            //output.WriteLine();

            if (!def.IsInterface && !Helper.isClassAsEnum(def))
            {
                output.WriteLine();
                output.WriteLine("QUANTKIT_EXPORT QDataStream &operator<<(QDataStream &, const " + def.Name + " &);");
            }
            WriteNamespaceEnd(def.Namespace, output);
            //if (def.Name != "CurrencyId" && def.Name != "EventType" && !def.IsEnum && !def.IsInterface)
            //    output.WriteLine("Q_DECLARE_SHARED(QuantKit::" + def.Name + ")");
            WriteHeadEnd(def.Name, output);
        }

        static void WriteEnumBody(ClassInfo info, ITextOutput output)
        {
            output.WriteLine("enum " + info.Name + " : unsigned char");
            output.WriteLine("{");
            output.Indent();
            var members = info.Declaration.Descendants.OfType<EnumMemberDeclaration>().ToList();
            for (int i = 0; i < members.Count(); ++i)
            {
                var item = members[i];
                var text = item.GetText();
                var split = text.Split('=');
                output.Write(split[0]);
                if (split.Count() >= 2)
                {
                    output.Write(" = ");
                    output.Write(split[1]);
                }
                if (i != members.Count() - 1)
                    output.WriteLine(",");
                else
                    output.WriteLine();
            }

            output.Unindent();
            output.WriteLine("};");
        }

        public static void WriteEnum(TypeDefinition def, ITextOutput output)
        {
            if (RenameUtil.EnumRenameDict.ContainsKey(def.FullName))
            {
                def.Name = RenameUtil.EnumRenameDict[def.FullName];
            }
            WriteHeadStart(def.Name, output);
            WriteNamespaceStart(def.Namespace, output);
            var info = InfoUtil.Info(def);
            WriteEnumBody(info, output);
            WriteNamespaceEnd(def.Namespace, output);
            WriteHeadEnd(def.Name, output);
        }
    }
}
