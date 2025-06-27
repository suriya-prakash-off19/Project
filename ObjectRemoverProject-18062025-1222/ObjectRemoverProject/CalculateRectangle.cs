using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ObjectRemoverProject
{

    public class Matrix3x3
    {
        public double A, B, C, D, E, F;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="a">A scale value of the matrix</param>
        /// <param name="b">B scale value of the matrix</param>
        /// <param name="c">C scale value of the matrix</param>
        /// <param name="d">D scale value of the matrix</param>
        /// <param name="e">E scale value of the matrix</param>
        /// <param name="f">F scale value of the matrix</param>
        public Matrix3x3(double a = 1, double b = 0, double c = 0, double d = 1, double e = 0, double f = 0)
        {
            A = a; B = b; C = c; D = d; E = e; F = f;
        }
        /// <summary>
        /// Change the value of the X and Y coordinates based on the matrix
        /// </summary>
        /// <param name="x">X coordinate to be transformed</param>
        /// <param name="y">Y coordinate to be transformed</param>
        /// <returns>Transformed X and Y coordinates based on the matrix</returns>
        public (double x, double y) Transform(double x, double y)
        {
            double nx = A * x + C * y + E;
            double ny = B * x + D * y + F;
            return (nx, ny);
        }
    }

    public class BoundingBox
    {
        public double MinX = double.PositiveInfinity;
        public double MinY = double.PositiveInfinity;
        public double MaxX = double.NegativeInfinity;
        public double MaxY = double.NegativeInfinity;

        /// <summary>
        /// Update the current bounding box's coordinates
        /// </summary>
        /// <param name="x">X coordinate to be checked</param>
        /// <param name="y">Y coordinate to be checked</param>
        public void Update(double x, double y)
        {
            if (x < MinX) MinX = x;
            if (y < MinY) MinY = y;
            if (x > MaxX) MaxX = x;
            if (y > MaxY) MaxY = y;
        }

        /// <summary>
        /// Get the data of the bounding box as a string
        /// </summary>
        /// <returns>string that contains boundingbox's data</returns>
        public override string ToString()
        {
            if (MinX == double.PositiveInfinity)
                return "Empty bounding box";

            return $"x={MinX}, y={MinY}, width={MaxX - MinX}, height={MaxY - MinY}";
        }
    }

    public class CalculateRectangle
    {

        /// <summary>
        /// Forms a rectangle based on the content stream given
        /// </summary>
        /// <param name="contentStream">stream where to calculate the rectangle</param>
        /// <returns>Rectangle that is formed by the content stream given</returns>
        public static Rectangle CalculateBoundingBoxFromContentStream(string contentStream)
        {
            if (string.IsNullOrWhiteSpace(contentStream))
                return null;

            Matrix3x3 currentMatrix = new Matrix3x3();
            BoundingBox bbox = new BoundingBox();

            Regex tokenRegex = new Regex(@"-?\d*\.?\d+(?:[Ee][+\-]?\d+)?|[a-zA-Z]+");

            var matches = tokenRegex.Matches(contentStream);
            List<string> tokens = matches.Cast<Match>().Select(m => m.Value).ToList();

            List<double> operandStack = new List<double>();

            double? currentX = null;
            double? currentY = null;

            double popDouble()
            {
                if (operandStack.Count == 0)
                    throw new Exception("Operand stack underflow");

                double val = operandStack[operandStack.Count - 1];
                operandStack.RemoveAt(operandStack.Count - 1);
                return val;
            }

            Matrix3x3 MultiplyMatrices(Matrix3x3 m1, Matrix3x3 m2)
            {
                return new Matrix3x3(
                    m1.A * m2.A + m1.B * m2.C,
                    m1.A * m2.B + m1.B * m2.D,
                    m1.C * m2.A + m1.D * m2.C,
                    m1.C * m2.B + m1.D * m2.D,
                    m1.E * m2.A + m1.F * m2.C + m2.E,
                    m1.E * m2.B + m1.F * m2.D + m2.F
                );
            }

            bool isTextInside = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
                {
                    operandStack.Add(num);
                }
                else
                {

                    switch (token)
                    {
                        case "Td":
                        case "TD":
                            {
                                double f = popDouble();
                                double e = popDouble();
                                currentMatrix = new Matrix3x3(currentMatrix.A, currentMatrix.B, currentMatrix.C, currentMatrix.D, e, f);
                                break;
                            }
                        case "Tm": 
                            {
                                if (!isTextInside)
                                    break;
                                if (operandStack.Count < 6)
                                {
                                    operandStack.Clear();
                                    break;
                                }
                                double f = operandStack[operandStack.Count - 1];
                                double e = operandStack[operandStack.Count - 2];
                                double d = operandStack[operandStack.Count - 3];
                                double c = operandStack[operandStack.Count - 4];
                                double b = operandStack[operandStack.Count - 5];
                                double a = operandStack[operandStack.Count - 6];
                                operandStack.RemoveRange(operandStack.Count - 6, 6);
                                currentMatrix = new Matrix3x3(a, b, c, d, e, f);
                                break;
                            }
                        case "BT":
                        case "Bt":
                            {
                                isTextInside = true;
                                break;
                            }
                        case "ET":
                        case "Et":
                            {
                                isTextInside = false;
                                break;
                            }
                        case "cm":
                            {
                                if (isTextInside)
                                    break;
                                if (operandStack.Count < 6)
                                {
                                    operandStack.Clear();
                                    break;
                                }

                                double f = popDouble();
                                double e = popDouble();
                                double d = popDouble();
                                double c = popDouble();
                                double b = popDouble();
                                double a = popDouble();

                                Matrix3x3 newMatrix = new Matrix3x3(a, b, c, d, e, f);
                                currentMatrix = MultiplyMatrices(currentMatrix, newMatrix);
                                break;
                            }
                        case "m":
                            {
                                if (isTextInside)
                                    break;
                                if (operandStack.Count < 2)
                                {
                                    operandStack.Clear();
                                    break;
                                }

                                double y = popDouble();
                                double x = popDouble();
                                (double tx, double ty) = currentMatrix.Transform(x, y);
                                currentX = tx;
                                currentY = ty;
                                bbox.Update(tx, ty);
                                break;
                            }
                        case "l":
                            {
                                if (isTextInside)
                                    break;
                                if (operandStack.Count < 2)
                                {
                                    operandStack.Clear();
                                    break;
                                }

                                double y = popDouble();
                                double x = popDouble();
                                (double tx, double ty) = currentMatrix.Transform(x, y);
                                currentX = tx;
                                currentY = ty;
                                bbox.Update(tx, ty);
                                break;
                            }
                        case "c":
                            {
                                if (isTextInside)
                                    break;
                                if (operandStack.Count < 6)
                                {
                                    operandStack.Clear();
                                    break;
                                }

                                double y3 = popDouble();
                                double x3 = popDouble();
                                double y2 = popDouble();
                                double x2 = popDouble();
                                double y1 = popDouble();
                                double x1 = popDouble();

                                (double tx1, double ty1) = currentMatrix.Transform(x1, y1);
                                (double tx2, double ty2) = currentMatrix.Transform(x2, y2);
                                (double tx3, double ty3) = currentMatrix.Transform(x3, y3);

                                bbox.Update(tx1, ty1);
                                bbox.Update(tx2, ty2);
                                bbox.Update(tx3, ty3);

                                currentX = tx3;
                                currentY = ty3;
                                break;
                            }
                        case "TJ":
                        case "Tj":
                            {
                                if (isTextInside)
                                {
                                    double textWidth = 50;
                                    double textHeight = 10;
                                    var (Tx, Ty) = currentMatrix.Transform(0, 0);
                                    bbox.Update(Tx, Ty);
                                    bbox.Update(Tx + textWidth, Ty + textHeight);
                                    operandStack.Clear();
                                }
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                }
            }
            return new Rectangle((float)bbox.MinX, (float)bbox.MinY,Math.Max(1,(float)Math.Abs(bbox.MaxX - bbox.MinX)),Math.Max(1,(float)Math.Abs(bbox.MaxY - bbox.MinY)));
        }

        /// <summary>
        /// Get all the rectangle objects data from the given stream
        /// </summary>
        /// <param name="contentStream">Content stream where the rectangles contained</param>
        /// <param name="indices">Indices where the rectangles contains</param>
        /// <returns></returns>
        public static List<Rectangle> GetRectangle(string contentStream,out int[] indices)
        {
            indices = new int[1];
            if (string.IsNullOrWhiteSpace(contentStream))
                return null;
            List<int> indexes = new List<int>();

            BoundingBox bbox = new BoundingBox();

            Regex tokenRegex = new Regex(@"-?\d*\.?\d+(?:[Ee][+\-]?\d+)?|[a-zA-Z]+");
            
            List<double> operandStack = new List<double>();
            
            double popDouble()
            {
                if (operandStack.Count == 0)
                    throw new Exception("Operand stack underflow");

                double val = operandStack[operandStack.Count - 1];
                operandStack.RemoveAt(operandStack.Count - 1);
                return val;
            }

            List<Rectangle> allRectagles = new List<Rectangle>();

            int index = 0;
            indexes.Add(index);
            Matrix3x3 currentMatrix = new Matrix3x3();
            foreach (var str in contentStream.Split('\n'))
            {
                var matches = tokenRegex.Matches(str);
                List<string> tokens = matches.Cast<Match>().Select(m => m.Value).ToList();
                for (int i = 0; i < tokens.Count; i++)
                {
                    string token = tokens[i];
                    if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
                    {
                        operandStack.Add(num);
                    }
                    else
                    {
                        switch (token)
                        {
                            case "re":
                                {
                                    if (operandStack.Count < 4)
                                        break;
                                    double height = popDouble();
                                    double width = popDouble();
                                    double tempY1 = popDouble();
                                    double tempX1 = popDouble();

                                    (double x1, double y1) = currentMatrix.Transform(tempX1, tempY1);

                                    double y2 = height + y1;
                                    double x2 = width + x1;

                                    Rectangle rectangle = new Rectangle(0, 0, 0, 0);

                                    rectangle.SetX((float)Math.Min(x1, x2));
                                    rectangle.SetY((float)Math.Min(y1, y2));
                                    rectangle.SetHeight(Math.Max(1,(float)Math.Abs(height)));
                                    rectangle.SetWidth(Math.Max(1,(float)Math.Abs(width)));
                                    allRectagles.Add(rectangle);
                                    indexes.Add(index+1);
                                    break;
                                }
                            case "cm":
                                {
                                    if (operandStack.Count < 6)
                                    {
                                        operandStack.Clear();
                                        break;
                                    }

                                    double f = popDouble();
                                    double e = popDouble();
                                    double d = popDouble();
                                    double c = popDouble();
                                    double b = popDouble();
                                    double a = popDouble();

                                    Matrix3x3 newMatrix = new Matrix3x3(a, b, c, d, e, f);
                                    currentMatrix = MultiplyMatrices(currentMatrix, newMatrix);
                                    break;
                                }
                            default:
                                {
                                    operandStack.Clear();
                                    break;
                                }
                        }
                        
                    }
                }
                index++;
            }
            indices = indexes.ToArray();
            return allRectagles;
        }

        /// <summary>
        /// Multiply two matrices
        /// </summary>
        /// <param name="m1">Matrix One that used for multiply</param>
        /// <param name="m2">Matrix Two that used for multiply</param>
        /// <returns></returns>
        private static Matrix3x3 MultiplyMatrices(Matrix3x3 m1, Matrix3x3 m2)
        {
            return new Matrix3x3(
                m1.A * m2.A + m1.B * m2.C,
                m1.A * m2.B + m1.B * m2.D,
                m1.C * m2.A + m1.D * m2.C,
                m1.C * m2.B + m1.D * m2.D,
                m1.E * m2.A + m1.F * m2.C + m2.E,
                m1.E * m2.B + m1.F * m2.D + m2.F
            );
        }

        /// <summary>
        /// Get the resource name from the given content
        /// </summary>
        /// <param name="content">Content that should be checked</param>
        /// <returns>returns the name of the resource if contains or else return null</returns>
        private static string GetResourceName(string content)
        {
            int startIndex = -1;
            if (content.Contains("Fm"))
                startIndex = content.IndexOf("/F");
            else if (content.Contains("Im"))
                startIndex = content.IndexOf("/I");
            int endIndex = startIndex + string.Join("", content.Skip(startIndex)).IndexOf("Do");
            if (startIndex == -1)
                return null;
            return content.Substring(startIndex, endIndex - startIndex - 1).Trim();
        }


    }
}