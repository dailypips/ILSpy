// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
    /// <summary>
    /// Decompiler logic for C#.
    /// </summary>
    [Export(typeof(Language))]
    public class CppLanguage : Language
    {
        string name = "C++";
        bool showAllMembers = false;
        Predicate<IAstTransform> transformAbortCondition = null;
        bool needPreProcess = true;
        public CppLanguage()
        {
        }

        public override string Name
        {
            get { return name; }
        }

        public override string FileExtension
        {
            get { return ".h"; }
        }

        public static string HppFileExtension
        {
            get { return ".h"; }
        }
        public static string HxxFileExtension
        {
            get { return "_p.h"; }
        }
        public static string CppFileExtension
        {
            get { return ".cpp"; }
        }

        public override string ProjectFileExtension
        {
            get { return ".hproj"; }
        }

        public static string OnlyIncludeNameSpace
        {
            get { return "SmartQuant"; }
        }
        public void GenerateCode(AstNode node, ITextOutput output)
        {
            var outputFormatter = new TextOutputFormatter(output) { FoldBraces = true };
            var formattingPolicy = FormattingOptionsFactory.CreateAllman();
            node.AcceptVisitor(new CppOutputVisitor(outputFormatter, formattingPolicy));
        }
        #region C++
        static bool isSkipClass(TypeDefinition def)
        {
            var dsdef = Util.GetTypeDefinition(def.Module, "ObjectStreamer");
            bool isInhertFromObjectStreamer = dsdef != null && Util.isInhertFrom(def, dsdef);
            bool isInhertFromEventArgs = Util.isInhertFrom(def, "System.EventArgs");
            if (def.Name == "DataObjectType" || isInhertFromEventArgs || isInhertFromObjectStreamer )
                return true;
            else
                return false;
        }

        void WriteCppCode(TypeDefinition def, ITextOutput output)
        {
            if (!IsNeedWriteCpp(def))
                return;
            if (def.Name == "CurrencyId")
                Helper.WriteCurrencyIdCode(def, output);
            else
            {
                Cpp.WriteClass(def, output);
            }
        }
        /*void WriteCxxCode(TypeDefinition def, TypeDeclaration decl, ITextOutput output)
        {
            if (def.IsEnum || def.IsInterface || isSkipClass(def))
                return;
            if (def.Name == "EventType" || def.Name == "CurrencyId")
                return;
            if (!def.IsEnum && !def.IsInterface)
                Cxx.WriteClass(def, decl, output);
        }*/
        void WriteHxxCode(TypeDefinition def, ITextOutput output)
        {
            if (!IsNeedWriteHxx(def))
                return;
            Hxx.WriteClass(def, output);
        }

        void WriteHppCode(TypeDefinition def, ITextOutput output)
        {
            if (!IsNeedWriteHpp(def))
                return;
            if (def.IsEnum)
                Hpp.WriteEnum(def, output);
            else
                Hpp.WriteClass(def, output);
        }

        public void PreProcess(ModuleDefinition module)
        {
            //TODO find public && internal field reference
            if (!needPreProcess)
                return;
            InfoUtil.BuildModuleDict(module);
            Helper.BuildEventTypeTable(module);
            needPreProcess = false;
        }
        #endregion
        #region Decompile
        public override void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(method.DeclaringType, includeNamespace: true));
            PreProcess(method.Module);
            var info = InfoUtil.Info(method);
            GenerateCode(info.Declaration, output);
        }

        public override void DecompileProperty(PropertyDefinition property, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(property.DeclaringType, includeNamespace: true));
            PreProcess(property.Module);
            var info = InfoUtil.Info(property);
            GenerateCode(info.Declaration, output);
        }

        public override void DecompileField(FieldDefinition field, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(field.DeclaringType, includeNamespace: true));
            PreProcess(field.Module);
            var info = InfoUtil.Info(field);
            GenerateCode(info.Declaration, output);
        }

        public override void DecompileEvent(EventDefinition ev, ITextOutput output, DecompilationOptions options)
        {
            WriteCommentLine(output, TypeToString(ev.DeclaringType, includeNamespace: true));
            PreProcess(ev.Module);
            var info = InfoUtil.Info(ev);
            GenerateCode(info.Declaration, output);
        }

        public override void DecompileType(TypeDefinition type, ITextOutput output, DecompilationOptions options)
        {
            PreProcess(type.Module);
            var info = InfoUtil.Info(type);
            GenerateCode(info.Declaration, output);
            /*AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: type);
            codeDomBuilder.AddType(type);
            codeDomBuilder.RunTransformations(transformAbortCondition);
            //RunTransformsAndGenerateCode(codeDomBuilder, output, options);
            GenerateCplusplusCode(codeDomBuilder, output, OnlyIncludeNameSpace);*/
            WriteHppCode(type, output);
            WriteHxxCode(type, output);
            WriteCppCode(type, output);
        }

        public override void DecompileAssembly(LoadedAssembly assembly, ITextOutput output, DecompilationOptions options)
        {
            PreProcess(assembly.ModuleDefinition);
            if (options.FullDecompilation && options.SaveAsProjectDirectory != null)
            {
                HashSet<string> directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var files = WriteCodeFilesInProject(assembly.ModuleDefinition, options, directories).ToList();
                WriteProjectFile(new TextOutputWriter(output), files, assembly.ModuleDefinition);
            }
            else
            {
                //base.DecompileAssembly(assembly, output, options);
                //output.WriteLine();
                ModuleDefinition mainModule = assembly.ModuleDefinition;
                Helper.ShowUnResolvedFieldAndMethod(mainModule, output);
                output.WriteLine();

                // don't automatically load additional assemblies when an assembly node is selected in the tree view
                /*using (options.FullDecompilation ? null : LoadedAssembly.DisableAssemblyLoad())
                {
                    AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: assembly.ModuleDefinition);
                    codeDomBuilder.AddAssembly(assembly.ModuleDefinition, onlyAssemblyLevel: !options.FullDecompilation);
                    codeDomBuilder.RunTransformations(transformAbortCondition);
                    GenerateCSharpCode(codeDomBuilder, output);
                }*/
            }
        }

        public static bool IsNeedWriteHpp(TypeDefinition def, string @namespace = null)
        {
            if (def != null && (@namespace == null || (@namespace != null && def.Namespace == @namespace)))
            {
                if (isSkipClass(def))
                    return false;
                return true;
            }
            return false;
        }

        public static bool IsNeedWriteHxx(TypeDefinition def, string @namespace =null)
        {
            var info = InfoUtil.Info(def);
            if (def != null && (@namespace == null || (@namespace != null && def.Namespace == @namespace)))
            {
                if (def.IsEnum || def.IsInterface || isSkipClass(def))
                    return false;
                if (def.Name == "EventType" || def.Name == "CurrencyId")
                    return false;
                if (info != null && !info.HasDerivedClass)
                    return false;
                return true;
            }
            return false;
        }

        public static bool IsNeedWriteCpp(TypeDefinition def, string @namespace =null)
        {
            if (def != null && (@namespace == null || (@namespace != null && def.Namespace == @namespace)))
            {
                if (def.IsEnum || def.IsInterface || isSkipClass(def))
                    return false;
                if (def.Name == "EventType")
                    return false;
                return true;
            }
            return false;
        }
        #endregion
        #region WriteProjectFile
        void WriteProjectFile(TextWriter writer, IEnumerable<Tuple<string, string>> files, ModuleDefinition module)
        {
            /*const string ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            using (XmlTextWriter w = new XmlTextWriter(writer))
            {
                w.Formatting = Formatting.Indented;
                w.WriteStartDocument();
                foreach (IGrouping<string, string> gr in (from f in files group f.Item2 by f.Item1 into g orderby g.Key select g))
                {
                    w.WriteStartElement("ItemGroup");
                    foreach (string file in gr.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                    {
                        w.WriteStartElement(gr.Key);
                        w.WriteAttributeString("Include", file);
                        w.WriteEndElement();
                    }
                    w.WriteEndElement();
                }

                w.WriteStartElement("Import");
                w.WriteAttributeString("Project", "$(MSBuildToolsPath)\\Microsoft.CSharp.targets");
                w.WriteEndElement();

                w.WriteEndDocument();
            }*/
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

        TypeDeclaration FindDeclaration(TypeDefinition t, AstBuilder builder)
        {
            var syntaxTree = builder.SyntaxTree;
            var decls = syntaxTree.Descendants.OfType<TypeDeclaration>().ToList();
            foreach (var item in decls)
            {
                var def = item.Annotation<TypeDefinition>();
                if (def == t)
                    return item;
            }
            return null;
        }

        IEnumerable<Tuple<string, string>> WriteCodeFilesInProject(ModuleDefinition module, DecompilationOptions options, HashSet<string> directories)
        {
            var files = module.Types.Where(t => IncludeTypeWhenDecompilingProject(t, options)).GroupBy(
                delegate(TypeDefinition type)
                {
                    string file = CleanUpName(type.Name);
                    if (string.IsNullOrEmpty(type.Namespace))
                    {
                        return file;
                    }
                    else
                    {
                        return file;
                        /*string dir = CleanUpName(type.Namespace);
                        if (OnlyIncludeNameSpace == null || OnlyIncludeNameSpace == type.Namespace)
                        {
                            if (directories.Add(dir))
                                Directory.CreateDirectory(Path.Combine(options.SaveAsProjectDirectory, dir));
                        }*/
                        //return Path.Combine(dir, file);
                    }
                }, StringComparer.OrdinalIgnoreCase).ToList();
            AstMethodBodyBuilder.ClearUnhandledOpcodes();
            Parallel.ForEach(
                files,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                delegate(IGrouping<string, TypeDefinition> file)
                {
                    var path = options.SaveAsProjectDirectory;
                    var t = file.First<TypeDefinition>();
                    string fname = file.Key;
                    if (IsNeedWriteHpp(t, OnlyIncludeNameSpace))
                    {
                        var HppPath = Path.Combine(path, "include");
                        HppPath = Path.Combine(HppPath, "QuantKit");
                        Directory.CreateDirectory(HppPath);
                        using (StreamWriter w = new StreamWriter(Path.Combine(HppPath, file.Key + HppFileExtension)))
                        {
/*                            AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
                            foreach (TypeDefinition type in file)
                            {
                                codeDomBuilder.AddType(type);
                            }
                            PreProcess(module);
                            codeDomBuilder.RunTransformations(transformAbortCondition);*/
                            //WriteHppCode(t, FindDeclaration(t, codeDomBuilder), new PlainTextOutput(w));
                            WriteHppCode(t, new PlainTextOutput(w));
                        }
                    }

                    if (IsNeedWriteHxx(t, OnlyIncludeNameSpace))
                    {
                        var HxxPath = Path.Combine(path, "src");
                        Directory.CreateDirectory(HxxPath);
                        using (StreamWriter w = new StreamWriter(Path.Combine(HxxPath, file.Key + HxxFileExtension)))
                        {
                            AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
                            foreach (TypeDefinition type in file)
                            {
                                codeDomBuilder.AddType(type);
                            }
                            PreProcess(module);
                            codeDomBuilder.RunTransformations(transformAbortCondition);
                            WriteHxxCode(t, new PlainTextOutput(w));
                        }
                    }
                    if (IsNeedWriteCpp(t, OnlyIncludeNameSpace))
                    {
                        var CppPath = Path.Combine(path, "src");
                        Directory.CreateDirectory(CppPath);
                        using (StreamWriter w = new StreamWriter(Path.Combine(CppPath, file.Key + CppFileExtension)))
                        {
                            AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
                            foreach (TypeDefinition type in file)
                            {
                                codeDomBuilder.AddType(type);
                            }
                            PreProcess(module);
                            codeDomBuilder.RunTransformations(transformAbortCondition);
                            WriteCppCode(t, new PlainTextOutput(w));
                        }
                    }
                    /*using (StreamWriter w = new StreamWriter(Path.Combine(options.SaveAsProjectDirectory, file.Key)))
                    {
                        AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
                        foreach (TypeDefinition type in file)
                        {
                            codeDomBuilder.AddType(type);
                        }
                        PreProcess(module);
                        codeDomBuilder.RunTransformations(transformAbortCondition);
                        GenerateCplusplusCode(codeDomBuilder, new PlainTextOutput(w), "SmartQuant");
                    }*/
                });
            AstMethodBodyBuilder.PrintNumberOfUnhandledOpcodes();
            return files.Select(f => Tuple.Create("Compile", f.Key));
        }
        #endregion

        #region Utils
        internal static string CleanUpName(string text)
        {
            int pos = text.IndexOf(':');
            if (pos > 0)
                text = text.Substring(0, pos);
            pos = text.IndexOf('`');
            if (pos > 0)
                text = text.Substring(0, pos);
            text = text.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                text = text.Replace(c, '_');
            return text;
        }

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
                b.SyntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
                GenerateCode(b.SyntaxTree, new PlainTextOutput(w));
                return Regex.Replace(w.ToString(), @"\s+", " ").TrimEnd();
            }

            return base.GetTooltip(member);
        }
        #endregion
    }
}
