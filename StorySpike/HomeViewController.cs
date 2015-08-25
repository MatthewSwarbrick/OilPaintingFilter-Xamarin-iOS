using System;
using System.Drawing;
using Foundation;
using UIKit;
using CoreGraphics;
using System.Runtime.InteropServices;
using CoreImage;

namespace OilPaintingApp
{
    public partial class HomeViewController : UIViewController
    {
        static bool UserInterfaceIdiomIsPhone
        {
            get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
        }

        public HomeViewController()
            : base(UserInterfaceIdiomIsPhone ? "HomeViewController_iPhone" : "HomeViewController_iPad", null)
        {
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();
			
            // Release any cached data, images, etc that aren't in use.
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var image = UIImage.FromBundle("Content/input");

            image = image.ResizeImageToBounds();

            var oilPaintingColours = GetOilPaintingColours(image);

            View.AddSubview(new OilPaintingView(oilPaintingColours, image.CGImage.Width, image.CGImage.Height));
        }

        private CGColor[,] GetOilPaintingColours(UIImage image)
        {
            var width = image.CGImage.Width;
            var height = image.CGImage.Height;
            var oilColours = new CGColor[width, height];
            
            var radius = 2;
            var intensity = 15;
            var intensityCount = new long[256];
            var sumR = new float[256];
            var sumG = new float[256];
            var sumB = new float[256];
            
            for( int pixelY = 0; pixelY < height; pixelY++)
            {
                for( int pixelX = 0; pixelX < width; pixelX++)
                {
                    // Find intensities of nearest nRadius pixels in four direction.
                    for( int borderPixelY = pixelY - radius; borderPixelY <= pixelY + radius; borderPixelY++ )
                    {
                        for( int borderPixelX = pixelX - radius; borderPixelX <= pixelX + radius; borderPixelX++ )
                        {
                            if(borderPixelX < 0 || borderPixelY < 0 || borderPixelX >= width || borderPixelY >= height)
                            {
                                continue;  
                            }

                            var pixelColour = image.GetPixelColour(new CGPoint(borderPixelX, borderPixelY));

                            int currentPixelRed = (int)Math.Round(pixelColour.Components[0] * 255);
                            int currentPixelGreen = (int)Math.Round(pixelColour.Components[1] * 255);
                            int currentPixelBlue = (int)Math.Round(pixelColour.Components[2] * 255);

                            // Find intensity of RGB value and apply intensity level.
                            int currentIntensity =  (int)( ( (float)( currentPixelRed + currentPixelGreen + currentPixelBlue ) / 3.0 ) * intensity ) / 255;
                            if( currentIntensity > 255 )
                                currentIntensity = 255;
                            int i = currentIntensity;
                            intensityCount[i]++;

                            sumR[i] = sumR[i] + currentPixelRed;
                            sumG[i] = sumG[i] + currentPixelGreen;
                            sumB[i] = sumB[i] + currentPixelBlue;
                        }
                    }

                    nfloat currentMax = 0;
                    int maxIndex = 0;
                    for( var index = 0; index < 256; index++ )
                    {
                        if( intensityCount[index] > currentMax )
                        {
                            currentMax = intensityCount[index];
                            maxIndex = index;
                        }
                    }

                    oilColours[pixelX, pixelY] = new CGColor((sumR[maxIndex] / currentMax) / 255f, (sumG[maxIndex] / currentMax) / 255f, (sumB[maxIndex] / currentMax) / 255f, 1);

                    Array.Clear(intensityCount, 0, intensityCount.Length);
                    Array.Clear(sumR, 0, sumR.Length);
                    Array.Clear(sumG, 0, sumG.Length);
                    Array.Clear(sumB, 0, sumB.Length);
                }
            }
            return oilColours;
        }
    }

    public static class UIImageExtensions
    {
        public static CGColor GetPixelColour(this UIImage image, CGPoint point)
        {
            var rawData = new byte[4];
            var handle = GCHandle.Alloc(rawData);
            CGColor resultColor = null;
               
            try
            {
                using (var colorSpace = CGColorSpace.CreateDeviceRGB())
                {
                    using (var context = new CGBitmapContext(rawData, 1, 1, 8, 4, colorSpace, CGImageAlphaInfo.PremultipliedLast))
                    {
                        context.DrawImage(new CGRect(-point.X, point.Y - image.Size.Height, image.Size.Width, image.Size.Height), image.CGImage);
                        float red   = (rawData[0]) / 255.0f;
                        float green = (rawData[1]) / 255.0f;
                        float blue  = (rawData[2]) / 255.0f;
                        float alpha = (rawData[3]) / 255.0f;
                        resultColor = new CGColor(red, green, blue, alpha);
                    }
                }
            }
            finally
            {
                handle.Free();
            }

            return resultColor;
        }

        public static UIImage ResizeImageToBounds(this UIImage image)
        {
            nfloat scaledWidth = 0, scaledHeight = 0;

            if (image.CGImage.Width != 0)
            {
                scaledWidth = UIScreen.MainScreen.Bounds.Width / image.CGImage.Width;
            }
            if (image.CGImage.Height != 0)
            {
                scaledHeight = UIScreen.MainScreen.Bounds.Height / image.CGImage.Height;
            }

            var scale = Math.Min(scaledWidth, scaledHeight);

            return image.Scale(new CGSize((float)(image.CGImage.Width * scale), (float)(image.CGImage.Height * scale)));
        }
    }

    public class OilPaintingView : UIView
    {
        public OilPaintingView(CGColor[,] colours, nfloat width, nfloat height)
        {
            DrawImage(colours, width, height);
        }

        public void DrawImage(CGColor[,] imageColours, nfloat imageWidth, nfloat imageHeight)
        {
            UIGraphics.BeginImageContext(new CGSize(imageWidth, imageHeight));
            using(CGContext g = UIGraphics.GetCurrentContext ())
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    for (int y = 0; y < imageHeight; y++)
                    {
                        g.SetFillColor(imageColours[x, y]);
                        g.FillRect(new CGRect(x, y, 1, 1));
                    }
                }

                var result = UIGraphics.GetImageFromCurrentImageContext ();
                var resultImageView = new UIImageView(result)
                {
                    Frame = new CGRect(0, 0, imageWidth, imageHeight)  
                };

                AddSubview(resultImageView);
            }
        }
    }
}