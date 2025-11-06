using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Errors;

namespace Nolvus.Core.Interfaces
{
    public interface IErrorHandler
    {
        List<FaultyMod> List { get; }
        int ErrorsCount { get; }
        bool ThresholdEnabled { get; }
    }
}
