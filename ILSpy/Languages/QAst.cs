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
    /*public interface GenerateCodeAbled
    {
        void GenerateCode(TextFormatter f);
        void GenerateHppCode(TextFormatter f);
        void GenerateCppCode(TextFormatter f);
        void GeneratePrivateCode(TextFormatter f);
    }*/

    public class QModule 
    {
        public ModuleDefinition def;
        public List<QType> types = new List<QType>();
        public AstBuilder dom;
        public CModule cmodule;

        public QModule(ModuleDefinition module, DecompilationOptions option)
        {
            def = module;
            dom = CreateAstBuilder(module, option);
            dom.AddAssembly(module.Assembly, onlyAssemblyLevel: false);
            cmodule = new CModule(this);
            var mv = new QModuleVisitor(this);
            dom.SyntaxTree.AcceptVisitor(mv);

            foreach (var t in types)
            {
                var tv = new QTypeVisitor(t);
                t.decl.AcceptVisitor(tv);
            }
            dom.SyntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            PostProcess();
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
        public QType FindType(string tname)
        {
            foreach(var item in types)
            {
                if (item.def.Name == tname)
                    return item;
            }
            return null;
        }
        public QMethod FindMethod(MethodDefinition m)
        {
            QType t = FindType(m.DeclaringType);
            return t.FindMethod(m);
        }

        public CMethod FindCMethod(MethodDefinition m)
        {
            QType t = FindType(m.DeclaringType);
            return t.FindCMethod(m);
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
                t.GenerateSyntaxTree();
            }

            foreach(var t in types)
            {
                t.postProcess();
            }
        }

        /*public void GenerateCode(TextFormatter f)
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

        }*/
    }

    

    public class QType
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

        //public List<CField> cfields = new List<CField>();
        //public List<CMethod> cmethods = new List<CMethod>();

        public CEntiry syntaxNode;

        void processEnumMerber()
        {
            var mlist = tdecl.Descendants.OfType<EnumMemberDeclaration>().ToList();
            var cenum = syntaxNode as CEnum;
            foreach(var item in mlist)
            {
                var m = new CEnumMember();
                var text = item.GetText();
                var split = text.Split('=');
                m.name = split[0];
                if (split.Count() >= 2)
                    m.optionValue = split[1];
                cenum.members.Add(m);
            }
        }

        void processCEntity()
        {
            syntaxNode.name = def.Name;
            syntaxNode.origin = this;
            syntaxNode.ns = def.Namespace;
        }

        public QType(QModule parent, TypeDefinition def, TypeDeclaration decl)
        {
            module = parent;
            this.def = def;
            tdecl = decl;
            if (def.IsEnum)
            {
                syntaxNode = new CEnum(parent.cmodule);
                processEnumMerber();
            }
            else if(def.IsInterface)
            {
                syntaxNode = new CClass(parent.cmodule){isInterface = true};
            }
            else
            {
                syntaxNode = new CClass(parent.cmodule);
            }
            processCEntity();
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
        public QProperty FindPropertyMethod(MethodDefinition def, bool getter)
        {
            foreach (var item in properties)
            {
                MethodDefinition pmethod;
                if (getter)
                    pmethod = item.def.GetMethod;
                else
                    pmethod = item.def.SetMethod;

                if (pmethod == def)
                {
                    return item;
                }
            }

            foreach(var item in indexers)
            {
                MethodDefinition pmethod;
                if (getter)
                    pmethod = item.def.GetMethod;
                else
                    pmethod = item.def.SetMethod;

                if (pmethod == def)
                {
                    return item;
                }
            }
            return null;
        }

        public CMethod FindCMethod(MethodDefinition def)
        {
            var m = FindMethod(def);
            if (m != null)
            {
                if (m.syntaxNode == null)
                    Console.WriteLine("Error");
                return m.syntaxNode;
            }
            if (def.IsGetter)
            {
                var p = FindPropertyMethod(def, true);
                if (p != null)
                {
                    if (p.syntaxGetterNode == null)
                        Console.WriteLine("Error");
                    else
                        return p.syntaxGetterNode;
                }
            }
            if (def.IsSetter)
            {
                var p = FindPropertyMethod(def, false);
                if (p != null)
                {
                    if (p.syntaxSetterNode == null)
                        Console.WriteLine("Error");
                    else
                        return p.syntaxSetterNode;
                }
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
        public void FindUsage()
        {
            // find field readby
            foreach (var f in fields)
            {
                foreach (var ft in module.types)
                {
                    var method = Util.FindFieldUsageInType(ft.def, f.def, true);
                    if (method != null)
                        f.AddReadBy(method);
                }
            }

            // find Field AssignBy
            foreach (var f in fields)
            {
                foreach (var ft in module.types)
                {
                    var method = Util.FindFieldUsageInType(ft.def, f.def, false);
                    if (method != null)
                        f.AddAssignBy(method);
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
                        p.AddGetBy(method);
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
                        p.AddSetBy(method);
                }
            }
        }

        public void GenerateSyntaxTree()
        {
            // process syntax tree
            foreach (var m in methods)
            {
                m.GeneratorSyntaxTree(syntaxNode);
            }

            foreach (var c in ctors)
            {
                c.GeneratorSyntaxTree(syntaxNode);
            }

            foreach(var f in fields)
            {
                f.GenerateSyntaxTree();
            }
            foreach(var f in constFields)
            {
                f.GenerateSyntaxTree();
            }
            foreach (var p in properties)
            {
                p.GenerateSyntaxTree(syntaxNode);
            }
            foreach(var p in indexers)
            {
                p.GenerateSyntaxTree(syntaxNode);
            }
        }

        void FieldRename()
        {
            foreach (var f in fields)
            {
                if (f.ReadBy.Count() > 0)
                {

                }
            }
        }
        void PropertyRename()
        {

        }
        void Field2Method()
        {

        }
        public void postProcess()
        {
            FindUsage();
            foreach (var f in fields)
                f.syntaxNode.Rename();
            FieldRename();
            Field2Method();
            PropertyRename();
        }

        /*public void GenerateCode(TextFormatter f)
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
            if(nspace!=null)
                nspace = nspace.Replace("SmartQuant", "QuantKit");
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
        }*/
    }

    /*public class QParameter
    {
        public QType type;
        public string typeName;
        public string externalType;
        public string name;
        public string optionValue;
        public object parent;
        public bool isPrimitive = false;
        public bool isEnum = false;
        //public bool isCtor = false;
        public QParameter(object obj)
        {
            this.parent = obj;
        }
    }*/

    public class QMethod
    {
        public MethodDefinition def;
        MethodDeclaration mdecl = null;
        ConstructorDeclaration cdecl = null;
        public QType parent;
        public CMethod syntaxNode;
        //public List<QParameter> parameters = new List<QParameter>();
        //public List<string> body = new List<string>();
        public bool IsConstructor
        {
            get
            {
                return def != null && def.IsConstructor;
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

        /*public void GenerateCode(TextFormatter f)
        {
            GenerateHppCode(f);
            f.WriteLine();
            GenerateCppCode(f);
            f.WriteLine();
            GeneratePrivateCode(f);
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
                type = type.Replace("[]", "");

                bool isEnum = false;
                QType moduleType = null;
                var ptype = Util.GetTypeRef(item.Type) as TypeDefinition;
                if (ptype != null)
                {
                    moduleType = parent.module.FindType(ptype);
                    if (moduleType != null)
                        isEnum = moduleType.def.IsEnum;
                }
                if (moduleType == null)
                {
                    moduleType = parent.module.FindType(type);
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
                    {
                        parent.externalIncludes.Add(p.typeName);
                    }
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
        }*/

        /*public void processCMethod(CClass obj)
        {
            var m = new CMethod(obj);
            m.origin = this;
            m.name = def.Name;
            m.rtype = Util.Type2CType(parent.module, def.ReturnType);
            m.isPrivate = def.IsPrivate;
            m.isPublic = def.IsPublic;
            m.isAbstract = def.IsAbstract;
            m.isVirtual = def.IsVirtual;
            m.isStatic = def.IsStatic;
            m.isCtor = def.IsConstructor;
            Util.processMethodBody(m.body, decl);
            Util.processParameters(parent.module, decl, m.parameters);
            //parent.cmethods.Add(m);
        }*/

        public void GeneratorSyntaxTree(CEntiry entity)
        {
            var obj = entity as CClass;
            var cm = new CMethod(obj);
            cm.origin = this;
            cm.name = def.Name;
            cm.rtype = Util.Type2CType(parent.module, def.ReturnType);
            cm.isPrivate = def.IsPrivate;
            cm.isPublic = def.IsPublic;
            cm.isAbstract = def.IsAbstract;
            cm.isVirtual = def.IsVirtual;
            cm.isStatic = def.IsStatic;
            cm.isCtor = def.IsConstructor;
            Util.processMethodBody(cm.body, decl);
            Util.processParameters(parent.module, decl, cm.parameters);
            syntaxNode = cm;
        }

        /*public void WriteParameters(TextFormatter f)
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

        }*/
    }

    public class QProperty 
    {
        public PropertyDefinition def;
        PropertyDeclaration pdecl = null;
        IndexerDeclaration idecl = null;
        public QType parent;

        public List<MethodDefinition> GetBy = new List<MethodDefinition>();
        public List<MethodDefinition> SetBy = new List<MethodDefinition>();

        public List<CMethod> ModuleGetBy = new List<CMethod>();
        public List<CMethod> ModuleSetBy = new List<CMethod>();

        public CMethod syntaxGetterNode;
        public CMethod syntaxSetterNode;

        /*public bool IsIndexer
        {
            get
            {
                return def.IsIndexer();
            }
        }*/

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

        /*void processIndexerParameters(List<CParameter> parameters)
        {
            var plist = decl.Descendants.OfType<ParameterDeclaration>().ToList();
            foreach (var item in plist)
            {
                var p = new CParameter();

                p.name = item.Name.Trim(); ;
                p.type = Util.Type2CType(parent.module, item.Type);

                var split = item.GetText().Split('=');
                string optionValue = null;
                if (split.Count() > 1)
                    optionValue = split[split.Count() - 1].Trim();

                // special process
                if (optionValue != null && p.name == "currencyId" && optionValue == "148")
                    optionValue = "currencyId.USD";

                if (optionValue != null)
                {
                    p.optionValue = optionValue;
                }
                parameters.Add(p);
            }
        }
        void processPropertyBody(List<string> body, AstNode node)
        {
            var csharpText = new StringWriter();
            var csharpoutput = new PlainTextOutput(csharpText);
            var outputFormatter = new TextOutputFormatter(csharpoutput) { FoldBraces = true };
            decl.AcceptVisitor(new CSharpOutputVisitor(outputFormatter, FormattingOptionsFactory.CreateAllman()));
            var blist = node.Descendants.OfType<BlockStatement>().ToList();

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
            foreach (var s in tlist)
                body.Add(s);
        }*/

        public void AddGetBy(MethodDefinition def)
        {
            CMethod method = parent.module.FindCMethod(def);
            if (method != null)
            {
                ModuleGetBy.Add(method);
            }
            else
                GetBy.Add(def);
        }
        public void AddSetBy(MethodDefinition def)
        {
            CMethod method = parent.module.FindCMethod(def);
            if (method != null)
            {
                ModuleSetBy.Add(method);
            }
            else
                SetBy.Add(def);
        }
        public void GenerateSyntaxTree(CEntiry obj)
        {
            var p = obj as CClass;
            if (def.IsIndexer())
            {
                var getter = idecl.Getter;
                if (!getter.IsNull)// && !getter.HasModifier(Modifiers.Private))
                {
                    var cm = new CMethod(p);
                    cm.origin = this;
                    cm.name = "getItem";
                    cm.rtype = Util.Type2CType(parent.module, idecl.ReturnType);
                    Util.processParameters(parent.module, decl, cm.parameters);
                    Util.processMethodBody(cm.body, getter);
                    cm.isPrivate = getter.HasModifier(Modifiers.Private);
                    cm.isInternal = getter.HasModifier(Modifiers.Internal);
                    cm.isPublic = getter.HasModifier(Modifiers.Public);
                    cm.isProtected = getter.HasModifier(Modifiers.Protected);
                    cm.isAbstract = getter.HasModifier(Modifiers.Abstract);
                    cm.isVirtual = getter.HasModifier(Modifiers.Virtual);
                    cm.isOverride = getter.HasModifier(Modifiers.Override);
                    cm.isStatic = getter.HasModifier(Modifiers.Static);
                    cm.isGetter = true;
                    syntaxGetterNode = cm;
                    //parent.cmethods.Add(cm);
                }

                var setter = idecl.Setter;
                if (!setter.IsNull)// && !setter.HasModifier(Modifiers.Private))
                {
                    var cm = new CMethod(p);
                    cm.origin = this;
                    cm.name = "setItem";
                    cm.rtype = Util.Type2CType(parent.module, idecl.ReturnType);
                    Util.processParameters(parent.module, decl, cm.parameters);
                    Util.processMethodBody(cm.body, setter);
                    cm.isPrivate = setter.HasModifier(Modifiers.Private);
                    cm.isInternal = setter.HasModifier(Modifiers.Internal);
                    cm.isPublic = setter.HasModifier(Modifiers.Public);
                    cm.isProtected = setter.HasModifier(Modifiers.Protected);
                    cm.isAbstract = setter.HasModifier(Modifiers.Abstract);
                    cm.isVirtual = setter.HasModifier(Modifiers.Virtual);
                    cm.isOverride = setter.HasModifier(Modifiers.Override);
                    cm.isStatic = setter.HasModifier(Modifiers.Static);
                    cm.isSetter = true;
                    syntaxSetterNode = cm;
                    //parent.cmethods.Add(cm);
                }
            }
            else
            {
                var getter = pdecl.Getter;
                if (!getter.IsNull)// && !getter.HasModifier(Modifiers.Private) && !pdecl.HasModifier(Modifiers.Override))
                {
                    var cm = new CMethod(p);
                    cm.origin = this;
                    cm.name = "get"+def.Name;
                    cm.rtype = Util.Type2CType(parent.module, pdecl.ReturnType);
                    Util.processMethodBody(cm.body, getter);
                    cm.isPrivate = getter.HasModifier(Modifiers.Private);
                    cm.isInternal = getter.HasModifier(Modifiers.Internal);
                    cm.isPublic = getter.HasModifier(Modifiers.Public);
                    cm.isProtected = getter.HasModifier(Modifiers.Protected);
                    cm.isAbstract = getter.HasModifier(Modifiers.Abstract);
                    cm.isVirtual = getter.HasModifier(Modifiers.Virtual);
                    cm.isOverride = getter.HasModifier(Modifiers.Override);
                    cm.isStatic = getter.HasModifier(Modifiers.Static);
                    cm.isGetter = true;
                    syntaxGetterNode = cm;
                    //parent.cmethods.Add(cm);
                }

                var setter = pdecl.Setter;
                if (!setter.IsNull)// && !setter.HasModifier(Modifiers.Private) && !pdecl.HasModifier(Modifiers.Override))
                {
                    var cm = new CMethod(p);
                    cm.origin = this;
                    cm.name = "set" + def.Name;
                    var rtype = new CType();
                    rtype.name = "void";
                    rtype.isVoid = true;
                    cm.rtype = rtype;
                    var param = new CParameter();
                    param.type = Util.Type2CType(parent.module, pdecl.ReturnType);
                    param.name = "value";
                    cm.parameters.Add(param);
                    Util.processMethodBody(cm.body, setter);
                    cm.isPrivate = setter.HasModifier(Modifiers.Private);
                    cm.isInternal = setter.HasModifier(Modifiers.Internal);
                    cm.isPublic = setter.HasModifier(Modifiers.Public);
                    cm.isProtected = setter.HasModifier(Modifiers.Protected);
                    cm.isAbstract = setter.HasModifier(Modifiers.Abstract);
                    cm.isVirtual = setter.HasModifier(Modifiers.Virtual);
                    cm.isOverride = setter.HasModifier(Modifiers.Override);
                    cm.isStatic = setter.HasModifier(Modifiers.Static);
                    cm.isSetter = true;
                    syntaxSetterNode = cm;
                    //parent.cmethods.Add(cm);
                }
            }
        }
        /*public void GenerateCode(TextFormatter f)
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

        }*/
    }

    public class QField
    {
        public FieldDefinition def;
        public FieldDeclaration decl;
        public QType parent;
        public List<MethodDefinition> ReadBy = new List<MethodDefinition>();
        public List<MethodDefinition> WriteBy = new List<MethodDefinition>();
        public List<CMethod> ModuleReadBy = new List<CMethod>();
        public List<CMethod> ModuleWriteBy = new List<CMethod>();

        public CField syntaxNode;
        //public string FieldName;

        /*public TypeReference FieldType
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
        }*/
        public void AddReadBy(MethodDefinition mdef)
        {
            CMethod method = parent.module.FindCMethod(mdef);
            if (method != null)
            {
                ModuleReadBy.Add(method);
                // make filed read as method
                var p = parent.syntaxNode as CClass;
                var m = new CMethod(p);
                m.origin = this;
                m.name = "get" + def.Name;
                m.rtype = Util.Type2CType(parent.module, def.FieldType);
                m.isPublic = true;
                m.isFieldGetter = true;
                syntaxNode.ReadBy.Add(method);
                syntaxNode.Getter = m;
                // rename ref body
            }
            else
                ReadBy.Add(mdef);
        }

        public void AddAssignBy(MethodDefinition mdef)
        {
            CMethod method = parent.module.FindCMethod(mdef);
            if (method != null)
            {
                ModuleWriteBy.Add(method);
                // make field assign as method
                var p = parent.syntaxNode as CClass;
                var m = new CMethod(p);
                m.origin = this;
                m.name = "set" + def.Name;
                m.rtype = new CType();
                m.rtype.name = "void";
                m.rtype.isVoid = true;
                var mparam = new CParameter();
                mparam.type = Util.Type2CType(parent.module, def.FieldType);
                mparam.name = "value";
                m.parameters.Add(mparam);
                m.isPublic = true;
                m.isFieldSetter = true;
                syntaxNode.AssignBy.Add(method);
                syntaxNode.Setter = m;
                // rename ref body
            }
            else
                WriteBy.Add(mdef);
        }
        public void GenerateSyntaxTree()
        {
            var p = parent.syntaxNode as CClass;
            syntaxNode = new CField(p);
            syntaxNode.origin = this;
            syntaxNode.name = def.Name;
            syntaxNode.type = Util.Type2CType(parent.module, def.FieldType);
            syntaxNode.optionValue = Util.OptionValue(decl);
            syntaxNode.isCompilerGenerated = def.IsCompilerGenerated();
            syntaxNode.isPublic = decl.HasModifier(Modifiers.Public);
            syntaxNode.isInternal = decl.HasModifier(Modifiers.Internal);
            syntaxNode.isConst = decl.HasModifier(Modifiers.Const);
            //parent.cfields.Add(node);
        }

        public QField(QType parent, FieldDefinition def, FieldDeclaration decl)
        {
            this.parent = parent;
            this.def = def;
            this.decl = decl;
            //this.FieldName = def.Name;
        }

        /*public void GenerateCode(TextFormatter f)
        {
            GenerateHppCode(f);
            f.WriteLine();
            GenerateCppCode(f);
            f.WriteLine();
            GeneratePrivateCode(f);
        }

        public void GenerateHppCode(TextFormatter f)
        {
            if (node.isCompilerGenerated) return;
            if (node.isPublic || node.isInternal || node.isConst)
            {
                if (node.isConst)
                    f.Write("const ");
                f.Write(node.type.name);
                f.Space();
                f.Write(node.name);
                if (node.optionValue != "")
                {
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
        }*/
    }

    public class QEvent
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

        /*public void GenerateCode(TextFormatter f)
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
        }*/
    }


}
