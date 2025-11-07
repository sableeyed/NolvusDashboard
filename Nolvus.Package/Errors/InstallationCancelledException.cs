using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nolvus.Package.Errors
{
    public class InstallationCancelledException : Exception
    {
        public InstallationCancelledException(string Message) :
            base(Message)
        {

        }
    }
}
