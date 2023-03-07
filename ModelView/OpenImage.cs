using Road_Marking_Detect.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Road_Marking_Detect.ModelView
{
    class OpenImage
    {
        Mat opened_Image = new Mat();
        Mat simple_Image = new Mat();
        List<Line> lines;
        public string consoleLog { get; private set; }
        public OpenImage()
        {
            opened_Image = GetImage.OpenFromFile(@"C:\Users\danma\Downloads\Дорога_1.jpg");
            opened_Image = Simplification.Resize(opened_Image);
            simple_Image = Simplification.imageSimplification(opened_Image);
            lines = Line.FindLines(simple_Image);
            Console.WriteLine("Success");
        }
        public Bitmap GetImageForPictureBox(bool is_simple = false, bool show_lines = false)
        {
            var image = is_simple ? simple_Image : opened_Image;

            if (show_lines)
            {
                 image = Line.DrawLines(lines, image);
            }
            
            return MatToBitmap(image);
        }

        private static Bitmap MatToBitmap(Mat mat)
        {
            using (var ms = mat.ToMemoryStream())
            {
                return (Bitmap)Image.FromStream(ms);
            }
        }

    }

}
