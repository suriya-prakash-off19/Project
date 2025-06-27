using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
namespace ObjectRemoverProject
{
    public class GraphicsState
    {
        public float[] CTM = new float[] { 1, 0, 0, 1, 0, 0 }; // Identity

        public GraphicsState Clone()
        {
            return new GraphicsState
            {
                CTM = (float[])CTM.Clone()
            };
        }

        public PointF Transform(PointF point)
        {
            float x = point.X * CTM[0] + point.Y * CTM[2] + CTM[4];
            float y = point.X * CTM[1] + point.Y * CTM[3] + CTM[5];
            return new PointF(x, y);
        }
    }

    public class PdfPolygon
    {
        public List<PointF> Points { get; set; } = new List<PointF>();
    }

    public class CalculatePolygen
    {
        /// <summary>
        /// Forms a List of polygen points based on the content stream given
        /// </summary>
        /// <param name="content">Content Stream that used to calculate the polygen points</param>
        /// <param name="objectIndex">List of start and end of the objects as per polygen points</param>
        /// <param name="pdfPage">PdfPage where the content streams contained</param>
        /// <param name="bezierSegments">Number of points to be generated for a curve drawn in the ContentStream</param>
        /// <returns>Collection of PdfPolygen calculated from the given content stream</returns>
        public static List<PdfPolygon> ParseContentStream(string content, out List<(int,int)> objectIndex, iText.Kernel.Pdf.PdfPage pdfPage, int bezierSegments = 50)
        {
            #region Remove using split
            var polygons = new List<PdfPolygon>();
            var currentPath = new List<PointF>();

            var stateStack = new Stack<GraphicsState>();
            var state = new GraphicsState();
            int index = 0;
            List<int> ObjectIndex = new List<int>();
            ObjectIndex.Add(index);
            var lines = content.Split(new[] { '\n' });
            try
            {
                foreach (var line in lines)
                {

                    var tokens = line.Trim().Split(new[] { ' ' });
                    if (tokens.Length == 0)
                        continue;

                    var args = new List<string>();
                    foreach (string cmd in tokens)
                    {
                        if (IsTag(cmd))
                        {
                            switch (cmd)
                            {
                                case "q":
                                    stateStack.Push(state.Clone());
                                    break;
                                case "Q":
                                    state = stateStack.Count > 0 ? stateStack.Pop() : state;
                                    break;
                                case "cm":
                                    if (args.Count >= 6)
                                    {
                                        List<float> m = new List<float>();

                                        foreach (var x in args)
                                        {
                                            if (float.TryParse(x, out float val))
                                            {
                                                m.Add(val);
                                            }
                                        }
                                        state.CTM = MultiplyMatrices(state.CTM, m.ToArray());
                                    }
                                    break;
                                case "m":
                                    if (args.Count >= 2)
                                    {
                                        if (currentPath.Count > 0)
                                        {
                                            polygons.Add(new PdfPolygon { Points = new List<PointF>(currentPath) });
                                            currentPath.Clear();
                                            ObjectIndex.Add(index);
                                        }
                                        float x = float.Parse(args[0], CultureInfo.InvariantCulture);
                                        float y = float.Parse(args[1], CultureInfo.InvariantCulture);
                                        currentPath.Add(state.Transform(new PointF(x, y)));
                                    }
                                    break;
                                case "l":
                                    if (args.Count >= 2)
                                    {
                                        float x = float.Parse(args[0], CultureInfo.InvariantCulture);
                                        float y = float.Parse(args[1], CultureInfo.InvariantCulture);
                                        currentPath.Add(state.Transform(new PointF(x, y)));
                                    }
                                    break;
                                case "c":
                                    if (args.Count >= 6)
                                    {
                                        float x1 = float.Parse(args[0], CultureInfo.InvariantCulture);
                                        float y1 = float.Parse(args[1], CultureInfo.InvariantCulture);
                                        float x2 = float.Parse(args[2], CultureInfo.InvariantCulture);
                                        float y2 = float.Parse(args[3], CultureInfo.InvariantCulture);
                                        float x3 = float.Parse(args[4], CultureInfo.InvariantCulture);
                                        float y3 = float.Parse(args[5], CultureInfo.InvariantCulture);

                                        PointF p0 = currentPath[currentPath.Count - 1];
                                        PointF p1 = state.Transform(new PointF(x1, y1));
                                        PointF p2 = state.Transform(new PointF(x2, y2));
                                        PointF p3 = state.Transform(new PointF(x3, y3));

                                        for (int i = 1; i <= bezierSegments; i++)
                                        {
                                            float t = i / (float)bezierSegments;
                                            float x = (float)(
                                                Math.Pow(1 - t, 3) * p0.X +
                                                3 * Math.Pow(1 - t, 2) * t * p1.X +
                                                3 * (1 - t) * t * t * p2.X +
                                                t * t * t * p3.X);
                                            float y = (float)(
                                                Math.Pow(1 - t, 3) * p0.Y +
                                                3 * Math.Pow(1 - t, 2) * t * p1.Y +
                                                3 * (1 - t) * t * t * p2.Y +
                                                t * t * t * p3.Y);
                                            currentPath.Add(new PointF(x, y));
                                        }
                                    }
                                    break;
                                case "y":
                                    if (args.Count >= 4)
                                    {
                                        float x1 = float.Parse(args[0], CultureInfo.InvariantCulture);
                                        float y1 = float.Parse(args[1], CultureInfo.InvariantCulture);
                                        float x3 = float.Parse(args[2], CultureInfo.InvariantCulture);
                                        float y3 = float.Parse(args[3], CultureInfo.InvariantCulture);

                                        PointF p0 = currentPath[currentPath.Count - 1];
                                        PointF p1 = state.Transform(new PointF(x1, y1)); 
                                        PointF p2 = state.Transform(new PointF(x3, y3)); 
                                        PointF p3 = p2;

                                        for (int i = 1; i <= bezierSegments; i++)
                                        {
                                            float t = i / (float)bezierSegments;
                                            float x = (float)(
                                                Math.Pow(1 - t, 3) * p0.X +
                                                3 * Math.Pow(1 - t, 2) * t * p1.X +
                                                3 * (1 - t) * t * t * p2.X +
                                                t * t * t * p3.X);
                                            float y = (float)(
                                                Math.Pow(1 - t, 3) * p0.Y +
                                                3 * Math.Pow(1 - t, 2) * t * p1.Y +
                                                3 * (1 - t) * t * t * p2.Y +
                                                t * t * t * p3.Y);
                                            currentPath.Add(new PointF(x, y));
                                        }
                                    }
                                    break;

                                case "v":
                                    if (args.Count >= 4)
                                    {
                                        float x2 = float.Parse(args[0], CultureInfo.InvariantCulture);
                                        float y2 = float.Parse(args[1], CultureInfo.InvariantCulture);
                                        float x3 = float.Parse(args[2], CultureInfo.InvariantCulture);
                                        float y3 = float.Parse(args[3], CultureInfo.InvariantCulture);

                                        PointF p0 = currentPath[currentPath.Count - 1]; 
                                        PointF p1 = p0; 
                                        PointF p2 = state.Transform(new PointF(x2, y2));
                                        PointF p3 = state.Transform(new PointF(x3, y3)); 

                                        for (int i = 1; i <= bezierSegments; i++)
                                        {
                                            float t = i / (float)bezierSegments;
                                            float x = (float)(
                                                Math.Pow(1 - t, 3) * p0.X +
                                                3 * Math.Pow(1 - t, 2) * t * p1.X +
                                                3 * (1 - t) * t * t * p2.X +
                                                t * t * t * p3.X);
                                            float y = (float)(
                                                Math.Pow(1 - t, 3) * p0.Y +
                                                3 * Math.Pow(1 - t, 2) * t * p1.Y +
                                                3 * (1 - t) * t * t * p2.Y +
                                                t * t * t * p3.Y);
                                            currentPath.Add(new PointF(x, y));
                                        }
                                    }
                                    break;
                                case "re":
                                    if (args.Count >= 4)
                                    {
                                        float x = float.Parse(args[0], CultureInfo.InvariantCulture);
                                        float y = float.Parse(args[1], CultureInfo.InvariantCulture);
                                        float w = float.Parse(args[2], CultureInfo.InvariantCulture);
                                        float h = float.Parse(args[3], CultureInfo.InvariantCulture);

                                        var p1 = state.Transform(new PointF(x, y));
                                        var p2 = state.Transform(new PointF(x + w, y));
                                        var p3 = state.Transform(new PointF(x + w, y + h));
                                        var p4 = state.Transform(new PointF(x, y + h));

                                        currentPath.Add(p1);
                                        currentPath.Add(p2);
                                        currentPath.Add(p3);
                                        currentPath.Add(p4);
                                        currentPath.Add(p1);

                                        polygons.Add(new PdfPolygon { Points = new List<PointF>(currentPath) });
                                        currentPath.Clear();
                                        int indexCpy = index + 1;
                                        while(indexCpy<lines.Length && IsGraphicsTag(lines[indexCpy].Trim()))
                                        {
                                            indexCpy++;
                                        }
                                        ObjectIndex.Add(indexCpy);
                                    }
                                    break;
                                case "s":
                                case "S":
                                case "f":
                                case "f*":
                                case "F":
                                case "F*":
                                case "h":
                                    if (currentPath.Count > 0 && CanAddTag(index,lines))
                                    {
                                        currentPath.Add(currentPath[0]);
                                        polygons.Add(new PdfPolygon { Points = new List<PointF>(currentPath) });
                                        currentPath.Clear();
                                        ObjectIndex.Add(index+1);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            args.Add(cmd);
                        }
                    }
                    index++;
                }
                ObjectIndex.Add(index);
                if (currentPath.Count > 0)
                {
                    polygons.Add(new PdfPolygon { Points = new List<PointF>(currentPath) });
                }
                
            }
            catch (Exception e)
            {
            }
            objectIndex = new List<(int, int)>();
            for(int i=0;i<ObjectIndex.Count-1;i++)
            {
                objectIndex.Add((ObjectIndex[i], ObjectIndex[i + 1]));
            }
            
            return polygons;
            #endregion

        }

        /// <summary>
        /// Checks if the given points is overlap or intersects or near the point given from the polygen points
        /// </summary>
        /// <param name="point">Point that should be checked</param>
        /// <param name="polygon">Polygen points where should be checked</param>
        /// <param name="tolerance">Distance that is acceptable if the point is near the polygen points</param>
        /// <param name="isFill">To check the given point inside the polygen points</param>
        /// <returns>Returns true if the given point is in the polygen points</returns>
        public static bool IsPointInsideOrNearPolygon(PointF point, List<PointF> polygon, float tolerance = 1,bool isFill = true)
        {
            
            if ( isFill && IsPointInPolygon(point, polygon))
                return true;

            foreach (var vertex in polygon)
            {
                if (Distance(point, vertex) <= tolerance)
                    return true;
            }

            for (int i = 0; i < polygon.Count; i++)
            {
                PointF a = polygon[i];
                PointF b = polygon[(i + 1) % polygon.Count];
                if (DistanceToSegment(point, a, b) <= tolerance)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the next graphics content is Curve or not
        /// </summary>
        /// <param name="lines">Content stream that is split by graphics content</param>
        /// <param name="index">Current index to be checked</param>
        /// <returns></returns>
        private static bool IsNextCurve(string[] lines, int index)
        {
            if (index + 1 == lines.Length)
                return false;
            var splitList = lines[index + 1].Split(' ');

            return splitList[splitList.Length - 1].Trim(' ') == "c" || splitList[splitList.Length - 1].Trim(' ') == "C";
        }

        /// <summary>
        /// Checks if the next index is also a Tag
        /// </summary>
        /// <param name="index">Current index</param>
        /// <param name="lines">Collection of string to check</param>
        /// <returns>Return True if the next index is not a tag</returns>
        private static bool CanAddTag(int index, string[] lines)
        {
            if (index + 1 >= lines.Length)
                return true;
            return !IsGraphicsTag(lines[index + 1]); 
        }

        /// <summary>
        /// Check if the given string contain graphics operators
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static bool IsGraphicsTag(string content)
        {
            string[] operators = { "h", "S", "s" , "W", "n","W*","f","F","f*","F*"};
            foreach (var Operator in operators)
            {
                if (ContainsTag(Operator, content))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if the content contains any tags
        /// </summary>
        /// <param name="content">String that used to check</param>
        /// <returns>returns true if it contains any one of the tag</returns>
        private static bool IsTag(string content)
        {
            string[] operators = { "q", "Q", "cm", "m", "l", "c", "h", "S", "s" , "f" ,"f*", "re", "Do", "W","W*", "n","TJ","Tj" , "Tm","n" };
            foreach(var Operator in operators)
            {
                if (ContainsTag(Operator, content))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if the given tag is in the given line
        /// </summary>
        /// <param name="tag">Tag string that should be checked</param>
        /// <param name="line">String that used to check</param>
        /// <returns> return true if it contains the tag</returns>
        private static bool ContainsTag(string tag, string line)
        {
            if (!line.Contains(tag))
                return false;
            int count = 0;
            int index = 0;

            while ((index = line.IndexOf(tag, index, StringComparison.Ordinal)) != -1)
            {
                if (index == 0 || (line[index - 1] == ' ' || line[index - 1] == '\n' || line[index - 1] == '\r' || line[index - 1] == ']' || line[index - 1] == ')' || line[index - 1] == '>'))
                {
                    int tempIndex = index + tag.Length;
                    if (tempIndex == line.Length || line[tempIndex] == ' ' || line[tempIndex] == '\r' || line[tempIndex] == '\n')
                    {
                        return true;
                    }
                }
                index += tag.Length;
                count++;
            }
            return false;
        }

        /// <summary>
        /// Multiply matrics in the form of Array
        /// </summary>
        /// <param name="m1">Array of the Matrix 1</param>
        /// <param name="m2">Array of the Matrix 2</param>
        /// <returns>Multiplied values of the two matrix given</returns>
        private static float[] MultiplyMatrices(float[] m1, float[] m2)
        {
            float a = m1[0] * m2[0] + m1[2] * m2[1];
            float b = m1[1] * m2[0] + m1[3] * m2[1];
            float c = m1[0] * m2[2] + m1[2] * m2[3];
            float d = m1[1] * m2[2] + m1[3] * m2[3];
            float e = m1[0] * m2[4] + m1[2] * m2[5] + m1[4];
            float f = m1[1] * m2[4] + m1[3] * m2[5] + m1[5];
            return new float[] { a, b, c, d, e, f };
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static bool IsPointInPolygon(PointF point, List<PointF> polygon)
        {
            int i, j;
            bool inside = false;
            for (i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) /
                     (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Calculate the distance between two points
        /// </summary>
        /// <param name="p1"> Point 1</param>
        /// <param name="p2"> Point 2</param>
        /// <returns>return the distance between two points</returns>
        private static float Distance(PointF p1, PointF p2)
        {
            float dx = p1.X - p2.X;
            float dy = p1.Y - p2.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float DistanceToSegment(PointF p, PointF a, PointF b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;

            if (dx == 0 && dy == 0)
                return Distance(p, a);

            float t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            PointF projection = new PointF(a.X + t * dx, a.Y + t * dy);
            return Distance(p, projection);
        }

    }
}
