/*
 * Replaced System.Drawing with SixLabors
 * Replacing Syncfusion.Pdf because it doesn't appear to be free
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Misc;

namespace Nolvus.Core.Interfaces
{
    public interface IReportService : INolvusService
    {
        Task<byte[]> GenerateReportToPdf(ModObjectList ModObjects, string CoverSvg, Action<string, int> Progress);
    }
}
