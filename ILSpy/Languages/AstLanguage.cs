using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.ILSpy.Options;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using ICSharpCode.ILSpy;

namespace QuantKit
{
    public interface GenerateCodeAbled
    {
        void GenerateCode(ITextOutput output);
    }

    public class QModule  : GenerateCodeAbled
    {
        public ModuleDefinition mdef;
        public List<QType> types = new List<QType>();
        public SyntaxTree syntaxTree;
        public QModule(ModuleDefinition moduledef)
        {
            mdef = moduledef;
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
        public void ProcessReference()
        {
            foreach (var t in types)
            {
                var bt = t.def.BaseType;
                foreach (var ft in types)
                {
                    if (ft.def == bt && bt != null)
                    {
                        t.bases.Add(ft);
                        ft.devires.Add(t);
                    }
                }
            }

            // find inherit interface
            foreach (var t in types)
            {
                TypeDeclaration decl = t.decl as TypeDeclaration;
                if (decl != null)
                {
                    foreach (var bt in decl.BaseTypes)
                    {
                        var dtype = bt.ToTypeReference().ToString();
                        foreach (var ft in types)
                        {
                            if (ft.def.IsInterface)
                                if (ft.def.Name == dtype)
                                {
                                    t.interfaces.Add(ft);
                                    ft.impls.Add(t);
                                }
                        }
                    }
                }
            }
        }

        public void GenerateCode(ITextOutput output)
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
        public List<QEvent> events = new List<QEvent>();
        public List<QType> bases = new List<QType>();
        public List<QType> devires = new List<QType>();
        public List<QType> interfaces = new List<QType>();
        public List<QType> impls = new List<QType>();
        public List<QMethod> ctors = new List<QMethod>();
        public List<QProperty> indexers = new List<QProperty>();

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
            foreach(var item in methods)
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
            fields.Add(f);
        }
        public void AddField(FieldDefinition fdef, FieldDeclaration fdecl)
        {
            var f = new QField(this, fdef, fdecl);
            AddField(f);
        }
        public QField FindField(FieldDefinition def)
        {
            foreach(var item in fields)
            {
                if (item.def == def)
                    return item;
            }
            return null;
        }
        public QField FindField(FieldDeclaration decl)
        {
            foreach(var item in fields)
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
            foreach(var item in events)
            {
                if (item.def == def)
                    return item;
            }
            return null;
        }
        public QEvent FindEvent(EventDeclaration decl)
        {
            foreach(var item in events)
            {
                if (item.decl == decl)
                    return item;
            }
            return null;
        }
        public void AddCtor(QMethod m)
        {
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

        public void GenerateCode(ITextOutput output)
        {

        }
    }

    public class QMethod : GenerateCodeAbled
    {
        public MethodDefinition def;
        MethodDeclaration mdecl = null;
        ConstructorDeclaration cdecl = null;
        public QType parent;
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

        public void GenerateCode(ITextOutput output)
        {

        }
    }

    public class QProperty : GenerateCodeAbled
    {
        public PropertyDefinition def;
        PropertyDeclaration pdecl = null;
        IndexerDeclaration idecl = null;
        public QType parent;

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

        public void GenerateCode(ITextOutput output)
        {

        }
    }

    public class QField : GenerateCodeAbled
    {
        public FieldDefinition def;
        public FieldDeclaration decl;
        public QType parent;
        public QField(QType parent, FieldDefinition def, FieldDeclaration decl)
        {
            this.parent = parent;
            this.def = def;
            this.decl = decl;
        }

        public void GenerateCode(ITextOutput output)
        {

        }
    }

    public class QEvent : GenerateCodeAbled
    {
        public EventDefinition def;
        public EventDeclaration decl;
        public QType parent;
        public QEvent(QType parent,EventDefinition def, EventDeclaration decl)
        {
            this.parent = parent;
            this.def = def;
            this.decl = decl;
        }

        public void GenerateCode(ITextOutput output)
        {

        }
    }

    [Export(typeof(Language))]
    public class AstLanguage : Language
    {
        string name = "QAST";
        bool showAllMembers = false;
        Predicate<IAstTransform> transformAbortCondition = null;
        AstBuilder globalCodeDomBuilder = null;
        List<QModule> globalModules = new List<QModule>();
        public AstLanguage()
        {
        }

        public override string Name
        {
            get { return name; }
        }

