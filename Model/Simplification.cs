using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Features2D;

namespace Road_Marking_Detect.Model
{
    class Simplification
    {
        static Mat ToGray(Mat mat)
        {
            return mat.CvtColor(ColorConversionCodes.RGB2GRAY);
        }
        static Mat ToBinary(Mat mat)
        {
            return mat.Threshold(200, 255, ThresholdTypes.Binary);
        }

        static Mat Blur(Mat mat)
        {
            return mat.Blur(new OpenCvSharp.Size(2, 2));
        }
        static double GetMiddleBrightness(Mat mat)
        {
            long mid = 0;
            for (int y = 0; y < mat.Cols; y++)
            {
                for (int x = 0; x < mat.Rows; x++)
                {
                    mid += mat.At<Vec3b>(x, y)[0];
                }
            }
            return (mid / mat.Rows / mat.Cols);
        }
        /// <param name="mat">Зображження</param>
        /// <param name="alpha">Коефіцієнт зміни контрасту</param>
        /// <param name="beta">Коефіцієнт зміни яскравості</param>
        /// <returns></returns>
        static Mat ContrastBrightness(Mat mat, double alpha = 1.0, int beta = 0, int startIndex = 0)
        {
            Mat new_mat = mat.Clone();
            for (int y = 0; y < mat.Cols; y++)
            {
                for (int x = startIndex; x < mat.Rows; x++)
                {
                    for (int c = 0; c < mat.Channels(); c++)
                    {
                        new_mat.At<Vec3b>(x, y)[c] = ToByte(alpha * mat.At<Vec3b>(x, y)[c] + beta);
                    }
                }
            }
            return new_mat;
        }
        static int GetBestBeta(Mat mat, int startIndex = 0)
        {
            long beta_cols = 0;
            for (int y = 0; y < mat.Cols; y++)
            {
                long beta_row = 0;
                for (int x = startIndex; x < mat.Rows; x++)
                {
                    long beta_channels = 0;
                    for (int c = 0; c < mat.Channels(); c++)
                    {
                        beta_channels += mat.At<Vec3b>(x, y)[c];
                    }
                    beta_row += beta_channels / mat.Channels();
                }
                beta_cols += beta_row / mat.Rows;
            }
            return (int)(126 - (beta_cols / mat.Cols));
        }
        static double GetBestAlpha(Mat mat, int startIndex = 0)
        {
            long alpha_cols = 0;
            for (int y = 0; y < mat.Cols; y++)
            {
                long alpha_row = 0;
                for (int x = startIndex; x < mat.Rows; x++)
                {
                    long alpha_channels = 0;
                    for (int c = 0; c < mat.Channels(); c++)
                    {
                        byte color = mat.At<Vec3b>(x, y)[c];
                        alpha_channels += Math.Abs(127 - color);
                    }
                    alpha_row += alpha_channels / mat.Channels();
                }
                alpha_cols += alpha_row / mat.Rows;
            }
            double alpha = 2 * ((126 - (alpha_cols / mat.Cols)) * 0.5) / 126.0f;
            if (alpha < 1)
                alpha = 1;
            return alpha;
        }
        static byte ToByte(double a)
        {
            if (a > 255)
                return 255;
            else if (a < 0)
                return 0;
            else
                return (byte)a;
        }
        public static Mat Resize(Mat mat)
        {
            int width = 640;
            int height = (int)(mat.Height / (mat.Width / (double)width));
            Mat new_mat = new Mat();
            Cv2.Resize(mat, new_mat, new OpenCvSharp.Size(width, height));

            return new_mat;
        }
        static Mat HideColor(Mat mat)
        {
            var new_mat = mat.Clone();
            for (int y = 0; y < mat.Cols; y++)
            {
                for (int x = 0; x < mat.Rows; x++)
                {
                    byte R = mat.At<Vec3b>(x, y)[0], G = mat.At<Vec3b>(x, y)[1], B = mat.At<Vec3b>(x, y)[2];
                    if ((Math.Abs(R - G) > 30 || Math.Abs(B - G) > 30))
                        for (int i = 0; i < 3; i++)
                            new_mat.At<Vec3b>(x, y)[i] = 0;
                }
            }
            return new_mat;

        }
        /// <param name="sky_h">Висота неба</param>
        /// <param name="border">Кількість яркості на чанк</param>
        /// <param name="chank_h">Висота чанку</param>
        /// <param name="maxBlackLines">Максимальна кількість недостатньо ярких чанків</param>
        /// <returns></returns>
        static Mat DeleteSky(Mat mat, out int sky_h, int border = 400, int chank_h = 20, int maxBlackChanks = 4)
        {
            double white_count = 0;
            Mat new_mat = mat.Clone();
            int blackLines = 0;
            bool flag = true;
            int y_last = 0;
            for (int j = 0; (white_count > border || blackLines <= maxBlackChanks || flag) && j < mat.Rows / 2 / chank_h; j++)
            {
                if (white_count > border)
                {
                    blackLines = 0;
                    flag = false;
                }
                else
                    blackLines++;
                white_count = 0;
                for (int x = 0; x < mat.Cols; x++)
                    for (int y_j = 0; y_j < chank_h; y_j++)
                    {
                        int y = j * chank_h + y_j;
                        int gray_sum = 0;
                        for (int c = 0; c < mat.Channels(); c++)
                            gray_sum += mat.At<Vec3b>(y, x)[c];
                        white_count += gray_sum / mat.Channels() / 200;
                        y_last = y;
                    }
            }
            for (int y = 0; y < y_last; y++)
            {
                for (int x = 0; x < mat.Cols; x++)
                    for (int c = 0; c < mat.Channels(); c++)
                        new_mat.At<Vec3b>(y, x)[c] = 0;

            }
            sky_h = y_last;

            return new_mat;
        }
        static Mat OnlyWhite(Mat mat, byte lvl = 30)
        {
            var new_mat = mat.Clone();
            for (int y = 0; y < mat.Cols; y++)
            {
                for (int x = 0; x < mat.Rows; x++)
                {
                    byte B = mat.At<Vec3b>(x, y)[0];//;, G = mat.At<Vec3b>(x, y)[1], B = mat.At<Vec3b>(x, y)[2];
                    //if ((Math.Abs(R - G) > 30 || Math.Abs(B - G) > 30))// || (B + R + G) / 3 < 170)
                    if (B < 255 - lvl)
                        for (int i = 0; i < 3; i++)
                            new_mat.At<Vec3b>(x, y)[i] = 0;
                }
            }
            return new_mat;

        }
        static Mat DellNoise(Mat mat, double border_min = 3, int countInLineMax = 2, int chank_w = 20)
        {
            int chank_h = chank_w;
            var new_mat = mat.Clone();
            int cnt = 0;
            for (int i = 0; i < mat.Cols / chank_w; i++)
            {
                for (int j = 0; j < mat.Rows / chank_h; j++)
                {
                    int blackWhiteCount = 0;
                    bool flagBlackWhite = false;
                    //int brightness_sum = 0;
                    for (int x_i = 0; x_i < chank_w; x_i++)
                        for (int y_j = 0; y_j < chank_h; y_j++)
                        {
                            int x = i * chank_w + x_i;
                            int y = j * chank_h + y_j;
                            int gray_sum = 0;
                            for (int c = 0; c < mat.Channels(); c++)
                                gray_sum += mat.At<Vec3b>(y, x)[c];
                            //brightness_sum += gray_sum/3;
                            bool pixel = gray_sum > 0 ? true : false;
                            if (flagBlackWhite && !pixel)
                            {
                                blackWhiteCount++;
                                flagBlackWhite = false;
                            }
                            else if (!flagBlackWhite && pixel)
                            {
                                flagBlackWhite = true;
                            }
                        }
                    //if (brightness_sum > 0)
                    //    brightness_sum += 0;
                    //double average_brightness = brightness_sum / (chank_h * chank_w);
                    //if (average_brightness > border_max || average_brightness < border_min)
                    if (blackWhiteCount >= chank_w * countInLineMax)

                        for (int x_i = 0; x_i < chank_w; x_i++)
                            for (int y_j = 0; y_j < chank_h; y_j++)
                            {
                                int x = i * chank_w + x_i;
                                int y = j * chank_h + y_j;
                                for (int c = 0; c < mat.Channels(); c++)
                                    new_mat.At<Vec3b>(y, x)[c] = 0;
                                cnt++;
                            }

                }
            }
            cnt = cnt;
            return new_mat;

        }

