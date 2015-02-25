using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.IO;
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
            set { 
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
    internal class CtorInitInfo
    {
        internal FieldDefinition field;
        internal string initExpr;
        internal string expr;
    }
    public class MethodInfo
    {
        public MethodDefinition def;
        public Modifiers modifiers;
        public List<MethodDefinition> UsedByOther = new List<MethodDefinition>();
        public List<MethodDefinition> UsedByThis = new List<MethodDefinition>();
        public string baseInit = null;
        internal List<CtorInitInfo> initList = new List<CtorInitInfo>();
        public List<string> method_body = null;
        AstNode decl = null;

        public AstNode Declaration
        {
            get
            {
                if (decl == null)
                {
                    decl = Util.getMethodDeclaration(this.def).Clone();
                }
                return decl;
            }
        }
        public string Name
        {
            get
            {
                if (this.def.IsGetter || this.def.IsSetter)
                {
                    if (this.def.Name.StartsWith("get_Is"))
                    {
                        return "is" + this.def.Name.Substring(6);
                    }
                    else
                    {
                        return this.def.Name.Remove(3, 1);
                    }
                }
                else
                {
                    if (this.def.IsAddOn || this.def.IsRemoveOn)
                    {
                        if (this.def.IsAddOn)
                            return this.def.Name.Remove(3, 1);
                        else
                            return this.def.Name.Remove(6, 1);
                    }
                    else
                    {
                        if (this.def.Name == "ToString")
                            return "toString";
                        else
                            return this.def.Name;
                    }
                }
            }
        }
        public string ReturnTypeName
        {
            get
            {
                bool isValueType;
                return Util.TypeString(this.def.ReturnType, out isValueType);
            }
        }
        public bool ReturnTypeIsVoid
        {
            get
            {
                return this.def.ReturnType.MetadataType == MetadataType.Void;
            }
        }

        public bool isCopyConstructor
        {
            get
            {
                if (this.def.IsConstructor && this.def.Parameters.Count() == 1 && this.def.Parameters[0].ParameterType == this.def.DeclaringType)
                    return true;
                else
                    return false;
            }
        }

        public TypeReference ReturnType
        {
            get
            {
                return this.def.ReturnType;
            }
        }
        public bool IsConstructor
        {
            get { return this.def.IsConstructor; }
        }

        public bool IsGetter
        {
            get { return this.def.IsGetter; }
        }
        public Collection<ParameterDefinition> Parameters
        {
            get { return this.def.Parameters; }
        }
        public bool IsSetter
        {
            get { return this.def.IsSetter; }
        }
        public bool isAddOn
        {
            get { return this.def.IsAddOn; }
        }
        public bool isRemoveOn
        {
            get { return this.isRemoveOn; }
        }
        public bool isNullConstructor
        {
            get
            {
                return this.def.IsConstructor && this.def.Parameters.Count() == 0;
            }
        }

        void special_tostring()
        {
            if (this.method_body.Count() > 0 && this.def.Name.ToLower() == "tostring")
            {
                if (this.method_body[0].Trim() == "return string.Concat(new object[]")
                {
                    List<string> newbody = new List<string>();
                    newbody.Add("\treturn");
                    for (int i = 1; i < this.method_body.Count(); ++i)
                    {
                        var s = this.method_body[i];
                        if (s.Trim() == "{" || s.Trim()=="});")
                            continue;
                        if (s.Contains(","))
                            newbody.Add(s.Replace(",", "+").Trim());
                        else
                            newbody.Add(s.Trim() + ";");
                    }
                    this.method_body = newbody;
                }
            }
        }

        void special_init()
        {
            List<CtorInitInfo> ctorInitList = new List<CtorInitInfo>();
            // is sample member init
            foreach (var minit in this.method_body)
            {
                var vlist = minit.Replace(";", "").Split('=');
                if (vlist.Count() == 2)
                {
                    var variable = vlist[0].Trim();
                    var expr = vlist[1].Trim();
                    foreach (var f in this.def.DeclaringType.Fields)
                    {
                        if (variable == f.Name)
                        {
                            var cinfo = new CtorInitInfo();
                            cinfo.field = f;
                            cinfo.initExpr = expr;
                            cinfo.expr = minit;
                            ctorInitList.Add(cinfo);
                            break;
                        }
                    }
                }
            }
            foreach (var cinfo in ctorInitList)
            {
                this.method_body.Remove(cinfo.expr);
            }

            // reorder list by filed order
            this.initList.Clear();
            foreach (var field in this.def.DeclaringType.Fields)
            {
                var info = InfoUtil.Info(field);
                if (info.modifiers.HasFlag(Modifiers.Const))
                    continue;
                foreach (var cinfo in ctorInitList)
                {
                    if (field == cinfo.field)
                        this.initList.Add(cinfo);
                }
            }
        }

        void special_ctor()
        {
            int baseCotrIndex = -1;
            List<Tuple<string, int>> alist = new List<Tuple<string, int>>(); // all localfield init before ctor
            List<Tuple<string, int>> blist = new List<Tuple<string, int>>(); // all localfield dup init list after ctor; blist < alist
            List<string> keepList = new List<string>(); // no dup list for local field init = alist - blist

            baseInit = null;
            for (int i = 0; i < method_body.Count(); ++i)
            {
                if (method_body[i].Contains("base..ctor"))
                {
                    baseCotrIndex = i;
                    var baselist = method_body[i].Replace(");", "").Split('(');
                    if (baselist.Count() == 2 && baselist[1].Trim()!="")
                        baseInit = baselist[1];
                    break;
                }
            }
            if (baseCotrIndex > 0)
            {
                // try to find field init
                for (int i = 0; i <= baseCotrIndex; ++i)
                {
                    if (!method_body[i].Contains("Class"))
                    {
                        var slist = method_body[i].Split('=');
                        if (slist.Count() == 2)
                        {
                            alist.Add(Tuple.Create(slist[0].Trim(), i));
                        }
                    }
                }
                if (alist.Count() > 0)
                {
                    for (int j = baseCotrIndex + 1; j < method_body.Count(); ++j)
                    {
                        var s = method_body[j];
                        var slist = s.Split('=');
                        if (slist.Count() == 2)
                        {
                            var localVar = slist[0];
                            foreach (var l in alist)
                            {
                                if (l.Item1 == localVar)
                                {
                                    blist.Add(l);
                                }
                            }
                        }
                    }

                    // remove dup var, find in alist && not in blist && save it in keepList
                    foreach (var aitem in alist)
                    {
                        bool isFound = false;
                        foreach (var bitem in blist)
                        {
                            if (aitem == bitem)
                            {
                                isFound = true;
                                break;
                            }
                        }
                        if (!isFound)
                        {
                            var s = method_body[aitem.Item2];
                            keepList.Add(s);
                        }
                    }
                }
                // remove method_body for base..ctor
                method_body.RemoveRange(0, baseCotrIndex + 1);
                method_body.InsertRange(0, keepList);
            }
        }
        public List<string> CtorInitBody
        {
            get {
                if (this.method_body == null)
                    build_method_body();
                bool isFirst= true;
                string line;
                List<string> olist = new List<string>();
                if (this.baseInit != null)
                {
                    olist.Add(": "+ this.def.DeclaringType.BaseType.Name + "Private (" + this.baseInit + ")");
                    isFirst = false;
                }
                if (isFirst)
                {
                    line = ": ";
                    isFirst = false;
                }
                else line = ", ";
                foreach (var cinfo in this.initList)
                {
                    if (cinfo.field.Name == "m_currencyId" && cinfo.initExpr.Trim() == "148")
                        olist.Add(line + cinfo.field.Name + "(CurrencyId::USD)");
                    else
                        olist.Add(line + cinfo.field.Name + "(" + cinfo.initExpr.Replace("List<", "QList<") + ")");
                }
                return olist; 
            }
        }
        internal void build_method_body()
        {
            method_body = new List<string>();
            var builder = new StringWriter();
            var output = new PlainTextOutput(builder);
            var node = this.Declaration;
            var outputFormatter = new TextOutputFormatter(output) { FoldBraces = true };
            var formattingPolicy = FormattingOptionsFactory.CreateAllman();
            node.AcceptVisitor(new CppOutputVisitor(outputFormatter, formattingPolicy));
            //
            string b = builder.ToString();
            var bb = b.Replace("\r\n", "\n");
            var bodylist = bb.Split('\n');
            var tlist = new List<string>();
            foreach (var item in bodylist)
            {
                var stat = item;
                tlist.Add(stat);
            }
            tlist.RemoveAt(0); // delete method define
            if (tlist.Count() > 0)
            {
                if (tlist[0].Trim() == "get" || tlist[0].Trim() == "set")
                    tlist.RemoveAt(0);

                if (tlist[0].Trim() == "{")
                    tlist.RemoveAt(0);

                while (tlist.Count() > 0)
                {
                    if (tlist[tlist.Count() - 1].Trim() == "")
                        tlist.RemoveAt(tlist.Count() - 1);
                    else
                        break;
                }

                if (tlist.Count() > 0)
                {
                    if (tlist[tlist.Count() - 1].Trim() == "}")
                        tlist.RemoveAt(tlist.Count() - 1);
                }
            }
            foreach (var s in tlist)
                this.method_body.Add(s);

            if (this.def.IsConstructor)
            {
                special_ctor();
                special_init();
            }
            else
            {
                special_tostring();
            }
        }
        public List<string> MethodBody
        {
            get
            {
                if (this.method_body == null)
                {
                    build_method_body();
                }
                return method_body;
            }
        }
        public PropertyDefinition Property
        {
            get
            {
                if (this.def.IsGetter || this.def.IsSetter)
                {
                    foreach (var p in this.def.DeclaringType.Properties)
                    {
                        if(this.def.IsGetter)
                        {
                            if (p.GetMethod == this.def)
                            {
                                return p;
                            }
                        }
                        else
                        {
                            if (p.SetMethod == this.def)
                                return p;
                        }
                    }
                }
                return null;
            }
        }

        public EventDefinition Event
        {
            get
            {
                if (this.def.IsAddOn || this.def.IsRemoveOn)
                {
                    foreach (var e in this.def.DeclaringType.Events)
                    {
                        if (this.def.IsAddOn)
                        {
                            if (e.AddMethod == this.def)
                                return e;
                        }
                        else
                        {
                            if (e.RemoveMethod == this.def)
                                return e;
                        }
                    }
                }
                return null;
                
            }
        }
        public MethodInfo(MethodDefinition m)
        {
            this.def = m;
            this.modifiers = Util.ConvertModifiers(m);
            if (m.Name == "ToString")
                modifiers = modifiers | Modifiers.Virtual;
            Rename();
        }
        
        void Rename()
        {
            if (RenameUtil.FieldAndMethodRenameDict.ContainsKey(this.def.DeclaringType.FullName))
            {
                var info = RenameUtil.FieldAndMethodRenameDict[this.def.DeclaringType.FullName];
                var mdict = info.Item2;
                if (mdict.ContainsKey(this.def.Name))
                {
                    this.def.Name = mdict[this.def.Name];
                }
            }
        }
        
        void verify()
        {
            if (this.modifiers.HasFlag(Modifiers.Private) && UsedByOther.Count() > 0)
                Console.WriteLine("Verify Error");
        }

        void RenameParametersName()
        {
            foreach (var p in this.def.Parameters)
            {
                if (p.Name.EndsWith("_0"))
                {
                    bool isFound = false;
                    foreach (var pp in this.def.Parameters)
                    {
                        if (pp == p)
                            continue;
                        if(pp.ParameterType == p.ParameterType)
                        {
                            isFound = true;
                            break;
                        }
                    }
                    if (!isFound)
                        p.Name = p.Name.Remove(p.Name.Count() - 2, 2);
                }
            }
        }
        internal void post()
        {
            RenameParametersName();
        }

        internal void inValidCache()
        {
            this.decl = null;
            this.method_body = null;
        }
    }
    public class ClassInfo
    {
        public TypeDefinition def;
        public Modifiers modifiers;
        public List<TypeDefinition> DerivedClasses = new List<TypeDefinition>();
        AstNode decl = null;
        List<ClassInfo> interfaces = null;
        List<MethodInfo> methods = null;
        
        public bool IsEnum
        {
            get { return this.def.IsEnum; }
        }
        public bool IsInterface
        {
            get { return this.def.IsInterface; }
        }
        public bool IsClass
        {
            get { return !this.def.IsEnum; }
        }
        public bool HasInterfaces
        {
            get { 
                var i = this.Interfaces;
                if (i.Count() > 0)
                    return true;
                else
                    return false;
            }
        }

        public List<MethodInfo> Methods
        {
            get
            {
                if (methods == null)
                {
                    this.methods = new List<MethodInfo>();
                    foreach (var m in def.Methods)
                    {
                        var info = InfoUtil.Info(m);
                        this.methods.Add(info);
                    }
                    foreach (var item in def.Interfaces)
                    {
                        switch (item.FullName)
                        {
                            case "System.Collections.ICollection":
                                this.methods.RemoveAll(x => x.def.Name == "CopyTo");
                                this.methods.RemoveAll(x => x.def.Name == "get_IsSynchronized");
                                this.methods.RemoveAll(x => x.def.Name == "get_SyncRoot");
                                this.methods.RemoveAll(x => x.def.Name == "get_Count");
                                break;
                            case "System.Collections.IEnumerable":
                                this.methods.RemoveAll(x => x.def.Name == "GetEnumerator");
                                break;
                            case "System.IDisposable":
                                this.methods.RemoveAll(x => x.def.Name == "Dispose");
                                break;
                        }
                    }
                }
                return this.methods;
            }
        }

        public List<ClassInfo> Interfaces
        {
            get {
                if (this.interfaces == null)
                {
                    this.interfaces = new List<ClassInfo>();
                    foreach (var item in def.Interfaces)
                    {
                        if (item.Namespace == def.Namespace)
                        {
                            var i = item as TypeDefinition;
                            var info = InfoUtil.Info(i);
                            this.interfaces.Add(info);
                        }

                    }
                }
                return this.interfaces;
            }
        }
        public string Name
        {
            get { return this.def.Name; }
            set
            {
                def.Name = value;
            }
        }
        public string Namespace
        {
            get { return this.def.Namespace; }
        }
        /*public ModuleDefinition Module
        {
            get { return this.def.Module; }
        }
        public TypeReference BaseType
        {
            get { return this.def.BaseType; }
        }
        public string FullName
        {
            get { return this.def.FullName; }
        }*/
        public MethodDefinition CopyConstructor
        {
            get
            {
                foreach (var m in this.def.Methods)
                {
                    var info = InfoUtil.Info(m);
                    if (info != null && info.isCopyConstructor)
                        return info.def;
                }
                return null;
            }
        }
        public MethodDefinition NullConstructor
        {
            get
            {
                foreach (var m in this.def.Methods)
                {
                    var info = InfoUtil.Info(m);
                    if (info != null && info.isNullConstructor)
                        return info.def;
                }
                return null;
            }
        }

        public AstNode Declaration
        {
            get
            {
                if (decl == null)
                {
                    decl = Util.getTypeDeclaration(this.def).Clone();
                }
                return decl;
            }
        }
        public bool IsBaseClassInModule
        {
            get
            {
                if (this.def.BaseType == null)
                    return true;
                var bclass = this.def.BaseType as TypeDefinition;
                if (bclass == null)
                    return true;
                if (bclass.Module != this.def.Module)
                    return true;
                return false;
            }
        }

        public bool isDerivedClass
        {
            get
            {
                return this.BaseTypeInModule != null;
            }
        }

        public bool HasDerivedClass
        {
            get
            {
                if (DerivedClasses.Count() > 0)
                    return true;
                return false;
            }
        }

        public TypeDefinition BaseTypeInModule
        {
            get
            {
                var tbase = this.def.BaseType as TypeDefinition;
                if (tbase == null || tbase.Module != this.def.Module)
                    return null;
                return tbase;
            }
        }
        public ClassInfo(TypeDefinition c)
        {
            this.def = c;
            this.modifiers = Util.ConvertModifiers(c);
            Rename();
        }
        
        void Rename()
        {
            if (RenameUtil.ClassRenameDict.ContainsKey(this.def.FullName))
            {
                this.def.Name = RenameUtil.ClassRenameDict[this.def.FullName];
            }
        }

        internal void post()
        {
            foreach (var t in this.def.Module.Types)
            {
                if (t == this.def)
                    continue;
                if (Util.isInhertFrom(t, this.def))
                    this.DerivedClasses.Add(t);
            }

            foreach (var f in this.def.Fields)
            {
                FieldInfo info = InfoUtil.Info(f);
                if (info != null)
                    info.post();
            }
            foreach(var p in this.def.Properties)
            {
                PropertyInfo info = InfoUtil.Info(p);
                if (info != null)
                    info.post();
            }

            foreach(var m in this.def.Methods)
            {
                var info = InfoUtil.Info(m);
                if (info != null)
                    info.post();
            }
            foreach (var e in this.def.Events)
            {
                var info = InfoUtil.Info(e);
                if (info != null)
                    info.post();
            }
        }
        internal void inValidCache()
        {
            foreach (var f in this.def.Fields)
            {
                FieldInfo info = InfoUtil.Info(f);
                if (info != null)
                    info.inValidCache();
            }
            foreach (var p in this.def.Properties)
            {
                PropertyInfo info = InfoUtil.Info(p);
                if (info != null)
                    info.inValidCache();
            }

            foreach (var m in this.def.Methods)
            {
                var info = InfoUtil.Info(m);
                if (info != null)
                    info.inValidCache();
            }
            foreach (var e in this.def.Events)
            {
                var info = InfoUtil.Info(e);
                if (info != null)
                    info.inValidCache();
            }
            this.decl = null;
        }
    }

    public class ModuleInfo
    {
        public ModuleDefinition Module;

        public ModuleInfo(ModuleDefinition m)
        {
            this.Module = m;
        }

        public void ScanCode()
        {
            InfoUtil.ScanCode(this.Module);
        }

        internal void post()
        {
            ScanCode();
            foreach (var t in Module.Types)
            {
                var info = InfoUtil.Info(t);
                if (info != null)
                    info.post();
            }
        }
        internal void inValidCache()
        {
            foreach (var t in Module.Types)
            {
                var info = InfoUtil.Info(t);
                if (info != null)
                    info.inValidCache();
            }
        }
    }

    public class InfoUtil {
        public static FieldInfo Info(FieldDefinition def)
        {
            if (def == null) return null;
            if (FieldInfoDict.ContainsKey(def))
                return FieldInfoDict[def];
            return null;
        }

        public static MethodInfo Info(MethodDefinition def)
        {
            if (def == null) return null;
            if (MethodInfoDict.ContainsKey(def))
                return MethodInfoDict[def];
            return null;
        }
        public static PropertyInfo Info(PropertyDefinition def)
        {
            if (def == null) return null;
            if (PropertyInfoDict.ContainsKey(def))
                return PropertyInfoDict[def];
            return null;
        }
        public static ClassInfo Info(TypeDefinition def)
        {
            if (def == null) return null;
            if (ClassInfoDict.ContainsKey(def))
                return ClassInfoDict[def];
            return null;
        }
        public static ModuleInfo Info(ModuleDefinition def)
        {
            if (def == null) return null;
            if (ModuleInfoDict.ContainsKey(def))
                return ModuleInfoDict[def];
            return null;
        }
        public static EventInfo Info(EventDefinition def)
        {
            if (def == null) return null;
            if (EventInfoDict.ContainsKey(def))
                return EventInfoDict[def];
            return null;
        }
        #region field reference

        public static Dictionary<FieldDefinition, FieldInfo> FieldInfoDict = new Dictionary<FieldDefinition, FieldInfo>();
        public static Dictionary<PropertyDefinition, PropertyInfo> PropertyInfoDict = new Dictionary<PropertyDefinition, PropertyInfo>();
        public static Dictionary<MethodDefinition, MethodInfo> MethodInfoDict = new Dictionary<MethodDefinition, MethodInfo>();
        public static Dictionary<EventDefinition, EventInfo> EventInfoDict = new Dictionary<EventDefinition, EventInfo>();
        public static Dictionary<TypeDefinition, ClassInfo> ClassInfoDict = new Dictionary<TypeDefinition, ClassInfo>();
        public static Dictionary<ModuleDefinition, ModuleInfo> ModuleInfoDict = new Dictionary<ModuleDefinition, ModuleInfo>();

        public static void BuildModuleDict(ModuleDefinition module)
        {

            if (ModuleInfoDict.ContainsKey(module))
                return;

            var info = new ModuleInfo(module);
            ModuleInfoDict.Add(module, info);

            foreach (var t in module.Types)
            {
                ClassInfo cinfo = new ClassInfo(t);
                ClassInfoDict.Add(t, cinfo);

                foreach (var f in t.Fields)
                {
                    FieldInfo finfo = new FieldInfo(f);
                    FieldInfoDict.Add(f, finfo);
                }

                foreach (var p in t.Properties)
                {
                    PropertyInfo pinfo = new PropertyInfo(p);
                    PropertyInfoDict.Add(p, pinfo);
                }

                foreach (var m in t.Methods)
                {
                    MethodInfo minfo = new MethodInfo(m);
                    MethodInfoDict.Add(m, minfo);
                }

                foreach (var e in t.Events)
                {
                    EventInfo einfo = new EventInfo(e);
                    EventInfoDict.Add(e, einfo);
                }
            }
            info.post();
            info.inValidCache();
        }
        #endregion

        #region code 
        public static void ScanCode(ModuleDefinition module)
        {
            foreach (var t in module.Types)
            {
                foreach (var m in t.Methods)
                {
                    ScanCode(m);
                }
            }
        }
        public static PropertyDefinition FindPropertyWithMethod(MethodDefinition m)
        {
            if (m.IsGetter)
            {
                foreach (var p in m.DeclaringType.Properties)
                {
                    if (p.GetMethod == m)
                        return p;
                }
            }
            else
            {
                foreach (var p in m.DeclaringType.Properties)
                {
                    if (p.SetMethod == m)
                        return p;
                }
            }
            return null;
        }

        public static void ScanCode(MethodDefinition method)
        {
            if (!method.HasBody)
                return;
            
            foreach (Instruction instr in method.Body.Instructions)
            {
                MethodReference mr = instr.Operand as MethodReference;
                if (mr != null)
                {
                    var def = mr.Resolve();
                    if (def != null && def.Module == method.Module)
                    {
                        var info = InfoUtil.Info(def);
                        if (info != null)
                        {
                            if (method.DeclaringType != def.DeclaringType)
                                info.UsedByOther.Add(method);
                            else
                                info.UsedByThis.Add(method);
                        }
                    }
                }

                /* Find ReadBy */
                // TODO error rename in field reference 
                if (isReadReference(instr.OpCode.Code))
                {
                    FieldReference fr = instr.Operand as FieldReference;
                    if (fr != null)
                    {
                        var def = fr.Resolve();
                        if (def != null && def.Module == method.Module)
                        {
                            var info = Info(def);
                            if (info != null)
                            {
                                if (method.IsGetter || method.IsSetter)
                                {
                                    var minfo = InfoUtil.Info(method);
                                    if (minfo != null)
                                    {
                                       if (minfo.MethodBody.Count() == 1)
                                        {
                                            var p = FindPropertyWithMethod(method);
                                            info.DeclareProperty = p;
                                        }
                                    }
                                }

                                if (method.DeclaringType != def.DeclaringType)
                                    info.ReadByOther.Add(method);
                                else
                                    info.ReadByThis.Add(method);
                            }
                            else
                                Console.Write("Error");
                        }
                    }
                }
            }
            /* Find AssignBy */
            foreach (Instruction instr in method.Body.Instructions)
            {
                if (isAssignReference(instr.OpCode.Code))
                {
                    FieldReference fr = instr.Operand as FieldReference;
                    if (fr != null)
                    {
                        var def = fr.Resolve();
                        if (def != null && def.Module == method.Module)
                        {
                            var info = Info(def);
                            if (info != null)
                            {
                                if (method.IsGetter || method.IsSetter)
                                {
                                    var p = FindPropertyWithMethod(method);
                                    if (p == null)
                                    {
                                        p = FindPropertyWithMethod(method);
                                        Console.Write("Error");
                                    }
                                    else
                                        info.DeclareProperty = p;
                                }

                                if (method.DeclaringType != def.DeclaringType)
                                    info.AssignByOther.Add(method);
                                else
                                    info.ReadByThis.Add(method);
                            }
                            else
                                Console.Write("Error");
                        }
                    }
                }
            }
        }

        public static bool isReadReference(Code code)
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
        public static bool isAssignReference(Code code)
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
    }
}