        public override string FileExtension
        {
            get { return ".ast"; }
        }

        public override string ProjectFileExtension
        {
            get { return ".csproj"; }
        }

        public AstBuilder codeDomBuilder
        {
            get
            {
                return globalCodeDomBuilder;
            }
        }

        public QModule findModule(ModuleDefinition module)
        {
            QModule result = null;
            foreach (var item in globalModules)
                if (item.mdef == module)
                    result = item;
            return result;
        }
        public QType findType(TypeDefinition tdef)
        {
            QModule module = findModule(tdef.Module);
            if (module == null) return null;

            return module.FindType(tdef);
        }

        public QMethod findMethod(MethodDefinition method)
        {
            QType type = findType(method.DeclaringType);
            if (type == null) return null;

            return type.FindMethod(method);
        }

        public QProperty findProperty(PropertyDefinition p)
        {
            QType type = findType(p.DeclaringType);
            if (type == null) return null;
            return type.FindProperty(p);
        }
        public QField findField(FieldDefinition f)
        {
            QType type = findType(f.DeclaringType);
            if (type == null) return null;
            return type.FindField(f);
        }
        public QEvent findEvent(EventDefinition e)
        {
            QType type = findType(e.DeclaringType);
            if (type == null) return null;
            return type.FindEvent(e);
        }
        public void BuildQAst(ModuleDefinition module, AstBuilder dom)
        {
            QModule current = findModule(module);
            if (current != null)
                return;
            var m = new QModule(module);
            globalModules.Add(m);
            var mv = new QModuleVisitor(m);
            dom.SyntaxTree.AcceptVisitor(mv);

            m.ProcessReference();

            foreach (var t in m.types)
            {
                var tv = new QTypeVisitor(t);
                t.decl.AcceptVisitor(tv);
            }
        }

        public void DecompileAllIfNeed(ModuleDefinition module, DecompilationOptions options)
        {
            if (globalCodeDomBuilder != null)
                return;
            var assembly = module.Assembly;
            globalCodeDomBuilder = CreateAstBuilder(options, currentModule: module);
            globalCodeDomBuilder.AddAssembly(module.Assembly, onlyAssemblyLevel: false);
            BuildQAst(module, globalCodeDomBuilder);
            globalCodeDomBuilder.SyntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
        }

        public void GenerateCode(AstNode node, ITextOutput output)
        {
            var outputFormatter = new TextOutputFormatter(output) { FoldBraces = true };
            var formattingPolicy = FormattingOptionsFactory.CreateAllman();
            node.AcceptVisitor(new CSharpOutputVisitor(outputFormatter, formattingPolicy));
        }

        public override void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(method.DeclaringType, includeNamespace: true));
            DecompileAllIfNeed(method.Module, options);
            QMethod m = findMethod(method);
            if (m != null)
            {
                output.WriteLine("//Found Method in Tree");
                GenerateCode(m.decl, output);
            }
            else
                output.WriteLine("//Cannot find this method in Syntax Tree");
        }

