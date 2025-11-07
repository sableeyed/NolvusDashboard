using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;


namespace Nolvus.Package.Conditions
{
    public class EnvironmentCompare : CompareCondition
    {
        public override void Load(XmlNode Node)
        {
            base.Load(Node);
        }
        public override bool IsValid(string GamePath, string InstallDir)
        {            
            string Value = ServiceSingleton.Instances.GetValueFromKey(DataToCompare);

            bool Valid = false;

            switch (this.Operator)
            {
                case 0: Valid = Value == ValueToCompare;
                    break;
                case 1:
                    Valid = Value != ValueToCompare;
                    break;

            }

            return Valid;
        }
    }
}
