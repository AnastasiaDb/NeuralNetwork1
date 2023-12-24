using AIMLbot.AIMLTagHandlers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetwork1
{
    /// <summary>
    /// Тип фигуры
    /// </summary>
    public enum FigureType : byte { canBleached = 0, canTBleached, machineWashable, dontwash, drying, iron, spinIsProhibited, Undef };
    
    public class GenerateImage
    {

        public SamplesSet learnImages = new SamplesSet();
        public SamplesSet testImages = new SamplesSet();


        /// <summary>
        /// Бинарное представление образа
        /// </summary>
        public bool[,] img = new bool[200, 200];
        
        //  private int margin = 50;
        private Random rand = new Random();
        
        /// <summary>
        /// Текущая сгенерированная фигура
        /// </summary>
        public FigureType current_figure = FigureType.Undef;

        /// <summary>
        /// Количество классов генерируемых фигур (7 - максимум)
        /// </summary>
        public int figure_count { get; set; } = 7;

        /// <summary>
        /// Диапазон смещения центра фигуры (по умолчанию +/- 20 пикселов от центра)
        /// </summary>
        public int figureCenterGitter { get; set; } = 50;

        /// <summary>
        /// Диапазон разброса размера фигур
        /// </summary>
        public int figureSizeGitter { get; set; } = 50;

        /// <summary>
        /// Диапазон разброса размера фигур
        /// </summary>
        public int figureSize { get; set; } = 100;
  
        public GenerateImage()
        {
            LoadImages();
        }

        public void LoadImages()
        {
            loadLearnFig(FigureType.canBleached);
            loadTestFig(FigureType.canBleached);

            loadLearnFig(FigureType.canTBleached);
            loadTestFig(FigureType.canTBleached);

            loadLearnFig(FigureType.machineWashable);
            loadTestFig(FigureType.machineWashable);

            loadLearnFig(FigureType.dontwash);
            loadTestFig(FigureType.dontwash);

            loadLearnFig(FigureType.drying);
            loadTestFig(FigureType.drying);

            loadLearnFig(FigureType.iron);
            loadTestFig(FigureType.iron);

            loadLearnFig(FigureType.spinIsProhibited);
            loadTestFig(FigureType.spinIsProhibited);
        }

        private void loadLearnFig(FigureType figure)
        {
            for (int k = 0; k < 45; k++)
            {
                Image image;
                try
                { image = Image.FromFile("../../Images/" + figure.ToString() + "/image_" + k + ".png"); }
                catch
                { continue; }
                var img = new Bitmap(image);

                var input = getBitMap(img);
                Sample fig = new Sample(input, 7, figure);
                learnImages.AddSample(fig);
            }
        }

        private void loadTestFig(FigureType figure)
        {
            for (int k = 45; k < 70; k++)
            {
                Image image;
                try
                { image = Image.FromFile("../../Images/" + figure.ToString() + "/image_" + k + ".png"); }
                catch
                { continue; }
                var img = new Bitmap(image);

                var input = getBitMap(img);
                Sample fig = new Sample(input, 7, figure);
                testImages.AddSample(fig);
            }
        }

        private double[] getBitMap(Bitmap img)
        {
            double[] input = new double[400];
            for (int i = 0; i < 400; i++)
                input[i] = 0;

            Color prev = img.GetPixel(0, 0);

            for (int i = 0; i < 200; i++)
                for (int j = 0; j < 200; j++)
                {
                    if (img.GetPixel(i, j).R != 255 && img.GetPixel(i, j).G != 255 && img.GetPixel(i, j).B != 255)
                    {
                        if (prev.R == 255 && prev.G == 255 && prev.B == 255)
                        {
                            prev = img.GetPixel(i, j);
                            input[i] += 1;
                            input[200 + j] += 1;
                        }
                    }
                    else if (img.GetPixel(i, j).R == 255 && img.GetPixel(i, j).G == 255 && img.GetPixel(i, j).B == 255)
                    {
                        if (prev.R != 255 && prev.G != 255 && prev.B != 255)
                        {
                            prev = img.GetPixel(i, j);
                            input[i] += 1;
                            input[200 + j] += 1;
                        }
                    }

                }
            return input;
        }
    }

}
