/*
 * Replaced System.Drawing with SixLabors
 * Replacing Syncfusion.Pdf because it doesn't appear to be free
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using System.Threading.Tasks;
using Nolvus.Core.Misc;

namespace Nolvus.Core.Interfaces
{
    public interface IReportService : INolvusService
    {
        Task<string> GenerateReportToClipBoard(ModObjectList ModObjects, Action<string, int> Progress);
        //Task<PdfDocument> GenerateReportToPdf(ModObjectList ModObjects, Image Image, Action<string, int> Progress);
        Task<byte[]> GenerateReportToPdf(ModObjectList ModObjects, Image Image, Action<string, int> Progress);
    }
}
