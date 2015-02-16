using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public interface GenerateCodeAbled
    {
        void GenerateCode(TextFormatter f);
        void GenerateHppCode(TextFormatter f);
        void GenerateCppCode(TextFormatter f);
        void GeneratePrivateCode(TextFormatter f);
    }

    public class CModule
    {
        public QModule origin;
        public List<CClass> classes = new List<CClass>();
        public List<CEnum> enums = new List<CEnum>();
        public CModule(QModule parent)
        {
            origin = parent;
        }
    }

    public class CType
    {
        public string name;
        public bool isPrimitive = false;
        public bool isModuleType = false;
        public bool isEnumType = false;
        public bool isArray = false;
        public bool isVoid = false;
    }

    public class CEntiry
    {
        public QType origin;
        public CModule module;
        public string ns;
        public string name;
        
        public bool isClass = false;
        public bool isEnum = false;
        public bool isInterface = false;
    }

    public class CClass : CEntiry, GenerateCodeAbled
    {
        public List<CType> bases = new List<CType>();
        public List<CType> devires = new List<CType>();
        public List<CMethod> methods = new List<CMethod>();
        public List<CField> fields = new List<CField>();
        public CClass(CModule module) : base()
        {
            this.isClass = true;
            this.module = module;
            this.module.classes.Add(this);
        }

        public void GenerateCode(TextFormatter f)
        {

        }
        public void GenerateHppCode(TextFormatter f)
        {

        }
        public void GenerateCppCode(TextFormatter f)
        {

        }
        public void GeneratePrivateCode(TextFormatter f)
        {

        }
    }
    public class CEnumMember
    {
        public string name;
        public string optionValue;
    }

    public class CEnum : CEntiry, GenerateCodeAbled
    {
        public List<CEnumMember> members = new List<CEnumMember>();
        public CEnum(CModule module) : base()
        {
            this.isEnum = true;
            this.module = module;
            this.module.enums.Add(this);
        }
        public void GenerateCode(TextFormatter f)
        {
            GenerateHppCode(f);
            f.NewLine();
            GeneratePrivateCode(f);
            f.NewLine();
            GenerateCppCode(f);
        }
        public void GenerateHppCode(TextFormatter f)
        {

        }
        public void GenerateCppCode(TextFormatter f)
        {

        }
        public void GeneratePrivateCode(TextFormatter f)
        {

        }
    }

    public class CParameter
    {
        public CType type;
        public string name;
        public string optionValue;

    }

    public class CMethod : GenerateCodeAbled
    {
        public object origin;
        public string name;
        public CType rtype;
        public List<CParameter> parameters = new List<CParameter>();
        public List<string> body = new List<string>();
        public bool isCtor = false;
        public bool isIndexer = false;
        public bool isPublic = false;
        public bool isInternal = false;
        public bool isPrivate = false;
        public bool isProtected = false;
        public bool isOverride = false;
        public bool isVirtual = false;
        public bool isAbstract = false;
        public bool isStatic = false;
        public bool isGetter = false;
        public bool isSetter = false;
        public bool isAddon = false;
        public bool isRemoveOn = false;
        public bool isFire = false;
        public bool isFieldGetter = false;
        public bool isFieldSetter = false;
        public CClass parent;

        public CMethod(CClass parent)
        {
            this.parent = parent;
            this.parent.methods.Add(this);
        }
        public void GenerateCode(TextFormatter f)
        {
            GenerateHppCode(f);
            f.NewLine();
            GeneratePrivateCode(f);
            f.NewLine();
            GenerateCppCode(f);
        }
        public void GenerateHppCode(TextFormatter f)
        {

        }
        public void GenerateCppCode(TextFormatter f)
        {

        }
        public void GeneratePrivateCode(TextFormatter f)
        {

        }
    }

    public class CField : GenerateCodeAbled
    {
        public object origin;
        public string name;
        public CType type;
        public string optionValue;
        public bool isCompilerGenerated = false;
        public bool isPublic = false;
        public bool isInternal = false;
        public bool isConst = false;
        public CClass parent;
        public List<CMethod> ReadBy = new List<CMethod>();
        public List<CMethod> AssignBy = new List<CMethod>();
        public CMethod Getter = null;
        public CMethod Setter = null;
        public CField(CClass parent)
        {
            this.parent = parent;
            this.parent.fields.Add(this);
        }

        public void Rename()
        {
            var set = new HashSet<QProperty>();
            var qfield = origin as QField;
            string newName = null;
            foreach (var item in ReadBy)
            {
                var prop = item.origin as QProperty;
                if (prop != null && qfield != null && prop.parent == qfield.parent)
                {
                    newName = prop.def.Name;
                    break;
                }
            }
            if(newName == null) {
                foreach (var item in AssignBy)
                {
                    var prop = item.origin as QProperty;
                    if (prop != null && qfield != null && prop.parent == qfield.parent)
                    {
                        newName = prop.def.Name;
                        break;
                    }
                }
            }

            if (newName != null)
            {
                foreach (var item in ReadBy)
                {
                    for (int i = 0; i < item.body.Count(); ++i)
                    {
                        item.body[i] = item.body[i].Replace(name, "get"+newName+"()");
                        
                    }
                    Console.Write("ForDebug");
                }
                foreach (var item in AssignBy)
                {
                    for (int i = 0; i < item.body.Count(); ++i)
                    {
                        item.body[i] = item.body[i].Replace(name, "set" + newName + "()");

                    }
                    Console.Write("ForDebug");
                }

                if(Getter != null)
                {
                    Getter.name = "get" + newName;
                }
                if(Setter != null)
                {
                    Setter.name = "set" + newName;
                }

                this.name = "m_" + newName;
            }
        }

        public void GenerateCode(TextFormatter f)
        {
            GenerateHppCode(f);
            f.NewLine();
            GeneratePrivateCode(f);
            f.NewLine();
            GenerateCppCode(f);
        }
        public void GenerateHppCode(TextFormatter f)
        {

        }
        public void GenerateCppCode(TextFormatter f)
        {

        }
        public void GeneratePrivateCode(TextFormatter f)
        {

        }
    }

}
