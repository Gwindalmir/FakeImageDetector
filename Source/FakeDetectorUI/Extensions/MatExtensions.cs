using Numpy;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gwindalmir.FakeDetectorUI.Extensions
{
    static class MatExtensions
    {
        public static NDarray ToNDarray(this Mat image)
        {
            byte[] imageArray;
            
            using (var newimage = image.Reshape(1))
                newimage.GetArray(out imageArray);

            var npImage = np.array(imageArray, np.uint8).reshape(image.Rows, image.Cols, image.Channels());
            npImage = npImage[":, :, ::-1"].copy();
            return npImage;
        }
    }
}
