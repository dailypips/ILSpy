using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.ILSpy;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public interface GenerateCodeAbled
    {
        void GenerateCode(TextFormatter f);
        void GenerateHppCode(TextFormatter f);
        void GenerateCppCode(TextFormatter f);
        void GeneratePrivateCode(TextFormatter f);
    }

    public class QModule : GenerateCodeAbled
    {
        public ModuleDefinition def;
        public List<QType> types = new List<QType>();
        public AstBuilder dom;

        public QModule(ModuleDefinition module, DecompilationOptions option)
        {
            def = module;
            dom = CreateAstBuilder(module, option);
            dom.AddAssembly(module.Assembly, onlyAssemblyLevel: false);

            var mv = new QModuleVisitor(this);
            dom.SyntaxTree.AcceptVisitor(mv);

            foreach (var t in types)
            {
                var tv = new QTypeVisitor(t);
                t.decl.AcceptVisitor(tv);
            }

            PostProcess();

            dom.SyntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
        }

        public AstBuilder CreateAstBuilder(ModuleDefinition currentModule, DecompilationOptions options)
        {
            return new AstBuilder(
                new DecompilerContext(currentModule)
                {
                    Settings = options.DecompilerSettings
                });
        }

        public void AddType(QType t)
        {
            types.Add(t);
        }

        public void AddType(TypeDefinition tdef, TypeDeclaration tdecl)
        {
            var t = new QType(this, tdef, tdecl);
            AddType(t);
        }
        public void AddType(TypeDefinition def, DelegateDeclaration decl)
        {
            var t = new QType(this, def, decl);
            AddType(t);
        }

        public QType FindType(TypeDefinition def)
        {
            foreach (var item in types)
            {
                if (item.def == def)
                    return item;
            }
            return null;
        }
        public QType FindType(TypeDeclaration decl)
        {
            foreach (var item in types)
            {
                if (item.decl == decl)
                    return item;
            }
            return null;
        }

        public QType FindType(DelegateDeclaration decl)
        {
            foreach (var item in types)
            {
                if (item.decl == decl)
                    return item;
            }
            return null;
        }

        public QMethod FindMethod(MethodDefinition m)
        {
            QType t = FindType(m.DeclaringType);
            return t.FindMethod(m);
        }

        public QProperty FindProperty(PropertyDefinition p)
        {
            QType t = FindType(p.DeclaringType);
            return t.FindProperty(p);
        }

        public QEvent FindEvent(EventDefinition e)
        {
            QType t = FindType(e.DeclaringType);
            return t.FindEvent(e);
        }

        public QField FindField(FieldDefinition f)
        {
            QType t = FindType(f.DeclaringType);
            return t.FindField(f);
        }

        public void PostProcess()
        {
            // find base type
            foreach (var t in types)
            {
                var bt = t.def.BaseType;
                if (bt == null)
                    continue;
                foreach (var ft in types)
                {
                    if (ft.def == bt)
                    {
                        //t.bases.Add(ft);
                        t.baseType = ft;
                        ft.devires.Add(t);
                    }
                }
            }

            // find interface
            foreach(var t in types)
            {
                TypeDeclaration decl = t.decl as TypeDeclaration;
                if (decl != null)
                {
                    foreach (var bt in decl.BaseTypes)
                    {
                        var dtype = bt.ToTypeReference().ToString();
                        if (t.baseType != null && dtype == t.baseType.def.Name)
                            continue;
                        bool found = false;
                        foreach(var ft in types)
                        {
                            if(ft.def.IsInterface)
                                if(ft.def.Name == dtype)
                                {
                                    t.interfaces.Add(ft);
                                    ft.impls.Add(t);
                                    found = true;
                                }
                        }
                        if (!found)
                            t.externalInterfaces.Add(dtype);
                    }
                }
            }

            foreach (var t in types)
            {
                t.preProcess();
            }

            foreach(var t in types)
            {
                t.postProcess();
            }
        }

        public void GenerateCode(TextFormatter f)
        {
            f.WriteLine("---Hpp---");
            GenerateHppCode(f);
            f.WriteLine("---Cpp---");
            GenerateCppCode(f);
            f.WriteLine("---Private--");
            GeneratePrivateCode(f);
        }

        public void GenerateHppCode(TextFormatter f)
        {

        }

        public void GenerateCppCode(TextFormatter f)
        {

        }

        public void GeneratePrivateCode(TextFormatter f)
        {

        }
    }


    public class QType : GenerateCodeAbled
    {
        public TypeDefinition def;
        TypeDeclaration tdecl = null;
        DelegateDeclaration ddecl = null;
        public NamespaceDeclaration ns;
        public QModule module;
        public QType baseType;
        public List<QMethod> methods = new List<QMethod>();
        public List<QProperty> properties = new List<QProperty>();
        public List<QField> fields = new List<QField>();
        public List<QField> constFields = new List<QField>();
        public List<QEvent> events = new List<QEvent>();
        //public List<QType> bases = new List<QType>();
        public List<QType> devires = new List<QType>();
        public List<QType> interfaces = new List<QType>();
        public List<string> externalInterfaces = new List<string>();
        public List<QType> impls = new List<QType>();
        public List<QMethod> ctors = new List<QMethod>();
        public List<QProperty> indexers = new List<QProperty>();
        public HashSet<QType> includes = new HashSet<QType>();
        public HashSet<string> externalIncludes = new HashSet<string>();


        public QType(QModule parent, TypeDefinition def, TypeDeclaration decl)
        {
            module = parent;
            this.def = def;
            tdecl = decl;
        }
        public QType(QModule parent, TypeDefinition def, DelegateDeclaration decl)
        {
            module = parent;
            this.def = def;
            this.ddecl = decl;
        }

        public bool IsDelegate
        {
            get
            {
                return ddecl != null;
            }
        }
        public AstNode decl
        {
            get
            {
                if (IsDelegate)
                    return ddecl;
                else
                    return tdecl;
            }
        }
        public void AddMethod(QMethod m)
        {
            methods.Add(m);
        }
        public void AddMethod(MethodDefinition methoddef, MethodDeclaration methoddecl)
        {
            var m = new QMethod(this, methoddef, methoddecl);
            AddMethod(m);
        }
        public void AddMethod(MethodDefinition def, ConstructorDeclaration decl)
        {
            var m = new QMethod(this, def, decl);
            AddCtor(m);
        }
        public QMethod FindMethod(MethodDefinition def)
        {
            List<QMethod> list;
            if (def.IsConstructor)
                list = ctors;
            else
                list = methods;

            foreach (var item in list)
            {
                if (item.def == def)
                    return item;
            }

            return null;
        }

        public QMethod FindMethod(MethodDeclaration decl)
        {
            foreach (var item in methods)
            {
                if (item.decl == decl)
                    return item;
            }
            return null;
        }

        public QMethod FindMethod(ConstructorDeclaration decl)
        {
            foreach (var item in ctors)
            {
                if (item.decl == decl)
                    return item;
            }
            return null;
        }
        public void AddProperty(QProperty p)
        {
            properties.Add(p);
        }
        public void AddProperty(PropertyDefinition pdef, PropertyDeclaration pdecl)
        {
            var p = new QProperty(this, pdef, pdecl);
            AddProperty(p);
        }
        public void AddProperty(PropertyDefinition def, IndexerDeclaration decl)
        {
            var p = new QProperty(this, def, decl);
            AddIndexer(p);
        }
        public QProperty FindProperty(PropertyDefinition def)
        {
            List<QProperty> list;
            if (def.IsIndexer())
                list = indexers;
            else
                list = properties;

            foreach (var item in list)
            {
                if (item.def == def)
                    return item;
            }
            return null;
        }
        public QProperty FindProperty(PropertyDeclaration decl)
        {
            foreach (var item in properties)
            {
                if (item.decl == decl)
                    return item;
            }
            return null;
        }
        public QProperty FindProperty(IndexerDeclaration decl)
        {
            foreach (var item in indexers)
            {
                if (item.decl == decl)
                    return item;
            }
            return null;
        }
        public void AddField(QField f)
        {
            var fdecl = f.decl;
            if (fdecl.HasModifier(Modifiers.Const))
                constFields.Add(f);
            else
                fields.Add(f);
        }
        public void AddField(FieldDefinition fdef, FieldDeclaration fdecl)
        {
            var f = new QField(this, fdef, fdecl);
            AddField(f);
        }
        public QField FindField(FieldDefinition def)
        {
            foreach (var item in fields)
            {
                if (item.def == def)
                    return item;
            }
            return null;
        }
        public QField FindField(FieldDeclaration decl)
        {
            foreach (var item in fields)
            {
                if (item.decl == decl)
                    return item;
            }
            return null;
        }

        public void AddEvent(QEvent e)
        {
            events.Add(e);
        }
        public void AddEvent(EventDefinition edef, EventDeclaration edecl)
        {
            var e = new QEvent(this, edef, edecl);
            AddEvent(e);
        }
        public QEvent FindEvent(EventDefinition def)
        {
            foreach (var item in events)
            {
                if (item.def == def)
                    return item;
            }
            return null;
        }
        public QEvent FindEvent(EventDeclaration decl)
        {
            foreach (var item in events)
            {
                if (item.decl == decl)
                    return item;
            }
            return null;
        }
        public void AddCtor(QMethod m)
        {
            if (m.def.Name == ".cctor")
            {
                if (m.decl.GetText().Contains("LicenseManager"))
                    return;
            }
            ctors.Add(m);
        }
        public void AddCtor(MethodDefinition def, ConstructorDeclaration decl)
        {
            var m = new QMethod(this, def, decl);
            AddCtor(m);
        }

        public void AddIndexer(QProperty m)
        {
            indexers.Add(m);
        }
        public void AddIndexer(PropertyDefinition def, IndexerDeclaration decl)
        {
            var m = new QProperty(this, def, decl);
            AddIndexer(m);
        }

        public void preProcess()
        {
            // process methods
            foreach (var m in methods)
            {
                m.postProgress();
            }

            foreach (var c in ctors)
            {
                c.postProgress();
            }

            // find field readby
            foreach (var f in fields)
            {
                foreach (var ft in module.types)
                {
                    var method = Util.FindFieldUsageInType(ft.def, f.def, true);
                    if (method != null)
                        f.ReadBy.Add(method);
                }
            }

            // find Field AssignBy
            foreach (var f in fields)
            {
                foreach (var ft in module.types)
                {
                    var method = Util.FindFieldUsageInType(ft.def, f.def, false);
                    if (method != null)
                        f.WriteBy.Add(method);
                }
            }
            // find Property ReadBy
            foreach (var p in properties)
            {
                var getter = p.def.GetMethod;
                if (getter == null)
                    continue;
                foreach (var ft in module.types)
                {
                    var method = Util.FindMethodUsageInType(ft.def, getter);
                    if (method != null)
                        p.GetBy.Add(method);
                }
            }
            // find Property Write
            foreach (var p in properties)
            {
                var setter = p.def.SetMethod;
                if (setter == null)
                    continue;
                foreach (var ft in module.types)
                {
                    var method = Util.FindMethodUsageInType(ft.def, setter);
                    if (method != null)
                        p.SetBy.Add(method);
                }
            }

        }

        void FieldRename()
        {

        }
        void PropertyRename()
        {

        }
        public void postProcess()
        {
            FieldRename();
            PropertyRename();
        }

        public void GenerateCode(TextFormatter f)
        {
            GenerateHppCode(f);
            f.WriteLine();
            GenerateCppCode(f);
            f.WriteLine();
            GeneratePrivateCode(f);
        }

        void WriteHppFields(TextFormatter f)
        {

        }
        void WriteHppProperty(TextFormatter f)
        {

        }
        void WriteHppMethod(TextFormatter f)
        {

        }
        void WriteHppCtor(TextFormatter f)
        {

        }
        void WriteHppDtor(TextFormatter f)
        {

        }
        void WriteHppTypeBody(TextFormatter f)
        {

        }
        void WriteHppBase(TextFormatter f)
        {
            bool first = true;
            if (tdecl != null)
            {
                foreach (var item in tdecl.BaseTypes)
                {
                    var dtype = item.ToTypeReference().ToString();
                    if (first)
                    {
                        first = false;
                        f.Write(" :");
                    }
                    else
                        f.Write(",");
                    f.Write(" public " + dtype);
                }
            }
        }

        void WriteHppEnumBody(TextFormatter f)
        {
            var mlist = tdecl.Descendants.OfType<EnumMemberDeclaration>().ToList();
            for (int i = 0; i < mlist.Count(); ++i )
            {
                    f.Write(mlist[i].GetText());
                    if (i != mlist.Count() - 1)
                        f.WriteLine(",");
                    else
                        f.NewLine();
            }
        }
        void WriteHppInterfaceTypeBody(TextFormatter f)
        {
            if (methods.Count() > 0)
            {
                f.Unindent();
                f.WriteLine("public:");
                f.Indent();
                foreach (var item in methods)
                    item.GenerateHppCode(f);
            }
        }

        void WriteHppClassTypeBody(TextFormatter f)
        {
            if (ctors.Count() > 0)
            {
                f.Unindent();
                f.WriteLine("public:");
                f.Indent();
                foreach (var item in ctors)
                    item.GenerateHppCode(f);
                //if (devires.Count() > 0)
                //{
                    f.WriteLine("~" + def.Name + "();");
                //}
                f.NewLine();
            }

            if (methods.Count() > 0 || properties.Count() > 0)
            {
                f.Unindent();
                f.WriteLine("public:");
                f.Indent();
            }

            foreach (var item in methods)
                item.GenerateHppCode(f);

            foreach (var item in properties)
                item.GenerateHppCode(f);

            //foreach (var item in fields)
            //    item.GenerateHppCode(f);

            foreach (var item in constFields)
                item.GenerateHppCode(f);

        }
        void WriteHppTypeHeader(TextFormatter f)
        {
            if (def.IsEnum)
            {
                //enum
                f.WriteLine("enum " + def.Name);
                f.WriteLine("{");
                f.Indent();
                WriteHppEnumBody(f);
                f.Unindent();
                f.WriteLine("}");
            }
            else if (def.IsInterface)
            {
                //interface
                f.Write("class QUANTKIT_EXPORT " + def.Name);
                WriteHppBase(f);
                f.NewLine();
                f.WriteLine("{");
                f.Indent();
                WriteHppInterfaceTypeBody(f);
//                f.NewLine();
                f.Unindent();
                f.WriteLine("}");
            }
            else if (def.IsClass)
            {
                //class
                f.Write("class QUANTKIT_EXPORT " + def.Name);
                WriteHppBase(f);
                f.NewLine();
                f.WriteLine("{");
                f.Indent();
                WriteHppClassTypeBody(f);
//                f.NewLine();
                f.Unindent();
                f.WriteLine("}");
            }
        }
        void WriteHppNameSpace(TextFormatter f)
        {
            string nspace = def.Namespace;
            bool hasNamespace = nspace != null && nspace != "";
            if (hasNamespace)
            {
                f.WriteLine("namespace " + nspace + " {");
                f.NewLine();
                f.WriteLine("namespace Internal { class " + def.Name + "Private; }");
                f.WriteLine();
            }
            WriteHppTypeHeader(f);
            
            if (hasNamespace)
            {
                f.NewLine();
                f.WriteLine("} // " + nspace);
            }
        }
        void WriteHppInclude(TextFormatter f)
        {
            f.WriteLine("#include <QuantKit/quantkit_global.h>");
            foreach (var item in externalIncludes)
            {
                f.Write("#include <");
                f.Write(item);
                f.WriteLine(">");
            }
            f.WriteLine("#include <QSharedDataPointer>");
            foreach(var item in includes)
            {
                f.Write("#include <QuantKit/");
                f.Write(item.def.Name);
                f.WriteLine(".h>");
            }
        }
        void WriteHppHeader(TextFormatter f)
        {
            string hname = "__QUANTKIT_" + def.Name.ToUpper() + "_H__";
            f.Write("#ifndef ");
            f.Write(hname);
            f.NewLine();
            f.Write("#define ");
            f.Write(hname);
            f.NewLine();
            f.NewLine();
            WriteHppInclude(f);
            f.NewLine();
            WriteHppNameSpace(f);
            f.NewLine();
            f.Write("#endif // ");
            f.Write(hname);
            f.NewLine();
        }
        public void GenerateHppCode(TextFormatter f)
        {
                WriteHppHeader(f);
        }

        public void GenerateCppCode(TextFormatter f)
        {
            if (def.IsEnum || def.IsInterface)
                return;
        }

        public void GeneratePrivateCode(TextFormatter f)
        {
            if(def.IsEnum || def.IsInterface)
                return;
        }
    }

    public class QParameter
    {
        public QType type;
        public string typeName;
        public string externalType;
        public string name;
        public string optionValue;
        public QMethod parent;
        public bool isPrimitive = false;
        public bool isEnum = false;
        //public bool isCtor = false;
        public QParameter(QMethod method)
        {
            this.parent = method;
        }
    }

    public class QMethod : GenerateCodeAbled
    {
        public MethodDefinition def;
        MethodDeclaration mdecl = null;
        ConstructorDeclaration cdecl = null;
        public QType parent;
        public List<QParameter> parameters = new List<QParameter>();
        public List<string> body = new List<string>();

        public bool IsConstructor
        {
            get
            {
                return def.IsConstructor;
            }
        }
        public AstNode decl
        {
            get
            {
                if (def.IsConstructor)
                    return cdecl;
                return mdecl;
            }
        }
        public QMethod(QType parent, MethodDefinition methoddef, MethodDeclaration methoddecl)
        {
            this.parent = parent;
            def = methoddef;
            mdecl = methoddecl;
        }

        public QMethod(QType parent, MethodDefinition methoddef, ConstructorDeclaration methoddecl)
        {
            this.parent = parent;
            def = methoddef;
            cdecl = methoddecl;
        }

        public void GenerateCode(TextFormatter f)
        {
            GenerateHppCode(f);
            f.WriteLine();
            GenerateCppCode(f);
            f.WriteLine();
            GeneratePrivateCode(f);
        }

        void processBody()
        {
            var csharpText = new StringWriter();
            var csharpoutput = new PlainTextOutput(csharpText);
            var outputFormatter = new TextOutputFormatter(csharpoutput) { FoldBraces = true };
            decl.AcceptVisitor(new CSharpOutputVisitor(outputFormatter, FormattingOptionsFactory.CreateAllman()));
            var blist = decl.Descendants.OfType<BlockStatement>().ToList();

            string b = csharpText.ToString();
            var bb = b.Replace("\r\n", "\n");
            var bodylist = bb.Split('\n');
            var tlist = new List<string>();
            foreach (var item in bodylist)
            {
                var stat = item.Trim();
                tlist.Add(stat);
            }
            tlist.RemoveAt(0); // delete method define
            if (tlist.Count() > 0)
            {
                if (tlist[0] == "{")
                    tlist.RemoveAt(0);

                while (tlist.Count() > 0)
                {
                    if (tlist[tlist.Count() - 1] == "")
                        tlist.RemoveAt(tlist.Count() - 1);
                    else
                        break;
                }

                if (tlist.Count() > 0)
                {
                    if (tlist[tlist.Count() - 1] == "}")
                        tlist.RemoveAt(tlist.Count() - 1);
                }
            }
            body = tlist;
        }

        void processParameters()
        {
            var plist = decl.Descendants.OfType<ParameterDeclaration>().ToList();
            foreach (var item in plist)
            {
                var p = new QParameter(this);

                bool isPrimitive;
                string name = item.Name.Trim();
                string type = Util.TypeToString(item.Type, out isPrimitive).Trim();

                bool isEnum = false;
                QType moduleType = null;
                var ptype = Util.GetTypeRef(item.Type) as TypeDefinition;
                if (ptype != null)
                {
                    moduleType = parent.module.FindType(ptype);
                    if (moduleType != null)
                        isEnum = moduleType.def.IsEnum;
                }

                p.name = name;
                p.type = moduleType;
                p.typeName = type;
                p.isEnum = isEnum;
                p.isPrimitive = isPrimitive;

                if (!p.isPrimitive)
                {
                    if (p.type != null)
                    {
                        parent.includes.Add(p.type);
                    }
                    else
                        parent.externalIncludes.Add(p.typeName);
                }

                var split = item.GetText().Split('=');
                string optionValue = null;
                if (split.Count() > 1)
                    optionValue = split[split.Count() - 1].Trim();

                // special process
                if (optionValue != null && name == "currencyId" && optionValue == "148")
                    optionValue = "currencyId.USD";

                if (optionValue != null)
                {
                    p.optionValue = optionValue;
                }
                parameters.Add(p);
            }
        }

        public void postProgress()
        {
            processBody();
            processParameters();
        }

        public void WriteParameters(TextFormatter f)
        {
            bool first = true;
            f.Write("(");
            foreach (var p in parameters)
            {
                string type;
                if (!(p.isPrimitive || p.isEnum))
                {
                    type = "const " + p.typeName + "&";
                }
                else
                    type = p.typeName;
                
                if (first)
                {
                    first = false;
                }
                else
                    f.Write(", ");
                f.Write(type); f.Space();
                f.Write(p.name);

                if (p.optionValue != null)
                {
                    f.Write(" = ");
                    f.Write(p.optionValue);
                }
            }
            f.Write(")");
        }
        void WritePublicBody(TextFormatter f)
        {
            f.WriteLine("{");
            f.Indent();
            foreach (var item in body)
                f.WriteLine(item);
            f.Unindent();
            f.WriteLine("}");
        }
        public void GenerateHppCode(TextFormatter f)
        {
            if (mdecl != null)
            {
                if (def.IsVirtual || parent.def.IsInterface)
                    f.Write("virtual ");
                bool isPrimitive;
                string type = Util.TypeToString(mdecl.ReturnType, out isPrimitive);
                f.Write(type);
                f.Space();
                f.Write(def.Name);
                f.Space();
                WriteParameters(f); 
                f.WriteLine(";");
                //WriteBody(f);
            }
            else // ctor
            {
                if (parameters.Count() == 1)
                    f.Write("explicit ");
                f.Write(parent.def.Name);
                f.Space();
                WriteParameters(f); 
                f.WriteLine(";");
                //WriteBody(f);
            }
        }

        public void GenerateCppCode(TextFormatter f)
        {

        }

        public void GeneratePrivateCode(TextFormatter f)
        {

        }
    }

    public class QProperty : GenerateCodeAbled
    {
        public PropertyDefinition def;
        PropertyDeclaration pdecl = null;
        IndexerDeclaration idecl = null;
        public QType parent;

        public List<MethodDefinition> GetBy = new List<MethodDefinition>();
        public List<MethodDefinition> SetBy = new List<MethodDefinition>();

        public bool IsIndexer
        {
            get
            {
                return def.IsIndexer();
            }
        }

        public AstNode decl
        {
            get
            {
                if (def.IsIndexer())
                    return idecl;
                else
                    return pdecl;
            }
        }
        public QProperty(QType parent, PropertyDefinition propdef, PropertyDeclaration propdecl)
        {
            this.parent = parent;
            def = propdef;
            pdecl = propdecl;
        }

        public QProperty(QType parent, PropertyDefinition def, IndexerDeclaration decl)
        {
            this.parent = parent;
            this.def = def;
            this.idecl = decl;
        }

        public void GenerateCode(TextFormatter f)
        {
            GenerateHppCode(f);
            f.WriteLine();
            GenerateCppCode(f);
            f.WriteLine();
            GeneratePrivateCode(f);
        }

        public static void WriteParameters(AstNode node, QModule module, TextFormatter f)
        {
            var plist = node.Descendants.OfType<ParameterDeclaration>().ToList();
            bool first = true;
            foreach (var item in plist)
            {
                bool isPrimitive;
                string name = item.Name.Trim();
                string type = Util.TypeToString(item.Type, out isPrimitive).Trim();

                bool isEnum = false;
                QType moduleType = null;
                var ptype = Util.GetTypeRef(item.Type) as TypeDefinition;
                if (ptype != null)
                {
                    moduleType = module.FindType(ptype);
                    if (moduleType != null)
                        isEnum = moduleType.def.IsEnum;
                }


                if (!(isPrimitive || isEnum))
                {
                    type = "const " + type + "&";
                }
                var split = item.GetText().Split('=');
                string optionValue = null;
                if (split.Count() > 1)
                    optionValue = split[split.Count() - 1].Trim();
                if (first)
                {
                    first = false;
                }
                else
                    f.Write(", ");
                f.Write(type); f.Space();
                f.Write(name);
                if (optionValue != null && name == "currencyId" && optionValue == "148")
                    optionValue = "currencyId.USD";
                if (optionValue != null)
                {
                    f.Write(" = ");
                    f.Write(optionValue);
                }
            }
        }

        public void GenerateHppCode(TextFormatter f)
        {
            if (IsIndexer)
            {
                var getter = idecl.Getter;
                if (!getter.IsNull && !getter.HasModifier(Modifiers.Private))
                {
                    bool isPrimitive;
                    string type = Util.TypeToString(idecl.ReturnType, out isPrimitive);
                    f.Write(type);
                    f.Write(" getItem"  + "(");
                    WriteParameters(idecl, parent.module, f);
                    f.WriteLine(");");
                }
                var setter = idecl.Setter;
                if (!setter.IsNull && !setter.HasModifier(Modifiers.Private))
                {
                    bool isPrimitive;
                    string type = Util.TypeToString(idecl.ReturnType, out isPrimitive);
                    if(!isPrimitive)
                        type = "const " +type +"& value";
                    f.Write("void setItem" + "(");
                    WriteParameters(idecl, parent.module, f);
                    f.WriteLine(");");
                }          
            }
            else
            {
                var getter = pdecl.Getter;
                if (!getter.IsNull && !getter.HasModifier(Modifiers.Private) && !pdecl.HasModifier(Modifiers.Override))
                {
                    bool isPrimitive;
                    string type = Util.TypeToString(pdecl.ReturnType, out isPrimitive);
                    f.Write(type);
                    f.WriteLine(" get" + def.Name + "();");
                }
                var setter = pdecl.Setter;
                if (!setter.IsNull && !setter.HasModifier(Modifiers.Private) && !pdecl.HasModifier(Modifiers.Override))
                {
                    bool isPrimitive;
                    string type = Util.TypeToString(pdecl.ReturnType, out isPrimitive);
                    if(!isPrimitive)
                        type = "const " +type +"&";
                     f.WriteLine("void set" + def.Name + "(" + type+" value);");
                }          
            }
        }

        public void GenerateCppCode(TextFormatter f)
        {

        }

        public void GeneratePrivateCode(TextFormatter f)
        {

        }
    }

    public class QField : GenerateCodeAbled
    {
        public FieldDefinition def;
        public FieldDeclaration decl;
        public QType parent;
        public List<MethodDefinition> ReadBy = new List<MethodDefinition>();
        public List<MethodDefinition> WriteBy = new List<MethodDefinition>();

        public string FieldName;

        public TypeReference FieldType
        {
            get
            {
                return def.FieldType;
            }
        }

        public string Name
        {
            get
            {
                return def.Name;
            }
        }
        public bool IsConst
        {
            get { return decl.HasModifier(Modifiers.Const); }
        }
        public bool IsPublic
        {
            get { return decl.HasModifier(Modifiers.Public); }
        }
        public bool IsInternal
        {
            get { return decl.HasModifier(Modifiers.Internal); }
        }
        public bool hasOptionValue
        {
            get { return decl.Descendants.OfType<VariableInitializer>().ToList().Count() > 0; }
        }

        public string optionValue
        {
            get
            {
                if (hasOptionValue)
                {
                    var ilist = decl.Descendants.OfType<VariableInitializer>().ToList();
                        var ovalue = ilist[0];
                        var s = ovalue.GetText();
                        var slist = s.Split('=');
                        //if (slist.Count() > 1)
                        //{
                            return slist[slist.Count() - 1];
                        //}
                        //else return "";
                }
                else return "";
            }
        }
        public QField(QType parent, FieldDefinition def, FieldDeclaration decl)
        {
            this.parent = parent;
            this.def = def;
            this.decl = decl;
            this.FieldName = def.Name;
        }

        public void Rename(string name)
        {

        }
        public void GenerateCode(TextFormatter f)
        {
            GenerateHppCode(f);
            f.WriteLine();
            GenerateCppCode(f);
            f.WriteLine();
            GeneratePrivateCode(f);
        }

        public void GenerateHppCode(TextFormatter f)
        {
            if (def.IsCompilerGenerated()) return;
            if (decl.HasModifier(Modifiers.Public) || decl.HasModifier(Modifiers.Internal) || decl.HasModifier(Modifiers.Const))
            {
                if (decl.HasModifier(Modifiers.Const))
                    f.Write("const ");
                f.WriteType(FieldType);
                f.Space();
                f.Write(Name);
                if(hasOptionValue){
                    f.Write(" = ");
                    f.Write(optionValue);
                }
                f.Write(";");
                f.NewLine();
            }
        }

        public void GenerateCppCode(TextFormatter f)
        {

        }

        public void GeneratePrivateCode(TextFormatter f)
        {
            if (decl.HasModifier(Modifiers.Public) || decl.HasModifier(Modifiers.Internal))
            {
                f.WriteType(FieldType);
                f.Space();
                f.Write(Name);
                f.Write(";");
                f.NewLine();
            }
        }
    }

    public class QEvent : GenerateCodeAbled
    {
        public EventDefinition def;
        public EventDeclaration decl;
        public QType parent;
        public QEvent(QType parent, EventDefinition def, EventDeclaration decl)
        {
            this.parent = parent;
            this.def = def;
            this.decl = decl;
        }

        public void GenerateCode(TextFormatter f)
        {
            GenerateHppCode(f);
            f.WriteLine();
            GenerateCppCode(f);
            f.WriteLine();
            GeneratePrivateCode(f);
        }

        public void GenerateHppCode(TextFormatter f)
        {
            f.Indent();
            //formatter.Write()
            f.Unindent();
        }

        public void GenerateCppCode(TextFormatter f)
        {

        }

        public void GeneratePrivateCode(TextFormatter f)
        {
           // output.WriteLine(FieldType.Name + " " + Name + ";");
        }
    }

    #region Util
    class Util
    {
        public static TypeReference GetTypeRef(AstNode expr)
        {
            var td = expr.Annotation<TypeDefinition>();
            if (td != null)
            {
                return td;
            }

            var tr = expr.Annotation<TypeReference>();
            if (tr != null)
            {
                return tr;
            }

            var ti = expr.Annotation<ICSharpCode.Decompiler.Ast.TypeInformation>();
            if (ti != null)
            {
                return ti.InferredType;
            }

            var ilv = expr.Annotation<ICSharpCode.Decompiler.ILAst.ILVariable>();
            if (ilv != null)
            {
                return ilv.Type;
            }

            var fr = expr.Annotation<FieldDefinition>();
            if (fr != null)
            {
                return fr.FieldType;
            }

            var pr = expr.Annotation<PropertyDefinition>();
            if (pr != null)
            {
                return pr.PropertyType;
            }

            var ie = expr as IndexerExpression;
            if (ie != null)
            {
                var it = GetTypeRef(ie.Target);
                if (it != null && it.IsArray)
                {
                    return it.GetElementType();
                }
            }

            return null;
        }

        public static TypeReference GetTargetTypeRef(MemberReferenceExpression memberReferenceExpression)
        {
            var pd = memberReferenceExpression.Annotation<PropertyDefinition>();
            if (pd != null)
            {
                return pd.DeclaringType;
            }

            var fd = memberReferenceExpression.Annotation<FieldDefinition>();
            if (fd == null)
                fd = memberReferenceExpression.Annotation<FieldReference>() as FieldDefinition;
            if (fd != null)
            {
                return fd.DeclaringType;
            }

            return GetTypeRef(memberReferenceExpression.Target);
        }
        public static string TypeToString(TypeReference type, out bool isPrimitiveType)
        {
            string result = type.Name;
            isPrimitiveType = false;
            if(type.IsPrimitive)
            {
                isPrimitiveType = true;
                switch (result)
                {
                    case "SByte":
                        result = "char";
                        break;
                    case "Byte":
                        result = "unsigned char";
                        break;
                    case "Boolean":
                        result = "bool";
                        break;
                    case "Int16":
                        result = "short";
                        break;
                    case "UInt16":
                        result = "unsigned short";
                        break;
                    case "Int32":
                        result = "int";
                        break;
                    case "UInt32":
                        result = "unsigned int";
                        break;
                    case "Int64":
                        result = "long";
                        break;
                    case "UInt64":
                        result = "unsigned long";
                        break;
                    case "Single":
                    case "Double":
                        result = "double";
                        break;
                    case "Char":
                        result = "char";
                        break;
                    default:
                        break;
                }
            }

            if (result == "String")
                result = "QString";
            else if (result == "DateTime")
                result = "QDateTime";
            else if (result == "Void")
                result = "void";

            return result;
        }
        public static string TypeToString(AstType type, out bool isPrimitiveType)
        {
            string result = type.GetText();
            isPrimitiveType = false;
            PrimitiveType pType = type as PrimitiveType;
            if (pType != null)
            {
                isPrimitiveType = true;
                var code = pType.KnownTypeCode;
                switch (code)
                {
                    case KnownTypeCode.SByte:
                        result = "char";
                        break;
                    case KnownTypeCode.Byte:
                        result = "unsigned char";
                        break;
                    case KnownTypeCode.String:
                        result = "QString";
                        isPrimitiveType = false;
                        break;
                    case KnownTypeCode.DateTime:
                        result = "QDateTime";
                        isPrimitiveType = false;
                        break;
                    default:
                        result = type.GetText();
                        break;
                }
            }

            if (result == "DateTime")
            {
                result = "QDateTime";
            }

            return result;
        }

        public static string TypeToString(TypeReference type, ICustomAttributeProvider typeAttributes = null)
        {
            if (type == null)
                return "NULL TYPE";

            ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;// | ConvertTypeOptions.IncludeNamespace;

            AstType astType = AstBuilder.ConvertType(type, typeAttributes, options);

            StringWriter w = new StringWriter();
            if (type.IsByReference)
            {
                ParameterDefinition pd = typeAttributes as ParameterDefinition;
                if (pd != null && (!pd.IsIn && pd.IsOut))
                    w.Write("out ");
                else
                    w.Write("ref ");

                if (astType is ComposedType && ((ComposedType)astType).PointerRank > 0)
                    ((ComposedType)astType).PointerRank--;
            }

            astType.AcceptVisitor(new CSharpOutputVisitor(w, FormattingOptionsFactory.CreateAllman()));
            return w.ToString();
        }

        public static string GetTargetTypeString(MemberReferenceExpression memberReferenceExpression)
        {
            TypeReference reference = GetTargetTypeRef(memberReferenceExpression);
            return TypeToString(reference);
        }

        public static bool IsReferencedBy(TypeDefinition type, TypeReference typeRef)
        {
            // TODO: move it to a better place after adding support for more cases.
            if (type == null)
                throw new ArgumentNullException("type");
            if (typeRef == null)
                throw new ArgumentNullException("typeRef");

            if (type == typeRef)
                return true;
            if (type.Name != typeRef.Name)
                return false;
            if (type.Namespace != typeRef.Namespace)
                return false;

            if (type.DeclaringType != null || typeRef.DeclaringType != null)
            {
                if (type.DeclaringType == null || typeRef.DeclaringType == null)
                    return false;
                if (!IsReferencedBy(type.DeclaringType, typeRef.DeclaringType))
                    return false;
            }

            return true;
        }

        public static MemberReference GetOriginalCodeLocation(MemberReference member)
        {
            if (member is MethodDefinition)
                return GetOriginalCodeLocation((MethodDefinition)member);
            return member;
        }

        public static MethodDefinition GetOriginalCodeLocation(MethodDefinition method)
        {
            if (method.IsCompilerGenerated())
            {
                return FindMethodUsageInType(method.DeclaringType, method) ?? method;
            }

            var typeUsage = GetOriginalCodeLocation(method.DeclaringType);

            return typeUsage ?? method;
        }

        /// <summary>
        /// Given a compiler-generated type, returns the method where that type is used.
        /// Used to detect the 'parent method' for a lambda/iterator/async state machine.
        /// </summary>
        public static MethodDefinition GetOriginalCodeLocation(TypeDefinition type)
        {
            if (type != null && type.DeclaringType != null && type.IsCompilerGenerated())
            {
                if (type.IsValueType)
                {
                    // Value types might not have any constructor; but they must be stored in a local var
                    // because 'initobj' (or 'call .ctor') expects a managed ref.
                    return FindVariableOfTypeUsageInType(type.DeclaringType, type);
                }
                else
                {
                    MethodDefinition constructor = GetTypeConstructor(type);
                    if (constructor == null)
                        return null;
                    return FindMethodUsageInType(type.DeclaringType, constructor);
                }
            }
            return null;
        }

        public static MethodDefinition GetTypeConstructor(TypeDefinition type)
        {
            return type.Methods.FirstOrDefault(method => method.Name == ".ctor");
        }

        public static MethodDefinition FindMethodUsageInType(TypeDefinition type, MethodDefinition analyzedMethod)
        {
            string name = analyzedMethod.Name;
            foreach (MethodDefinition method in type.Methods)
            {
                bool found = false;
                if (!method.HasBody)
                    continue;
                foreach (Instruction instr in method.Body.Instructions)
                {
                    MethodReference mr = instr.Operand as MethodReference;
                    if (mr != null && mr.Name == name &&
                        IsReferencedBy(analyzedMethod.DeclaringType, mr.DeclaringType) &&
                        mr.Resolve() == analyzedMethod)
                    {
                        found = true;
                        break;
                    }
                }

                method.Body = null;

                if (found)
                    return method;
            }
            return null;
        }
        public static bool CanBeReadReference(Code code)
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
        public static bool CanBeAssignReference(Code code)
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
        public static MethodDefinition FindFieldUsageInType(TypeDefinition type, FieldDefinition analyzedField, bool ReadByOrAssignBy /*true is Read false is Assign*/)
        {
            string name = analyzedField.Name;
            foreach (MethodDefinition method in type.Methods)
            {
                bool found = false;
                if (!method.HasBody)
                    continue;
                foreach (Instruction instr in method.Body.Instructions)
                {
                    bool isAccessBy;
                    if (ReadByOrAssignBy)
                        isAccessBy = CanBeReadReference(instr.OpCode.Code);
                    else
                        isAccessBy = CanBeAssignReference(instr.OpCode.Code);
                    if (isAccessBy)
                    {
                        FieldReference mr = instr.Operand as FieldReference;
                        if (mr != null && mr.Name == name &&
                            IsReferencedBy(analyzedField.DeclaringType, mr.DeclaringType) &&
                            mr.Resolve() == analyzedField)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                method.Body = null;

                if (found)
                    return method;
            }
            return null;
        }

        public static MethodDefinition FindVariableOfTypeUsageInType(TypeDefinition type, TypeDefinition variableType)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                bool found = false;
                if (!method.HasBody)
                    continue;
                foreach (var v in method.Body.Variables)
                {
                    if (v.VariableType.ResolveWithinSameModule() == variableType)
                    {
                        found = true;
                        break;
                    }
                }

                method.Body = null;

                if (found)
                    return method;
            }
            return null;
        }

        public static TypeDefinition GetTypeDef(AstNode expr)
        {
            var tr = GetTypeRef(expr);
            var td = tr as TypeDefinition;
            if (td == null && tr != null)
                td = tr.Resolve();
            return td;
        }

        public static string GetClassName(AstNode expr)
        {
            string className = "";
            var plist = expr.Ancestors.OfType<TypeDeclaration>().ToList();
            if (plist.Count > 0)
            {
                className = plist[0].Name;
            }
            return className;
        }

        public static string GetClassBaseName(AstNode expr)
        {
            string baseName = "";
            var typedecl = expr as TypeDeclaration;

            if (typedecl == null)
            {
                var plist = expr.Ancestors.OfType<TypeDeclaration>().ToList();
                if (plist.Count > 0)
                {
                    TypeDeclaration type = plist[0];
                    var blist = type.BaseTypes.ToList();
                    if (blist.Count() > 0)
                    {
                        baseName = blist[0].GetText();
                    }
                }
            }
            else
            {
                var blist = typedecl.BaseTypes.ToList();
                if (blist.Count() > 0)
                {
                    baseName = blist[0].GetText();
                }
            }
            return baseName;
        }
    }

    #endregion
    #region visitor
    class QModuleVisitor : DepthFirstAstVisitor
    {
        QModule module;
        public QModuleVisitor(QModule module)
        {
            this.module = module;
        }

        public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            base.VisitTypeDeclaration(typeDeclaration);
            var anon = typeDeclaration.Annotation<TypeDefinition>();
            if (anon != null)
            {
                var t = new QType(module, anon, typeDeclaration);
                module.AddType(t);
                typeDeclaration.AddAnnotation(t);
            }
        }

        public override void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
        {
            base.VisitDelegateDeclaration(delegateDeclaration);
            var anon = delegateDeclaration.Annotation<TypeDefinition>();
            if (anon != null)
            {
                var t = new QType(module, anon, delegateDeclaration);
                module.AddType(t);
                delegateDeclaration.AddAnnotation(t);
            }
        }
    }

    class QTypeVisitor : DepthFirstAstVisitor
    {
        QType type;
        public QTypeVisitor(QType type)
        {
            this.type = type;
        }

        public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            base.VisitMethodDeclaration(methodDeclaration);
            var anon = methodDeclaration.Annotation<MethodDefinition>();
            if (anon != null)
            {
                var m = new QMethod(type, anon, methodDeclaration);
                type.AddMethod(m);
                methodDeclaration.AddAnnotation(m);
            }
        }

        public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
        {
            base.VisitPropertyDeclaration(propertyDeclaration);
            var anon = propertyDeclaration.Annotation<PropertyDefinition>();
            if (anon != null)
            {
                var p = new QProperty(type, anon, propertyDeclaration);
                type.AddProperty(p);
                propertyDeclaration.AddAnnotation(p);
            }
        }

        public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
        {
            base.VisitFieldDeclaration(fieldDeclaration);
            var anon = fieldDeclaration.Annotation<FieldDefinition>();
            if (anon != null)
            {
                var f = new QField(type, anon, fieldDeclaration);
                type.AddField(f);
                fieldDeclaration.AddAnnotation(f);
            }
        }

        public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
        {
            base.VisitEventDeclaration(eventDeclaration);
            var anon = eventDeclaration.Annotation<EventDefinition>();
            if (anon != null)
            {
                var e = new QEvent(type, anon, eventDeclaration);
                type.AddEvent(e);
                eventDeclaration.AddAnnotation(e);
            }
        }

        public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
        {
            base.VisitConstructorDeclaration(constructorDeclaration);
            var anon = constructorDeclaration.Annotation<MethodDefinition>();
            if (anon != null)
            {
                var m = new QMethod(type, anon, constructorDeclaration);
                type.AddCtor(m);
                constructorDeclaration.AddAnnotation(m);
            }
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
        {
            base.VisitNamespaceDeclaration(namespaceDeclaration);
            type.ns = namespaceDeclaration;
        }

        public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
        {
            base.VisitIndexerDeclaration(indexerDeclaration);
            var anon = indexerDeclaration.Annotation<PropertyDefinition>();
            if (anon != null)
            {
                var p = new QProperty(type, anon, indexerDeclaration);
                type.AddIndexer(p);
                indexerDeclaration.AddAnnotation(p);
            }
        }
    }
