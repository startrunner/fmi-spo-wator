using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaTor.Simulation;

namespace Wator_Display_Forms
{
    public partial class Form1 : Form
    {
        private readonly Parameters gameParameters;

        public Form1(Parameters gameParameters, SeaBlock[,] theSea)
        {
            this.gameParameters = gameParameters;
            InitializeComponent();

            object previousImage = null;
            var timer = new Timer();

            var image = new Bitmap(gameParameters.SeaSizeX, gameParameters.SeaSizeY);
            xImage.Image = image;

            List<(int x, int y)> coordinates = new();
            for (int x = 0; x < gameParameters.SeaSizeX; x++)
                for (int y = 0; y < gameParameters.SeaSizeY; y++)
                    coordinates.Add((x, y));

            coordinates =
                coordinates
                .Select(x => (x, comparand: Guid.NewGuid()))
                .OrderBy(x => x.comparand)
                .Select(x => x.x)
                .ToList();

            timer.Tick += (sender, e) => {
                xImage.Refresh();

                foreach ((int x, int y) in coordinates)
                {

                    Color color = Color.White;
                    var block = theSea[x, y];
                    if (block.Type == OceanBlockType.Fish) color = Color.Green;
                    if (block.Type == OceanBlockType.Shark) color = Color.Blue;

                    //pixels[x, y] = GetColor(color);
                    image.SetPixel(x, y, color);
                }



                //Int32[,] pixels = new Int32[gameParameters.SeaSizeX, gameParameters.SeaSizeY];
                //for (int x = 0; x < gameParameters.SeaSizeX; x++)
                //{
                //    for (int y = 0; y < gameParameters.SeaSizeY; y++)
                //    {
                //        Color color = Color.White;
                //        var block = theSea[x, y];
                //        if (block.Type == OceanBlockType.Fish) color = Color.Green;
                //        if (block.Type == OceanBlockType.Shark) color = Color.Blue;

                //        pixels[x, y] = GetColor(color);
                //    }
                //}

                //var image = DrawImage(pixels);

                //var img = image;
                //if (!ReferenceEquals(img, previousImage)) xImage.Image = img;
            };

            timer.Start();
        }

        private static Int32 GetColor(Color color) => GetColor(color.R, color.G, color.B);
        private static Int32 GetColor(byte r, byte g, byte b)
        {
            return Int32.Parse(Color.FromArgb(r, g, b).ToString().Trim('#'), System.Globalization.NumberStyles.HexNumber);
        }

        //private static Bitmap DrawImage(Int32[,] pixels)
        //{
        //    int resX = pixels.GetUpperBound(0) + 1;
        //    int resY = pixels.GetUpperBound(1) + 1;

        //    WriteableBitmap writableImg = new WriteableBitmap(resX, resY, 96, 96, PixelFormats.Bgr32, null);

        //    //lock the buffer
        //    writableImg.Lock();

        //    for (int i = 0; i < resX; i++)
        //    {
        //        for (int j = 0; j < resY; j++)
        //        {
        //            IntPtr backbuffer = writableImg.BackBuffer;
        //            //the buffer is a monodimensionnal array...
        //            backbuffer += j * writableImg.BackBufferStride;
        //            backbuffer += i * 4;
        //            System.Runtime.InteropServices.Marshal.WriteInt32(backbuffer, pixels[i, j]);
        //        }
        //    }

        //    //specify the area to update
        //    writableImg.AddDirtyRect(new Int32Rect(0, 0, resX, resY));
        //    //release the buffer and show the image
        //    writableImg.Unlock();

        //    return writableImg;
        //}

    }
}
