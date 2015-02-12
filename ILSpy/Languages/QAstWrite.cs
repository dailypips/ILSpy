using ICSharpCode.Decompiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuantKit
{
    class QAstWrite
    {
        public string HppFileExtension
        {
            get { return ".h"; }
        }
        public string CppFileExtension
        {
            get { return "*.cpp"; }
        }
        public string PrivateHppFileSuffix
        {
            get { return "_p"; }
        }

        QModule module;

        public QAstWrite(QModule module)
        {
            this.module = module;
        }

        public static string CleanUpName(string text)
        {
            int pos = text.IndexOf(':');
            if (pos > 0)
                text = text.Substring(0, pos);
            pos = text.IndexOf('`');
            if (pos > 0)
                text = text.Substring(0, pos);
            text = text.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                text = text.Replace(c, '-');
            return text;
        }

        string getFileName(TypeDefinition type, string SaveAsProjectDirectory, string extension)
        {
            string file = CleanUpName(type.Name) + extension;
            if (string.IsNullOrEmpty(type.Namespace))
            {
                return Path.Combine(SaveAsProjectDirectory, file);
            }
            else
            {
                string dir = Path.Combine(SaveAsProjectDirectory, CleanUpName(type.Namespace));
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return Path.Combine(dir, file);
            }
        }

        public void GenerateHppCode(QType type, ITextOutput output)
        {
            type.GenerateHppCode(output);
        }

        public void GenerateCppCode(QType type, ITextOutput output)
        {
            type.GenerateCppCode(output);
        }

        public void GeneratePrivateCode(QType type, ITextOutput output)
        {
            type.GeneratePrivateCode(output);
        }

        public void WriteProjectFile(string file)
        {
            using (StreamWriter w = new StreamWriter(file))
            {

            }
        }

        public void WriteCodeFilesInProject(string projectDir)
        {
            foreach (var t in module.types)
            {
                string fname = getFileName(t.def, projectDir, HppFileExtension);
                using (StreamWriter w = new StreamWriter(fname))
                {
                    GenerateHppCode(t, new PlainTextOutput(w));
                }
            }

            foreach (var t in module.types)
            {
                string fname = getFileName(t.def, projectDir, CppFileExtension);
                using (StreamWriter w = new StreamWriter(fname))
                {
                    GenerateCppCode(t, new PlainTextOutput(w));
                }
            }

            foreach (var t in module.types)
            {
                string fname = getFileName(t.def, projectDir, PrivateHppFileSuffix + HppFileExtension);
                using (StreamWriter w = new StreamWriter(fname))
                {
                    GeneratePrivateCode(t, new PlainTextOutput(w));
                }
            }
        }
    }
}
