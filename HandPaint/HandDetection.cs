using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Emgu.CV;
using Emgu.CV.Structure;
using Size = System.Drawing.Size;

namespace HandPaint2
{
    public static class HandDetection
    {
        
        public static Bitmap DetectHand(Bitmap Source)
        {
            Image<Bgr, byte> ImageFrame = new Image<Bgr, byte>(Source); 
            Image<Gray, byte> grayFrame = ImageFrame.Convert<Gray, byte>(); 
            CascadeClassifier haar = new CascadeClassifier("C:\\Projekty\\Studia\\PT\\Projekt\\PT_Projekt\\HandPaint\\hand.xml"); 
            var hands = haar.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty); 
            foreach (var hand in hands)
                ImageFrame.Draw(hand, new Bgr(System.Drawing.Color.Green), 3); 
            return ImageFrame.Bitmap; 
        }
    }
}
