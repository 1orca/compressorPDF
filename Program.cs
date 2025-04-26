using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Pdf;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;
using iText.IO.Image;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using iText.Kernel.Geom;
using iText.Layout;
using Microsoft.VisualBasic.FileIO;
using Path = System.IO.Path;
using System.Runtime.CompilerServices;

namespace CompresorPDF
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Uso: MiApp.exe \"archivo.pdf\" -calidad 5");
                return;
            }
            string inputPath = args[0];
            
            int calidad = 50; // va
                              


            string nombreSinExtension = Path.GetFileNameWithoutExtension(inputPath);
            string carpeta = Path.GetDirectoryName(inputPath);

            string outputPath = carpeta + @"\" + nombreSinExtension + "_comprimido.pdf"; ////nuevo nombre de archivo
            // Analizar otros argumentos opcionales
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-calidad" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int calidadParam))
                    {
                        if (calidadParam >= 1 && calidadParam <= 100)
                        {
                            calidad = calidadParam;
                        }
                        else
                        {
                            Console.WriteLine("La calidad debe estar entre 1 y 100.");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("La calidad debe ser un número entero.");
                        return;
                    }
                }
            }

            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath, new WriterProperties().SetFullCompressionMode(true));
            using var pdfOriginal = new PdfDocument(reader);
            using var pdfComprimido = new PdfDocument(writer);

            int numberOfPages = pdfOriginal.GetNumberOfPages();

            for (int i = 1; i <= numberOfPages; i++)
            {
                var page = pdfOriginal.GetPage(i);
                var resources = page.GetResources();
                var xobjects = resources.GetResource(PdfName.XObject);

                if (xobjects == null) continue;

                foreach (var name in xobjects.KeySet())
                {
                    var xobj = xobjects.Get(name);

                    if (xobj is PdfStream stream && PdfName.Image.Equals(stream.GetAsName(PdfName.Subtype)))
                    {
                        var imgXObject = new iText.Kernel.Pdf.Xobject.PdfImageXObject(stream);
                        byte[] originalBytes = imgXObject.GetImageBytes(true);

                        using var image = Image.Load<Rgba32>(originalBytes);

                        // redimensionames
                        int targetMax = 1024;
                        double ratio = Math.Min((double)targetMax / image.Width, (double)targetMax / image.Height);
                        int newW = (int)(image.Width * ratio);
                        int newH = (int)(image.Height * ratio);

                        image.Mutate(x => x.Resize(newW, newH));

                        using var ms = new MemoryStream();

                        //comprimimos
                        image.Save(ms, new JpegEncoder { Quality = calidad });
                        byte[] compressedBytes = ms.ToArray();

                        
                        var imageData = ImageDataFactory.Create(compressedBytes);
                        var newPage = pdfComprimido.AddNewPage(new PageSize(PageSize.LETTER));
                        
                        var doc = new Document(pdfComprimido);
                        var pdfImage = new iText.Layout.Element.Image(imageData);

                        // Ajustar tamaño
                        pdfImage.ScaleToFit(PageSize.LETTER.GetWidth(), PageSize.LETTER.GetHeight());
                        pdfImage.SetFixedPosition(i,
                            (PageSize.LETTER.GetWidth() - pdfImage.GetImageScaledWidth()) / 2, //centrado
                            (PageSize.LETTER.GetHeight() - pdfImage.GetImageScaledHeight()) / 2 //centrado
                        );

                        doc.Add(pdfImage);
                        doc.Flush();
                        break; // Solo una imagen por página
                    }
                }
            }
            pdfComprimido.Close();
        }
    }
}