#endregion

#region formatter
    public class TextFormatter : IDisposable
    {
        public readonly ITextOutput output;
        int indentation;
        bool needsIndent = true;
        //bool isAtStartOfLine = true;
        int line, column;

        public int Indentation
        {
            get { return this.indentation; }
            set { this.indentation = value; }
        }

        public TextLocation Location
        {
            get { return new TextLocation(line, column + (needsIndent ? indentation * IndentationString.Length : 0)); }
        }

        public string IndentationString { get; set; }

        public TextFormatter(ITextOutput output)
        {
            if (output == null)
                throw new ArgumentNullException("output");
            this.output = output;
            this.IndentationString = "\t";
            this.line = 1;
            this.column = 1;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Write(string content)
        {
            WriteIndentation();
            output.Write(content);
            column += content.Length;
            //isAtStartOfLine = false;
        }

        public void WriteLine(string content)
        {
            Write(content);
            NewLine();
        }
        public void WriteLine()
        {
            NewLine();
        }
        public void WriteType(TypeReference r)
        {
            bool isPrimitive;
            string real = Util.TypeToString(r, out isPrimitive);
            Write(real);
        }

        public void Space()
        {
            WriteIndentation();
            column++;
            output.Write(' ');
        }

        protected void WriteIndentation()
        {
            if (needsIndent)
            {
                needsIndent = false;
                for (int i = 0; i < indentation; i++)
                {
                    output.Write(this.IndentationString);
                }
                column += indentation * IndentationString.Length;
            }
        }

        public void NewLine()
        {
            output.WriteLine();
            column = 1;
            line++;
            needsIndent = true;
            //isAtStartOfLine = true;
        }

        public void Indent()
        {
            indentation++;
        }

        public void Unindent()
        {
            indentation--;
        }

        public void WriteComment(CommentType commentType, string content)
        {
            WriteIndentation();
            switch (commentType)
            {
                case CommentType.SingleLine:
                    output.Write("//");
                    output.WriteLine(content);
                    column += 2 + content.Length;
                    needsIndent = true;
                    //isAtStartOfLine = true;
                    break;
                case CommentType.MultiLine:
                    output.Write("/*");
                    output.Write(content);
                    output.Write("*/");
                    column += 2;
                    UpdateEndLocation(content, ref line, ref column);
                    column += 2;
                    //isAtStartOfLine = false;
                    break;
                case CommentType.Documentation:
                    output.Write("///");
                    output.WriteLine(content);
                    column += 3 + content.Length;
                    needsIndent = true;
                    //isAtStartOfLine = true;
                    break;
                case CommentType.MultiLineDocumentation:
                    output.Write("/**");
                    output.Write(content);
                    output.Write("*/");
                    column += 3;
                    UpdateEndLocation(content, ref line, ref column);
                    column += 2;
                    //isAtStartOfLine = false;
                    break;
                default:
                    output.Write(content);
                    column += content.Length;
                    break;
            }
        }

        static void UpdateEndLocation(string content, ref int line, ref int column)
        {
            if (string.IsNullOrEmpty(content))
                return;
            for (int i = 0; i < content.Length; i++)
            {
                char ch = content[i];
                switch (ch)
                {
                    case '\r':
                        if (i + 1 < content.Length && content[i + 1] == '\n')
                            i++;
                        goto case '\n';
                    case '\n':
                        line++;
                        column = 0;
                        break;
                }
                column++;
            }
        }

        /*public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
        {
            // pre-processor directive must start on its own line
            if (!isAtStartOfLine)
                NewLine();
            WriteIndentation();
            output.Write('#');
            string directive = type.ToString().ToLowerInvariant();
            output.Write(directive);
            column += 1 + directive.Length;
            if (!string.IsNullOrEmpty(argument))
            {
                output.Write(' ');
                output.Write(argument);
                column += 1 + argument.Length;
            }
            NewLine();
        }
        */
        /*public override void WritePrimitiveValue(object value, string literalValue = null)
        {
            if (literalValue != null)
            {
                output.Write(literalValue);
                column += literalValue.Length;
                return;
            }

            if (value == null)
            {
                // usually NullReferenceExpression should be used for this, but we'll handle it anyways
                output.Write("null");
                column += 4;
                return;
            }

            if (value is bool)
            {
                if ((bool)value)
                {
                    output.Write("true");
                    column += 4;
                }
                else
                {
                    output.Write("false");
                    column += 5;
                }
                return;
            }

            if (value is string)
            {
                string tmp = "\"" + ConvertString(value.ToString()) + "\"";
                column += tmp.Length;
                output.Write(tmp);
            }
            else if (value is char)
            {
                string tmp = "'" + ConvertCharLiteral((char)value) + "'";
                column += tmp.Length;
                output.Write(tmp);
            }
            else if (value is decimal)
            {
                string str = ((decimal)value).ToString(NumberFormatInfo.InvariantInfo) + "m";
                column += str.Length;
                output.Write(str);
            }
            else if (value is float)
            {
                float f = (float)value;
                if (float.IsInfinity(f) || float.IsNaN(f))
                {
                    // Strictly speaking, these aren't PrimitiveExpressions;
                    // but we still support writing these to make life easier for code generators.
                    output.Write("float");
                    column += 5;
                    Write(".");
                    if (float.IsPositiveInfinity(f))
                    {
                        output.Write("PositiveInfinity");
                        column += "PositiveInfinity".Length;
                    }
                    else if (float.IsNegativeInfinity(f))
                    {
                        output.Write("NegativeInfinity");
                        column += "NegativeInfinity".Length;
                    }
                    else
                    {
                        output.Write("NaN");
                        column += 3;
                    }
                    return;
                }
                if (f == 0 && 1 / f == float.NegativeInfinity)
                {
                    // negative zero is a special case
                    // (again, not a primitive expression, but it's better to handle
                    // the special case here than to do it in all code generators)
                    output.Write("-");
                    column++;
                }
                var str = f.ToString("R", NumberFormatInfo.InvariantInfo) + "f";
                column += str.Length;
                output.Write(str);
            }
            else if (value is double)
            {
                double f = (double)value;
                if (double.IsInfinity(f) || double.IsNaN(f))
                {
                    // Strictly speaking, these aren't PrimitiveExpressions;
                    // but we still support writing these to make life easier for code generators.
                    output.Write("double");
                    column += 6;
                    Write(".");
                    if (double.IsPositiveInfinity(f))
                    {
                        output.Write("PositiveInfinity");
                        column += "PositiveInfinity".Length;
                    }
                    else if (double.IsNegativeInfinity(f))
                    {
                        output.Write("NegativeInfinity");
                        column += "NegativeInfinity".Length;
                    }
                    else
                    {
                        output.Write("NaN");
                        column += 3;
                    }
                    return;
                }
                if (f == 0 && 1 / f == double.NegativeInfinity)
                {
                    // negative zero is a special case
                    // (again, not a primitive expression, but it's better to handle
                    // the special case here than to do it in all code generators)
                    output.Write("-");
                }
                string number = f.ToString("R", NumberFormatInfo.InvariantInfo);
                if (number.IndexOf('.') < 0 && number.IndexOf('E') < 0)
                {
                    number += ".0";
                }
                output.Write(number);
            }
            else if (value is IFormattable)
            {
                StringBuilder b = new StringBuilder();
                //				if (primitiveExpression.LiteralFormat == LiteralFormat.HexadecimalNumber) {
                //					b.Append("0x");
                //					b.Append(((IFormattable)val).ToString("x", NumberFormatInfo.InvariantInfo));
                //				} else {
                b.Append(((IFormattable)value).ToString(null, NumberFormatInfo.InvariantInfo));
                //				}
                if (value is uint || value is ulong)
                {
                    b.Append("u");
                }
                if (value is long || value is ulong)
                {
                    b.Append("L");
                }
                output.Write(b.ToString());
                column += b.Length;
            }
            else
            {
                output.Write(value.ToString());
                column += value.ToString().Length;
            }
        }

        /// <summary>
        /// Gets the escape sequence for the specified character within a char literal.
        /// Does not include the single quotes surrounding the char literal.
        /// </summary>
        public static string ConvertCharLiteral(char ch)
        {
            if (ch == '\'')
            {
                return "\\'";
            }
            return ConvertChar(ch);
        }

        /// <summary>
        /// Gets the escape sequence for the specified character.
        /// </summary>
        /// <remarks>This method does not convert ' or ".</remarks>
        static string ConvertChar(char ch)
        {
            switch (ch)
            {
                case '\\':
                    return "\\\\";
                case '\0':
                    return "\\0";
                case '\a':
                    return "\\a";
                case '\b':
                    return "\\b";
                case '\f':
                    return "\\f";
                case '\n':
                    return "\\n";
                case '\r':
                    return "\\r";
                case '\t':
                    return "\\t";
                case '\v':
                    return "\\v";
                default:
                    if (char.IsControl(ch) || char.IsSurrogate(ch) ||
                        // print all uncommon white spaces as numbers
                        (char.IsWhiteSpace(ch) && ch != ' '))
                    {
                        return "\\u" + ((int)ch).ToString("x4");
                    }
                    else
                    {
                        return ch.ToString();
                    }
            }
        }

        /// <summary>
        /// Converts special characters to escape sequences within the given string.
        /// </summary>
        public static string ConvertString(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in str)
            {
                if (ch == '"')
                {
                    sb.Append("\\\"");
                }
                else
                {
                    sb.Append(ConvertChar(ch));
                }
            }
            return sb.ToString();
        }*/
    }
#endregion
}
