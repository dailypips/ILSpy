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
    [Export(typeof(Language))]
    public class AstLanguage : Language
    {
        string name = "QAST";
        bool showAllMembers = false;
        Predicate<IAstTransform> transformAbortCondition = null;
        QModule globalModule = null;

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
            get { return ".pro"; }
        }


        public void DecompileAllIfNeed(ModuleDefinition module, DecompilationOptions options)
        {
            if (globalModule == null)
                globalModule = new QModule(module, options);
        }

        public override void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(method.DeclaringType, includeNamespace: true));
            DecompileAllIfNeed(method.Module, options);
            QMethod m = globalModule.FindMethod(method);
            if (m != null)
            {
                output.WriteLine("//Found Method in Tree");
                //m.GenerateCode(new TextFormatter(output));
            }
            else
                output.WriteLine("//Cannot find this method in Syntax Tree");
        }

        public override void DecompileProperty(PropertyDefinition property, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(property.DeclaringType, includeNamespace: true));
            DecompileAllIfNeed(property.Module, options);
            QProperty p = globalModule.FindProperty(property);
            if (p != null)
            {
                output.WriteLine("//Found Property in Tree");
                //p.GenerateCode(new TextFormatter(output));
            }
            else
                output.WriteLine("//Cannot find this property in Syntax Tree");
        }

        public override void DecompileField(FieldDefinition field, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(field.DeclaringType, includeNamespace: true));
            DecompileAllIfNeed(field.Module, options);
            QField f = globalModule.FindField(field);
            if (f != null)
            {
                output.WriteLine("//Found Field in Tree");
                //f.GenerateCode(new TextFormatter(output));
            }
            else
                output.WriteLine("//Cannot find this field in Syntax Tree");
        }

        public override void DecompileEvent(EventDefinition ev, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(ev.DeclaringType, includeNamespace: true));
            DecompileAllIfNeed(ev.Module, options);
            QEvent e = globalModule.FindEvent(ev);
            if (e != null)
            {
                output.WriteLine("//Found Event in Tree");
                //e.GenerateCode(new TextFormatter(output));
            }
            else
                output.WriteLine("//Cannot find this Event in Syntax Tree");
        }

        public override void DecompileType(TypeDefinition type, ITextOutput output, DecompilationOptions options)
        {
            DecompileAllIfNeed(type.Module, options);
            QType t = globalModule.FindType(type);
            if (t != null)
            {
                output.WriteLine("//Found Type in Tree");
                var node = t.syntaxNode as GenerateCodeAbled;
                if (node != null)
                    node.GenerateCode(new TextFormatter(output));
                //t.GenerateCode(new TextFormatter(output));
            }
            else
                output.WriteLine("//Cannot find this Type in Syntax Tree");
        }

        public override void DecompileAssembly(LoadedAssembly assembly, ITextOutput output, DecompilationOptions options)
        {
            if (options.FullDecompilation && options.SaveAsProjectDirectory != null)
            {
                var writer = new QAstWrite(globalModule);
                WriteCodeFilesInProject(writer, options);
                WriteProjectFile(writer, options);
            }
            else
            {
                base.DecompileAssembly(assembly, output, options);
                output.WriteLine();
                DecompileAllIfNeed(assembly.ModuleDefinition, options);
            }
        }

        #region WriteProjectFile
        void WriteProjectFile(QAstWrite astoutput, DecompilationOptions options)
        {
            astoutput.WriteProjectFile(Path.Combine(options.SaveAsProjectDirectory, globalModule.def.Name + ProjectFileExtension));
        }
        #endregion

        #region WriteCodeFilesInProject

        void WriteCodeFilesInProject(QAstWrite astoutput, DecompilationOptions options)
        {
            astoutput.WriteCodeFilesInProject(options.SaveAsProjectDirectory);
        }
        #endregion

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
    }
}
