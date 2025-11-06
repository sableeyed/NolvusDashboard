using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Misc;

namespace Nolvus.Core.Interfaces
{
    public interface IGameService : INolvusService
    {
        bool IsGameInstalled();
        string GetSkyrimSEDirectory();
        List<string> GamePlugins { get; }
        List<string> GamePluginFiles { get; }
        List<ModObject> GamePluginAsObjects();
    }
}
