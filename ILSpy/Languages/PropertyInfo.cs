using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public class PropertyInfo
    {
        public PropertyDefinition def;

        AstNode decl = null;
        FieldInfo field = null;
        public AstNode Declaration
        {
            get
            {
                if (decl == null)
                {
                    decl = Util.getPropertyDeclaration(this.def).Clone();
                }
                return decl;
            }
        }

        public string Name
        {
            get { return this.def.Name; }
        }

        public FieldInfo Field
        {
            get { return this.field; }
            set { this.field = value; }
        }
        public PropertyInfo(PropertyDefinition p)
        {
            this.def = p;
        }

        internal void post()
        {

        }

        internal void inValidCache()
        {
            this.decl = null;
        }
    }
}