        public override void DecompileProperty(PropertyDefinition property, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(property.DeclaringType, includeNamespace: true));
            DecompileAllIfNeed(property.Module, options);
            QProperty p = findProperty(property);
            if (p != null)
            {
                output.WriteLine("//Found Property in Tree");
                GenerateCode(p.decl, output);
            }
            else
                output.WriteLine("//Cannot find this property in Syntax Tree");
        }

        public override void DecompileField(FieldDefinition field, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(field.DeclaringType, includeNamespace: true));
            DecompileAllIfNeed(field.Module, options);
            QField f = findField(field);
            if (f != null)
            {
                output.WriteLine("//Found Field in Tree");
                GenerateCode(f.decl, output);
            }
            else
                output.WriteLine("//Cannot find this field in Syntax Tree");
        }

        public override void DecompileEvent(EventDefinition ev, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(ev.DeclaringType, includeNamespace: true));
            DecompileAllIfNeed(ev.Module, options);
            QEvent e = findEvent(ev);
            if (e != null)
            {
                output.WriteLine("//Found Event in Tree");
                GenerateCode(e.decl, output);
            }
            else
                output.WriteLine("//Cannot find this Event in Syntax Tree");
        }

        public override void DecompileType(TypeDefinition type, ITextOutput output, DecompilationOptions options)
        {
            DecompileAllIfNeed(type.Module, options);
            QType t = findType(type);
            if (t != null)
            {
                output.WriteLine("//Found Type in Tree");
                GenerateCode(t.decl, output);
            }
            else
                output.WriteLine("//Cannot find this Type in Syntax Tree");
        }

        public override void DecompileAssembly(LoadedAssembly assembly, ITextOutput output, DecompilationOptions options)
        {
            if (options.FullDecompilation && options.SaveAsProjectDirectory != null)
            {
                HashSet<string> directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var files = WriteCodeFilesInProject(assembly.ModuleDefinition, options, directories).ToList();
                //files.AddRange(WriteResourceFilesInProject(assembly, options, directories));
                WriteProjectFile(new TextOutputWriter(output), files, assembly.ModuleDefinition);
            }
            else
            {
                base.DecompileAssembly(assembly, output, options);
                output.WriteLine();
                ModuleDefinition mainModule = assembly.ModuleDefinition;

                DecompileAllIfNeed(assembly.ModuleDefinition, options);

                foreach (var m in globalModules)
                {
                    foreach (var t in m.types)
                    {
                        output.Write(t.def.FullName);
                        output.Write("<");
                        foreach (var item in t.bases)
                        {
                            output.Write(item.def.FullName);
                        }
                        output.Write(">");
                        output.Write("(");
                        foreach (var item in t.interfaces)
                        {
                            output.Write(item.def.FullName);
                        }
                        output.Write(")");
                        output.WriteLine();
                    }
                }
                //globalCodeDomBuilder.GenerateCode(output);
            }
        }

        #region WriteProjectFile
        void WriteProjectFile(TextWriter writer, IEnumerable<Tuple<string, string>> files, ModuleDefinition module)
        {

        }
        #endregion

        #region WriteCodeFilesInProject
        bool IncludeTypeWhenDecompilingProject(TypeDefinition type, DecompilationOptions options)
        {
            if (type.Name == "<Module>" || AstBuilder.MemberIsHidden(type, options.DecompilerSettings))
                return false;
            if (type.Namespace == "XamlGeneratedNamespace" && type.Name == "GeneratedInternalTypeHelper")
                return false;
            return true;
        }

        IEnumerable<Tuple<string, string>> WriteAssemblyInfo(ModuleDefinition module, DecompilationOptions options, HashSet<string> directories)
        {
            // don't automatically load additional assemblies when an assembly node is selected in the tree view
            using (LoadedAssembly.DisableAssemblyLoad())
            {
                AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
                codeDomBuilder.AddAssembly(module, onlyAssemblyLevel: true);
                codeDomBuilder.RunTransformations(transformAbortCondition);

                string prop = "Properties";
                if (directories.Add("Properties"))
                    Directory.CreateDirectory(Path.Combine(options.SaveAsProjectDirectory, prop));
                string assemblyInfo = Path.Combine(prop, "AssemblyInfo" + this.FileExtension);
                using (StreamWriter w = new StreamWriter(Path.Combine(options.SaveAsProjectDirectory, assemblyInfo)))
                    codeDomBuilder.GenerateCode(new PlainTextOutput(w));
                return new Tuple<string, string>[] { Tuple.Create("Compile", assemblyInfo) };
            }
        }

        IEnumerable<Tuple<string, string>> WriteCodeFilesInProject(ModuleDefinition module, DecompilationOptions options, HashSet<string> directories)
        {
            var files = module.Types.Where(t => IncludeTypeWhenDecompilingProject(t, options)).GroupBy(
                delegate(TypeDefinition type)
                {
                    string file = ICSharpCode.ILSpy.TextView.DecompilerTextView.CleanUpName(type.Name) + this.FileExtension;
                    if (string.IsNullOrEmpty(type.Namespace))
                    {
                        return file;
                    }
                    else
                    {
                        string dir = ICSharpCode.ILSpy.TextView.DecompilerTextView.CleanUpName(type.Namespace);
                        if (directories.Add(dir))
                            Directory.CreateDirectory(Path.Combine(options.SaveAsProjectDirectory, dir));
                        return Path.Combine(dir, file);
                    }
                }, StringComparer.OrdinalIgnoreCase).ToList();
            AstMethodBodyBuilder.ClearUnhandledOpcodes();
            Parallel.ForEach(
                files,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                delegate(IGrouping<string, TypeDefinition> file)
                {
                    using (StreamWriter w = new StreamWriter(Path.Combine(options.SaveAsProjectDirectory, file.Key)))
                    {
                        AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
                        foreach (TypeDefinition type in file)
                        {
                            codeDomBuilder.AddType(type);
                        }
                        codeDomBuilder.RunTransformations(transformAbortCondition);
                        codeDomBuilder.GenerateCode(new PlainTextOutput(w));
                    }
                });
            AstMethodBodyBuilder.PrintNumberOfUnhandledOpcodes();
            return files.Select(f => Tuple.Create("Compile", f.Key)).Concat(WriteAssemblyInfo(module, options, directories));
        }
        #endregion

        AstBuilder CreateAstBuilder(DecompilationOptions options, ModuleDefinition currentModule = null, TypeDefinition currentType = null, bool isSingleMember = false)
        {
            if (currentModule == null)
                currentModule = currentType.Module;
            DecompilerSettings settings = options.DecompilerSettings;
            if (isSingleMember)
            {
                settings = settings.Clone();
                settings.UsingDeclarations = false;
            }
            return new AstBuilder(
                new DecompilerContext(currentModule)
                {
                    CancellationToken = options.CancellationToken,
                    CurrentType = currentType,
                    Settings = settings
                });
        }

        public override string TypeToString(TypeReference type, bool includeNamespace, ICustomAttributeProvider typeAttributes = null)
        {
            ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
            if (includeNamespace)
                options |= ConvertTypeOptions.IncludeNamespace;

            return TypeToString(options, type, typeAttributes);
        }

        string TypeToString(ConvertTypeOptions options, TypeReference type, ICustomAttributeProvider typeAttributes = null)
        {
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

        public override string FormatPropertyName(PropertyDefinition property, bool? isIndexer)
        {
            if (property == null)
                throw new ArgumentNullException("property");

            if (!isIndexer.HasValue)
            {
                isIndexer = property.IsIndexer();
            }
            if (isIndexer.Value)
            {
                var buffer = new System.Text.StringBuilder();
                var accessor = property.GetMethod ?? property.SetMethod;
                if (accessor.HasOverrides)
                {
                    var declaringType = accessor.Overrides.First().DeclaringType;
                    buffer.Append(TypeToString(declaringType, includeNamespace: true));
                    buffer.Append(@".");
                }
                buffer.Append(@"this[");
                bool addSeparator = false;
                foreach (var p in property.Parameters)
                {
                    if (addSeparator)
                        buffer.Append(@", ");
                    else
                        addSeparator = true;
                    buffer.Append(TypeToString(p.ParameterType, includeNamespace: true));
                }
                buffer.Append(@"]");
                return buffer.ToString();
            }
            else
                return property.Name;
        }

        public override string FormatTypeName(TypeDefinition type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return TypeToString(ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions, type);
        }

        public override bool ShowMember(MemberReference member)
        {
            return showAllMembers || !AstBuilder.MemberIsHidden(member, new DecompilationOptions().DecompilerSettings);
        }

        public override MemberReference GetOriginalCodeLocation(MemberReference member)
        {
            if (showAllMembers || !DecompilerSettingsPanel.CurrentDecompilerSettings.AnonymousMethods)
                return member;
            else
                return ICSharpCode.ILSpy.TreeNodes.Analyzer.Helpers.GetOriginalCodeLocation(member);
        }

        public override string GetTooltip(MemberReference member)
        {
            MethodDefinition md = member as MethodDefinition;
            PropertyDefinition pd = member as PropertyDefinition;
            EventDefinition ed = member as EventDefinition;
            FieldDefinition fd = member as FieldDefinition;
            if (md != null || pd != null || ed != null || fd != null)
            {
                AstBuilder b = new AstBuilder(new DecompilerContext(member.Module) { Settings = new DecompilerSettings { UsingDeclarations = false } });
                b.DecompileMethodBodies = false;
                if (md != null)
                    b.AddMethod(md);
                else if (pd != null)
                    b.AddProperty(pd);
                else if (ed != null)
                    b.AddEvent(ed);
                else
                    b.AddField(fd);
                b.RunTransformations();
                foreach (var attribute in b.SyntaxTree.Descendants.OfType<AttributeSection>())
                    attribute.Remove();

                StringWriter w = new StringWriter();
                b.GenerateCode(new PlainTextOutput(w));
                return Regex.Replace(w.ToString(), @"\s+", " ").TrimEnd();
            }

            return base.GetTooltip(member);
        }

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
    }
}
