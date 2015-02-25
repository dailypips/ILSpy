using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public class TypeInfo
    {
        TypeReference reference;
        bool is_genesis_type = false;
        bool is_external_type = false;
        bool isValueType = false;
        string typename;
        public TypeInfo(TypeReference reference)
        {
            this.reference = reference;
        }
        public TypeInfo(TypeDefinition def)
        {
            this.reference = def;
        }
        public TypeInfo(string typename)
        {
            this.typename = typename;
            this.is_external_type = true;
        }
        public bool isPrimitiveType
        {
            get
            {
                if (is_external_type)
                    return false;
                else
                    return this.reference.IsPrimitive;
            }
        }
        public bool IsGenericType
        {
            get
            {
                return false;
            }
        }

        public bool isEnumType
        {
            get
            {
                if (is_external_type)
                    return false;
                var def = this.reference.Resolve();
                if (def == null)
                    return false;
                var info = InfoUtil.Info(def);
                if (info == null)
                    return def.IsEnum;
                else
                    return info.IsValueType;
            }
        }
        public string TypeName
        {
            get
            {
                return null;
            }
        }
        /*public string Name
        {
            get { return base.Name; }
            set
            {
                base.Name = value;
                fullname = null;
            }
        }

        public string Namespace
        {
            get { return @namespace; }
            set
            {
                @namespace = value;
                fullname = null;
            }
        }

        public virtual bool IsValueType
        {
            get { return value_type; }
            set { value_type = value; }
        }

        public ModuleDefinition Module
        {
            get
            {
                if (is_external_type)
                    return null;
                else
                    return this.reference.Module;
            }
        }*/
        public string DefaultValue
        {
            get
            {
                MetadataType type = this.reference.MetadataType;
                switch (type)
                {
                    case MetadataType.Boolean:
                        return "false";
                    default:
                        return "0";
                }
            }
        }

        string TypeString(TypeReference typeRef)
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
    }
}
