using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace Nolvus.Services.Lib
{
    public class LibService : ILibService
    {
        private static readonly HttpClient _http = new HttpClient();

        public Image GetImageFromUrl(string url)
        {
            var bytes = _http.GetByteArrayAsync(url).GetAwaiter().GetResult();
            return Image.Load(bytes);
        }

        public Image GetImageFromWebStream(string imageUrl)
        {
            var bytes = _http.GetByteArrayAsync(imageUrl).GetAwaiter().GetResult();
            return Image.Load(bytes);
        }

        public Image ResizeKeepAspectRatio(Image source, int width, int height)
        {
            return source.Clone(ctx =>
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center,
                    Sampler = KnownResamplers.Bicubic
                }));
        }

        public Image SetImageOpacity(Image source, float opacity)
        {
            return source.Clone(ctx => ctx.Opacity(opacity));
        }

        public Image SetImageGradient(Image inputImage)
        {

            int w = inputImage.Width;
            int h = inputImage.Height;

            using var gradient = new Image<Rgba32>(w, h);
            gradient.Mutate(g =>
            {
                var brush = new LinearGradientBrush(
                    new PointF(0, 0),
                    new PointF(0, h),
                    GradientRepetitionMode.None,
                    new ColorStop(0f, Color.White),
                    new ColorStop(1f, Color.Transparent));


                g.Fill(brush);
            });

            var result = inputImage.CloneAs<Rgba32>();

            gradient.ProcessPixelRows(result, (gradientAccessor, resultAccessor) =>
            {
                for (int y = 0; y < gradientAccessor.Height; y++)
                {
                    var gradRow = gradientAccessor.GetRowSpan(y);
                    var resRow  = resultAccessor.GetRowSpan(y);

                    for (int x = 0; x < gradRow.Length; x++)
                    {
                        var r = resRow[x];
                        r.A = gradRow[x].A;
                        resRow[x] = r;
                    }
                }
            });

            return result;
        }

        public byte[] ImageToByteArray(Image image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        public string EncryptString(string value)
        {
            string Key = "1F2CAB925HFBBCAE589632FABC547892";
            string IV = "ACtEDg70CcU=";

            using var tripleDES = new TripleDESCryptoServiceProvider
            {
                Key = Convert.FromBase64String(Key),
                IV = Convert.FromBase64String(IV),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            using var encryptor = tripleDES.CreateEncryptor(tripleDES.Key, tripleDES.IV);
            var input = Encoding.UTF8.GetBytes(value);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(input, 0, input.Length);
                cs.FlushFinalBlock();
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public string DecryptString(string value)
        {
            string Key = "1F2CAB925HFBBCAE589632FABC547892";
            string IV = "ACtEDg70CcU=";

            using var tripleDES = new TripleDESCryptoServiceProvider
            {
                Key = Convert.FromBase64String(Key),
                IV = Convert.FromBase64String(IV),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            using var decryptor = tripleDES.CreateDecryptor(tripleDES.Key, tripleDES.IV);
            var input = Convert.FromBase64String(value);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                cs.Write(input, 0, input.Length);
                cs.FlushFinalBlock();
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
