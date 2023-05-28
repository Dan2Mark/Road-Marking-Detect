using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Road_Marking_Detect.Model
{
    public class LineBlock
    {
        public char Name;
        public int X0;
        public int Y0;
        public double leftShift;
        public double rightShift;
        public double width;
        public int length;
        public LineBlock(int X0, int Y0, double leftShift, double rightShift, double width, int length, char Name)
        {
            this.X0 = X0;
            this.Y0 = Y0;
            this.leftShift = leftShift;
            this.rightShift = rightShift;
            this.width = width;
            this.length = length;
            this.Name = Name;
        }
        public override string ToString()
        {
            return $"({Name}) StartPoint({X0}:{Y0})\tShift = {leftShift} | {rightShift};\t Width = {width};\t Length = {length}";
        }
        public static List<LineBlock> GetBlocks(int x0, List<int> start, List<int> end, char Name, double maxDelta = 0.1, int minBlockLength = 20)
        {
            List<LineBlock> lineBlocks = new List<LineBlock>();
            LineBlock selectedBlock;
            int
            x_start = x0,
            y_start,
            x = x0,
            x_j = 0;
            double
                leftShift,
                rightShift,
                averageLeftShift = 0,
                averageRightShift = 0,
                comparableLeftShift = 0,
                comparableRightShift = 0,
                averageWidth = 0;
                if (Name == '0')
                    Console.Write("");
            for (int blockNum = 0; x_j < start.Count && x_j < end.Count; blockNum++)
            {
                double
                    prevLeftShift = averageLeftShift,
                    prevRightShift = averageRightShift,
                    prevAverageWidth = averageWidth;
                int start_min = start[0], end_max = end[0];
                bool startFlag = false, endFlag = false, flag = true;
                if (blockNum > 0)
                    blockNum = blockNum;
                int y_i = x_j;
                if (y_i >= start.Count)
                    y_i = start.Count - 1;
                y_start = start[y_i]; //blockNum == 0 ? 0 : (int)Math.Round(lineBlocks.Last().Y0 + (x_j * averageLeftShift));
                int x_i;
                for (x_i = 0; x_j < end.Count && ((Math.Abs(comparableLeftShift - averageLeftShift) < maxDelta && Math.Abs(comparableRightShift - averageRightShift) < maxDelta) || x_i <= minBlockLength); x_i++, x_j++)
                {
                    x = x0 + x_j;
                    //обчисленя точного початку лінії
                    if (x_i > 0)
                    {
                        if (start_min > start[x_j] && !startFlag)
                        {
                            start_min = start[x_j];
                        }
                        else if (!startFlag && flag)
                            startFlag = true;
                        if (end_max < end[x_j] && !endFlag)
                        {
                            end_max = end[x_j];
                        }
                        else if (!endFlag && flag)
                            endFlag = true;

                        if (!(endFlag && startFlag) && (endFlag || startFlag) && flag) //XOR
                        {
                            if (x_i > 1)
                            {
                                averageLeftShift = prevLeftShift;
                                averageRightShift = prevRightShift;
                                averageWidth = prevAverageWidth;
                                y_start = start[x_j];
                                x_start = x;
                                x_i = 0;
                            }
                                flag = false;
                        }
                    }
                    if (x_i == 0)
                        x_start = x;
                    y_start = (int)Math.Round((start[x_j] - (x_i * averageLeftShift) + y_start * x_i) / (x_i + 1));
                    if (x_i <= minBlockLength)
                    {
                        if (x_i > 0)
                            averageWidth = (averageWidth * (x_i - 1) + (end[x_j] - start[x_j])) / x_i;
                    }
                    else
                        Name = Name;
                    if (x_i > 0)
                    {
                        if (x_i == minBlockLength - 1)
                        {
                            comparableLeftShift = averageLeftShift;
                            comparableRightShift = averageRightShift;
                        }
                        leftShift = start[x_j] - start[x_j - 1];
                        averageLeftShift = (averageLeftShift * (x_i - 1) + leftShift) / x_i;
                        rightShift = end[x_j] - end[x_j - 1];
                        averageRightShift = (averageRightShift * (x_i - 1) + rightShift) / x_i;
                        if (Name == 'Q')
                        {
                            //Console.WriteLine($"({start[0]})");
                            for (int i = 0; i < (start[0] < start[start.Count - 1] ? start[x_j] - start[0] : start[x_j] - start[start.Count - 1]); i++)
                                Console.Write(" ");
                            for (int i = 0; i < end[x_j] - start[x_j]; i++)
                            {
                                Console.Write(((char)('0' + blockNum)).ToString());
                            }
                            Console.WriteLine($"\tShift: {leftShift}|{rightShift} / {averageLeftShift}|{averageRightShift} / {comparableLeftShift}|{comparableRightShift}");
                        }
                    }
                }
                LineBlock lastBlock = null;
                bool isAddBlock = true;
                bool isDelBlock = false;
                int blockLength = x_i;
                if (lineBlocks.Count > 0)
                {
                    lastBlock = lineBlocks.Last();
                    if (Math.Abs(averageLeftShift - lastBlock.leftShift) <= 0.5 && Math.Abs(averageRightShift - lastBlock.rightShift) <= 0.5)
                        isAddBlock = true;
                    else if (lastBlock.length < x_i - x_i * 0.4)
                    {
                        isAddBlock = true;
                        isDelBlock = true;
                    }
                    else if (lastBlock.length > x_i + x_i * 0.4)
                    {
                        isAddBlock = false;
                    }
                    else
                        isAddBlock = true;
                    if (isDelBlock)
                    {
                        blockLength += lastBlock.length;
                        x_start = lastBlock.X0;
                        y_start = lastBlock.Y0;
                        lineBlocks.Remove(lastBlock);
                    }
                }
                if (isAddBlock)
                    lineBlocks.Add(new LineBlock(x_start, y_start, averageLeftShift, averageRightShift, averageWidth, blockLength, Name));
                else if (lastBlock != null)
                    lastBlock.length += blockLength;

            }
            if (lineBlocks.Count == 0)
                Console.WriteLine();
            return lineBlocks;
        }
    }

}
