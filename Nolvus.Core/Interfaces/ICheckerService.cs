using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Misc;

namespace Nolvus.Core.Interfaces
{
    public interface ICheckerService : INolvusService
    {
        Task<ModObjectList> CheckModList(List<ModObject> Mo2List, List<ModObject> NolvusList, Action<string> Status);
    }
}
