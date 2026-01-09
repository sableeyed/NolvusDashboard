using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nolvus.Core.Events;
using Nolvus.Core.Enums;
using SixLabors.ImageSharp;


namespace Nolvus.Core.Interfaces
{
    public interface IInstallableElement
    {
        string Name { get; set; }
        string Version { get; set; }
        string ImagePath { get; set; }
        string Author { get; set; }        
        Task Install(CancellationToken Token, ModInstallSettings Settings = null);
        ModProgress Progress { get; }
        Image Image { get; set; }
        string Description { get; set; }
        bool IsInstallable();
        bool IsInstallable(string Value);
        ElementAction Action { get; set; }
    }
}
