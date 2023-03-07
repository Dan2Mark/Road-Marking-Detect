using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Road_Marking_Detect.Model
{
    public class Line
    {
        public List<LineBlock> lineBlocks;
        public string Name;
        public double DottsRelation = 0;
        public int X0 { get { return lineBlocks[0].X0; } }
        public int Y0 { get { return lineBlocks[0].Y0; } }
        public double leftShift { get { return lineBlocks.Sum((x) => x.leftShift) / lineBlocks.Count; } }
        public double rightShift { get { return lineBlocks.Sum((x) => x.rightShift) / lineBlocks.Count; } }
        public double width { get { return lineBlocks.Sum((x) => x.width - Math.Abs(x.rightShift + x.leftShift) / 2) / lineBlocks.Count; } }
        public int length
        {
            get
            {
                if (lineBlocks.Count > 2)
                    Name = Name;
                var lastBlock = lineBlocks.Last();
                return (lastBlock.X0 + lastBlock.length - X0);
            }
        }

        public Line(List<LineBlock> lineBlocks, char name)
        {
            this.lineBlocks = lineBlocks;
            Name = name.ToString();
        }
        public bool IsThisLine(Line line, int min_length = 2)
        {
            if (line.Name == "5" && Name == "6")
                Console.WriteLine();
            var lastBlock = lineBlocks.Last();
            if (Math.Abs(lastBlock.leftShift - line.lineBlocks[0].leftShift) < 3 && line.length >= min_length && length >= min_length)
            {
                int prob_y = Y0 - (int)Math.Round(leftShift * (X0 - line.X0));
                if (Math.Abs(prob_y - line.Y0) < Math.Abs(line.X0 - X0) * 1)
                {
                    int interval = line.X0 - (X0 + length);
                    Name += line.Name;
                    if (interval < 0)
                    {
                        interval = 0;
                    }
                    double newDottsRelation = interval / (double)line.length;
                    if (newDottsRelation >= 3.5 && newDottsRelation <= 0.25 || (DottsRelation > 0 && Math.Abs(DottsRelation - newDottsRelation) > 0.2))
                        return false;
                    DottsRelation = (DottsRelation + newDottsRelation) / 2;
                    List<LineBlock> newLineBlocks = line.lineBlocks;
                    for (int i = 0; i < lineBlocks.Count; i++)
                    {
                        if (line.X0 + line.length <= X0 + length)
                            newLineBlocks.Add(lineBlocks[i]);
                    }
                    lineBlocks = newLineBlocks;
                    return true;
                }
            }
            return false;
        }
        public (int, int) GetConvergecePoint()
        {
            double
                k1 = 1 / leftShift,
                k2 = 1 / rightShift;
            double
                b1 = (-Y0 * k1) + X0,
                b2 = (-(Y0 + width) * k2) + X0;
            int
                x = (int)Math.Round((b2 - b1) / (k1 - k2)),
                y = (int)Math.Round(k1 * x + b1);
            return (x, y);
        }
        public override string ToString()
        {
            var point = GetConvergecePoint();
            return $"({Name}) StartPoint ({X0}:{Y0})\tShift = {leftShift} | {rightShift};\t Width = {width};\t Length = {length/*}; DottsRelation = {DottsRelation*/},\t Count = {lineBlocks.Count},\t Point: ({point.Item1};{point.Item2})";
        }
        static Random random = new Random();
        public static Mat DrawLines(List<Line> lines, Mat mat)
        {
            Mat new_mat = mat.Clone();
            foreach (var line in lines)
            {

                Console.WriteLine(line.ToString());
                byte color = (byte)random.Next(0, 255);
                byte color2 = (byte)random.Next(50, 205);
                byte color3 = (byte)random.Next(0, 255);
                foreach (var blockLine in line.lineBlocks)
                {
                    Console.WriteLine("\t- " + blockLine.ToString());
                    for (int x = blockLine.X0, x_i = 0; x < mat.Rows && x_i < blockLine.length; x++, x_i++)
                    {
                        double rightShift = blockLine.Y0 + Math.Round(blockLine.rightShift * x_i) + blockLine.width;//line.Y0 + line.width + Math.Round(line.rightShift * x_i);
                        for (double y = blockLine.Y0 + Math.Round(blockLine.leftShift * x_i) - 1, y_j = 0; Math.Round(y) < mat.Cols && Math.Round(y) > 0 && y <= Math.Round(rightShift); y++, y_j++)
                        {
                            new_mat.At<Vec3b>(x, (int)Math.Round(y))[0] = color;
                            new_mat.At<Vec3b>(x, (int)Math.Round(y))[1] = 100;
                            new_mat.At<Vec3b>(x, (int)Math.Round(y))[2] = color2;
                        }
                    }
                }
            }
            return new_mat;
        }
        public static List<Line> FindLines(Mat mat, int delta = 3, int min_length = 40, int max_interval = 1)
        {
            List<int> startYList = new List<int>();
            List<int> endYList = new List<int>();
            List<Line> prob_lines = new List<Line>(); //probably lines
            List<Line> lines = new List<Line>();
            int cnt = 0;
            char [,] matrix = new char[mat.Rows, mat.Cols];
            for (int j = 0; j < mat.Rows; j++)
                for (int i = 0; i < mat.Cols; i++)
                    matrix[j, i] = ' ';
            for (int j = 0; j < mat.Rows; j++)
            {

                for (int i = 0; i < mat.Cols; i++)
                {

                    int white_lines_count = 0;
                    int black_lines_count = 0;
                    double average_width = mat.Cols - j;
                    double shift = -5;
                    double average_left_shift = 0; //коефіцієнт сдвигу (наклону) лінії 
                    double average_right_shift = 0;
                    double average_y_start = 0;
                    double lines_count = 0;
                    if (i < 5)
                        shift = -i;
                    byte pixel1 = mat.At<Vec3b>(j, i)[0];
                    if (pixel1 > 0)
                    {
                        int x_j = 0;
                         int y_start_previos = 0, y_end_previos = 0, y_end = 0;

                            if (33 + (cnt % 94) == 'P')
                                cnt = cnt;
                        int y_start = i;
                        for (; x_j + j < mat.Rows && black_lines_count <= max_interval; x_j++)
                        {
                            int y_i = 0;
                            int x = j + x_j;
                            double sum = (x_j < 1 ? shift : average_left_shift) * (x_j < 1 ? 0 : (x_j - 1)) * (average_left_shift < 0 ? 1 : 1);
                            int y_prob = (int)(i + y_i + sum - delta);
                            int y = y_prob >= 0 ? y_prob : 0;
                            bool flag_shift = false;
                            int pixel_count = (int)Math.Round(Math.Abs(average_left_shift) + average_width + delta);
                            int black_pixel_count = 0;
                            bool flagStart = false;
                            for (; y_i < pixel_count + delta + 2; y_i++, y++)
                            {
                                if (y >= mat.Cols)
                                    break;
                                byte pixel = mat.At<Vec3b>(x, y)[0];
                                if (33 + (cnt % 94) == 'E')
                                    matrix[x, y] = pixel > 0 ? (char)(33 + (cnt % 94)) : '-';
                                if (pixel > 0)
                                {
                                    if (!flagStart)
                                    {
                                        y_start = y;
                                        flagStart = true;
                                    }
                                    matrix[x, y] = (char)(33 + (cnt % 94));
                                    flag_shift = true;
                                }
                                mat.At<Vec3b>(x, y)[0] = 0;
                                if (flag_shift)
                                {
                                    if (pixel <= 0)
                                    {
                                        black_pixel_count++;

                                        if (black_pixel_count > 2 && y > y_start + (y_end - y_start) / 2)
                                        {
                                            int prob_y_end_previos = y_end;
                                            y_end = y - black_pixel_count + 1;
                                            int possible_space =  (int)Math.Abs(average_left_shift) + 1;
                                            //Перевірка на дотикання ліній
                                            if (x_j == 0 || (y_start >= y_start_previos - possible_space && y_start <= y_end_previos + possible_space) || (y_end >= y_start_previos - possible_space && y_end <= y_end_previos + possible_space) || (y_start <= y_start_previos + possible_space && y_end >= y_end_previos - possible_space))
                                            {
                                                y_end_previos = x_j > 0 ? prob_y_end_previos : y_end;
                                                break;
                                            }
                                            //інакше
                                            y_end = prob_y_end_previos;
                                            flag_shift = false;
                                        }
                                    }
                                    else
                                        black_pixel_count = 0;
                                }
                            }
                            if (flag_shift)
                            {
                                white_lines_count++;
                                black_lines_count = 0;
                                average_width = (average_width * lines_count + (y_end - y_start + 1)) / (lines_count + 1);
                                average_y_start = (average_y_start * lines_count + (y_start - (average_left_shift * lines_count))) / (lines_count + 1);
                                if (lines_count > 0)
                                {
                                    average_left_shift = (average_left_shift * (lines_count - 1) + (y_start - y_start_previos)) / lines_count;
                                    average_right_shift = (average_right_shift * (lines_count - 1) + (y_end - y_end_previos)) / lines_count;
                                }
                                startYList.Add(y_start);
                                endYList.Add(y_end);

                                y_start_previos = y_start;
                                lines_count++;
                            }
                            else
                            {
                                black_lines_count++;
                                startYList.Add(y_start_previos);
                                endYList.Add(y_end_previos);
                            }
                        }
                        if (black_lines_count > max_interval)
                        {
                            startYList.RemoveRange(startYList.Count - 1, 1);
                            endYList.RemoveRange(endYList.Count - 1, 1);
                        }
                        if (x_j - black_lines_count >= 3)
                        {
                            var lineBlocks = LineBlock.GetBlocks(j, startYList, endYList, (char)(33 + (cnt % 94)), 0.05, 15);
                            if (lineBlocks.Count != 0)
                            {
                                var prob_line = new Line(lineBlocks, (char)(33 + (cnt % 94)));
                                prob_lines.Add(prob_line);
                            }
                            cnt++;
                            if (cnt == 95)
                                cnt++;
                        }
                        startYList.Clear();
                        endYList.Clear();
                    }
                }
            }
            for (int i = prob_lines.Count() - 1; i >= 0; i--)
            {
                if (Math.Abs(prob_lines[i].rightShift - prob_lines[i].leftShift) > 1)
                    continue;
                for (int j = i - 1; j > 0; j--)
                {
                    if (Math.Abs(prob_lines[j].rightShift - prob_lines[j].leftShift) > 1)
                        continue;
                    if (prob_lines[i].IsThisLine(prob_lines[j]))
                    {
                        prob_lines.Remove(prob_lines[j]);
                        j--;
                        i--;
                    }
                }
                if (prob_lines[i].length >= min_length)
                    lines.Add(prob_lines[i]);
            }
            drawMatrix(matrix);
            return lines;
        }

        static void drawMatrix(char[,] matrix)
        {
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                Console.Write($"{i}");
                for (int j = 0; j < matrix.GetLength(1); j++)
                    Console.Write(matrix[i, j]);
                Console.WriteLine();
            }
        }
    }
}