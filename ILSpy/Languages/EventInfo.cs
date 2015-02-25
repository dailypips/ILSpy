using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public class EventInfo
    {
        public EventDefinition def;
        AstNode decl = null;

        public AstNode Declaration
        {
            get
            {
                if (decl == null)
                {
                    decl = Util.getEventDeclaration(def).Clone();
                }
                return decl;
            }
        }
        public string Name
        {
            get { return this.def.Name; }
        }
        public EventInfo(EventDefinition def)
        {
            this.def = def;
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
