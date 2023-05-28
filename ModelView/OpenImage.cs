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
        public MarkingQualityDetection markingQualityDetection;
        Mat opened_Image = new Mat();
        Mat simple_Image = new Mat();
        List<Line> lines;
        public string consoleLog { get; private set; }
        public OpenImage(string file)
        {
            opened_Image = GetImage.OpenFromFile(file);
            opened_Image = Simplification.Resize(opened_Image);
            simple_Image = Simplification.imageSimplification(opened_Image);
            lines = Line.FindLines(simple_Image);
            markingQualityDetection = new MarkingQualityDetection(MatToBitmap(opened_Image), lines);
        }
        public Bitmap GetImageForPictureBox(bool is_simple = false, bool show_lines = false)
        {
            var image = MatToBitmap(is_simple ? simple_Image : opened_Image);
            if (show_lines)
            {
                 image = markingQualityDetection.GetBadLines(1,image);
            }
            return image;
        }
        public string GetProcentOfWhiteText()
        {
            return "Яскравість дорожньої розмітки " + markingQualityDetection.GetProcentOfWhite() +"%";
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
