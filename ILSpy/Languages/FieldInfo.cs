using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public class FieldInfo
    {
        public FieldDefinition def;
        public Modifiers modifiers;
        public List<MethodDefinition> ReadByOther = new List<MethodDefinition>();
        public List<MethodDefinition> AssignByOther = new List<MethodDefinition>();
        public List<MethodDefinition> ReadByThis = new List<MethodDefinition>();
        public List<MethodDefinition> AssignByThis = new List<MethodDefinition>();

        PropertyDefinition property = null;
        AstNode decl = null;

        public AstNode Declaration
        {
            get
            {
                if (decl == null)
                {
                    decl = Util.getFieldDeclaration(this.def).Clone();
                }
                return decl;
            }
        }

        public PropertyDefinition DeclareProperty
        {
            get { return property; }
            set
            {
                property = value;
                if (property == null)
                    Console.Write("Error");
                if (property.Name == null)
                    Console.Write("Error");
                var name = Util.lowerFirstChar(property.Name);
                this.def.Name = "m_" + name;
                var info = InfoUtil.Info(value);
                if (info != null)
                    info.Field = this;
                decl = null;
            }
        }

        public string GetterMethodName
        {
            get
            {
                string name = def.Name;
                if (name.StartsWith("m_"))
                    name = name.Remove(0, 2);
                return "get" + Util.upperFirstChar(name);
            }
        }
        public string SetterMethodName
        {
            get
            {
                string name = def.Name;
                if (name.StartsWith("m_"))
                    name = name.Remove(0, 2);
                return "set" + Util.upperFirstChar(name);
            }
        }
        public string Name
        {
            get { return this.def.Name; }
        }

        public string FieldTypeName
        {
            get
            {
                bool isValueType;
                return Util.TypeString(this.def.FieldType, out isValueType);
            }
        }
        public FieldInfo(FieldDefinition f)
        {
            this.def = f;
            modifiers = Util.ConvertModifiers(f);
            Rename();
        }

        void Rename()
        {
            if (def.Name == "framework_0")
                def.Name = "m_framework";
            if (RenameUtil.FieldAndMethodRenameDict.ContainsKey(this.def.DeclaringType.FullName))
            {
                var info = RenameUtil.FieldAndMethodRenameDict[this.def.DeclaringType.FullName];
                var mdict = info.Item1;
                if (mdict.ContainsKey(this.def.Name))
                {
                    this.def.Name = mdict[this.def.Name];
                }
            }
        }

        void verify()
        {
            if (this.modifiers.HasFlag(Modifiers.Private) && (ReadByOther.Count() > 0 || AssignByOther.Count() > 0))
                Console.WriteLine("Verify Error");
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
