using Gwindalmir.ImageAnalysis;
using Gwindalmir.ImageAnalysis.Algorithms;
using NUnit.Framework;
using System;
using System.IO;

namespace Gwindalmir.ImageAnalyzerTests
{
    public class ErrorLevelAnalysisTests : TestBase
    {
        private void TestProcessImage(string filename)
        {
            var image = new ErrorLevelAnalysis(filename);
            image.AnalyzeImage();
        }

        [TestCase("00001.jpg")]
        public void TestProcessImageReal(string filename)
        {
            TestProcessImage(Path.Combine(BasePath, "test", "real", filename));
        }

        [TestCase(@"00V5CZZSSO.jpg")]
        public void TestProcessImageFake(string filename)
        {
            TestProcessImage(Path.Combine(BasePath, "test", "fake", filename));
        }

        [TestCase(@"G:\My Drive\Capstone\Xoc_Autopilot.jpg")]
        public void TestProcessImageOther(string filename)
        {
            TestProcessImage(filename);
        }
    }
}
