﻿using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public class ClassInfo
    {
        public TypeDefinition def;
        public Modifiers modifiers;
        public List<TypeDefinition> DerivedClasses = new List<TypeDefinition>();
        TypeInfo base_type = null;
        AstNode decl = null;
        List<ClassInfo> interfaces = null;
        List<MethodInfo> methods = null;
        List<TypeInfo> typeinfos = null;

        public string FullName
        {
            get { return this.def.FullName; }
        }
        public Collection<FieldDefinition> Fields
        {
            get
            {
                return this.def.Fields;
            }
        }
        public bool isInhertFrom(TypeDefinition @base)
        {
            var d = this.def.BaseType as TypeDefinition;
            while (d != null)
            {
                if (d == @base)
                    return true;
                d = d.BaseType as TypeDefinition;
            }
            return false;
        }
        public string AddPath(string path)
        {
           /* if (isDerivedClass && isInhertFrom("Event") && def.Name != "DataObject")
            {
                return Path.Combine(path, "Event");
            }
            if (isDerivedClass && !HasDerivedClass && isInhertFrom("ObjectStreamer"))
                return Path.Combine(path, "Stream");*/
            return path;
        }
        public string IncludeName
        {
            get
            {
               /* if (isDerivedClass && isInhertFrom("Event") && def.Name != "DataObject")
                    return "Event/" + def.Name;
                if (isDerivedClass && !HasDerivedClass && isInhertFrom("ObjectStreamer"))
                    return "Stream/" + def.Name;*/
                return def.Name;
            }
        }

        public bool isInhertFrom(string name)
        {
            var d = InfoUtil.Info(name);
            return isInhertFrom(d);
        }
        public bool isSkipClass
        {
            get
            {
                bool isInhertFromEventArgs = Util.isInhertFrom(def, "System.EventArgs");
                //var objectStreamerDefinition = Util.GetTypeDefinition("ObjectStreamer");
                //bool isInhertFromObjectStreamer= Util.isInhertFrom(this.def, objectStreamerDefinition);
                if (def.Name == "DataObjectType" || isInhertFromEventArgs || def.Name.Contains("`") )//|| isInhertFromObjectStreamer)
                    return true;
                else return false;
            }
        }
        public bool isInhertFrom(ClassInfo @base)
        {
            return isInhertFrom(@base.def);
        }

        public bool isClassAsEnum
        {
            get
            {
                return this.def.Name == "EventType" || this.def.Name == "CurrencyId";
            }
        }
        public bool IsValueType
        {
            get { return this.def.IsEnum || isClassAsEnum; }
        }
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
            get
            {
                var i = this.Interfaces;
                if (i.Count() > 0)
                    return true;
                else
                    return false;
            }
        }
        public ModuleDefinition Module
        {
            get { return this.def.Module; }
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

        public List<TypeInfo> TypeInfos
        {
            get
            {
                if (this.typeinfos == null)
                {
                    var result = new List<TypeInfo>();
                    
                    if (BaseType != null)
                        typeinfos.Add(BaseType);

                    foreach (var f in Fields)
                    {
                        typeinfos.Add(new TypeInfo(f.FieldType));
                    }
                    foreach (var m in Methods)
                    {
                        foreach (var p in m.def.Parameters)
                        {
                            typeinfos.Add(new TypeInfo(p.ParameterType));
                        }
                    }
                }
                return typeinfos;
            }
        }
        public List<ClassInfo> Interfaces
        {
            get
            {
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

        public string PrivateNamespace
        {
            get
            {
                return string.Empty;
            }
        }
        public string PrivateFullName
        {
            get
            {
                return PrivateName;
            }
        }
        public string PrivateName
        {
            get
            {
                return Name + "Private";
            }
        }
        public TypeInfo BaseType
        {
            get
            {
                return this.base_type;
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
            foreach (var p in this.def.Properties)
            {
                PropertyInfo info = InfoUtil.Info(p);
                if (info != null)
                    info.post();
            }

            foreach (var m in this.def.Methods)
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
            typeinfos = null;
        }
    }
}