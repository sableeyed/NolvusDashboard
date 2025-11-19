using System;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Services;
using Nolvus.Package.Rules;

namespace Nolvus.Package.Mods
{
    public class BSArch : NexusSoftware
    {

        protected override async Task DoCopy()
        {
            var Tsk = Task.Run(() =>
            {
                try
                {
                    var instance = ServiceSingleton.Instances.WorkingInstance;

                    var installDirectory = Path.Combine(instance.InstallDir, "TOOLS", Name);
                    Directory.CreateDirectory(installDirectory);

                    var extractDir = Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir);

                    int counter = 0;
                    foreach (var rule in Rules)
                    {
                        rule.Execute(
                            instance.StockGame, // GamePath
                            extractDir,         // ExtractDir (source)
                            installDirectory,   // ModDir (destination root for this tool)
                            instance.InstallDir // InstanceDir
                        );

                        CopyingProgress(++counter, Rules.Count);
                    }
                }
                finally
                {
                    ServiceSingleton.Files.RemoveDirectory(
                        Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir),
                        true
                    );
                }
            });

            await Tsk;
        }

        // protected override async Task DoCopy()
        // {
        //     var Tsk = Task.Run(() =>
        //     {
        //         try
        //         {
        //             try
        //             {
        //                 var InstallDirectory = Path.Combine(ServiceSingleton.Instances.WorkingInstance.InstallDir, "TOOLS", Name);

        //                 Directory.CreateDirectory(InstallDirectory);

        //                 var Rules = new DirectoryCopy().CreateFileRules(Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir), 2, string.Empty, string.Empty);

        //                 var Counter = 0;

        //                 foreach (var Rule in Rules)
        //                 {
        //                     Rule.Execute(ServiceSingleton.Instances.WorkingInstance.StockGame, 
        //                                  Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir), 
        //                                  InstallDirectory, 
        //                                  InstallDirectory);

        //                     CopyingProgress(++Counter, Rules.Count);
        //                 }
        //             }
        //             finally
        //             {
        //                 ServiceSingleton.Files.RemoveDirectory(Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir), true);
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             throw ex;
        //         }
        //     });

        //     await Tsk;
        // }
    }
}
