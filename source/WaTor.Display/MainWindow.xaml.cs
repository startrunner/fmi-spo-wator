using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using WaTor.Simulation;

namespace WaTor.Display
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Border[,] displays;
        private readonly Parameters gameParameters;

        public MainWindow(Parameters gameParameters, SeaBlock[,] theSea)
        {
            this.gameParameters = gameParameters;
            InitializeComponent();

            object previousImage = null;
            new DispatcherTimer(
                gameParameters.ScreenRefreshRate,
                    DispatcherPriority.Background,
                    (sender, e) =>
                    {
                        Int32[,] pixels = new Int32[gameParameters.SeaSizeX, gameParameters.SeaSizeY];
                        for (int x = 0; x < gameParameters.SeaSizeX; x++)
                        {
                            for (int y = 0; y < gameParameters.SeaSizeY; y++)
                            {
                                Color color = Colors.White;
                                var block = theSea[x, y];
                                if (block.Type == OceanBlockType.Fish) color = Colors.Green;
                                if (block.Type == OceanBlockType.Shark) color = Colors.Blue;

                                pixels[x, y] = GetColor(color);
                            }
                        }

                        var image = DrawImage(pixels);

                        var img = image;
                        if (!ReferenceEquals(img, previousImage)) xImage.Source = img;
                    },
                    Dispatcher.CurrentDispatcher
                ).Start();
        }

        private static Int32 GetColor(Color color) => GetColor(color.R, color.G, color.B);
        private static Int32 GetColor(byte r, byte g, byte b)
        {
            return Int32.Parse(Color.FromRgb(r, g, b).ToString().Trim('#'), System.Globalization.NumberStyles.HexNumber);
        }

        private static BitmapSource DrawImage(Int32[,] pixels)
        {
            int resX = pixels.GetUpperBound(0) + 1;
            int resY = pixels.GetUpperBound(1) + 1;

            WriteableBitmap writableImg = new WriteableBitmap(resX, resY, 96, 96, PixelFormats.Bgr32, null);

            //lock the buffer
            writableImg.Lock();

            for (int i = 0; i < resX; i++)
            {
                for (int j = 0; j < resY; j++)
                {
                    IntPtr backbuffer = writableImg.BackBuffer;
                    //the buffer is a monodimensionnal array...
                    backbuffer += j * writableImg.BackBufferStride;
                    backbuffer += i * 4;
                    System.Runtime.InteropServices.Marshal.WriteInt32(backbuffer, pixels[i, j]);
                }
            }

            //specify the area to update
            writableImg.AddDirtyRect(new Int32Rect(0, 0, resX, resY));
            //release the buffer and show the image
            writableImg.Unlock();

            return writableImg;
        }
    }
}
