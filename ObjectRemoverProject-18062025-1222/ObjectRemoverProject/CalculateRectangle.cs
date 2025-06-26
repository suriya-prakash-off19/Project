using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ObjectRemoverProject
{
    public class CalculateRectangle
    {
        public class Matrix3x3
        {
            public double A, B, C, D, E, F;

            public Matrix3x3(double a = 1, double b = 0, double c = 0, double d = 1, double e = 0, double f = 0)
            {
                A = a; B = b; C = c; D = d; E = e; F = f;
            }
            public (double x, double y) Transform(double x, double y)
            {
                double nx = A * x + C * y + E;
                double ny = B * x + D * y + F;
                return (nx, ny);
            }
        }

        private class BoundingBox
        {
            public double MinX = double.PositiveInfinity;
            public double MinY = double.PositiveInfinity;
            public double MaxX = double.NegativeInfinity;
            public double MaxY = double.NegativeInfinity;

            public void Update(double x, double y)
            {
                if (x < MinX) MinX = x;
                if (y < MinY) MinY = y;
                if (x > MaxX) MaxX = x;
                if (y > MaxY) MaxY = y;
            }

            public override string ToString()
            {
                if (MinX == double.PositiveInfinity)
                    return "Empty bounding box";

                return $"x={MinX}, y={MinY}, width={MaxX - MinX}, height={MaxY - MinY}";
            }
        }

        public static List<Rectangle> GetRectangle(string contentStream,out int[] indices)
        {
            indices = new int[1];
            if (string.IsNullOrWhiteSpace(contentStream))
                return null;
            List<int> indexes = new List<int>();

            BoundingBox bbox = new BoundingBox();

            Regex tokenRegex = new Regex(@"-?\d*\.?\d+(?:[Ee][+\-]?\d+)?|[a-zA-Z]+");
            
            List<double> operandStack = new List<double>();

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

        public static Rectangle CalculateRectangleFromXObject(PdfPage page, string content)
        {
            PdfDictionary resourcesTemp = page.GetResources().GetResource(PdfName.XObject);

            PdfDictionary objarray = page.GetResources().GetResource(PdfName.XObject) as PdfDictionary;
            if (objarray != null)
            {
                foreach (PdfName key in objarray.KeySet())
                {
                    PdfStream contentStream = objarray.Get(key) as PdfStream;
                    byte[] contentBytes = contentStream.GetBytes();
                    string contentString = Encoding.UTF8.GetString(contentBytes);
                }
            }
            Rectangle rect = null;
            int startIndex = -1;
            if (content.Contains("Fm"))
                startIndex = content.IndexOf("/F");
            else if (content.Contains("Im"))
                startIndex = content.IndexOf("/I");
            int endIndex = startIndex + string.Join("", content.Skip(startIndex)).IndexOf("Do");
            string getExtXobject = "";
            if (startIndex!=-1 && endIndex>startIndex)
            {
                getExtXobject = content.Substring(startIndex+1, endIndex - startIndex - 1).Trim(); 
            }
            
            PdfObject obj = resourcesTemp?.Get(new PdfName(getExtXobject));

            if (obj is PdfStream streamTemp)
            {
                PdfName subtype = streamTemp.GetAsName(PdfName.Subtype);
                if (PdfName.Form.Equals(subtype))
                {
                    var temp1 = streamTemp.GetAsArray(PdfName.BBox)?.ToArray().Select(l => (float)double.Parse(l.ToString())).ToArray();
                    rect = new Rectangle(Math.Min(temp1[0], temp1[2]), Math.Min(temp1[1], temp1[3]), Math.Abs(temp1[0] - temp1[2]), Math.Abs(temp1[1] - temp1[3]));
                }
            }
            return rect;
        }

        public static string GetXobjectName(string v)
        {
            int startIndex = -1;
            if (v.Contains("Fm"))
                startIndex = v.IndexOf("/F");
            else if (v.Contains("Im"))
                startIndex = v.IndexOf("/I");
            int endIndex = startIndex + string.Join("", v.Skip(startIndex)).IndexOf("Do");
            if (startIndex == -1)
                return null;
            return v.Substring(startIndex, endIndex - startIndex - 1).Trim();
        }

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
                        case "h": 
                            {
                               
                                break;
                            }
                        case "q": 
                            {
                                
                                break;
                            }
                        case "Q": 
                            {
                               
                                break;
                            }
                        case "re":
                            break;
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
    }
}
public class VectorObjectExtractor
{
    public List<string> ExtractVectorObjects(string pdfPath)
    {
        List<string> vectorObjects = new List<string>();

        using (PdfReader reader = new PdfReader(pdfPath))
        using (PdfDocument pdfDoc = new PdfDocument(reader))
        {
            // Process each page in the PDF
            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var page = pdfDoc.GetPage(i);
                for(int j=0;j<page.GetContentStreamCount();j++)
                {
                    var contentStream = page.GetContentStream(j);

                    // Read the content stream as a string
                    string content = Encoding.ASCII.GetString(contentStream.GetBytes());

                    // Extract vector graphics commands
                    ExtractGraphicsCommands(content, vectorObjects);
                }
                
            }
        }

        return vectorObjects;
    }

    private void ExtractGraphicsCommands(string content, List<string> vectorObjects)
    {
        // Regex to match vector graphics commands (m, l, c, etc.)
        var tokenRegex = new Regex(@"(-?\d*\.?\d+(?:[Ee][+\-]?\d+)?)|([mlc])");
        var matches = tokenRegex.Matches(content);

        // Store the current path
        string currentPath = string.Empty;

        foreach (Match match in matches)
        {
            if (double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                // If it's a number, add it to the current path
                currentPath += $"{number} ";
            }
            else
            {
                // If it's a command, finalize the current path
                if (currentPath.Length > 0)
                {
                    vectorObjects.Add($"Path: {currentPath.Trim()}");
                    currentPath = string.Empty; // Reset for the next path
                }

                // Add the command to the vector objects
                vectorObjects.Add($"Command: {match.Value}");
            }
        }

        // If there's any remaining path, add it
        if (currentPath.Length > 0)
        {
            vectorObjects.Add($"Path: {currentPath.Trim()}");
        }
    }
}



