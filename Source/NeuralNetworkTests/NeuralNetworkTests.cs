using Gwindalmir.NeuralNetwork;
using NUnit.Framework;
using System;
using System.IO;

namespace Gwindalmir.NeuralNetworkTests
{
    public class NeuralNetworkTests
    {
        const string NeuralNetworkModelFilename = @"C:\Capstone\publish\vgg16_LG.h5";
        const string TestSourceDirectory = @"C:\Capstone\DataSets\real-vs-fake";
        static readonly Tuple<int, int> ImageSize = Tuple.Create(256, 256);

        NeuralNetworkModel Model = null;
        
        [OneTimeSetUp]
        public void Setup()
        {
            Model = new NeuralNetworkModel(NeuralNetworkModelFilename, ImageSize.Item2, ImageSize.Item1);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            Model.Dispose();
            Model = null;
        }

        //[Test]
        public void Test1()
        {
            var nn = new NeuralNetworkModel(ImageSize.Item1, ImageSize.Item2);
            nn.Train(@"C:\Capstone\DataSets\real-vs-fake\processed_training", @"C:\Capstone\DataSets\real-vs-fake\processed_validation");
        }

        [TestCase(@"processed_training\LG\fake\00AUP94LQS.jpg", "fake")]
        [TestCase(@"processed_test\LG\fake\0QFZBD9CBW.jpg", "fake")]
        [TestCase(@"processed_test\LG\fake\1OXU88LLF6.jpg", "fake")]
        [TestCase(@"processed_training\LG\real\00121.jpg", "real")]
        [TestCase(@"processed_training\LG\real\30450.jpg", "real")]
        [TestCase(@"processed_training\LG\real\57562.jpg", "real")]
        [TestCase(@"processed_test\LG\real\00587.jpg", "real")]
        [TestCase(@"processed_test\LG\real\00222.jpg", "real")]
        [TestCase(@"processed_test\LG\real\52820.jpg", "real")]
        public void TestLG(string filename, string expected)
        {
            var actual = Model.PredictClass(Path.Combine(TestSourceDirectory, filename));
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
