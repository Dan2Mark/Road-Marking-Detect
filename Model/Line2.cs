using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Road_Marking_Detect.Model
{
    class Line2
    {
        System.Drawing.Point startPoint;
        public int X0 { get { return startPoint.X; } }
        public int Y0 { get { return startPoint.Y; } }
        double leftShift = 0;
        double rightShift = 0;
        int length = 0;
        int startWidth = 0;
        int lastWidth = 0;
        int lastY = 0;

        private bool justChecked = false;
        public Line2(int X0, int Y0, int startWidth)
        {
            startPoint.X = X0;
            startPoint.Y = Y0;
            this.startWidth = startWidth;
        }

        static Line2 getBestLine(List<Line2> lines, int x, int y, int width)
        {
            Line2 leftNearestLine = null;
            Line2 rightNearestLine = null;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                //if (line.justChecked)
                //    continue;
                if (line.X0 > x)
                {
                    if (rightNearestLine == null)
                        rightNearestLine = line;
                    else if (rightNearestLine.X0 < line.X0)
                        rightNearestLine = line;

                }
                if (line.X0 < x)
                {
                    if (leftNearestLine == null)
                        leftNearestLine = line;
                    else if (leftNearestLine.X0 > line.X0)
                        leftNearestLine = line;
                }
            }
            Line2 bestLine;
            if (leftNearestLine != null && rightNearestLine != null)
            {
                int leftSuit = Math.Abs(leftNearestLine.lastWidth - width) + Math.Abs(leftNearestLine.X0 - x);
                int rightSuit = Math.Abs(rightNearestLine.lastWidth - width) + Math.Abs(rightNearestLine.X0 - x);
                bestLine = leftSuit < rightSuit ? leftNearestLine : rightNearestLine;
                //bestLine.justChecked = true;
            }
            else if (leftNearestLine != null)
                bestLine = leftNearestLine;
            else if (rightNearestLine != null)
                bestLine = rightNearestLine;
            else
                bestLine = new Line2(x, y, width);

            return bestLine;
        }
        private void AddNewLayer(int x, int y, int width)
        {
            if (lastY > 0)
                length += lastY - y;
            else
                length++;
            lastY = y;
            leftShift = (leftShift * (length - 1) + (X0 - x)) / length;
            rightShift = (rightShift * (length - 1) + (X0 - x));
        }
        static public List<Line2> PickOutLines(Mat mat, int delta = 10)
        {
            List<Line2> lines = new List<Line2>();
            for (int y = mat.Rows - 1; y >= 0; y--)
            {
                byte upperThreshold = 0;
                byte lowerThreshold = 255;
                int Brightness = 0;
                int countBrightness = 0;
                List<(int, int)> lines_points = new List<(int, int)>();
                byte[] pixels = new byte[mat.Cols];
                for (int x = 0; x < mat.Cols; x++)
                {
                    pixels[x] = mat.At<Vec3b>(y, x)[0];
                    Brightness += pixels[x];
                    if (pixels[x] > 0)
                        countBrightness++;
                    if (pixels[x] > upperThreshold)
                        upperThreshold = pixels[x];
                    if (pixels[x] < lowerThreshold)
                        lowerThreshold = pixels[x];
                }
                double averageBrightness = countBrightness > 0 ? Brightness / countBrightness : 0;
                bool brightnessFlag = false, riseFlag = false;
                int lineWidth = 0;
                int blackPixelCount = 0;
                int lastAverageBrightness = 0;
                if (averageBrightness > 1)
                    for (int x = 1; x < mat.Cols; x++)
                    {
                        lastAverageBrightness = (pixels[x] + lastAverageBrightness * 4) / 5;
                        if ((Math.Abs(pixels[x] - lastAverageBrightness) < 5 || pixels[x] > lastAverageBrightness) && pixels[x] >= (upperThreshold * 2 + averageBrightness) / 3)
                        {
                            lineWidth++;
                        }

                        else if (blackPixelCount < 3 && lineWidth > 0)
                        {
                            blackPixelCount++;
                        }
                        else if (blackPixelCount >= 3)
                        {
                            lines_points.Add((x - lineWidth - 3, lineWidth));
                            blackPixelCount = 0;
                            lineWidth = 0;
                        }
                        //else
                            //new_mat.At<Vec3b>(y, x)[0] = 0;
                        if (lines_points.Count > 0)
                        {
                            int maxlength = (int)((lines_points.Max(t => t.Item2) + lines_points.Average(t => t.Item2)) / 2);
                            if (maxlength < 5)
                                maxlength = 5;
                            if (maxlength > 20)
                                maxlength = 20;
                            foreach (var line in lines_points)
                            {
                                if (line.Item2 > maxlength)
                                    continue;
                                //for (int x1 = line.Item1; x1 < line.Item1 + line.Item2; x1++)
                                //{
                                //    for (int c = 0; c < 3; c++)
                                //        new_mat.At<Vec3b>(y, x1)[c] = 255;
                                //}
                                var bestLine = getBestLine(lines, line.Item1, y, line.Item2);
                                if (!lines.Contains(bestLine))
                                    lines.Add(bestLine);
                                else
                                    bestLine.AddNewLayer(line.Item1, y, line.Item2);
                            }
                        }
                    }
            }
            lines = lines.FindAll(line => line.length > 20);
            return lines;
        }
        static Random random = new Random();
        public static Mat DrawLines(List<Line2> lines, Mat mat)
        {
            Mat new_mat = mat.Clone();
            foreach (var line in lines)
            {

                Console.WriteLine(line.ToString());
                byte color = (byte)random.Next(0, 255);
                byte color2 = (byte)random.Next(50, 205);
                byte color3 = (byte)random.Next(0, 255);
                for (int x = line.X0, x_i = 0; x >= 0 && x_i < line.length; x--, x_i++)
                {
                    double rightShift = line.Y0 + Math.Round(line.rightShift * x_i) + line.startWidth;//line.Y0 + line.width + Math.Round(line.rightShift * x_i);
                    for (double y = line.Y0 + Math.Round(line.leftShift * x_i) - 1, y_j = 0; Math.Round(y) < mat.Cols && Math.Round(y) > 0 && y <= Math.Round(rightShift); y++, y_j++)
                    {
                        new_mat.At<Vec3b>(x, (int)Math.Round(y))[0] = color;
                        new_mat.At<Vec3b>(x, (int)Math.Round(y))[1] = color3;
                        new_mat.At<Vec3b>(x, (int)Math.Round(y))[2] = color2;
                    }
                }
            }
            return new_mat;
        }
    }
}
