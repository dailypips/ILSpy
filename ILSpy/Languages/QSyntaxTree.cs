using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    class QSModule
    {
        public QModule origin;
    }

    class QSEntiry
    {
        public QType origin;
        public string ns;
        public string name;
        public bool isClass = false;

        public bool isEnum = false;
    }

    public class QSField
    {
        public string name;
        public string type;
        public string init;
    }

    class QSClass : QSEntiry
    {
        public QSClass() : base()
        {
            this.isClass = true;
        }
    }
    public class QSEnumMember
    {
        public string name;
        public string init;
    }

    class QSEnum : QSEntiry
    {
        public List<QSEnumMember> members = new List<QSEnumMember>();
        public QSEnum() : base()
        {
            this.isEnum = true;
        }
    }

    class QSParameter
    {
        public string type;
        public string name;
        public string optionValue;
    }

    class QSMethod
    {
        public object origin; // QMethod QProperty QField
        public List<string> body;
        public string name;
        public string rtype;
        public List<QSParameter> parameters;
    }
}
