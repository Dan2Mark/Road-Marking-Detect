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

        public MarkingQualityDetection(Bitmap bmp, List<Line> lines)
        {
            img = bmp;

            for (int i = 0; i < lines.Count; i++)
            {
                var procOfBrigh = GetProcentOfWhiteInLine(lines[i]);
                if (procOfBrigh > 0.3)
                    this.lines.Add((lines[i], procOfBrigh));
                else
                    Console.WriteLine(lines[i]);
            }
        }
        public Mat GetBadLines(Mat mat, int procentsOfBrightness)
        {
            double badest = 0;//lines.Min(line => line.Item2);
            double best = 1;//lines.Max(line => line.Item2);
            Mat new_mat = mat.Clone();
            foreach (var line in this.lines)
            {
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
                            new_mat.At<Vec3b>(x, (int)Math.Round(y))[0] = 10;
                            new_mat.At<Vec3b>(x, (int)Math.Round(y))[1] = G;
                            new_mat.At<Vec3b>(x, (int)Math.Round(y))[2] = R;
                        }
                    }
                }
            }
            return new_mat;
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
            int countOfPixelsOutTheLine = 0;
            double brightnessOutTheLine = 0;

            for (int i = 0; i < line.lineBlocks.Count; i++)
            {
                LineBlock blockLine = line.lineBlocks[i];
                for (int x = blockLine.X0, x_i = 0; x < img.Height && x_i < blockLine.length; x++, x_i++)
                {
                    double y = blockLine.Y0 + Math.Round(blockLine.leftShift * x_i) - 4, y_j = 0;
                    if (y < 0)
                        y = 0;
                    for (; y_j < 3 && Math.Round(y) < img.Width; y++, y_j++)
                    {
                        var pixel = img.GetPixel((int)y, x);
                        brightnessOutTheLine += pixel.GetBrightness();
                        countOfPixelsOutTheLine++;
                    }
                    double rightShift = blockLine.Y0 + Math.Round(blockLine.rightShift * x_i) + blockLine.width;//line.Y0 + line.width + Math.Round(line.rightShift * x_i);
                    for (; Math.Round(y) < img.Width && Math.Round(y) > 0 && y <= Math.Round(rightShift); y++, y_j++)
                    {
                        var pixel = img.GetPixel((int)y, x);
                        brightness += pixel.GetBrightness();
                        countOfPixels++;
                    }
                    for (int j = 0; Math.Round(y) < img.Width && j < 3 && y < 640; j++, y++)
                    {
                        var pixel = img.GetPixel((int)y, x);
                        brightnessOutTheLine += pixel.GetBrightness();
                        countOfPixelsOutTheLine++;
                    }
                }
            }
            brightnessOutTheLine =  countOfPixelsOutTheLine > 0 ? brightnessOutTheLine / countOfPixelsOutTheLine : 0;
            brightness = ((countOfPixels > 0 ? brightness / countOfPixels : 0) - brightnessOutTheLine) * 3;
             
            return  brightness;
        }
    }
}
