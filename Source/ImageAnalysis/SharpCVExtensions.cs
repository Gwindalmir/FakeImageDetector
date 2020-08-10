//#define SHARPCV
#if SHARPCV
using SharpCV;
using static SharpCV.Binding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gwindalmir.ImageAnalysis
{
    static class SharpCVExtensions
    {
        public static void SaveImage(this Mat image, string filename)
        {
            cv2.imwrite(filename, image);
        }

        public static bool Empty(this Mat image)
        {
            return false;
        }

        public static Mat Reshape(this Mat image, params int[] shape)
        {
            return image.data.reshape(shape);
        }
    }

    static class NativeMethods
    {
        public static void highgui_waitKey(int delay, out int retval)
        {
            retval = 0;
            Binding.cv2.waitKey(delay);
        }
    }
}
#endif
