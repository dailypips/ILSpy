using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantKit
{
    public class ModuleInfo
    {
        public ModuleDefinition Module;

        public List<ClassInfo> classinfos = null;

        public List<ClassInfo> Types
        {
            get
            {
                if (this.classinfos == null)
                {
                    this.classinfos = new List<ClassInfo>();
                    foreach (var t in this.Module.Types)
                    {
                        ClassInfo info = InfoUtil.Info(t);
                        this.classinfos.Add(info);
                    }
                }
                return this.classinfos;
            }
        }
        public ModuleInfo(ModuleDefinition m)
        {
            this.Module = m;
        }
        #region internal
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
            this.classinfos = null;
        }
        #endregion
    }
}
