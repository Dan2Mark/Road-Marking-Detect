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
                        y_last = y + chank_h * 3;
                    }
            }
            for (int y = 0; y < y_last && y < mat.Rows; y++)
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
        static Mat DellNoise(Mat mat, double border_min = 3, int countInLineMax = 3, int chank_w = 40)
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
            for (int y = 0; y < mat.Rows; y++)
            {
                //    byte upperThreshold = 0;
                //    byte lowerThreshold = 255;
                //    int Brightness = 0;
                //    int countBrightness = 0;
                //    List<(int, int)> lines = new List<(int, int)>();
                //    byte[] pixels = new byte[mat.Cols];
                //    int lightPixelCount = 0;

                double averageChangeBright = 0;
                byte pixel = mat.At<Vec3b>(y, 0)[0], lastPixel;
                bool whiteFlag = false;

                for (int x = 0; x < mat.Cols; x++)
                {
                    lastPixel = pixel;
                    pixel = (byte)((mat.At<Vec3b>(y, x)[0] + mat.At<Vec3b>(y, x)[1] + mat.At<Vec3b>(y, x)[2])/3);
                    averageChangeBright = (1 * averageChangeBright + (pixel - lastPixel)) / 2;
                    if (averageChangeBright >= 7)
                        whiteFlag = true;
                    if (averageChangeBright <= -0.5)
                        whiteFlag = false;
                    if (whiteFlag)
                        for (byte c = 0; c < 3; c++)
                        new_mat.At<Vec3b>(y, x)[c] = 255;
                    /*
                    Brightness += pixels[x];
                    countBrightness++;
                    if (pixels[x] > upperThreshold)
                        upperThreshold = pixels[x];
                    if (pixels[x] < lowerThreshold)
                        lowerThreshold = pixels[x];
                    if (pixels[x] >= upperThreshold)
                        lightPixelCount++;*/
                }


                /*
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
                    }*/
            }
            return new_mat;
        }
        static public Mat imageSimplification(Mat new_mat)
        {
            new_mat = Resize(new_mat);       //зміна розміру зображення
            //new_mat = HideColor(new_mat);  //закрашує усе, що не є відтінками сірого - чорним
            //new_mat = ToGray(new_mat);     //перевод зображення до чорно білого
            int skyHeight;      //координата кінця неба
            new_mat = DeleteSky(new_mat, out skyHeight, 100,4); //видалення неба
            new_mat = Blur(new_mat);         //розмивання
            new_mat = new_mat.BilateralFilter(75, 75, 75); //ще одне розмивання
            //new_mat = ContrastBrightness(new_mat, GetBestAlpha(new_mat,skyHeight) * 1.5f, GetBestBeta(new_mat, skyHeight), skyHeight); //зміна контрасту та яскравості
            new_mat = PickOutLines(new_mat); //закраска усього чорним, окрім ліній
            //new_mat = DellNoise(new_mat, 15, 2, 16); //видалення шумів
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
    }
}