        static public Mat PickOutLines(Mat mat, int delta = 2)
        {
            Mat new_mat = GetBlackPicture(mat);
            for (int rowsCols = 0; rowsCols < 1; rowsCols++)
                for (int y = 0; (y < mat.Rows && rowsCols == 0) || (y < mat.Cols && rowsCols == 1); y++)
                {
                    byte upperThreshold = 0;
                    byte lowerThreshold = 255;
                    int Brightness = 0;
                    int countBrightness = 0;
                    List<(int, int)> lines = new List<(int, int)>();
                    byte[] pixels = new byte[rowsCols == 0 ? mat.Cols : mat.Rows];
                    int lightPixelCount = 0;
                    for (int x = 0; (x < mat.Cols && rowsCols == 0) || (x < mat.Rows && rowsCols == 1); x++)
                    {
                        pixels[x] = rowsCols == 0 ? mat.At<Vec3b>(y, x)[0] : mat.At<Vec3b>(x, y)[0];
                        Brightness += pixels[x];
                        countBrightness++;
                        if (pixels[x] > upperThreshold)
                            upperThreshold = pixels[x];
                        if (pixels[x] < lowerThreshold)
                            lowerThreshold = pixels[x];
                        if (pixels[x] >= upperThreshold)
                            lightPixelCount++;
                    }

                    double changeFactor = 0;
                    double averageBrightness = countBrightness > 0 ? Brightness / countBrightness : 0;
                    bool brightnessFlag = false, riseFlag = false;
                    int lineWidth = 0;
                    int blackPixelCount = 0;
                    int lastAverageBrightness = 0;
                    bool startFlag = false;
                    int xStart = 0;
                    if (averageBrightness > 1)
                        for (int x = 1, cnt = 0; x < pixels.Length; x++, cnt++)
                        {
                            var newChangeFactor = pixels[x] - pixels[x - 1];
                            changeFactor = (changeFactor * cnt + newChangeFactor) / (cnt + 1);

                            if (!startFlag && newChangeFactor - changeFactor > 30 && pixels[x] >= averageBrightness)
                            {
                                startFlag = true;
                                xStart = x;
                                cnt = 2;
                                lineWidth = 0;
                            }
                            else if (startFlag && newChangeFactor - changeFactor < -30)
                            {
                                startFlag = false;
                            }
                            lastAverageBrightness = (pixels[x] + lastAverageBrightness * 3) / 4;
                            //  if ( (Math.Abs(pixels[x] - lastAverageBrightness) < 5 || pixels[x] > lastAverageBrightness)  && pixels[x] >= (upperThreshold * delta + averageBrightness) / (delta +1))
                            if (startFlag)
                            {
                                lineWidth++;
                            }

                            else if (blackPixelCount < 3 && lineWidth > 0)
                            {
                                blackPixelCount++;
                            }
                            else if (blackPixelCount >= 3)
                            {
                                lines.Add((x - lineWidth - 3, lineWidth));
                                blackPixelCount = 0;
                                lineWidth = 0;
                            }
                            //else
                            //new_mat.At<Vec3b>(y, x)[0] = 0;
                            if (lines.Count > 0)
                            {
                                int maxlength = (int)((lines.Max(t => t.Item2) + lines.Average(t => t.Item2)) / 2);
                                if (maxlength < 5)
                                    maxlength = 5;
                                if (maxlength > 20)
                                    maxlength = 20;
                                foreach (var line in lines)
                                {
                                    if (line.Item2 > maxlength)
                                        continue;
                                    bool whiteFlag = false;
                                    for (int x1 = line.Item1; x1 < line.Item1 + line.Item2; x1++)
                                    {
                                        for (int c = 0; c < 3; c++)
                                            if (rowsCols == 0)
                                                new_mat.At<Vec3b>(y, x1)[c] = 255;
                                            else if (whiteFlag)
                                            {
                                                new_mat.At<Vec3b>(x1, y)[c] = 255;
                                            }
                                            else if (new_mat.At<Vec3b>(x, y)[0] > 0)
                                                whiteFlag = true;

                                    }
                                }
                            }
                        }
                }
            return new_mat;
        }
        static public Mat imageSimplification(Mat new_mat)
        {
            new_mat = Resize(new_mat);
            //new_mat = HideColor(new_mat);
            //new_mat = ToGray(new_mat);
            int skyHeight;
            new_mat = DeleteSky(new_mat, out skyHeight, 100,4);
            new_mat = Blur(new_mat);
            new_mat = new_mat.BilateralFilter(75, 75, 75);
            new_mat = ContrastBrightness(new_mat, GetBestAlpha(new_mat,skyHeight), GetBestBeta(new_mat, skyHeight), skyHeight);
            //new_mat = new_mat.CvtColor(ColorConversionCodes.GRAY2RGB);
            //new_mat = PickOutLines(new_mat);
            //new_mat = OnlyWhite(new_mat, 50);
            //new_mat = new_mat.Canny(40, 160);
            //new_mat = DellNoise(new_mat, 15, 2, 16);
            new_mat = ToBinary(new_mat);
            return new_mat;
        }

