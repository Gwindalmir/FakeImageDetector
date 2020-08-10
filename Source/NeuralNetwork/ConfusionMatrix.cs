using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Gwindalmir.NeuralNetwork
{
    /// <summary>
    /// Confusion matrix of a neural network's processed dataset.
    /// </summary>
    public class ConfusionMatrix
    {
        public string[] ClassLabels { get; set; }

        /// <summary>
        /// This is the matrix itself.
        /// </summary>
        /// <remarks>
        /// Matrix is represented like this:
        ///    Class 1  2 .. n
        /// Class 1  x  x .. k
        /// Class 2  x  x .. k
        /// ..
        /// Class N  k  k .. j
        /// </remarks>
        public int[,] ClassCounts { get; }

        public ConfusionMatrix(string [] classLabels)
        {
            ClassLabels = classLabels;
            ClassCounts = new int[ClassLabels.Length, ClassLabels.Length];
        }

        /// <summary>
        /// The overall accuracy of the network.
        /// </summary>
        public float Accuracy
        {
            get
            {
                // Accuracy is the sum of the diagonal / sum of all
                var correct = Enumerable.Range(0, ClassCounts.GetLength(1)).Select(y => ClassCounts[y, y]).Sum();
                var total = ClassCounts.Cast<int>().Sum();

                return (float)correct / total;
            }
        }

        public void AddClass(int classIndex, params int[] data)
        {
            // classIndex is the "actual" value
            foreach(var item in data)
            {
                if (item == classIndex)
                    ClassCounts[classIndex, classIndex]++;
                else
                    ClassCounts[classIndex, item]++;
            }
        }

        internal void AddData(int[] actualClass, int[] predictedClass)
        {
            if (actualClass.Length != predictedClass.Length)
                throw new ArgumentException("actualClass.Length != predictedClass.Length");

            for(var idx = 0; idx < actualClass.Length; idx++)
            {
                ClassCounts[actualClass[idx], predictedClass[idx]]++;
            }
        }

        /// <summary>
        /// Combines the predictions from one <see cref="ConfusionMatrix"/> to the current instance.
        /// </summary>
        /// <param name="other"></param>
        public void AddMatrix(int[,] other)
        {
            if (other.Length != ClassCounts.Length)
                throw new ArithmeticException("Matrix sizes don't match.");

            for (var x = 0; x < ClassCounts.GetLength(0); x++)
            {
                for (var y = 0; y < ClassCounts.GetLength(1); y++)
                {
                    ClassCounts[x, y] += other[x, y];
                }
            }
        }

        /// <summary>
        /// Returns the precision, or positive predictive value, for the specified class.
        /// </summary>
        /// <param name="classIndex"></param>
        /// <returns></returns>
        public float GetPrecision(int classIndex)
        {
            // precision is true positive / true positive + false positive
            var tp = ClassCounts[classIndex, classIndex];
            var fp = Enumerable.Range(0, ClassCounts.GetLength(1)).Select(x => x != classIndex ? ClassCounts[x, classIndex] : 0).Sum();

            return (float)tp / (tp + fp);
        }

        /// <summary>
        /// Returns the recall, or true positive rate, for the specified class.
        /// </summary>
        /// <param name="classIndex"></param>
        /// <returns></returns>
        public float GetRecall(int classIndex)
        {
            // precision is true positive / true positive + false negative
            var tp = ClassCounts[classIndex, classIndex];
            var fn = Enumerable.Range(0, ClassCounts.GetLength(1)).Select(x => x != classIndex ? ClassCounts[classIndex, x] : 0).Sum();

            return (float)tp / (tp + fn);
        }

        /// <summary>
        /// Returns the confusion matrix as a formatted <see cref="DataTable"/>.
        /// </summary>
        /// <returns></returns>
        public DataTable GetDataTable()
        {
            var result = new DataTable("Confusion Matrix");
            GetDataTable(ref result);
            return result;
        }

        /// <summary>
        /// Returns the confusion matrix as a formatted <see cref="DataTable"/>.
        /// </summary>
        /// <param name="result">An existing <see cref="DataTable"/> instance to fill.</param>
        /// <param name="clear">Clear the rows of the existing <see cref="DataSet"/>.</param>
        /// <returns></returns>
        public DataTable GetDataTable(ref DataTable result, bool clear = true)
        {
            if(result == null)
                result = new DataTable("Confusion Matrix");

            if (clear)
                result.Clear();

            if (result.Columns.Count == 0)
            {
                result.Columns.Add("");

                foreach (var label in ClassLabels)
                    result.Columns.Add($"Predicted {label}");

                result.Columns.Add("Recall");
            }

            var rowdata = new List<object>();
            for (var x = 0; x < ClassLabels.Length; x++)
            {
                rowdata.Add($"True {ClassLabels[x]}");
                rowdata.AddRange(Enumerable.Range(0, ClassCounts.GetLength(1))
                                                    .Select(y => ClassCounts[x, y]).Cast<object>());
                rowdata.Add(GetRecall(x));
                result.Rows.Add(rowdata.ToArray());
                rowdata.Clear();
            }
            rowdata.Add("Precision");
            rowdata.AddRange(Enumerable.Range(0, ClassCounts.GetLength(1)).Select(y => GetPrecision(y)).Cast<object>().ToArray());
            rowdata.Add(Accuracy);
            result.Rows.Add(rowdata.ToArray());
            return result;
        }

        public static ConfusionMatrix operator+(ConfusionMatrix a, ConfusionMatrix b)
        {
            var cm = new ConfusionMatrix(a.ClassLabels);
            cm.AddMatrix(a.ClassCounts);
            cm.AddMatrix(b.ClassCounts);
            return cm;
        }
    }
}
