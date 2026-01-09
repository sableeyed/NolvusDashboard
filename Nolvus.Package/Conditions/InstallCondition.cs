using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;

namespace Nolvus.Package.Conditions
{
    public abstract class InstallCondition : IInstallCondition
    {
        public int Operator { get; set; }

        public abstract bool IsValid();
        public abstract bool IsValid(string Value);

        public virtual void Load(XmlNode Node)
        {
            Operator = System.Convert.ToInt16(Node["Operator"].InnerText);
        }
    }
}
