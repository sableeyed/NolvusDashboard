using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Misc;
using Nolvus.Core.Services;

namespace Nolvus.Services.Checker
{
    public class CheckerService : ICheckerService
    {
        public async Task<ModObjectList> CheckModList(List<ModObject> Mo2List, List<ModObject> NolvusList, Action<string> Status)
        {
            return await Task.Run(() =>
            {
                Status("Checking differences...");                
                return new ModObjectList().Merge(Mo2List, NolvusList);              
            });
        }
    }
}
