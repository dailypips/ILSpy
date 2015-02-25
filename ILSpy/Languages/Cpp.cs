using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    class Cpp
    {

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
                output.WriteLine("} // " + nspace);
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
                            output.Write(" = " + p.Constant.ToString().ToLower());
                    }
                    else if (p.HasConstant && p.Constant == null)
                    {
                        output.Write(" = null");
                    }
                }
            }
        }

        static void WriteMethodHead(MethodDefinition def, ITextOutput output, bool isDeclaration)
        {
            if (def.IsConstructor && def.IsStatic)
                return;

            var info = InfoUtil.Info(def);

            if (def.IsConstructor)
            {
                output.Write(def.DeclaringType.Name + "::" + def.DeclaringType.Name);
            }
            else
            {
                output.Write(info.ReturnType.TypeName);
                output.Write(" ");
                output.Write(def.DeclaringType.Name + "::"+info.Name);
            }
            output.Write("(");
            WriteParameters(def, output, false);
            output.Write(")");
        }

        static void BuildIncludeList(TypeDefinition def, out List<string> externalList, out List<string> moduleClassList, out List<string> moduleEnumOrInterfaceList)
        {
            List<string> elist = new List<string>();
            List<string> mlist = new List<string>();
            List<string> clist = new List<string>();
            bool hasBaseType = def.BaseType != null && def.BaseType.Name != "Object" && def.BaseType.Name != "IDisposable";
            if (hasBaseType)
                mlist.Add(def.BaseType.Name); /* basetype must be include file */
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

            foreach (var m in classList)
            {
                if (m != def.Name )
                {
                    TypeDefinition type = Util.GetTypeDefinition(def.Module, m);
                    if (CppLanguage.IsNeedWriteHxx(type))
                        output.WriteLine("#include <QuantKit/" + m + CppLanguage.HxxFileExtension + ">");
                    else
                        output.WriteLine("#include <QuantKit/" + m  + CppLanguage.HppFileExtension+ ">");
                }
            }
            var tevent = Util.GetTypeDefinition(def.Module, "Event");
            if (tevent == def || Util.isInhertFrom(def, tevent))
            {
                if (classList.Count() > 0)
                    output.WriteLine();
                if (CppLanguage.IsNeedWriteHxx(def))
                    output.WriteLine("#include <QuantKit/EventType.h>");
            }
        }

        static void WriteFieldAsMethod(FieldDefinition field, ITextOutput output, bool isGetter)
        {
            var info = InfoUtil.Info(field);

            if (isGetter)
            {
                bool isValueType;
                string type = Util.TypeString(field.FieldType, out isValueType);
                output.Write(type);
                output.Write(" ");
                output.Write(info.GetterMethodName);
                output.WriteLine("() const");
            }
            else
            {
                bool isValueType;
                string type = Util.TypeString(field.FieldType, out isValueType);
                output.Write("void ");
                output.Write(info.SetterMethodName);
                output.Write("(");
                output.Write(type);
                output.WriteLine(" value)");
            }
        }
        static void WriteEqualFunction(TypeDefinition def , ITextOutput output)
        {
            output.WriteLine("bool " + def.Name + "::operator==(const " + def.Name + " &other) const");
            output.WriteLine("{");
            output.Indent();
            output.WriteLine("if(d && other.d)");
            output.Indent();
            output.WriteLine("return (*d == *other.d);");
            output.Unindent();
            output.WriteLine("else");
            output.Indent();
            output.WriteLine("return (d==other.d)");
            output.Unindent();
            output.Unindent();
            output.WriteLine("}");
        }
        static void WriteDeconstructor(TypeDefinition def, ITextOutput output)
        {
            output.WriteLine(def.Name + "::~" + def.Name + "()");
            output.WriteLine("{");
            output.WriteLine("}");
        }
        static void WriteCopyConstructor(TypeDefinition def, ITextOutput output)
        {
            output.WriteLine(def.Name + "::" + def.Name + " (const " + def.Name + " &other)");
            output.Indent();
            output.WriteLine(": d(other.d)");
            output.Unindent();
            output.WriteLine("{");      
            output.WriteLine("}");
        }
        static void WriteAssignConstructor(TypeDefinition def, ITextOutput output)
        {
            output.WriteLine(def.Name + "::" + def.Name + " &operator=(const " + def.Name + " &other)");
            output.WriteLine("{");
            output.Indent();
            output.WriteLine("d = other.d;");
            output.WriteLine("return *this;");
            output.Unindent();
            output.WriteLine("}");
        }
        static void WriteAddCppMethod(TypeDefinition def, ITextOutput output)
        {
            if (def.IsInterface)
                return;
            WriteDeconstructor(def, output);
            output.WriteLine();
            WriteCopyConstructor(def, output);
            output.WriteLine();
            WriteAssignConstructor(def, output);
            output.WriteLine();
            WriteEqualFunction(def, output);
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
        static void WriteParameterNames(MethodDefinition def, ITextOutput output)
        {
            bool isFirst = true;
            foreach (var p in def.Parameters)
            {
                if (isFirst)
                    isFirst = false;
                else
                    output.Write(", ");
                output.Write(p.Name);
            }
        }

        static void WriteCtor(MethodDefinition def , ITextOutput output)
        {
            if (def.IsConstructor && def.IsStatic)
                return;
            if (!def.IsConstructor)
                return;
            if (def.DeclaringType != null && def.DeclaringType.IsInterface)
                return;

            output.Write(def.DeclaringType.Name + "::" + def.DeclaringType.Name);

            output.Write("(");
            WriteParameters(def, output, false);
            output.Write(")");

            output.WriteLine();
            output.Indent();
            output.Write(": d_ptr(new " + def.DeclaringType.Name + "Private");
            if (def.Parameters.Count() > 0)
            {
                output.Write("(");
                WriteParameterNames(def, output);
                output.Write(")");
            }
            output.WriteLine(")");
            output.Unindent();
            output.WriteLine("{");
            output.WriteLine("}");
        }

        static void WriteMethod(MethodDefinition def, ITextOutput output)
        {
            if (def.IsConstructor && def.IsStatic)
                return;
            if (def.DeclaringType != null && def.DeclaringType.IsInterface)
                return;

            var info = InfoUtil.Info(def);

            if (def.IsConstructor)
            {
                output.Write(def.DeclaringType.Name + "::" + def.DeclaringType.Name);
            }
            else
            {
                output.Write(info.ReturnType.TypeName);
                output.Write(" ");
                output.Write(def.DeclaringType.Name + "::" + info.Name);
            }
            output.Write("(");
            WriteParameters(def, output, false);
            output.Write(")");
            if (def.IsGetter)
            {
                output.Write(" const");
            }

            output.WriteLine();
            output.WriteLine("{");
            if (!def.IsConstructor)
            {
                output.Indent();
                if (def.ReturnType.MetadataType != MetadataType.Void)
                {
                    output.Write("return ");
                }
                output.Write("d_ptr->");
                output.Write(info.Name);
                output.Write("(");
                WriteParameterNames(def, output);
                output.WriteLine(");");
                output.Unindent();
            }
            output.WriteLine("}");
        }

        static void WriteFieldMethodBody(FieldDefinition def, ITextOutput output, bool isRead)
        {
            var info = InfoUtil.Info(def);
            if (!isRead)
            {
                output.WriteLine("{");
                output.Indent();
                output.Write("d_ptr->");
                output.Write(info.GetterMethodName);
                output.WriteLine("(value);");
                output.Unindent();
                output.WriteLine("}");
            }
            else
            {
                output.WriteLine("{");
                output.Indent();
                string value = Util.GetDefaultValue(def.FieldType);
                output.WriteLine("return d_ptr->");
                output.Write(info.SetterMethodName);
                output.WriteLine("()");
                output.Unindent();
                output.WriteLine("}");
            }
        }

        static void WriteProxyClassBody(TypeDefinition def, ITextOutput output)
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
                    output.WriteLine();
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
                //if (protectedSection.Count() > 0 || privateSection.Count() > 0)
                //    output.WriteLine();
            }

            /* write protected section */
            /*if (!def.IsInterface && !Helper.isClassAsEnum(def) && hasChildClass(def))
            {
                if (protectedSection.Count() > 0)
                {
                    output.WriteLine("//protected");
                    //output.Indent();
                    foreach (var m in protectedSection)
                    {
                        WriteMethod(m, output);
                    }
                    //output.Unindent();
                    if (privateSection.Count() > 0)
                        output.WriteLine();
                }
            }*/

            /*if (!def.IsInterface && !Helper.isClassAsEnum(def))
            {
                if (privateSection.Count() > 0)
                {
                    output.WriteLine("//private:");
                    //output.Indent();
                    foreach (var m in privateSection)
                    {
                        WriteMethod(m, output);
                    }
                    //output.Unindent();
                }
            }*/
            //output.WriteLine("};");
        }

        public static void WriteClass(TypeDefinition def,  ITextOutput output)
        {
            if (def.IsInterface || Helper.isClassAsEnum(def))
                return;
            output.WriteLine("#include <QuantKit/" + def.Name + ".h>");
            output.WriteLine();
            WriteIncludeBody(def, output);
            var info = InfoUtil.Info(def);
            bool isFinalClass = info != null && !info.HasDerivedClass;
            if (!CppLanguage.IsNeedWriteHxx(def))
            {
                if (info.BaseTypeInModule != null)
                    output.WriteLine("#include \"" + info.BaseTypeInModule.Name + CppLanguage.HxxFileExtension + "\"");
                var tevent = Util.GetTypeDefinition(def.Module, "Event");
                if (tevent == def || Util.isInhertFrom(def, tevent))
                {
                    if (!CppLanguage.IsNeedWriteHxx(def))
                        output.WriteLine("#include <QuantKit/EventType.h>");
                }
                Hxx.WriteClassBody(def, output);
            }
            else
            {
                output.WriteLine("#include \"" + def.Name + CppLanguage.HxxFileExtension + "\"");
            }
            //output.WriteLine("#include <QuantKit/" + def.Name + CppLanguage.HxxFileExtension + ">");
            //output.WriteLine();
            //WriteIncludeBody(def, output);
            output.WriteLine();
            output.WriteLine("using namespace QuantKit;");
            //output.WriteLine("using namespace QuantKit::Internal;");
            output.WriteLine();
            //WriteNamespaceStart(def.Namespace, output);
            //output.WriteLine("namespace Internal {");
            //output.WriteLine();
            Cxx.WritePrivateClassBody(def, output);

            //output.WriteLine("} // namespace Internal");
            output.WriteLine();

            output.WriteLine("// Pubic API ");
            output.WriteLine();

            WriteProxyClassBody(def, output);

            if (!def.IsInterface && !Helper.isClassAsEnum(def))
            {
                output.WriteLine();
                output.WriteLine("QDataStream& " + def.Name + "::operator<<(QDataStream &stream, const " + def.Name + " &" +def.Name.ToLower() +")");
                output.WriteLine("{");
                output.Indent();
                output.WriteLine("return stream << " + def.Name.ToLower()+".toString();");
                output.Unindent();
                output.WriteLine("}");
            }

            output.WriteLine();
            //WriteNamespaceEnd(def.Namespace, output);
        }
    }
}
