using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;

namespace Nolvus.Core.Interfaces
{
    public interface IOptionService : INolvusService
    {
        Task<List<IMod>> PrepareModsToUpdate(string Option, string CurrentValue, string NewValue);
    }
}