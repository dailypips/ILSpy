using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    class Hxx
    {
        static void WriteHeadStart(string name, ITextOutput output)
        {
            string hname = "__QUANTKIT_" + name.ToUpper() + "_PRIVATE_H__";
            output.WriteLine("#ifndef " + hname);
            output.WriteLine("#define " + hname);
        }

        static void WriteHeadEnd(string name, ITextOutput output)
        {
            string hname = "__QUANTKIT_" + name.ToUpper() + "_PRIVATE_H__";
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
                output.WriteLine("} // namepsace " + nspace);
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
           /* var info = InfoUtil.Info(def);
            bool isCopyContructor = info.isCopyConstructor;
            if (isCopyContructor)
            {
                var cinfo = info.DeclaringType;
                output.Write("const " + cinfo.PrivateName + " &" + def.Parameters[0].Name);
                return;
            }*/
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
            bool isCopyConstructor = info != null && info.isCopyConstructor;
            if (def.IsConstructor)
            {
                if (def.Parameters.Count() == 1 && !isCopyConstructor)
                    output.Write("explicit ");
                output.Write(def.DeclaringType.Name +"Private");
            }
            else
            {
                //if (info.modifiers.HasFlag(Modifiers.Virtual))
                //    output.Write("virtual ");
                output.Write(info.ReturnType.TypeName);
                output.Write(" ");
                output.Write(info.Name);
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
            /*bool hasBaseType = def.BaseType != null && def.BaseType.Name != "Object";
            if (hasBaseType)
                mlist.Add(def.BaseType.Name); // basetype must be include file*/
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

            var info = InfoUtil.Info(def);
            
            // include base private header file
            if (info.BaseTypeInModule != null)
                output.WriteLine("#include \"" + info.BaseTypeInModule.Name + CppLanguage.HxxFileExtension +"\"");

            foreach (var m in classList)
            {
                if (m != def.Name)
                {
                    output.WriteLine("#include \"" + m + CppLanguage.HxxFileExtension +"\"");
                    //output.WriteLine("#include <QuantKit/" + m + ".h>");
                }
            }
        }

        static void WriteMethod(MethodDefinition m, ITextOutput output)
        {
            if (m.IsConstructor && m.IsStatic)
                return;
            var info = InfoUtil.Info(m);

            if (m.DeclaringType.IsInterface || info.modifiers.HasFlag(Modifiers.Virtual) || info.modifiers.HasFlag(Modifiers.Override))
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
                output.WriteLine("() const;");
            }
            else
            {
                bool isValueType;
                string type = Util.TypeString(field.FieldType, out isValueType);
                output.Write("void set");
                output.Write(Util.upperFirstChar(name));
                output.Write("(");
                output.Write(type);
                output.WriteLine(" value);");
            }
        }
        static void WriteAddCppMethod(TypeDefinition def, ITextOutput output)
        {
            if (def.IsInterface)
                return;
            output.WriteLine("virtual ~" + def.Name + "Private();");
            output.WriteLine();
            //output.WriteLine(def.Name + "Private &operator=(const " + def.Name + "Private &other);");
            output.Unindent();
            output.Indent();
            if (def.Fields.Count() > 0)
                output.WriteLine("bool operator==(const " + def.Name + "Private &other) const;");
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

        static void WriteFields(TypeDefinition def, ITextOutput output)
        {
            if (def.Fields.Count() > 0)
            {
                output.WriteLine("public:");
                output.Indent();
                foreach (var f in def.Fields)
                {
                    var info = InfoUtil.Info(f);
                    output.Write(info.FieldTypeName);
                    output.Write(" ");
                    output.Write(f.Name);
                    output.WriteLine(";");
                }
                output.Unindent();
                output.WriteLine();
            }
        }

        public static void WriteClassBody(TypeDefinition def, ITextOutput output)
        {
            WriteNamespaceStart(def.Namespace, output);
            List<MethodDefinition> ctorSections = new List<MethodDefinition>();
            List<MethodDefinition> publicSections = new List<MethodDefinition>();
            List<MethodDefinition> protectedSection = new List<MethodDefinition>();
            List<MethodDefinition> privateSection = new List<MethodDefinition>();

            ConvertToSection(def, ctorSections, publicSections, protectedSection, privateSection);

            List<FieldDefinition> GetAndSet = new List<FieldDefinition>();
            List<FieldDefinition> OnlyGet = new List<FieldDefinition>();
            List<FieldDefinition> OnlySet = new List<FieldDefinition>();
            FindFieldAccess(def, GetAndSet, OnlyGet, OnlySet);

            var info = InfoUtil.Info(def);
            var baseType = info.BaseTypeInModule;
            if (baseType != null)
            {
                output.Write("class " + def.Name + "Private : public " + baseType.Name+"Private"); 
            }
            else
            {
                output.Write("class " + def.Name + "Private : public QSharedData");
            }
            output.WriteLine();
            output.WriteLine("{");
            WriteFields(def, output);
            /* write ctor section */
            if (ctorSections.Count() > 0)
            {
                output.WriteLine("public:");
                output.Indent();

                foreach (var m in ctorSections)
                {
                    var minfo = InfoUtil.Info(m);
                    if (!minfo.isCopyConstructor)
                        WriteMethod(m, output);
                }
                WriteAddCppMethod(def, output);
                output.Unindent();
                if (publicSections.Count() > 0 || protectedSection.Count() > 0 || privateSection.Count() > 0)
                    output.WriteLine();
            }

            /* write public method */
            if (publicSections.Count() > 0)
            {
                output.WriteLine("public:");
                output.Indent();
                /* write filed as method */
                for (int i = 0; i < GetAndSet.Count(); ++i)
                {
                    var f = GetAndSet[i];
                    WriteFieldAsMethod(f, output, true);
                    WriteFieldAsMethod(f, output, false);
                    if (i < GetAndSet.Count() - 1)
                        output.WriteLine();
                }
                if (GetAndSet.Count() > 0 && OnlySet.Count() > 0)
                    output.WriteLine();

                for (int i = 0; i < OnlySet.Count(); ++i)
                {
                    var f = OnlySet[i];
                    WriteFieldAsMethod(f, output, true);
                    WriteFieldAsMethod(f, output, false);
                    if (i < OnlySet.Count() - 1)
                        output.WriteLine();
                }

                if (OnlyGet.Count() > 0 && (GetAndSet.Count() > 0 || OnlySet.Count() > 0))
                    output.WriteLine();
                for (int i = 0; i < OnlyGet.Count(); ++i)
                {
                    var f = OnlyGet[i];
                    WriteFieldAsMethod(f, output, true);
                    if (i < OnlyGet.Count() - 1)
                        output.WriteLine();
                }

                if (publicSections.Count() > 0 && (GetAndSet.Count() > 0 || OnlySet.Count() > 0 || OnlyGet.Count() > 0))
                    output.WriteLine();

                foreach (var m in publicSections)
                {
                    WriteMethod(m, output);
                }
                output.Unindent();
                if (protectedSection.Count() > 0 || privateSection.Count() > 0)
                    output.WriteLine();
            }

            /* write protected section */
            if (!def.IsInterface && !Helper.isClassAsEnum(def) && hasChildClass(def))
            {
                if (protectedSection.Count() > 0)
                {
                    output.WriteLine("//protected");
                    output.Indent();
                    foreach (var m in protectedSection)
                    {
                        WriteMethod(m, output);
                    }
                    output.Unindent();
                    if (privateSection.Count() > 0)
                        output.WriteLine();
                }
            }

            if (!def.IsInterface && !Helper.isClassAsEnum(def))
            {
                if (privateSection.Count() > 0)
                {
                    output.WriteLine("//private:");
                    output.Indent();
                    foreach (var m in privateSection)
                    {
                        WriteMethod(m, output);
                    }
                    output.Unindent();
                }
            }
            output.WriteLine("};");
            WriteNamespaceEnd(def.Namespace, output);
        }

        public static void WriteClass(TypeDefinition def, ITextOutput output)
        {
            if (def.IsInterface || Helper.isClassAsEnum(def))
                return;
            var info = InfoUtil.Info(def);
            WriteHeadStart(def.Name, output);
            output.WriteLine();
            output.WriteLine("#include <QuantKit/" + def.Name + ".h>");
            output.WriteLine();

            if (info.IsBaseClassInModule)
            {
                output.WriteLine("#include <QSharedData>");
                output.WriteLine();
            }
            WriteIncludeBody(def, output);
            

            WriteClassBody(def, output);



            WriteHeadEnd(def.Name, output);
        }
    }
}
