using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Enums;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Misc;
using Nolvus.Core.Services;
using Nolvus.Package.Rules;
using Nolvus.Package.Conditions;
using Nolvus.Package.Patchers;
using System.Xml;

namespace Nolvus.Package.Mods
{
    public class ENBPreset : NexusMod, IENBPreset
    {

        public bool Installed
        {
            get
            {
                return GetFieldValueByKey("EnbCode") == ServiceSingleton.Instances.WorkingInstance.Options.AlternateENB;
            }
        }

        public override void Load(XmlNode Node, List<InstallableElement> Elements)
        {
            base.Load(Node, Elements);            
        }

        protected override async Task PrepareDirectory()
        {
            await Remove();
        }

        public override async Task Remove()
        {
            var Tsk = Task.Run(() =>
            {
                try
                {
                    
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });

            await Tsk;
        }
    }
}
