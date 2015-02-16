using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuantKit
{
    #region Util
    class Util
    {
        public static string OptionValue(AstNode decl)
        {
            var ilist = decl.Descendants.OfType<VariableInitializer>().ToList();
            if (ilist.Count() > 0)
            {
                var ovalue = ilist[0];
                var s = ovalue.GetText();
                var slist = s.Split('=');
                if (slist.Count() > 1)
                    return slist[slist.Count() - 1];
                else
                    return "";
            }
            else
                return "";
        }

        static CType T2CT(QModule module, string tname, bool isPrimitive)
        {
            var result = new CType();
            result.isPrimitive = isPrimitive;
            if (tname.Contains("[]"))
            {
                result.isArray = true;
                tname = tname.Replace("[]", "");
            }
            result.name = tname;
            QType moduleType = module.FindType(tname);
            if (moduleType != null)
            {
                result.isModuleType = true;
                result.isEnumType = moduleType.def.IsEnum;
            }
            return result;
        }
        public static CType Type2CType(QModule module, TypeReference resolvType)
        {
            bool isPrimitive;
            var tname = Util.TypeToString(resolvType, out isPrimitive).Trim();
            return T2CT(module, tname, isPrimitive);
        }

        public static CType Type2CType(QModule module, AstType resolvType)
        {
            bool isPrimitive;
            var tname = Util.TypeToString(resolvType, out isPrimitive).Trim();
            return T2CT(module, tname, isPrimitive);
        }

        public static void processParameters(QModule module, AstNode node, List<CParameter> parameterlist)
        {
            var plist = node.Descendants.OfType<ParameterDeclaration>().ToList();
            foreach (var item in plist)
            {
                var p = new CParameter();

                p.name = item.Name.Trim(); ;
                p.type = Util.Type2CType(module, item.Type);

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
                parameterlist.Add(p);
            }
        }

        public static void processMethodBody(List<string> body, AstNode node)
        {
            var csharpText = new StringWriter();
            var csharpoutput = new PlainTextOutput(csharpText);
            var outputFormatter = new TextOutputFormatter(csharpoutput) { FoldBraces = true };
            node.AcceptVisitor(new CSharpOutputVisitor(outputFormatter, FormattingOptionsFactory.CreateAllman()));

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
                if (tlist[0] == "get" || tlist[0] == "set")
                    tlist.RemoveAt(0);

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
        }

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
            if (type.IsPrimitive)
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
            else if (result == "List")
                result = "QList";
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
            else if (result == "List")
                result = "QList";
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
