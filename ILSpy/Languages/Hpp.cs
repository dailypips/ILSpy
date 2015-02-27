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

        static void WriteParameters(MethodInfo info, ITextOutput output, bool outputOptionValue)
        {
            bool isFirst = true;
            foreach (var p in info.Parameters)
            {
                if (isFirst)
                    isFirst = false;
                else
                    output.Write(", ");
                string ptype = p.ParameterType.TypeName;
                if (!p.ParameterType.isValueType)
                    ptype = "const " + ptype + "&";
                output.Write(ptype);
                output.Write(" ");
                output.Write(p.Name);
                if (outputOptionValue)
                {
                    if (p.HasConstant)
                        output.Write(" = " + p.ConstantValue);
                }
            }
        }

        static void WriteMethodHead(MethodInfo info, ITextOutput output, bool isDeclaration)
        {
            if (info.IsConstructor && info.IsStatic)
                return;

            bool isCopyConstructor = info!=null && info.isCopyConstructor;
            if (info.IsConstructor)
            {
                if (info.Parameters.Count() == 1 && !isCopyConstructor)
                    output.Write("explicit ");
                output.Write(info.DeclaringType.Name);
            }
            else
            {
                output.Write(info.ReturnType.TypeName);
                output.Write(" ");
                output.Write(info.Name);
            }
            output.Write("(");
            WriteParameters(info, output, isDeclaration);
            output.Write(")");
            /*if (modifiers.HasFlag(Modifiers.Override))
            {
                output.Write(" Q_DECL_OVERRIDE");
            }*/
        }

        static void BuildIncludeList(ClassInfo info, out List<string> externalList, out List<string> moduleClassList, out List<string> moduleEnumOrInterfaceList)
        {
            List<string> elist = new List<string>();
            List<string> mlist = new List<string>();
            List<string> clist = new List<string>();

            //var info = InfoUtil.Info(def);
            bool hasBaseType = info.BaseTypeInModule != null;
            if (hasBaseType)
                mlist.Add(info.def.BaseType.Name); // basetype must be include file
            if (info.HasInterfaces)
            {
                foreach (var i in info.Interfaces)
                {
                    if (i.Namespace != info.Namespace)
                        continue;
                    if (i.Module == info.Module)
                        mlist.Add(i.Name);
                    else
                        elist.Add(i.Name);
                }
            }
            foreach (var method in info.Methods)
            {
                Util.AddInclude(info.Namespace, method.ReturnType.reference, elist, clist, mlist);
                foreach (var p in method.Parameters)
                    Util.AddInclude(info.Namespace, p.ParameterType.reference, elist, clist, mlist);
                //special for currencyId
                foreach (var p in method.Parameters)
                {
                    if (p.Name == "currencyId" && p.HasConstant && p.Constant != null)
                        mlist.Add("CurrencyId");
                }
            }
            foreach (var f in info.Fields)
            {
                Util.AddInclude(info.Namespace, f.FieldType, elist, clist, mlist);
            }

            externalList = elist.Distinct<string>().ToList();
            moduleClassList = clist.Distinct<string>().ToList();
            moduleEnumOrInterfaceList = mlist.Distinct<string>().ToList();
        }

        static void WriteIncludeBody(ClassInfo info, ITextOutput output)
        {
            List<string> externalList;
            List<string> moduleEnumOrInterfaceList;
            List<string> classList;

            BuildIncludeList(info, out externalList, out classList, out moduleEnumOrInterfaceList);
            //TODO IEnumable and etc.. ICollection...
            output.WriteLine("#include <QuantKit/quantkit_global.h>");
            foreach (var e in externalList)
            {
                if (!e.StartsWith("Class"))
                    output.WriteLine("#include <" + e + ">");
            }
            if (!info.IsInterface && !info.isClassAsEnum)
            {
                output.WriteLine("#include <QtAlgorithms>");
                output.WriteLine("#include <QSharedDataPointer>");
            }

            if (moduleEnumOrInterfaceList.Count() > 0)
                output.WriteLine();

            foreach (var m in moduleEnumOrInterfaceList)
            {
                if (m != info.Name)
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

        static void WriteMethod(MethodInfo m, ITextOutput output)
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

        static void WriteClassHead(ClassInfo info, ITextOutput output)
        {
            bool isValueType;
            output.Write("class QUANTKIT_EXPORT " + Util.TypeString(info.def, out isValueType));
            bool hasBaseType = info.def.BaseType != null && info.def.BaseType.Name != "Object";
            //var info = InfoUtil.Info(def);
            if (hasBaseType || info.HasInterfaces)
            {
                bool isFirst = true;
                if (hasBaseType)
                {
                    output.Write(" : public ");
                    output.Write(info.def.BaseType.Name);
                    isFirst = false;
                }

                foreach (var item in info.Interfaces)
                {
                    //if (item.Namespace != def.Namespace)
                    //    continue;
                    if (!isFirst)
                        output.Write(" , public ");
                    else
                    {
                        output.Write(" : public ");
                        isFirst = false;
                    }
                    output.Write(Util.TypeString(item.def, out isValueType));
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
        static void WriteAddCppMethod(ClassInfo info, ITextOutput output)
        {
            if (info.IsInterface)
                return;
            
            output.WriteLine("~" + info.Name + "();");
            output.WriteLine();
            /*output.WriteLine(info.Name + "(const " + info.Name + " &other) { qSwap(d_ptr, other.d_ptr); return *this; }");
            output.WriteLine(info.Name +" &operator=(const "+info.Name+" &other);");
            output.Unindent();
            output.WriteLine("#ifdef Q_COMPILER_RVALUE_REFS");
            output.Indent();
            output.WriteLine("inline "+info.Name+" &operator=("+info.Name+" &&other) { qSwap(d_ptr, other.d_ptr); return *this; }");
            output.WriteLine("inline " + info.Name + "(const " + info.Name + " &&other)  { qSwap(d_ptr, other.d_ptr); return *this; }");
            output.Unindent();
            output.WriteLine("#endif");
            output.Indent();
            output.WriteLine("inline void swap(" +info.Name+" &other)  { qSwap(d_ptr, other.d_ptr); }");*/
            output.WriteLine("inline bool operator==(const " + info.Name + " &other) const;");// { return d_ptr == other.d_ptr; }");
            output.WriteLine("inline bool operator!=(const " + info.Name + " &other) const { return !(this->operator==(other));  }");
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

        static void ConvertToSection(ClassInfo info, List<MethodInfo> ctorSections, List<MethodInfo> pulicSections, List<MethodInfo> protectedSection, List<MethodInfo> privateSection)
        {
            List<MethodInfo> ctors = new List<MethodInfo>();
            List<MethodInfo> props = new List<MethodInfo>();
            List<MethodInfo> others = new List<MethodInfo>();

            //var info = InfoUtil.Info(def);

            foreach (var m in info.Methods)
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
                var modifiers = Util.ConvertModifiers(m.def);
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
                var modifiers = Util.ConvertModifiers(m.def);
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
            
        static void FindFieldAccess(ClassInfo info, List<FieldDefinition> GetAndSet, List<FieldDefinition> onlyGet, List<FieldDefinition> onlySet)
        {
            List<FieldDefinition> gets = new List<FieldDefinition>();
            List<FieldDefinition> sets = new List<FieldDefinition>();

            foreach (var f in info.Fields)
            {
                var finfo = InfoUtil.Info(f);
                if (finfo != null)
                {
                    if (finfo.ReadByOther.Count() > 0 || finfo.AssignByOther.Count() > 0)
                        if (finfo.DeclareProperty == null)
                            gets.Add(f);

                    if (finfo.AssignByOther.Count() > 0)
                        if (finfo.DeclareProperty == null)
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
        static void WriteDefaultNullConstructor(ClassInfo info, ITextOutput output)
        {
            output.WriteLine(info.Name + "();");
        }
        static void WriteDefaultCopyConstructor(ClassInfo info, ITextOutput output)
        {
            output.WriteLine(info.Name + "(const " + info.Name + " &other);");
        }
        static void WriteClassBody(ClassInfo info, ITextOutput output)
        {
            if (info.Name == "CurrencyId")
            {
                Helper.WriteCurrencyIdInclude(info, output);
                return;
            }
            if (info.Name == "EventType")
            {
                Helper.WriteEventTypeInclude(info, output);
                return;
            }

            //var info = InfoUtil.Info(def);

            List<MethodInfo> ctorSections = new List<MethodInfo>();
            List<MethodInfo> publicSections = new List<MethodInfo>();
            List<MethodInfo> protectedSection = new List<MethodInfo>();
            List<MethodInfo> privateSection = new List<MethodInfo>();

            ConvertToSection(info, ctorSections, publicSections, protectedSection, privateSection);

            List<FieldDefinition> GetAndSet = new List<FieldDefinition>();
            List<FieldDefinition> OnlyGet = new List<FieldDefinition>();
            List<FieldDefinition> OnlySet = new List<FieldDefinition>();
            FindFieldAccess(info, GetAndSet, OnlyGet, OnlySet);

            WriteClassHead(info, output);
            output.WriteLine();
            output.WriteLine("{");

            /* write ctor section */
            if (ctorSections.Count() > 0)
            {
                output.WriteLine("public:");
                output.Indent();


                //if (tinfo != null && tinfo.NullConstructor == null)
                //    WriteDefaultNullConstructor(def, output);
                //if (info != null && info.CopyConstructor == null)
                //    WriteDefaultCopyConstructor(info, output);
                foreach (var m in ctorSections)
                {
                    if (!m.isCopyConstructor)
                        WriteMethod(m, output);
                }
                WriteAddCppMethod(info, output);
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
            //var cinfo = InfoUtil.Info(def);
            bool hasChildClass = info.HasDerivedClass;
            bool isDerivedClass = info.isDerivedClass;
            if(!info.IsInterface && !info.isClassAsEnum &&  hasChildClass)//hasChildClass(def))
            {
                output.WriteLine();
                output.WriteLine("protected:");
                output.Indent();
                output.WriteLine(info.Name + "(" + info.PrivateName + "& dd);");
                if (!isDerivedClass)
                    output.WriteLine("QSharedDataPointer<" + info.PrivateName + "> d_ptr;");
                output.Unindent();
            }

            if (!info.IsInterface && !info.isClassAsEnum)
            {
                output.WriteLine();
                output.WriteLine("private:");
                output.Indent();
                //if (!hasChildClass(def))
                if(!hasChildClass && !isDerivedClass)
                {
                    output.WriteLine("QSharedDataPointer<" + info.PrivateName + "> d_ptr;");
                    output.WriteLine();
                }
                if (isDerivedClass)
                {
                    output.WriteLine("QK_DECLARE_PRIVATE(" + info.Name + ")");
                    output.WriteLine();
                }
                output.WriteLine("friend QUANTKIT_EXPORT QDataStream &operator<<(QDataStream & stream, const " + info.Name + " &"+ info.Name.ToLower()+");");
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

        static void WriteForward(ClassInfo info, ITextOutput output)
        {
            List<string> elist;
            List<string> mlist;
            List<string> clist;
            BuildIncludeList(info, out elist, out clist, out mlist);
            if (info != null)
            {
                foreach (var m in clist)
                {
                    if (m != info.Name)
                    {
                        output.WriteLine("class " + m + ";");
                        //output.WriteLine("#include <QuantKit/" + m + ".h>");
                    }
                }
                if (clist.Count() > 0)
                    output.WriteLine();
            }
        }
        public static void WriteClass(ClassInfo info, ITextOutput output)
        {

            //var info = InfoUtil.Info(def);
            WriteHeadStart(info.Name, output);
            output.WriteLine();
            WriteIncludeBody(info, output);
            WriteNamespaceStart(info.Namespace, output);
            if (!info.IsInterface && !info.isClassAsEnum)
            {
                output.WriteLine("class " + info.PrivateName + ";");
                output.WriteLine();
            }
            WriteForward(info, output);
            WriteClassBody(info, output);
            //output.WriteLine();

            if (!info.IsInterface && !info.isClassAsEnum)
            {
                output.WriteLine();
                output.WriteLine("QUANTKIT_EXPORT QDataStream &operator<<(QDataStream &, const " + info.Name + " &);");
            }
            WriteNamespaceEnd(info.Namespace, output);
            //if (def.Name != "CurrencyId" && def.Name != "EventType" && !def.IsEnum && !def.IsInterface)
            //    output.WriteLine("Q_DECLARE_SHARED(QuantKit::" + def.Name + ")");
            WriteHeadEnd(info.Name, output);
        }

        static void WriteEnumBody(ClassInfo info, ITextOutput output)
        {
            output.WriteLine("enum " + info.Name + " : unsigned char");
            output.WriteLine("{");
            output.Indent();
            int firstValue = 0;
            for (int i = 1; i < info.Fields.Count();++i )
            {
                output.Write(info.Fields[i].Name);
 
                if (info.Fields[i].HasConstant)
                {
                    int value = Int32.Parse(info.Fields[i].Constant.ToString());
                    if (value != i-1 + firstValue)
                    {
                        output.Write(" = " + value.ToString());
                        firstValue = value;
                    }
                }
                if( i!= info.Fields.Count() -1)
                    output.WriteLine(",");
                else
                    output.WriteLine();
            }
                /*var members = info.Declaration.Descendants.OfType<EnumMemberDeclaration>().ToList();
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
                }*/

                output.Unindent();
            output.WriteLine("};");
        }

        public static void WriteEnum(ClassInfo info, ITextOutput output)
        {
            if (RenameUtil.EnumRenameDict.ContainsKey(info.FullName))
            {
                info.def.Name = RenameUtil.EnumRenameDict[info.def.FullName];
            }
            WriteHeadStart(info.Name, output);
            WriteNamespaceStart(info.Namespace, output);
            WriteEnumBody(info, output);
            WriteNamespaceEnd(info.Namespace, output);
            WriteHeadEnd(info.Name, output);
        }
    }
}
