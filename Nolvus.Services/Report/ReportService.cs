using System;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Misc;
using Nolvus.Core.Services;
using SixLabors.ImageSharp;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using SixLabors.ImageSharp.Formats.Png;

namespace Nolvus.Services.Report
{
    public class ReportService : IReportService
    {
        public Task<string> GenerateReportToClipBoard(ModObjectList ModObjects, Action<string, int> Progress)
        {
            return Task.Run(async () =>
            {
                Progress("Generating report", 10);
                var instance = ServiceSingleton.Instances.WorkingInstance;

                var result = "";

                return result;
            });
        }

        public async Task<string> GenerateReportToPdf(ModObjectList ModObjects, SixLabors.ImageSharp.Image coverImage, Action<string, int> Progress)
        {
            Progress("Generating report", 5);

            var instance = ServiceSingleton.Instances.WorkingInstance;
            var outputPath = Path.Combine(
                ServiceSingleton.Folders.ReportDirectory,
                $"{instance.Name}-v{instance.Version}.pdf"
            );

            var cpu = await ServiceSingleton.Globals.GetCPUInfo();
            var gpu = string.Join(Environment.NewLine, ServiceSingleton.Globals.GetVideoAdapters().ToArray());
            var ram = await ServiceSingleton.Globals.GetRamCount();

            Progress("Generating report", 30);

            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);
                    page.Background("#363636");

                    page.Content().Column(col =>
                    {
                        var coverBytes = ImageSharpToBytes(coverImage);
                        col.Item().Image(coverBytes);


                        col.Item().PaddingVertical(10).Text("HARDWARE CONFIGURATION")
                            .FontSize(20).Bold().FontColor("#FFA500")
                            .AlignCenter();

                        col.Item().Text($"CPU: {cpu}").FontColor("#FFFFFF");
                        col.Item().Text($"GPU: {gpu}").FontColor("#FFFFFF");
                        col.Item().Text($"RAM: {(ram != "RAM count not found" ? $"{ram} GB" : ram)}").FontColor("#FFFFFF");

                        col.Item().PageBreak();

                        col.Item().Text("INSTANCE CONFIGURATION")
                            .FontSize(20).Bold().FontColor("#FFA500").AlignCenter();

                        col.Item().Text($"Name: {instance.Name}, Profile: {ModObjects.Profile ?? "Default"}").FontColor("#FFFFFF");
                        col.Item().Text($"Version: {instance.Version}").FontColor("#FFFFFF");
                        col.Item().Text($"Install Directory: {instance.InstallDir}").FontColor("#FFFFFF");
                        col.Item().Text($"Archive Directory: {instance.ArchiveDir}").FontColor("#FFFFFF");

                        col.Item().Text(" ");
                        col.Item().Text("Settings").FontSize(14).Bold().FontColor("#FFA500");

                        col.Item().Text($"Resolution: {instance.Settings.Width}x{instance.Settings.Height}").FontColor("#FFFFFF");
                        col.Item().Text($"Screen Ratio: {instance.Settings.Ratio}").FontColor("#FFFFFF");
                        col.Item().Text($"Language: {instance.Settings.LgName}").FontColor("#FFFFFF");
                        col.Item().Text($"CDN: {instance.Settings.CDN}").FontColor("#FFFFFF");

                        col.Item().PageBreak();

                        col.Item().Text("INSTANCE STATUS")
                            .FontSize(20).Bold().FontColor("#FFA500")
                            .AlignCenter();

                        col.Item().Text($"Added Mods: {ModObjects.AddedModsCount}").FontColor("#FFFFFF");
                        col.Item().Text($"Removed Mods: {ModObjects.RemovedModsCount}").FontColor("#FFFFFF");
                        col.Item().Text($"Version Changed: {ModObjects.VersionMismatchCount}").FontColor("#FFFFFF");
                        col.Item().Text($"Disabled Mods: {ModObjects.DisabledModsCount}").FontColor("#FFFFFF");

                        if (ModObjects.AddedModsCount > 0)
                        {
                            col.Item().PageBreak();
                            col.Item().Text("ADDED MODS").FontSize(16).Bold().FontColor("#FFA500");
                            foreach (var mod in ModObjects.AddedMods)
                                col.Item().Text(mod.Name).FontColor("#FFFFFF");
                        }
                    });
                });
            })
            .GeneratePdf(outputPath);

            Progress("Generating report", 100);
            return outputPath;
        }

        private static byte[] ImageSharpToBytes(SixLabors.ImageSharp.Image image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }
    }
}
