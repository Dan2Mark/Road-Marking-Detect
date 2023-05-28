using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Word;
using OpenCvSharp;
using Road_Marking_Detect.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Road_Marking_Detect.ModelView
{
    internal class CreateReport
    {
        public static void Create(Bitmap img, MarkingQualityDetection mcd)
        {
            var dataForDiagramm = mcd.getCountPixelsForQuality();
            int a = dataForDiagramm.a,
                b = dataForDiagramm.b,
                c = dataForDiagramm.c,
                d = dataForDiagramm.d;
            Microsoft.Office.Interop.Word._Application oWord = new Microsoft.Office.Interop.Word.Application();
            _Document doc = oWord.Documents.Add();  
            oWord.Visible = true;

            //Назва документу
            Object oMissing = System.Reflection.Missing.Value;
            var par1 = doc.Content.Paragraphs.Add(ref oMissing);
            par1.Range.Font.Size = 16;
            par1.Format.Alignment = WdParagraphAlignment.wdAlignParagraphJustify;
            par1.Range.Text = "ЗВІТ З КОНТРОЛЮ ЯСКРАВОСТІ ДОРОЖНЬОЇ РОЗМІТКИ";
            par1.Range.InsertParagraphAfter();

            
            var par2 = doc.Content.Paragraphs.Add(ref oMissing);
            int procOfWhite = mcd.GetProcentOfWhite();
            par2.Range.Font.Size = 14;
            par2.Range.Text = $"Середня оцінка якості розмітки дорівнює - {procOfWhite}%.\n";

            //зображення
            Clipboard.SetDataObject(mcd.GetBadLines());
            var par3 = doc.Content.Paragraphs.Add(ref oMissing);
            par3.Range.Paste();
            par3.Range.InsertParagraphAfter();

            Microsoft.Office.Interop.Word.Shape chartShape = doc.Shapes.AddChart2(-1, Microsoft.Office.Core.XlChartType.xlPie, 25, 450);

            // діаграмма
            chartShape.Chart.ClearToMatchStyle();   
            var chartData = chartShape.Chart.ChartData;
            chartData.Workbook.Worksheets[1].Cells[2, 1].Value = "0-25%";
            chartData.Workbook.Worksheets[1].Cells[2, 2].Value = a;
            chartData.Workbook.Worksheets[1].Cells[3, 1].Value = "25-50%";
            chartData.Workbook.Worksheets[1].Cells[3, 2].Value = b;
            chartData.Workbook.Worksheets[1].Cells[4, 1].Value = "50-75%";
            chartData.Workbook.Worksheets[1].Cells[4, 2].Value = c;
            chartData.Workbook.Worksheets[1].Cells[5, 1].Value = "75-100%";
            chartData.Workbook.Worksheets[1].Cells[5, 2].Value = d;
            chartShape.Chart.SeriesCollection(1).Points(1).Interior.Color = Color.Red;       // Красный
            chartShape.Chart.SeriesCollection(1).Points(2).Interior.Color = Color.FromArgb(209,130,51);     // Оранжевый
            chartShape.Chart.SeriesCollection(1).Points(3).Interior.Color = Color.FromArgb(171,187,51);     // Лаймовый
            chartShape.Chart.SeriesCollection(1).Points(4).Interior.Color = Color.Green;
            chartShape.Chart.HasTitle = true;
            chartShape.Chart.ChartTitle.Text = "Кількість ліній за яркістю";


            if (a/((b+c+d)/3) > 0.5)
            {
                par3.Range.InsertBreak(WdBreakType.wdPageBreak);
                var par4 = doc.Content.Paragraphs.Add(ref oMissing);
                par4.Range.InsertParagraphAfter();
                par4.Format.Alignment = WdParagraphAlignment.wdAlignParagraphDistribute;
                par4.Range.Text = "Лінії, яким потрібна заміна наведені на наступному зображенні:\n";
                Clipboard.Clear();
                Clipboard.SetDataObject(mcd.GetBadLines(0.25f, img));
                var par5 = doc.Content.Paragraphs.Add(ref oMissing);
                par5.Range.Paste();
                par5.Range.InsertParagraphAfter();
            }
        }
    }
}
