using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public class ParameterInfo
    {
        public ParameterDefinition def;

        public string ParameterTypeString
        {
            get
            {
                bool isValueType;
                return Util.TypeString(this.def.ParameterType, out isValueType);
            }
        }
        public TypeReference ParameterType
        {
            get { return this.def.ParameterType; }
        }
        public bool isEnumType
        {
            get
            {
                return true;
            }
        }
        public ParameterInfo(ParameterDefinition def)
        {
            this.def = def;
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

    }
}
