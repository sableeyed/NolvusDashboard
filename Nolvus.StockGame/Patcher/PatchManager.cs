using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Nolvus.Core.Events;
using Nolvus.Core.Services;
using Nolvus.Core.Utils;
using Nolvus.Core.Enums;
using Nolvus.StockGame.Core;
using Nolvus.StockGame.Meta;
using Nolvus.Core.Utils;

namespace Nolvus.StockGame.Patcher
{
    public class PatcherManager
    {
        #region Fields

        string _WorkingDir = string.Empty;
        string _LibDir = string.Empty;
        string _PatchDir = string.Empty;

        #endregion

        #region Events

        public event DownloadProgressChangedHandler OnDownload;
        public event ExtractProgressChangedHandler OnExtract;

        event OnItemProcessedHandler OnItemProcessedEvent;

        public event OnItemProcessedHandler OnItemProcessed
        {
            add
            {
                if (OnItemProcessedEvent != null)
                {
                    lock (OnItemProcessedEvent)
                    {
                        OnItemProcessedEvent += value;
                    }
                }
                else
                {
                    OnItemProcessedEvent = value;
                }
            }
            remove
            {
                if (OnItemProcessedEvent != null)
                {
                    lock (OnItemProcessedEvent)
                    {
                        OnItemProcessedEvent -= value;
                    }
                }
            }
        }

        event OnStepProcessedHandler OnStepProcessedEvent;

        public event OnStepProcessedHandler OnStepProcessed
        {
            add
            {
                if (OnStepProcessedEvent != null)
                {
                    lock (OnStepProcessedEvent)
                    {
                        OnStepProcessedEvent += value;
                    }
                }
                else
                {
                    OnStepProcessedEvent = value;
                }
            }
            remove
            {
                if (OnStepProcessedEvent != null)
                {
                    lock (OnStepProcessedEvent)
                    {
                        OnStepProcessedEvent -= value;
                    }
                }
            }
        }

        #endregion

        public PatcherManager(string WorkingDir, string LibDir, string PatchDir)
        {
            _WorkingDir = WorkingDir;
            _LibDir = LibDir;
            _PatchDir = PatchDir;
        }

        #region Methods

        private void Downloading(object sender, DownloadProgress e)
        {
            if (OnDownload != null)
            {
                OnDownload(this, e);
            }
        }

        private void Extracting(object sender, ExtractProgress e)
        {
            if (OnExtract != null)
            {
                OnExtract(this, e);
            }
        }

        private void ElementProcessed(int Value, int Total, StockGameProcessStep Step, string ItemName)
        {
            OnItemProcessedHandler Handler = this.OnItemProcessedEvent;
            ItemProcessedEventArgs Event = new ItemProcessedEventArgs(Value, Total, Step, ItemName);
            if (Handler != null) Handler(this, Event);
        }

        private void StepProcessed(string Step)
        {
            OnStepProcessedHandler Handler = this.OnStepProcessedEvent;
            StepProcessedEventArgs Event = new StepProcessedEventArgs(0, 0, Step);
            if (Handler != null) Handler(this, Event);
        }

        private async Task DoDownloadPatchFile(PatchingInstruction Instruction)
        {            
            var Tsk = Task.Run(async () => 
            {
                try
                {
                    if (!File.Exists(Path.Combine(_PatchDir, Instruction.PatchFile)))
                    {
                        this.StepProcessed("Downloading patch file " + Instruction.PatchFile);

                        string DownloadedFile = Path.Combine(_PatchDir, Instruction.PatchFile);

                        await ServiceSingleton.Files.DownloadFile(Instruction.DownLoadLink, DownloadedFile, Downloading);

                        this.StepProcessed("Patching file downloaded");
                    }
                }
                catch(Exception ex)
                {
                    ServiceSingleton.Logger.Log(string.Format("Error during patch file download with message {0}", ex.Message));
                    throw ex;
                }              
            });

            await Tsk;                              
        }    


