using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public class IncludeInfo
    {
        TypeReference reference;

        public bool isModuleType
        {
            get { return true; }
        }
        public bool isEnumOrInterface
        {
            get {
                TypeDefinition define = reference.Resolve();
                return define.IsEnum || define.IsInterface; 
            }
        }
        public TypeDefinition BaseType
        {
            get
            {
                TypeDefinition def = reference.Resolve();
                if (def == null)
                    return null;

                TypeDefinition baseType = def.BaseType as TypeDefinition;
                if (baseType != null)
                {
                    if (baseType.Module == def.Module)
                        return baseType;
                }
                return null;
            }
        }

        public IncludeInfo(TypeReference reference)
        {
            this.reference = reference;
        }
    }
    class Helper
    {
        #region include process

        /*static void BuildIncludeList(TypeDefinition def, out List<string> externalList, out List<string> moduleClassList, out List<string> moduleEnumOrInterfaceList)
        {
            List<IncludeInfo> ilist = new List<IncludeInfo>();

            if (def.BaseType != null)
                ilist.Add(new IncludeInfo(def.BaseType));

            if (def.HasInterfaces)
            {
                foreach (var i in def.Interfaces)
                    ilist.Add(new IncludeInfo(i));
            }

            foreach (var m in def.Methods)
            {

            }

            List<string> elist = new List<string>();
            List<string> mlist = new List<string>();
            List<string> clist = new List<string>();
            bool hasBaseType = def.BaseType != null && def.BaseType.Name != "Object";
            if (hasBaseType)
                mlist.Add(def.BaseType.Name); //
            if (def.HasInterfaces)
            {
                foreach (var i in def.Interfaces)
                {
                    if (i.Module == def.Module)
                        mlist.Add(i.Name);
                    else
                        elist.Add(i.Name);
                }
            }
            foreach (var m in def.Methods)
            {
                Util.AddInclude(def.Namespace, m.ReturnType, elist, clist, mlist);
                foreach (var p in m.Parameters)
                    Util.AddInclude(def.Namespace, p.ParameterType, elist, clist, mlist);
                //special for currencyId
                foreach (var p in m.Parameters)
                {
                    if (p.Name == "currencyId" && p.HasConstant && p.Constant != null)
                        mlist.Add("CurrencyId");
                }
            }
            foreach (var f in def.Fields)
            {
                Util.AddInclude(def.Namespace, f.FieldType, elist, clist, mlist);
            }

            externalList = elist.Distinct<string>().ToList();
            moduleClassList = clist.Distinct<string>().ToList();
            moduleEnumOrInterfaceList = mlist.Distinct<string>().ToList();
        }*/
        #endregion
        #region Manual Code
        public static void WriteGlobalHpp(ITextOutput output)
        {
            output.WriteLine("#ifndef __QUANTKIT_GLOBAL_H__");
            output.WriteLine("#define __QUANTKIT_GLOBAL_H__");
            output.WriteLine();
            output.WriteLine("#include <QtCore/qglobal.h>");
            output.WriteLine();
            output.WriteLine("#if defined(QUANTKIT_LIBRARY)");
            output.WriteLine("#  define QUANTKIT_EXPORT Q_DECL_EXPORT");
            output.WriteLine("#else");
            output.WriteLine("#  define QUANTKIT_EXPORT Q_DECL_IMPORT");
            output.WriteLine("#endif");
            output.WriteLine();
            output.WriteLine("#endif // __QUANTKIT_GLOBAL_H__");
        }

        public static void WriteQtBoost(ITextOutput output)
        {
            output.WriteLine("#ifndef __QUANTKIT_QTBOOST_H__");
            output.WriteLine("#define __QUANTKIT_QTBOOST_H__");
            output.WriteLine();
            output.WriteLine("#include <boost/functional/hash.hpp>");
            output.WriteLine();
            output.WriteLine("#include <QString>");
            output.WriteLine("#include <QDateTime>");
            output.WriteLine("#include <QHash>");
            output.WriteLine("#include <QVariant>");
            output.WriteLine();
            output.WriteLine("inline std::size_t hash_value(const QChar& c)        { return qHash(c); }");
            output.WriteLine("inline std::size_t hash_value(const QString& s)      { return qHash(s); }");
            output.WriteLine("inline std::size_t hash_value(const QDate& d)        { return qHash(d.toJulianDay()); }");
            output.WriteLine("inline std::size_t hash_value(const QTime& t)        { return qHash(t.toString()); }");
            output.WriteLine("inline std::size_t hash_value(const QDateTime& dt)   { return qHash(dt.toString()); }");
            output.WriteLine("inline std::size_t hash_value(const QVariant& v)     { return qHash(v.toString()); }");
            output.WriteLine("template <typename T0, typename T1>");
            output.WriteLine("inline std::size_t hash_value(const QPair<T0, T1> & p)");
            output.WriteLine("{");
            output.WriteLine("   std::size_t seed = 0;");
            output.WriteLine("   boost::hash_combine(seed, p.first);");
            output.WriteLine("   boost::hash_combine(seed, p.second);");
            output.WriteLine("   return seed;");
            output.WriteLine("}");
            output.WriteLine();
            output.WriteLine("#endif // __QUANTKIT_QTBOOST_H__");
        }

        public static void WriteQtExtension(ITextOutput output)
        {
            output.WriteLine("#ifndef __QUANTKIT_QT_EXTENSION_H__");
            output.WriteLine("#define __QUANTKIT_QT_EXTENSION_H__");
            output.WriteLine();
            output.WriteLine("#define QK_DECLARE_PRIVATE(Class) \\");
            output.WriteLine("   inline Internal::Class##Private* d(); \\");
            output.WriteLine("   inline const Internal::Class##Private* d() const ;");
            output.WriteLine();
            output.WriteLine("#define QK_IMPLEMENTATION_PRIVATE(Class) \\");
            output.WriteLine("   inline Internal::Class##Private* Class::d() { return static_cast<Internal::Class##Private*>(d_ptr.data()); } \\");
            output.WriteLine("   inline const Internal::Class##Private* Class::d() const { return static_cast<const Internal::Class##Private* const>(d_ptr.data());}");
            output.WriteLine();
            output.WriteLine("#endif // __QUANTKIT_QT_EXTENSION_H__");
        }

        public static void WriteCurrencyIdCode(TypeDefinition def, ITextOutput output)
        {
            output.WriteLine("#include <QuantKit/CurrencyId.h>");
            output.WriteLine();
            output.WriteLine("#include <boost/multi_index_container.hpp>");
            output.WriteLine("#include <boost/multi_index/hashed_index.hpp>");
            output.WriteLine("#include <boost/multi_index/ordered_index.hpp>");
            output.WriteLine("#include <boost/multi_index/member.hpp>");
            output.WriteLine("#include <boost/multi_index/tag.hpp>");
            output.WriteLine();
            output.WriteLine("#include \"qt_boost.h\"");
            output.WriteLine();
            output.WriteLine("namepsace QuantKit {");
            output.WriteLine();
            output.WriteLine("class CurrencyItem {");
            output.WriteLine("public:");
            output.Indent();
            output.WriteLine("QString name;");
            output.WriteLine("unsigned char code;");
            output.WriteLine("CurrencyItem(const QString& currency, unsigned char id) : name(currency), code(id)  {}");
            output.Unindent();
            output.Write("};");
            output.WriteLine();
            output.WriteLine("struct by_name{};");
            output.WriteLine("struct by_code{};");
            output.WriteLine();
            output.WriteLine("typedef boost::multi_index::multi_index_container<\n  CurrencyItem,\n  boost::multi_index::indexed_by<\n    boost::multi_index::hashed_unique<\n      boost::multi_index::tag<by_name>,\n      boost::multi_index::member<\n        CurrencyItem, QString, &CurrencyItem::name\n      >\n    >,\n    boost::multi_index::hashed_unique<\n      boost::multi_index::tag<by_code>,\n      boost::multi_index::member<\n        CurrencyItem, unsigned char, &CurrencyItem::code\n      >\n    >\n  >\n> CurrencyIdContainer;");
            output.WriteLine("\ntypedef CurrencyIdContainer::index<by_name>::type CurrencyId_by_name;\ntypedef CurrencyIdContainer::index<by_code>::type CurrencyId_by_code;\n");
            output.WriteLine("static CurrencyIdContainer currencyIds = {");
            output.Indent();
            List<FieldDefinition> constList = new List<FieldDefinition>();
            foreach (var f in def.Fields)
            {
                var modifiers = Util.ConvertModifiers(f);
                if (modifiers.HasFlag(Modifiers.Const) && f.Constant != null)
                    constList.Add(f);
            }

            for (int i = 0; i < constList.Count(); ++i)
            {
                var f = constList[i];
                output.Write("{\"");
                output.Write(f.Name);
                output.Write("\", ");
                output.Write(f.Constant.ToString());
                if (i < constList.Count() - 1)
                    output.WriteLine("},");
                else
                    output.WriteLine("}");
            }
            output.Unindent();
            output.WriteLine("};");
            output.WriteLine("\nstatic QString nullstring = QString();\n\nunsigned char CurrencyId::GetId(const QString& name)\n{\n    CurrencyId_by_name& index = currencyIds.get<by_name>();\n    CurrencyId_by_name::iterator it = index.find(name);\n    if (it != index.end())\n        return it->code;\n    else\n        return 0;\n}\n\nconst QString& CurrencyId::GetName(unsigned char id)\n{\n    CurrencyId_by_code& index = currencyIds.get<by_code>();\n    CurrencyId_by_code::iterator it = index.find(id);\n    if (it != index.end())\n        return it->name;\n    else\n        return nullstring;\n}\n\n} // namespace QuantKit");
        }

        public static void WriteCurrencyIdInclude(ClassInfo info, ITextOutput output)
        {
            output.WriteLine("class QUANTKIT_EXPORT CurrencyId");
            output.WriteLine("{");
            output.WriteLine("public:");
            output.Indent();
            output.WriteLine("/*");
            output.WriteLine("* return 0 means not found");
            output.WriteLine("*/");
            output.WriteLine("static unsigned char GetId(const QString &name);");
            output.WriteLine();
            output.WriteLine("/*");
            output.WriteLine("* return QString().isEmpty() == true means not found");
            output.WriteLine("*/");
            output.WriteLine("static const QString& GetName(unsigned char id);");
            output.WriteLine();
            foreach (var f in info.def.Fields)
            {
                if (f.Constant != null)
                {
                    output.Write("static const unsigned char ");
                    output.Write(f.Name);
                    output.Write(" = ");
                    output.Write(f.Constant.ToString());
                    output.WriteLine(";");
                }
            }
            output.Unindent();
            output.WriteLine("};");
        }

        public static void WriteEventTypeInclude(ClassInfo def, ITextOutput output)
        {
            output.WriteLine("class QUANTKIT_EXPORT EventType");
            output.WriteLine("{");
            output.WriteLine("public:");
            output.Indent();
            if (eventList == null)
                BuildEventTypeTable(def.Module);
            foreach (var f in Helper.eventList)
            {
                output.Write("static const unsigned char ");
                output.Write(f.Name);
                output.Write(" = ");
                output.Write(f.Constant.ToString());
                output.WriteLine(";");
            }
            output.Unindent();
            output.WriteLine("};");
        }
        #endregion
        public static bool isClassAsEnum(TypeDefinition def)
        {
            if (def.Name == "EventType" || def.Name == "CurrencyId")
                return true;
            else
                return false;
        }
        #region unresolved
        public static void ShowUnResolvedFieldAndMethod(ModuleDefinition module, ITextOutput output)
        {

            Dictionary<TypeDefinition, Tuple<List<FieldDefinition>, List<MethodDefinition>>> rdict = new Dictionary<TypeDefinition,Tuple<List<FieldDefinition>,List<MethodDefinition>>>();
            foreach (var t in module.Types)
            {
                if (t.IsEnum)
                    continue;
                if (t.Namespace != "SmartQuant")
                    continue;
                if (t.Name == "CurrencyId" || t.Name == "EventType" || t.Name == "DataObjectType" || t.Name == "AccountDataField")
                    continue;

                List<FieldDefinition> flist = new List<FieldDefinition>();
                List<MethodDefinition> mlist =new List<MethodDefinition>();

                foreach (var f in t.Fields)
                {
                    var modifiers = Util.ConvertModifiers(f);
                    if (modifiers.HasFlag(Modifiers.Const))
                        continue;

                    if (!f.Name.StartsWith("m_"))
                    {
                        flist.Add(f);
                        //output.WriteLine("fieldRenameList.Add(Tuple.Create(\"" + t.Name + "\", \"" + f.Name + "\", \"" + f.Name + "\"));");
                    }
                }
                
                foreach (var m in t.Methods)
                {
                    if (m.Name.StartsWith("method_") || m.Name.StartsWith("vmethod_"))
                    {
                        mlist.Add(m);
                        //output.WriteLine("methodRenameList.Add(Tuple.Create(\"" + t.Name + "\", \"" + m.Name + "\", \"" + m.Name + "\"));");
                    }
                }

                if(flist.Count() > 0 || mlist.Count() > 0) {
                    rdict.Add(t, Tuple.Create(flist, mlist));
                }
            }

            foreach(var t in rdict)
            {
                output.WriteLine("{ \"" + t.Key.FullName +"\", Tuple.Create( new Dictionary<string, string>(){");
                output.Indent();
                output.Indent();
                output.WriteLine("// field ");
                foreach(var f in t.Value.Item1){
                    output.WriteLine("{\"" + f.Name+"\",\"" + f.Name+"\"},");
                }
                output.Unindent();
                output.WriteLine("},new Dictionary<string,string>(){");
                output.Indent();
                output.WriteLine("// method ");
                foreach(var m in t.Value.Item2){
                    output.WriteLine("{\"" + m.Name+"\",\"" + m.Name+"\"},");
                }
                output.WriteLine("})");
                output.Unindent();
                output.Unindent();
                output.WriteLine("},");
            }
        }
        #endregion
        #region BuildEventType
        public static Hashtable eventTable = null;
        public static List<FieldDefinition> eventList = null;
        public static void BuildEventTypeTable(ModuleDefinition module)
        {
            if (eventTable != null)
                return;
            eventTable = new Hashtable();
            List<FieldDefinition> elist = new List<FieldDefinition>();
            TypeDefinition eventtype = Util.GetTypeDefinition(module, "EventType");
            TypeDefinition dataobject = Util.GetTypeDefinition(module, "DataObjectType");

            foreach (var f in eventtype.Fields)
            {
                if (f.Constant != null)
                {
                    elist.Add(f);
                }
            }
            foreach (var f in dataobject.Fields)
            {
                string name = f.Name;
                if (!f.Name.StartsWith("Class"))
                {
                    bool found = false;
                    foreach (var item in elist)
                    {
                        if (item.Name == f.Name)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        elist.Add(f);
                    }
                }
            }
            eventList = elist.OrderBy(x => int.Parse(x.Constant.ToString())).ToList();
            foreach (var f in eventList)
            {
                if (!eventTable.Contains(f.Constant))
                    eventTable.Add(f.Constant, f.Name);
            }
        }
        #endregion
    }
}
