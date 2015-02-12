using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.ILSpy.Languages
{

    class asttype
    {
        TypeDefinition tdef;
        TypeDeclaration tdec;
        List<asttype> bases;
        List<asttype> children;
    }

    class astmethod
    {
        MethodDefinition mdef;
        MethodDeclaration mdec;
    }

    class astproperty
    {
        PropertyDefinition pdef;
        PropertyDeclaration pdec;
    }

    class astfield
    {
        FieldDefinition fdef;
        FieldDeclaration fdec;
    }

    class astevent
    {
        EventDefinition edef;
        EventDeclaration edec;
    }

}
