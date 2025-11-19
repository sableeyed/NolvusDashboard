using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Package.Rules;


namespace Nolvus.Package.Mods
{
    public class xEdit : NexusSoftware
    {   

        protected override async Task DoCopy()
        {
            var Tsk = Task.Run(() =>
            {
                try
                {
                    var Instance = ServiceSingleton.Instances.WorkingInstance;

                    var InstallDirectory = Path.Combine(Instance.InstallDir, "TOOLS", Name);
                    var CacheDir = Path.Combine(InstallDirectory, "Cache");

                    Directory.CreateDirectory(InstallDirectory);
                    Directory.CreateDirectory(CacheDir);

                    var extractDir = Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir);

                    int counter = 0;

                    foreach (var rule in Rules)
                    {
                        rule.Execute(
                            Instance.StockGame,     // GamePath
                            extractDir,             // ExtractDir
                            InstallDirectory,       // ModDir → tool root directory
                            Instance.InstallDir     // InstanceDir
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
        //                 var Instance = ServiceSingleton.Instances.WorkingInstance;

        //                 var InstallDirectory = Path.Combine(Instance.InstallDir, "TOOLS", Name);
        //                 var CacheDir = Path.Combine(InstallDirectory, "Cache");                        

        //                 Directory.CreateDirectory(InstallDirectory);
        //                 Directory.CreateDirectory(CacheDir);
                        
        //                 var DirectoryRule = (Rules.First() as DirectoryCopy);
        //                 var FileRules = DirectoryRule.CreateFileRules(Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir), DirectoryRule.Destination, string.Empty, string.Empty);                        

        //                 var Counter = 0;

        //                 foreach (var Rule in Rules)
        //                 {
        //                     Rule.Execute(Instance.StockGame, Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir), Instance.InstallDir, Instance.InstallDir);
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