        public static Mat GetBlackPicture(Mat mat, byte b = 0)
        {
            Mat new_mat = mat.Clone();
            for (int i = 0; i < mat.Rows; i++)
            {
                for (int j = 0; j < mat.Cols; j++)
                {
                    for (int k = 0; k < 3; k++)
                        if (mat.At<Vec3b>(i, j)[k] - (255 - b) > 0)
                            new_mat.At<Vec3b>(i, j)[k] = (byte)(mat.At<Vec3b>(i, j)[k] - (255 - b));
                        else new_mat.At<Vec3b>(i, j)[k] = 0;
                }
            }
            return new_mat;
        }
        //static Mat HoughLines(Mat start_mat, Mat mat)
        //{
        //    Mat new_mat = mat.Clone();

        //    var lines = Cv2.HoughLinesP(mat, 1, Math.PI / 180, 20, 5,2);
        //    Console.WriteLine(lines.Length);
        //    new_mat = new_mat.CvtColor(ColorConversionCodes.GRAY2RGB);
        //    var rand = new Random();
        //    list_lines = lines.ToList();

        //    //var list_lines = AverageSlopeIntercept(new_mat, lines.ToList(), new List<LineSegmentPoint>());
        //    /*for (int i = 0; i < list_lines.Count; i++)
        //    {
        //        var line = list_lines[i];
        //        int rnd = rand.Next(0, 255);
        //        new_mat.Line(line.P1, line.P2, new Scalar(rnd, rand.Next(0, 255), 255 - rnd), 2);
        //    }*/
        //    return new_mat;
        //}

