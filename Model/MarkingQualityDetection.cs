using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Road_Marking_Detect.Model
{
    internal class MarkingQualityDetection
    {
        Bitmap img;
        List<(Line, double)> lines = new List<(Line, double)>();
        int bad, notbad, notgood, good;
        public (int a, int b, int c, int d) getCountPixelsForQuality()
        {
            return (bad, notbad, notgood, good);
        }
        public MarkingQualityDetection(Bitmap bmp, List<Line> lines)
        {
            img = bmp;
            for (int i = 0; i < lines.Count; i++)
            {
                var procOfBrigh = GetProcentOfWhiteInLine(lines[i]);
                    this.lines.Add((lines[i], procOfBrigh));
            }
        }

        public Bitmap GetBadLines(double maxBrightness = 1, Bitmap img = null)
        {
            //DrawMatrix();
            if (img == null)
                img = this.img;
            double badest = getMinBlack();//lines.Min(line => line.Item2);
            double best = getMaxWhite();//lines.Max(line => line.Item2);
            
            foreach (var line in this.lines)
            {
                if (line.Item2 > maxBrightness)
                    continue;
                byte R = (byte)((line.Item2 - best) * (255 - 0) / (badest - best) + 0);
                byte G = (byte)((line.Item2 - badest) * (255 - 0) / (best - badest) + 0);
                for (int i = 0; i < line.Item1.lineBlocks.Count; i++)
                {
                    LineBlock blockLine = line.Item1.lineBlocks[i];
                    for (int x = blockLine.X0, x_i = 0; x < img.Height && x_i < blockLine.length; x++, x_i++)
                    {
                        double rightShift = blockLine.Y0 + Math.Round(blockLine.rightShift * x_i) + blockLine.width;//line.Y0 + line.width + Math.Round(line.rightShift * x_i);
                        for (double y = blockLine.Y0 + Math.Round(blockLine.leftShift * x_i) - 1, y_j = 0; Math.Round(y) < img.Width && Math.Round(y) > 0 && y <= Math.Round(rightShift); y++, y_j++)
                        {
                            if (line.Item2 < 0.25f)
                                bad++;
                            else if (line.Item2 < 0.5f)
                                notbad++;
                            else if (line.Item2 < 0.75f)
                                notgood++;
                            else
                                good++;
                            img.SetPixel((int)Math.Round(y), x, Color.FromArgb(R, G, 10));
                        }
                    }
                }
            }
            return img;
        }
        public int GetProcentOfWhite()
        {
            double brightness = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                brightness = (brightness * i + lines[i].Item2) / (i + 1);
            }
            return (int)(brightness * 100);
        }
        private double GetProcentOfWhiteInLine(Line line)
        {
            int countOfPixels = 0;
            double brightness = 0;

            for (int i = 0; i < line.lineBlocks.Count; i++)
            {
                LineBlock blockLine = line.lineBlocks[i];
                for (int x = blockLine.X0, x_i = 0; x < img.Height && x_i < blockLine.length; x++, x_i++)
                {
                    double y = blockLine.Y0 + Math.Round(blockLine.leftShift * x_i), y_j = 0;
                    if (y < 0)
                        y = 0;
                    double rightShift = blockLine.Y0 + Math.Round(blockLine.rightShift * x_i) + blockLine.width;
                    for (; Math.Round(y) < img.Width && Math.Round(y) > 0 && y <= Math.Round(rightShift); y++, y_j++)
                    {
                        var pixel = img.GetPixel((int)y, x);
                        brightness += pixel.GetBrightness();
                        countOfPixels++;
                    }
                }
            }
            brightness = ((countOfPixels > 0 ? brightness / countOfPixels : 0));
             
            return  brightness;
        }

        float getMinBlack()
        {
            float min = 1;
            for (int x = 0; x < img.Height; x++)
            {
                for (int y = 0; y < img.Width; y++)
                {
                    float pixel = img.GetPixel(y, x).GetBrightness();
                    if (min > pixel)
                        min = pixel;
                    if (min == 0)
                        break;
                }
                if (min == 0)
                    break;
            }
            return min;
        }
        float getMaxWhite()
        {
            float max = 0;
            for (int x = 0; x < img.Height; x++)
            {
                for (int y = 0; y < img.Width; y++)
                {
                    float pixel = img.GetPixel(y, x).GetBrightness();
                    if (max < pixel)
                        max = pixel;
                    if (max == 1)
                        break;
                }
                if (max == 1)
                    break;
            }
            return max;
        }
        void DrawMatrix()
        {
            for (int i = 0; i < img.Width; i++)
            {
                for (int  j = 0;  j < img.Height;  j++)
                {
                    Console.WriteLine(Math.Round(img.GetPixel(i, j).GetBrightness(),3));
                }
                Console.WriteLine("========");
            }
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
