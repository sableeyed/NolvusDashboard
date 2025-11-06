using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Nolvus.Core.Misc;

namespace Nolvus.Core.Interfaces
{
    public interface IModOrganizer : ISoftware, IModsContainer
    {
        Task<List<ModObject>> GetModsMetaData(string Profile, Action<string, int> Progress = null);
        List<string> GetProfiles();
    }
}