        //static Random randm = new Random();
        //static List<LineSegmentPoint> AverageSlopeIntercept(Mat mat, List<LineSegmentPoint> lines, List<LineSegmentPoint> lines_out, int error = 40, int i = 0)
        //{
        //    if (i > lines.Count - 1)
        //        return lines;
        //    LineSegmentPoint main_line = lines[i];
        //    var lines_1 = new List<LineSegmentPoint>(lines);
        //    var m_const = GetFunctionConst(main_line);
        //    double
        //        x_min = Min(main_line.P1.X, main_line.P2.X),
        //        x_max = Max(main_line.P1.X, main_line.P2.X),
        //        y_min = Min(main_line.P1.Y, main_line.P2.Y),
        //        y_max = Max(main_line.P1.Y, main_line.P2.Y);

        //    int rnd = randm.Next(0, 255);
        //    int rnd_1 = randm.Next(0, 255);
        //    foreach (var line in lines)
        //    {
        //        if (main_line == line)
        //            continue;
        //        Scalar color = new Scalar(rnd, randm.Next(0, 255), 255 - rnd);
        //        var l_const = GetFunctionConst(line);
        //        double delta_k = Math.Abs(l_const.Item1 - m_const.Item1);
        //        double delta_b = Math.Abs(l_const.Item2 - m_const.Item2);
        //        if (delta_k < 10 && delta_b < 3)
        //        {
        //            //m_const = l_const;
        //            Console.WriteLine($"k{m_const.Item1}\t b{m_const.Item2}    ||   k{l_const.Item1}\t b{l_const.Item2}");
        //            x_min = Min(Min(line.P1.X, line.P2.X), x_min);
        //            x_max = Max(Max(line.P1.X, line.P2.X), x_max);
        //            y_min = Min(Min(line.P1.Y, line.P2.Y), y_min);
        //            y_max = Max(Max(line.P1.Y, line.P2.Y), y_max);
        //            lines_1.Remove(line);
        //            mat.Line(line.P1, line.P2, new Scalar(rnd, rnd_1, 255 - rnd), 2);
        //            mat.Line(line.P1, line.P2, 2);
        //        }
        //    }
        //    lines_out.Add(new LineSegmentPoint(new OpenCvSharp.Point(x_min, y_min), new OpenCvSharp.Point(x_max, y_max)));
        //    return AverageSlopeIntercept(mat, lines_1, lines_out, error, i + 1);
        //}
        //static double Max(double a, double b)
        //{
        //    if (a > b)
        //        return a;
        //    else
        //        return b;
        //}
        //static double Min(double a, double b)
        //{
        //    if (a < b)
        //        return a;
        //    else
        //        return b;
        //}
        //public static (double, double) GetFunctionConst(LineSegmentPoint line) 
        //{
        //    double k = 0;
        //    if ((line.P2.X - line.P1.X) != 0) 
        //        k = (line.P2.Y - line.P1.Y)/(double)(line.P2.X - line.P1.X);
        //    double b = (-line.P1.X * k) + line.P1.Y;
        //    return (k, b);
        //}
        //static void DeleteLines(double k = 20)
        //{
        //    List<LineSegmentPoint> new_list_lines = new List<LineSegmentPoint>();
        //    foreach (var line in list_lines)
        //    {
        //        var parameters = GetFunctionConst(line);
        //        bool flag = false;
        //        foreach (var line2 in list_lines)
        //        {
        //            var parameters2 = GetFunctionConst(line);
        //            double a = (Math.Abs(parameters2.Item1 - parameters.Item1)), b = (Math.Abs(parameters2.Item2 - parameters.Item2)), 
        //                a1 = (parameters2.Item1 + parameters.Item1) / k, b1 = a1 = (parameters2.Item1 + parameters.Item1) / k;
        //            if ((Math.Abs(parameters2.Item1 - parameters.Item1) > (parameters2.Item1 + parameters.Item1) / k)/* && (Math.Abs(parameters2.Item2 - parameters.Item2) < (parameters2.Item2 + parameters.Item2) / k)*/)
        //                flag = true;
        //        }
        //        if (flag)
        //            new_list_lines.Add(line);
        //    }
        //    list_lines = new_list_lines;
        //}
        //    static public /*(LineSegmentPoint, LineSegmentPoint)*/ Mat GetRoadBorders(Mat mat)
        //{
        //    LineSegmentPoint left_line = new LineSegmentPoint(), right_line = new LineSegmentPoint() ;
        //    var new_mat = mat.Clone();
        //    List<(double, double)> left_borders = new List<(double, double)>();
        //    List<(double, double)> right_borders = new List<(double, double)>();