        private async Task DoPatchFile(PatchingInstruction Instruction, string SourceDir, string DestDir, bool KeepPatches)
        {
            try
            {
                await DoDownloadPatchFile(Instruction);

                StepProcessed("Patching game file : " + Instruction.DestFile.Name);
                ElementProcessed(0, 1, StockGameProcessStep.PatchGameFile, Instruction.DestFile.Name);

                string SourceFileName = Instruction.SourceFile.GetFullName(SourceDir);
                string DestinationFileName = Instruction.DestFile.GetFullName(DestDir);
                string PatchFileName = Path.Combine(_PatchDir, Instruction.PatchFile);

                Directory.CreateDirectory(DestDir);

                ServiceSingleton.Logger.Log("Executing xdelta3 patch:");
                ServiceSingleton.Logger.Log($"  Source      = {SourceFileName}");
                ServiceSingleton.Logger.Log($"  Patch       = {PatchFileName}");
                ServiceSingleton.Logger.Log($"  Destination = {DestinationFileName}");

                var xdeltaPath = Path.Combine(ServiceSingleton.Folders.LibDirectory, "xdelta3");
                var psi = new ProcessStartInfo
                {
                    WorkingDirectory = DestDir,
                    FileName = xdeltaPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                psi.ArgumentList.Add("-d");
                psi.ArgumentList.Add("-f");
                psi.ArgumentList.Add("-s");
                psi.ArgumentList.Add(SourceFileName);     // arg 1
                psi.ArgumentList.Add(PatchFileName);      // arg 2
                psi.ArgumentList.Add(DestinationFileName); // arg 3

                using var process = new Process();
                process.StartInfo = psi;

                process.Start();

                string stdout = await process.StandardOutput.ReadToEndAsync();
                string stderr = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                ServiceSingleton.Logger.Log($"Exit code: {process.ExitCode}");
                if (!string.IsNullOrWhiteSpace(stdout))
                    ServiceSingleton.Logger.Log($"stdout: {stdout}");
                if (!string.IsNullOrWhiteSpace(stderr))
                    ServiceSingleton.Logger.Log($"stderr: {stderr}");

                if (process.ExitCode != 0)
                {
                    throw new GameFilePatchingException(
                        $"Failed to patch game file : {Instruction.DestFile.Name}",
                        stderr + Environment.NewLine + stdout
                    );
                }

                if (!KeepPatches && File.Exists(PatchFileName))
                    File.Delete(PatchFileName);

                StepProcessed("Game file : " + Instruction.DestFile.Name + " patched");
                ElementProcessed(1, 1, StockGameProcessStep.PatchGameFile, Instruction.DestFile.Name);
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"Error during game file patching with message: {ex.Message}");
                throw;
            }
        }  

        private void CheckPatchedFile(PatchingInstruction Instruction, string DestDir)
        {
            string FileName = Instruction.DestFile.GetFullName(DestDir);

            StepProcessed("Checking integrity for patched game file " + Instruction.DestFile.Name);
            ElementProcessed(0, 1, StockGameProcessStep.CheckPatchedGameFile, Instruction.DestFile.Name);            

            string FileHash = ServiceSingleton.Files.GetHash(FileName);

            if (FileHash != Instruction.DestFile.Hash)
            {
                throw new GameFileIntegrityException("Hash for game file : " + FileName + " does not match!");
            }

            this.StepProcessed("Patched game file " + Instruction.DestFile.Name + " integrity ok");
            ElementProcessed(1, 1, StockGameProcessStep.CheckPatchedGameFile, Instruction.DestFile.Name);
        }

        public async Task PatchFile(string sourceFile, string destinationFile, string patchFile)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    var workingDirectory = new FileInfo(destinationFile).DirectoryName ?? ".";
                    
                    var xdeltaPath = Path.Combine(ServiceSingleton.Folders.LibDirectory, "xdelta3");
                    var psi = new ProcessStartInfo
                    {
                        FileName = xdeltaPath,
                        Arguments = $"-d -f -s \"{sourceFile}\" \"{patchFile}\" \"{destinationFile}\"",
                        WorkingDirectory = workingDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = new Process { StartInfo = psi };

                    List<string> output = new();
                    proc.OutputDataReceived += (_, e) => { if (e.Data != null) output.Add(e.Data); };
                    proc.ErrorDataReceived += (_, e) => { if (e.Data != null) output.Add(e.Data); };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        throw new Exception(
                            $"xdelta3 failed (exit {proc.ExitCode})\n{string.Join(Environment.NewLine, output)}");
                    }

                    ServiceSingleton.Logger.Log(
                        $"Patched {Path.GetFileName(destinationFile)} successfully (exit {proc.ExitCode})");
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log($"PatchFile error: {ex.Message}");
                    throw;
                }
            });

            await task;
        }

        private void DeleteFile(PatchingInstruction Instruction, string DestDir)
        {
            switch (Instruction.SourceFile.Location)
            {
                case FileLocation.Data:
                    File.Delete(Path.Combine(DestDir, "Data", Instruction.SourceFile.Name));
                    break;

                default:
                    File.Delete(Path.Combine(DestDir, Instruction.SourceFile.Name));
                    break;
            }            
        }

        public async Task PatchFile(PatchingInstruction Instruction, string SourceDir, string DestDir, bool KeepPatches)
        {            
            var Tsk = Task.Run(async ()=>
            {
                try
                {
                    //await DoDownloadBinaries();

                    StepProcessed("About to patch game file : " + Instruction.DestFile.Name);

                    switch (Instruction.Action)
                    {
                        case PatcherAction.Delete:
                            DeleteFile(Instruction, DestDir);
                            StepProcessed("Game file : " + Instruction.DestFile.Name + " deleted");
                            break;
                        case PatcherAction.Patch:                            
                            await DoPatchFile(Instruction, SourceDir, DestDir, KeepPatches);                                                        
                            CheckPatchedFile(Instruction, DestDir);                            
                            break;
                    }                    
                }
                catch(Exception ex)
                {
                    throw ex;
                }
            });

            await Tsk;            
        }

        #endregion

    }
}
