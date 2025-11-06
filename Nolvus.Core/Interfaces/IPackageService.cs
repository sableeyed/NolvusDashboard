using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Events;
using Nolvus.Core.Misc;
using Nolvus.Api.Installer.Library;

namespace Nolvus.Core.Interfaces
{
    public interface IPackageService : INolvusService, ISoftwareProvider, IModsContainer
    {        
        Task Load(IInstallPackageDTO Package, Action<string, int> Progress = null);
        Task Merge(IEnumerable<IInstallPackageDTO> Packages, Action<string, int> Progress = null);
        Task InstallModList(ModInstallSettings Settings = null);
        int ModsCount { get; }
        List<IMOElement> InstallList { get; }
        List<string> OptionalEsps { get; }
        List<string> LoadOrder { get; }
        List<IInstallableElement> InstallingModsQueue { get; }
        IProgressQueue ProgressQueue { get; }
        string LoadedVersion { get; }        
        double InstallProgression { get; }
        IErrorHandler ErrorHandler { get; }        
        bool Processing { get; }                  
    }
}
