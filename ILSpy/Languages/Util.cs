using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.ILSpy;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuantKit
{
    class Util
    {
        #region FindFieldUsage
        public static bool IsFieldReadByMethod(MethodDefinition method, FieldDefinition analyzedField)
        {
            return FindFieldUsageInMethod(method, analyzedField, true);
        }

        public static bool IsFieldAssignByMethod(MethodDefinition method, FieldDefinition analyzedField)
        {
            return FindFieldUsageInMethod(method, analyzedField, false);
        }

        public static PropertyDefinition FindFieldReadByProperty(FieldDefinition analyzedField)
        {
            var type = analyzedField.DeclaringType as TypeDefinition;
            if (type != null)
            {
                foreach (var p in type.Properties)
                {
                    bool found = IsFieldReadByMethod(p.GetMethod, analyzedField);
                    if (found)
                        return p;
                }
            }
            return null;
        }

        public static MethodDefinition FindFieldReadByInType(TypeDefinition type, FieldDefinition analyzedField)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                bool found = IsFieldReadByMethod(method, analyzedField);
                if (found)
                    return method;
            }
            return null;
        }

        public static MethodDefinition FindFieldAssignByInType(TypeDefinition type, FieldDefinition analyzedField)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                bool found = IsFieldAssignByMethod(method, analyzedField);
                if (found)
                    return method;
            }
            return null;
        }

        #region internal field find 
        static bool FindFieldUsageInMethod(MethodDefinition method, FieldDefinition analyzedField, bool ReadByOrAssignBy)
        {
            if (method == null)
                return false;
            if (!method.HasBody)
                return false;
            bool found = false;
            string name = analyzedField.Name;
            foreach (Instruction instr in method.Body.Instructions)
            {
                bool isAccessBy;
                if (ReadByOrAssignBy)
                    isAccessBy = CanBeReadReference(instr.OpCode.Code);
                else
                    isAccessBy = CanBeAssignReference(instr.OpCode.Code);
                if (isAccessBy)
                {
                    FieldReference fr = instr.Operand as FieldReference;
                    if (fr != null && fr.Name == name &&
                        IsReferencedBy(analyzedField.DeclaringType, fr.DeclaringType) &&
                        fr.Resolve() == analyzedField)
                    {
                        found = true;
                        break;
                    }
                }
            }
            return found;
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
        #endregion
        #endregion

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

                if (found)
                    return method;
            }
            return null;
        }

        public static bool IsReferencedBy(TypeDefinition type, TypeReference typeRef)
        {
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

        public static string ReplaceCSharpTypeToCppType(string name)
        {
            switch (name)
            {
                case "List":
                    return "QList";
            }
            return name;
        }

        public static string ReplaceGenericType(string name)
        {
            string cname = name;
            int index = 0;
            for (index = 0; index < cname.Count(); ++index)
            {
                if (cname[index] == '`')
                    break;
            }
            cname = cname.Remove(index, cname.Count() - index);
            return ReplaceCSharpTypeToCppType(cname);
        }
        public static void AddInclude(string module, TypeReference typeRef, List<string>elist, List<string>clist, List<string>mlist)
        {
            MetadataType type = typeRef.MetadataType;
            switch(type)
            {
                case MetadataType.Boolean:
                case MetadataType.Byte:
                case MetadataType.SByte:
                case MetadataType.Char:
                case MetadataType.Double:
                case MetadataType.Int16:
                case MetadataType.Int32:
                case MetadataType.Int64:
                case MetadataType.UInt16:
                case MetadataType.UInt32:
                case MetadataType.UInt64:
                case MetadataType.Void:
                case MetadataType.IntPtr:
                case MetadataType.UIntPtr:
                case MetadataType.MVar:
                case MetadataType.Var:
                    break;
                case MetadataType.OptionalModifier:
                case MetadataType.RequiredModifier:
                case MetadataType.Pinned:
                case MetadataType.Sentinel:
                case MetadataType.Single:
                case MetadataType.FunctionPointer:
                case MetadataType.Pointer:
                case MetadataType.TypedByReference:
                    break;
                case MetadataType.Array:
                    var atype = typeRef as Mono.Cecil.ArrayType;
                    AddInclude(module, atype.GetElementType(), elist, clist, mlist);
                    break;
                case MetadataType.GenericInstance:
                    var gi = typeRef as GenericInstanceType;
                    AddInclude(module, gi.GetElementType(), elist, clist, mlist);
                    foreach (var item in gi.GenericArguments)
                        AddInclude(module, item, elist, clist, mlist);
                    break;
                case MetadataType.Class:
                    var cname = ReplaceGenericType(typeRef.Name);
                    if (cname == "BinaryReader" || cname == "BinaryWriter")
                        cname = "QByteArray";
                    var def = typeRef.Resolve();
                    if (typeRef.Namespace == module)
                    {
                        if ((def != null && def.IsInterface) || (def!=null && Helper.isClassAsEnum(def)))
                            mlist.Add(cname);
                        else
                            clist.Add(cname);
                    }
                    else
                        elist.Add(cname);
                    break;
                case MetadataType.Object:
                    elist.Add("QVariant");
                    break;
                case MetadataType.String:
                    elist.Add("QString");
                    break;
                case MetadataType.ValueType:
                    if (typeRef.Name == "DateTime")
                    {
                        elist.Add("QDateTime");
                    }
                    else
                    {
                        if (typeRef.Namespace == module)
                        {
                            var vdef = typeRef.Resolve();
                            if (vdef != null && (vdef.IsInterface || vdef.IsEnum))
                                mlist.Add(typeRef.Name);
                            else
                                clist.Add(typeRef.Name);
                        }
                        else
                            elist.Add(typeRef.Name);
                    }
                    break;
                case MetadataType.ByReference:
                    var refType = typeRef as Mono.Cecil.ByReferenceType;
                    AddInclude(module, refType.ElementType, elist, clist, mlist);
                    break;
                default:
                    break;
            }
        }

        public static string GetDefaultValue(TypeReference typeRef)
        {
            MetadataType type = typeRef.MetadataType;
            switch (type)
            {
                case MetadataType.Boolean:
                    return "false";
                case MetadataType.Byte:
                case MetadataType.SByte:
                case MetadataType.Char:
                case MetadataType.Double:
                case MetadataType.Int16:
                case MetadataType.Int32:
                case MetadataType.Int64:
                case MetadataType.UInt16:
                case MetadataType.UInt32:
                case MetadataType.UInt64:
                    return "0";
/*                case MetadataType.Array:
                    var atype = typeRef as Mono.Cecil.ArrayType;
                    return TypeString(atype.GetElementType(), out noused);
                case MetadataType.GenericInstance:
                    var gi = typeRef as GenericInstanceType;
                    string result = TypeString(gi.GetElementType(), out noused);
                    result += "<";
                    bool isFirst = true;
                    foreach (var item in gi.GenericArguments)
                    {
                        if (isFirst)
                            isFirst = false;
                        else
                            result += ",";
                        result += TypeString(item, out noused);
                    }
                    result += ">";
                    return result;
                case MetadataType.Class:
                    var cname = ReplaceGenericType(typeRef.Name);
                    return cname;
                case MetadataType.Object:
                    return "QVariant";
                case MetadataType.String:
                    return "QString";
                case MetadataType.ValueType:
                    if (typeRef.Name == "DateTime")
                    {
                        return "QDateTime";
                    }
                    return typeRef.Name;

                case MetadataType.MVar:
                    return typeRef.Name;
                case MetadataType.Var:
                    return typeRef.Name;

                case MetadataType.ByReference:
                    var refType = typeRef as Mono.Cecil.ByReferenceType;
                    TypeReference otype = refType.ElementType;
 //                   string refstring = TypeString(otype, out isValueType);
//                    return refstring + "&";
                case MetadataType.TypedByReference:
                    return typeRef.Name;

                case MetadataType.IntPtr:
                case MetadataType.UIntPtr:
                    return typeRef.Name;

                case MetadataType.FunctionPointer:
                case MetadataType.Pointer:
                    return typeRef.Name;

                case MetadataType.OptionalModifier:
                case MetadataType.RequiredModifier:
                case MetadataType.Pinned:
                case MetadataType.Sentinel:
                case MetadataType.Single:


                default:
                    isValueType = false;
                    return typeRef.Name;*/
                default:
                    return "null";
            }
        }
        public static string TypeString(TypeReference typeRef, out bool isValueType)
        {
            MetadataType type = typeRef.MetadataType;
            bool noused;
            switch (type)
            {
                case MetadataType.Boolean:
                    isValueType = true;
                    return "bool";
                case MetadataType.Byte:
                    isValueType = true;
                    return "unsigned char";
                case MetadataType.SByte:
                case MetadataType.Char:
                    isValueType = true;
                    return "char";
                case MetadataType.Double:
                    isValueType = true;
                    return "double";
                case MetadataType.Int16:
                    isValueType = true;
                    return "short";
                case MetadataType.Int32:
                    isValueType = true;
                    return "int";
                case MetadataType.Int64:
                    isValueType = true;
                    return "long";
                case MetadataType.UInt16:
                    isValueType = true;
                    return "unsigned short";
                case MetadataType.UInt32:
                    isValueType = true;
                    return "quint32";
                case MetadataType.UInt64:
                    isValueType = true;
                    return "quint64";
                case MetadataType.Void:
                    isValueType = false;
                    return "void";
                case MetadataType.Array:
                    var atype = typeRef as Mono.Cecil.ArrayType;
                    isValueType = true;
                    return TypeString(atype.GetElementType(), out noused);
                case MetadataType.GenericInstance:
                    isValueType = false;
                    var gi = typeRef as GenericInstanceType;
                    string result = TypeString(gi.GetElementType(), out noused);
                    result += "<";
                    bool isFirst = true;
                    foreach (var item in gi.GenericArguments)
                    {
                        if (isFirst)
                            isFirst = false;
                        else
                            result += ",";
                        result += TypeString(item, out noused);
                    }
                    result += ">";
                    return result;
                case MetadataType.Class:
                    isValueType = false;
                    var cname = ReplaceGenericType(typeRef.Name);
                    if (cname == "BinaryReader" || cname == "BinaryWriter")
                        cname = "QByteArray";
                    return cname;
                case MetadataType.Object:
                    isValueType = false;
                    return "QVariant";
                case MetadataType.String:
                    isValueType = false;
                    return "QString";
                case MetadataType.ValueType:
                    if (typeRef.Name == "DateTime")
                    {
                        isValueType = false;
                        return "QDateTime";
                    }
                    isValueType = true;
                    return typeRef.Name;

                case MetadataType.MVar:
                    isValueType = false;
                    return typeRef.Name;
                case MetadataType.Var:
                    isValueType = false;
                    return typeRef.Name;
                
                case MetadataType.ByReference:
                    var refType = typeRef as Mono.Cecil.ByReferenceType;
                    isValueType = false;
                    TypeReference otype = refType.ElementType;
                    string refstring = TypeString(otype, out isValueType);
                    return refstring + "&";
                case MetadataType.TypedByReference:
                    isValueType = false;
                    return typeRef.Name;

                case MetadataType.IntPtr:
                case MetadataType.UIntPtr:
                    isValueType = true;
                    return typeRef.Name;

                case MetadataType.FunctionPointer:
                case MetadataType.Pointer:
                    isValueType = true;
                    return typeRef.Name;

                case MetadataType.OptionalModifier:
                case MetadataType.RequiredModifier:
                case MetadataType.Pinned:
                case MetadataType.Sentinel:
                case MetadataType.Single:


                default:
                    isValueType = false;
                    return typeRef.Name;
            }
        }

        public static AstBuilder CreateAstBuilder(ModuleDefinition currentModule = null, TypeDefinition currentType = null, bool isSingleMember = false)
        {
            if (currentModule == null)
                currentModule = currentType.Module;
            var options = new DecompilationOptions();
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
        static void AddFieldsAndCtors(AstBuilder codeDomBuilder, TypeDefinition declaringType, bool isStatic)
        {
            foreach (var field in declaringType.Fields)
            {
                if (field.IsStatic == isStatic)
                    codeDomBuilder.AddField(field);
            }
            foreach (var ctor in declaringType.Methods)
            {
                if (ctor.IsConstructor && ctor.IsStatic == isStatic)
                    codeDomBuilder.AddMethod(ctor);
            }
        }
        static void RunTransformsAndGenerateCode(AstBuilder astBuilder, IAstTransform additionalTransform = null)
        {
            astBuilder.RunTransformations(null);
            if (additionalTransform != null)
            {
                additionalTransform.Run(astBuilder.SyntaxTree);
            }
        }

        sealed class SelectFieldTransform : IAstTransform
        {
            readonly FieldDefinition field;

            public SelectFieldTransform(FieldDefinition field)
            {
                this.field = field;
            }

            public void Run(AstNode compilationUnit)
            {
                foreach (var child in compilationUnit.Children)
                {
                    if (child is EntityDeclaration)
                    {
                        if (child.Annotation<FieldDefinition>() != field)
                            child.Remove();
                    }
                }
            }
        }

        class SelectCtorTransform : IAstTransform
        {
            readonly MethodDefinition ctorDef;

            public SelectCtorTransform(MethodDefinition ctorDef)
            {
                this.ctorDef = ctorDef;
            }

            public void Run(AstNode compilationUnit)
            {
                ConstructorDeclaration ctorDecl = null;
                foreach (var node in compilationUnit.Children)
                {
                    ConstructorDeclaration ctor = node as ConstructorDeclaration;
                    if (ctor != null)
                    {
                        if (ctor.Annotation<MethodDefinition>() == ctorDef)
                        {
                            ctorDecl = ctor;
                        }
                        else
                        {
                            // remove other ctors
                            ctor.Remove();
                        }
                    }
                    // Remove any fields without initializers
                    FieldDeclaration fd = node as FieldDeclaration;
                    if (fd != null && fd.Variables.All(v => v.Initializer.IsNull))
                        fd.Remove();
                }
                if (ctorDecl.Initializer.ConstructorInitializerType == ConstructorInitializerType.This)
                {
                    // remove all fields
                    foreach (var node in compilationUnit.Children)
                        if (node is FieldDeclaration)
                            node.Remove();
                }
            }
        }

        public static AstNode getMethodDeclaration(MethodDefinition method)
        {
            var codeDomBuilder = CreateAstBuilder(currentType: method.DeclaringType, isSingleMember: true);
            if (method.IsConstructor && !method.IsStatic && !method.DeclaringType.IsValueType)
            {
                // also fields and other ctors so that the field initializers can be shown as such
                AddFieldsAndCtors(codeDomBuilder, method.DeclaringType, method.IsStatic);
                RunTransformsAndGenerateCode(codeDomBuilder, new SelectCtorTransform(method));
            }
            else
            {
                codeDomBuilder.AddMethod(method);
                RunTransformsAndGenerateCode(codeDomBuilder);
            }
            codeDomBuilder.SyntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            return codeDomBuilder.SyntaxTree.Members.ToList()[0];
        }

        public static AstNode getTypeDeclaration(TypeDefinition type)
        {
            var codeDomBuilder = CreateAstBuilder(currentType: type, isSingleMember: true);
            codeDomBuilder.AddType(type);
            RunTransformsAndGenerateCode(codeDomBuilder);
            codeDomBuilder.SyntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            return codeDomBuilder.SyntaxTree.Members.ToList()[0];
        }

        public static AstNode getFieldDeclaration(FieldDefinition field)
        {
            var codeDomBuilder = CreateAstBuilder(currentType: field.DeclaringType, isSingleMember: true);
            if (field.IsLiteral)
            {
                codeDomBuilder.AddField(field);
            }
            else
            {
                // also decompile ctors so that the field initializer can be shown
                AddFieldsAndCtors(codeDomBuilder, field.DeclaringType, field.IsStatic);
            }
            RunTransformsAndGenerateCode(codeDomBuilder, new SelectFieldTransform(field));
            codeDomBuilder.SyntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            return codeDomBuilder.SyntaxTree.Members.ToList()[0];
        }

        public static AstNode getPropertyDeclaration(PropertyDefinition property)
        {
            AstBuilder codeDomBuilder = CreateAstBuilder(currentType: property.DeclaringType, isSingleMember: true);
            codeDomBuilder.AddProperty(property);
            RunTransformsAndGenerateCode(codeDomBuilder);
            codeDomBuilder.SyntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            return codeDomBuilder.SyntaxTree.Members.ToList()[0];
        }

        public static AstNode getEventDeclaration(EventDefinition ev)
        {
            AstBuilder codeDomBuilder = CreateAstBuilder(currentType: ev.DeclaringType, isSingleMember: true);
            codeDomBuilder.AddEvent(ev);
            RunTransformsAndGenerateCode(codeDomBuilder);
            codeDomBuilder.SyntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            return codeDomBuilder.SyntaxTree.Members.ToList()[0];
        }

        /*public static string TypeToString(TypeReference type, out bool isPrimitiveType)
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
                return result;
            }
 
            if (type.HasGenericParameters)
            {

            }
            else if (type.IsArray)
            {

            }
            else if (type.IsVoid())
            {
                return "void";
            }
            else if (type.is)
            if (result == "String")
                result = "QString";
            else if (result == "DateTime")
                result = "QDateTime";
            else if (result == "Void")
                result = "void";
            else if (result == "List")
                result = "QList";
            else if (result == "Object")
                result = "QVariant";
            return result;
        }*/

        public static TypeDefinition GetTypeDef(AstType type)
        {
            var td = type.Annotation<TypeDefinition>();

            if (td == null)
            {
                var tr = type.Annotation<TypeReference>();
                if (tr != null)
                {
                    td = tr.Resolve();
                }
            }

            return td;
        }

        public static TypeReference GetTypeRef(AstType type)
        {
            return type.Annotation<TypeDefinition>() as TypeReference ?? type.Annotation<TypeReference>();
        }

        /*public static string TypeToString(AstType type, out bool isPrimitiveType)
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
        }*/

        public static bool needFieldConvertToGetterMethod(TypeDefinition def, FieldDefinition analyzedField)
        {
            var module = def.Module;
            bool result = false;
            foreach (var t in module.Types)
            {
                if (t == def)
                    continue;
                var m = FindFieldReadByInType(t, analyzedField);
                if (m != null)
                    return true;
            }

            foreach (var m in def.Methods)
            {
                var found = IsFieldReadByMethod(m, analyzedField);
                if (found)
                    return true;
            }
            return result;
        }

        public static bool needFieldConvertToSetterMethod(TypeDefinition def, FieldDefinition analyzedField)
        {
            var module = def.Module;
            bool result = false;
            foreach (var t in module.Types)
            {
                if (t == def)
                    continue;
                var m = FindFieldAssignByInType(t, analyzedField);
                if (m != null)
                    return true;
            }

            foreach (var m in def.Methods)
            {
                var found = IsFieldAssignByMethod(m, analyzedField);
                if (found)
                    return true;
            }
            return result;
        }

        #region ConvertModifiers
        public static Modifiers ConvertModifiers(TypeDefinition typeDef)
        {
            Modifiers modifiers = Modifiers.None;
            if (typeDef.IsNestedPrivate)
                modifiers |= Modifiers.Private;
            else if (typeDef.IsNestedAssembly || typeDef.IsNestedFamilyAndAssembly || typeDef.IsNotPublic)
                modifiers |= Modifiers.Internal;
            else if (typeDef.IsNestedFamily)
                modifiers |= Modifiers.Protected;
            else if (typeDef.IsNestedFamilyOrAssembly)
                modifiers |= Modifiers.Protected | Modifiers.Internal;
            else if (typeDef.IsPublic || typeDef.IsNestedPublic)
                modifiers |= Modifiers.Public;

            if (typeDef.IsAbstract && typeDef.IsSealed)
                modifiers |= Modifiers.Static;
            else if (typeDef.IsAbstract)
                modifiers |= Modifiers.Abstract;
            else if (typeDef.IsSealed)
                modifiers |= Modifiers.Sealed;

            return modifiers;
        }

        public static Modifiers ConvertModifiers(FieldDefinition fieldDef)
        {
            Modifiers modifiers = Modifiers.None;
            if (fieldDef.IsPrivate)
                modifiers |= Modifiers.Private;
            else if (fieldDef.IsAssembly || fieldDef.IsFamilyAndAssembly)
                modifiers |= Modifiers.Internal;
            else if (fieldDef.IsFamily)
                modifiers |= Modifiers.Protected;
            else if (fieldDef.IsFamilyOrAssembly)
                modifiers |= Modifiers.Protected | Modifiers.Internal;
            else if (fieldDef.IsPublic)
                modifiers |= Modifiers.Public;

            if (fieldDef.IsLiteral)
            {
                modifiers |= Modifiers.Const;
            }
            else
            {
                if (fieldDef.IsStatic)
                    modifiers |= Modifiers.Static;

                if (fieldDef.IsInitOnly)
                    modifiers |= Modifiers.Readonly;
            }

            RequiredModifierType modreq = fieldDef.FieldType as RequiredModifierType;
            if (modreq != null && modreq.ModifierType.FullName == typeof(IsVolatile).FullName)
                modifiers |= Modifiers.Volatile;

            return modifiers;
        }

        public static Modifiers ConvertModifiers(MethodDefinition methodDef)
        {
            if (methodDef == null)
                return Modifiers.None;
            Modifiers modifiers = Modifiers.None;
            if (methodDef.IsPrivate)
                modifiers |= Modifiers.Private;
            else if (methodDef.IsAssembly || methodDef.IsFamilyAndAssembly)
                modifiers |= Modifiers.Internal;
            else if (methodDef.IsFamily)
                modifiers |= Modifiers.Protected;
            else if (methodDef.IsFamilyOrAssembly)
                modifiers |= Modifiers.Protected | Modifiers.Internal;
            else if (methodDef.IsPublic)
                modifiers |= Modifiers.Public;

            if (methodDef.IsStatic)
                modifiers |= Modifiers.Static;

            if (methodDef.IsAbstract)
            {
                modifiers |= Modifiers.Abstract;
                if (!methodDef.IsNewSlot)
                    modifiers |= Modifiers.Override;
            }
            else if (methodDef.IsFinal)
            {
                if (!methodDef.IsNewSlot)
                {
                    modifiers |= Modifiers.Sealed | Modifiers.Override;
                }
            }
            else if (methodDef.IsVirtual)
            {
                if (methodDef.IsNewSlot)
                    modifiers |= Modifiers.Virtual;
                else
                    modifiers |= Modifiers.Override;
            }
            if (!methodDef.HasBody && !methodDef.IsAbstract)
                modifiers |= Modifiers.Extern;

            return modifiers;
        }

        #endregion

        public static TypeDefinition GetTypeDefinition(string name)
        {
            foreach (var module in InfoUtil.ModuleInfoDict.Keys)
            {
                var def = GetTypeDefinition(module, name);
                if (def != null)
                    return def;
            }
            return null;
        }
        public static TypeDefinition GetTypeDefinition(ModuleDefinition module, string name)
        {
            foreach (var t in module.Types)
                if (t.Name == name)
                    return t;
            return null;
        }

        public static bool isInhertFrom(TypeDefinition def, string baseClass)
        {
            if (def.BaseType != null && def.BaseType.FullName == baseClass)
                return true;
            else
                return false;
        }

        public static bool isInhertFrom(TypeDefinition def, TypeDefinition @base)
        {
            var d = def.BaseType as TypeDefinition;
            while (d != null)
            {
                if (d == @base)
                    return true;
                d = d.BaseType as TypeDefinition;
            }
            return false;
        }
        public static string lowerFirstChar(string s)
        {
            return  (s.Count() > 1 && Char.IsUpper(s[1])) ? s :
                            Char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static string upperFirstChar(string s)
        {
            return Char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
