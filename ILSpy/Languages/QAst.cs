using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.ILSpy;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
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
        void GenerateCode(ITextOutput output);
        void GenerateHppCode(ITextOutput output);
        void GenerateCppCode(ITextOutput output);
        void GeneratePrivateCode(ITextOutput output);
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
            output.WriteLine("---Hpp---");
            GenerateHppCode(output);
            output.WriteLine("---Cpp---");
            GenerateCppCode(output);
            output.WriteLine("---Private--");
            GeneratePrivateCode(output);
        }

        public void GenerateHppCode(ITextOutput output)
        {

        }

        public void GenerateCppCode(ITextOutput output)
        {

        }

        public void GeneratePrivateCode(ITextOutput output)
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
            GenerateHppCode(output);
            output.WriteLine();
            GenerateCppCode(output);
            output.WriteLine();
            GeneratePrivateCode(output);
        }

        public void GenerateHppCode(ITextOutput output)
        {
            using (var f = new TextFormatter(output))
            {
                string hname = "__QUANTKIT_" + def.Name.ToUpper() + "_H__";
                f.Write("#ifndef ");
                f.Write(hname);
                f.NewLine();
                f.Write("#define ");
                f.Write(hname);
                f.NewLine();
                f.Indent();

                f.Unindent();
                f.Write("#endif // ");
                f.Write(hname);
                f.NewLine();

            }
        }

        public void GenerateCppCode(ITextOutput output)
        {

        }

        public void GeneratePrivateCode(ITextOutput output)
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
            GenerateHppCode(output);
            output.WriteLine();
            GenerateCppCode(output);
            output.WriteLine();
            GeneratePrivateCode(output);
        }

        public void GenerateHppCode(ITextOutput output)
        {

        }

        public void GenerateCppCode(ITextOutput output)
        {

        }

        public void GeneratePrivateCode(ITextOutput output)
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
            GenerateHppCode(output);
            output.WriteLine();
            GenerateCppCode(output);
            output.WriteLine();
            GeneratePrivateCode(output);
        }

        public void GenerateHppCode(ITextOutput output)
        {

        }

        public void GenerateCppCode(ITextOutput output)
        {

        }

        public void GeneratePrivateCode(ITextOutput output)
        {

        }
    }

    public class QField : GenerateCodeAbled
    {
        public FieldDefinition def;
        public FieldDeclaration decl;
        public QType parent;
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
        public QField(QType parent, FieldDefinition def, FieldDeclaration decl)
        {
            this.parent = parent;
            this.def = def;
            this.decl = decl;
        }

        public void GenerateCode(ITextOutput output)
        {
            GenerateHppCode(output);
            output.WriteLine();
            GenerateCppCode(output);
            output.WriteLine();
            GeneratePrivateCode(output);
        }

        public void GenerateHppCode(ITextOutput output)
        {
            if (def.IsCompilerGenerated()) return;
            using( TextFormatter f = new TextFormatter(output)) 
            {
                if(decl.HasModifier(Modifiers.Public) || decl.HasModifier(Modifiers.Internal))
                {
                    f.WriteType(FieldType);
                    f.Space();
                    f.Write(Name);
                    f.Write(";");
                    f.NewLine();
                }
            }
        }

        public void GenerateCppCode(ITextOutput output)
        {

        }

        public void GeneratePrivateCode(ITextOutput output)
        {
            using (TextFormatter f = new TextFormatter(output))
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

        public void GenerateCode(ITextOutput output)
        {
            GenerateHppCode(output);
            output.WriteLine();
            GenerateCppCode(output);
            output.WriteLine();
            GeneratePrivateCode(output);
        }

        public void GenerateHppCode(ITextOutput output)
        {
            var formatter = new TextFormatter(output);
            formatter.Indent();
            //formatter.Write()
            formatter.Unindent();
        }

        public void GenerateCppCode(ITextOutput output)
        {

        }

        public void GeneratePrivateCode(ITextOutput output)
        {
           // output.WriteLine(FieldType.Name + " " + Name + ";");
        }
    }

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
        readonly ITextOutput output;
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

        public void WriteType(TypeReference r)
        {
            string real = (r.IsPrimitive && r.Name == "Byte") ? "unsigned char" : r.Name;
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
