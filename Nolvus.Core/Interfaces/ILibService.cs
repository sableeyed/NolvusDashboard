/*
 * Replaced System.Drawing with SixLabors
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace Nolvus.Core.Interfaces
{
    public interface ILibService : INolvusService
    {
        Image ResizeKeepAspectRatio(Image source, int width, int height);
        Image SetImageOpacity(Image Source, float Opacity);
        Image SetImageGradient(Image InputImage);
        string EncryptString(string value);
        string DecryptString(string value);
        Image GetImageFromWebStream(string ImageUrl);
        Image GetImageFromUrl(string ImageUrl);
        byte[] ImageToByteArray(Image Image);
    }
}
