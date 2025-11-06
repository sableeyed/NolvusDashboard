using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

namespace Nolvus.Core.Errors
{
    public class FaultyMod
    {
        public IInstallableElement Mod { get; set; }
        public Exception Error { get; set; }
    }
}
