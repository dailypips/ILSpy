using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public class ParameterInfo
    {
        public MethodInfo minfo;
        public ParameterDefinition def;
        TypeInfo type;

        public ParameterInfo(MethodInfo info, ParameterDefinition def)
        {
            this.minfo = info;
            this.def = def;
            type = new TypeInfo(def.ParameterType);
        }

        public TypeInfo ParameterType
        {
            get { return this.type; }
        }

        public bool HasConstant
        {
            get { return this.def.HasConstant; }
        }
        public object Constant
        {
            get { return this.def.Constant; }
        }
        public string Name
        {
            get { return this.def.Name; }
        }

        public string ConstantValue
        {
            get
            {
                var p = this.def;

                if (p.HasConstant && p.Constant == null)
                {
                    if (p.ParameterType.MetadataType == MetadataType.String)
                        return "\"\"";
                    else
                        return "0";
                }

                if (p.HasConstant && p.Constant != null)
                {
                    if (p.Name == "currencyId" && p.Constant.ToString() == "148")
                        return ("CurrencyId::USD");
                    else
                    {
                        var c = p.Constant as string;
                        if (c != null)
                            return "\"" + p.Constant.ToString() +"\"";
                        else
                        {
                            if (this.type.isEnumType)
                            {
                                var t = this.def.ParameterType as TypeDefinition;
                                if (t == null)
                                    return p.Constant.ToString();
                                foreach (var field in t.Fields)
                                {
                                    if (field.HasConstant && field.Constant.ToString() == p.Constant.ToString())
                                    {
                                        return field.DeclaringType.Name + "::" + field.Name;
                                    }
                                }
                            }
                            else
                                return p.Constant.ToString().ToLower();
                        }
                    }
                }

                return "\"\"";
            }
        }
    }
}
