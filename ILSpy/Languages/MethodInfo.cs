using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuantKit
{
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
        List<ParameterInfo> parameters = null;
        TypeInfo return_type;

        #region IsFunction

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
        public bool IsConstructor
        {
            get { return this.def.IsConstructor; }
        }

        public bool IsGetter
        {
            get { return this.def.IsGetter; }
        }

        public bool IsSetter
        {
            get { return this.def.IsSetter; }
        }
        public bool IsAddOn
        {
            get { return this.def.IsAddOn; }
        }
        public bool IsRemoveOn
        {
            get { return this.def.IsRemoveOn; }
        }
        public bool IsStatic
        {
            get { return this.def.IsStatic; }
        }
        public bool IsPublic
        {
            get { return this.IsPublic; }
        }
        public bool IsPrivate
        {
            get { return this.IsPrivate; }
        }
        public bool isNullConstructor
        {
            get
            {
                return this.def.IsConstructor && this.def.Parameters.Count() == 0;
            }
        }
        #endregion
        public List<ParameterInfo> Parameters
        {
            get {
                if (this.parameters == null)
                {
                    this.parameters = new List<ParameterInfo>();
                    foreach (var p in this.def.Parameters)
                    {
                        var pinfo = new ParameterInfo(p);
                        this.parameters.Add(pinfo);
                    }
                }
                return parameters; 
            }
        }

        public ClassInfo DeclaringType
        {
            get { return InfoUtil.Info(this.def.DeclaringType); }
        }

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

        public TypeInfo ReturnType
        {
            get
            {
                return this.return_type;
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

            this.return_type = new TypeInfo(this.def.ReturnType);
        }
        #region process
        #region spcial process
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
                        if (s.Trim() == "{" || s.Trim() == "});")
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
                    if (baselist.Count() == 2 && baselist[1].Trim() != "")
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
            get
            {
                if (this.method_body == null)
                    build_method_body();
                bool isFirst = true;
                string line;
                List<string> olist = new List<string>();
                if (this.baseInit != null)
                {
                    olist.Add(": " + this.def.DeclaringType.BaseType.Name + "Private (" + this.baseInit + ")");
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
        #endregion
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
            this.parameters = null;
        }
        #endregion
    }
}