        //    List<(double, double, int)> constants = new List<(double, double, int)>();

        //    if (list_lines.Count != 0)
        //    {
        //        foreach (var line in list_lines)
        //        {
        //            var parameters = GetFunctionConst(line);

        //            bool flag = false;
        //            int j = 0;
        //            foreach (var parameter in constants)
        //            {
        //                if (parameter.Item2 > 373 && parameter.Item2 < 380 && parameters.Item2 > 373 && parameters.Item2 < 380)
        //                {
        //                    double a = (Math.Abs(parameter.Item1 - parameters.Item1)), b = (Math.Abs(parameter.Item2 - parameters.Item2)),
        //                        a1 = (parameter.Item1 + parameters.Item1) / 30, b1 = (parameter.Item1 + parameters.Item1) / 30.0f;
        //                }
        //                if ((Math.Abs(parameter.Item1 - parameters.Item1) < (parameter.Item1 + parameters.Item1) / 30) && (Math.Abs(parameter.Item2 - parameters.Item2) < (parameter.Item2 + parameters.Item2) / 30))
        //                {
        //                    flag = true;
        //                    break;
        //                }
        //                j++;
        //            }
        //            int lang = (int)Math.Sqrt(Math.Pow(line.P1.X + line.P2.X, 2) + Math.Pow(line.P1.Y + line.P2.Y, 2));
        //            if (flag)
        //            {
        //                constants[j] = (constants[j].Item1, constants[j].Item2, constants[j].Item3 + lang);
        //            }
        //            else
        //            {
        //                constants.Add((parameters.Item1, parameters.Item2, lang));
        //            }
        //        }
        //        int max_1 = 0, max_2, i_max_1 = 0, i_max_2 = 0;
        //        int i = 0;
        //        foreach (var parameter in constants)
        //        {
        //            if (parameter.Item3 > max_1)
        //            {
        //                max_2 = max_1;
        //                i_max_2 = i_max_1;
        //                max_1 = parameter.Item3;
        //                i_max_1 = i;
        //            }
        //            i++;
        //            if (parameter.Item3 > 1300)
        //            {
        //                var line = GetLineFromConst((constants[i_max_1].Item1, constants[i_max_1].Item2), 680);
        //                new_mat.Line(line.P1, line.P2, new Scalar(255, 0, 0), 2);
        //            }
        //        }

        //        left_line = GetLineFromConst((constants[i_max_1].Item1, constants[i_max_1].Item2), 680);

        //        right_line = GetLineFromConst((constants[i_max_2].Item1, constants[i_max_2].Item2), 680);
        //    }
        //    //            return (left_line, right_line);
        //    return new_mat;
        //}
        //static LineSegmentPoint GetLineFromConst((double, double) constant, int max_x)
        //{
        //    double
        //        y1 = constant.Item1 * 0 + constant.Item2,
        //        y2 = constant.Item1 * max_x + constant.Item2; // y = k*x + b
        //        return new LineSegmentPoint(
        //            new OpenCvSharp.Point(
        //                    0,
        //                    y1
        //                ),
        //            new OpenCvSharp.Point(
        //                    max_x,
        //                    y2
        //                )
        //            );
        //}

        //static (double, double) AverageConst(List<(double,double)> constants)
        //{

        //    double sum_k = 0, sum_b = 0;
        //    foreach (var constant in constants)
        //    {
        //        sum_k += constant.Item1;
        //        sum_b += constant.Item2;
        //    }
        //    return (sum_k / constants.Count, sum_b / constants.Count);
        //}
    }
}
