using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectRemoverProject
{

    public class CalculatePolygen
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

        public static List<PdfPolygon> ParseContentStream(string content, out List<(int,int)> objectIndex, iText.Kernel.Pdf.PdfPage pdfPage, int bezierSegments = 50)
        {
            #region remove using normal
            //var polygons = new List<PdfPolygon>();
            //var currentPath = new List<PointF>();

            //var stateStack = new Stack<GraphicsState>();
            //var state = new GraphicsState();
            //int index = 0;
            //List<int> ObjectIndex = new List<int>();
            //ObjectIndex.Add(index);
            //var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            //foreach (var line in lines)
            //{
            //    var tokens = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            //    if (tokens.Length == 0)
            //        continue;

            //    var cmd = tokens[tokens.Length - 1];
            //    var args = new string[tokens.Length - 1];
            //    Array.Copy(tokens, 0, args, 0, tokens.Length - 1);
            //    switch (cmd)
            //    {
            //        case "q":
            //            stateStack.Push(state.Clone());
            //            break;
            //        case "Q":
            //            state = stateStack.Count > 0 ? stateStack.Pop() : state;
            //            break;
            //        case "cm":
            //            if (args.Length >= 6)
            //            {
            //                List<float> m = new List<float>();

            //                foreach (var x in args)
            //                {
            //                    if (float.TryParse(x, out float val))
            //                    {
            //                        m.Add(val);
            //                    }
            //                }

            //                //float[] m = Array.ConvertAll(args, a => float.Parse(a, CultureInfo.InvariantCulture));
            //                state.CTM = MultiplyMatrices(state.CTM, m.ToArray());
            //            }
            //            break;
            //        case "m":
            //            if (args.Length >= 2)
            //            {
            //                if (currentPath.Count > 0)
            //                {
            //                    polygons.Add(new PdfPolygon { Points = new List<PointF>(currentPath) });
            //                    currentPath.Clear();
            //                    ObjectIndex.Add(index);
            //                }
            //                float x = float.Parse(args[0], CultureInfo.InvariantCulture);
            //                float y = float.Parse(args[1], CultureInfo.InvariantCulture);
            //                currentPath.Add(state.Transform(new PointF(x, y)));
            //            }
            //            break;
            //        case "l":
            //            if (args.Length >= 2)
            //            {
            //                float x = float.Parse(args[0], CultureInfo.InvariantCulture);
            //                float y = float.Parse(args[1], CultureInfo.InvariantCulture);
            //                currentPath.Add(state.Transform(new PointF(x, y)));
            //            }
            //            break;
            //        case "c":
            //            if (args.Length >= 6)
            //            {
            //                float x1 = float.Parse(args[0], CultureInfo.InvariantCulture);
            //                float y1 = float.Parse(args[1], CultureInfo.InvariantCulture);
            //                float x2 = float.Parse(args[2], CultureInfo.InvariantCulture);
            //                float y2 = float.Parse(args[3], CultureInfo.InvariantCulture);
            //                float x3 = float.Parse(args[4], CultureInfo.InvariantCulture);
            //                float y3 = float.Parse(args[5], CultureInfo.InvariantCulture);

            //                PointF p0 = currentPath[currentPath.Count - 1];
            //                PointF p1 = state.Transform(new PointF(x1, y1));
            //                PointF p2 = state.Transform(new PointF(x2, y2));
            //                PointF p3 = state.Transform(new PointF(x3, y3));

            //                for (int i = 1; i <= bezierSegments; i++)
            //                {
            //                    float t = i / (float)bezierSegments;
            //                    float x = (float)(
            //                        Math.Pow(1 - t, 3) * p0.X +
            //                        3 * Math.Pow(1 - t, 2) * t * p1.X +
            //                        3 * (1 - t) * t * t * p2.X +
            //                        t * t * t * p3.X);
            //                    float y = (float)(
            //                        Math.Pow(1 - t, 3) * p0.Y +
            //                        3 * Math.Pow(1 - t, 2) * t * p1.Y +
            //                        3 * (1 - t) * t * t * p2.Y +
            //                        t * t * t * p3.Y);
            //                    currentPath.Add(new PointF(x, y));
            //                }
            //            }
            //            break;

            //        case "re":
            //            if (args.Length >= 4)
            //            {
            //                float x = float.Parse(args[0], CultureInfo.InvariantCulture);
            //                float y = float.Parse(args[1], CultureInfo.InvariantCulture);
            //                float w = float.Parse(args[2], CultureInfo.InvariantCulture);
            //                float h = float.Parse(args[3], CultureInfo.InvariantCulture);

            //                // Define rectangle corners in order (PDF draws lower-left first)
            //                var p1 = state.Transform(new PointF(x, y));
            //                var p2 = state.Transform(new PointF(x + w, y));
            //                var p3 = state.Transform(new PointF(x + w, y + h));
            //                var p4 = state.Transform(new PointF(x, y + h));

            //                currentPath.Add(p1);
            //                currentPath.Add(p2);
            //                currentPath.Add(p3);
            //                currentPath.Add(p4);
            //                currentPath.Add(p1); // Close rectangle path

            //                polygons.Add(new PdfPolygon { Points = new List<PointF>(currentPath) });
            //                currentPath.Clear();
            //                ObjectIndex.Add(index + 1);
            //            }
            //            break;
            //        case "s":
            //        case "S":
            //        case "f":
            //        case "F":
            //        case "h":
            //            if (currentPath.Count > 0)
            //            {
            //                currentPath.Add(currentPath[0]); // close path
            //                polygons.Add(new PdfPolygon { Points = new List<PointF>(currentPath) });
            //                currentPath.Clear();
            //                ObjectIndex.Add(index + 1);
            //            }
            //            break;
            //        default:
            //            // Ignore other commands for now
            //            break;
            //    }
            //    index++;
            //}
            //ObjectIndex.Add(index);
            //if (currentPath.Count > 0)
            //{
            //    polygons.Add(new PdfPolygon { Points = new List<PointF>(currentPath) });
            //}
            //objectIndex = new List<int>(ObjectIndex); 
            // return polygons;
            #endregion

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
                        if (IsOperator(cmd))
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
                                        while(indexCpy<lines.Length && IsGraphicsOperator(lines[indexCpy].Trim()))
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
                                    if (currentPath.Count > 0 && canAddTag(index,lines))
                                    {
                                        currentPath.Add(currentPath[0]);
                                        polygons.Add(new PdfPolygon { Points = new List<PointF>(currentPath) });
                                        currentPath.Clear();
                                        ObjectIndex.Add(index+1);
                                    }
                                    break;
                                case "Do":
                                    {
                                        var rect = CalculateRectangle.CalculateRectangleFromXObject(pdfPage, content);
                                        float X = rect.GetX();
                                        float Y = rect.GetY();
                                        float W = rect.GetWidth();
                                        float H = rect.GetHeight();

                                        var P1 = state.Transform(new PointF(X, Y));
                                        var P2 = state.Transform(new PointF(X + W, Y));
                                        var P3 = state.Transform(new PointF(X + W, Y + H));
                                        var P4 = state.Transform(new PointF(X, Y + H));

                                        currentPath.Add(P1);
                                        currentPath.Add(P2);
                                        currentPath.Add(P3);
                                        currentPath.Add(P4);
                                        currentPath.Add(P1);

                                        polygons.Add(new PdfPolygon { Points = new List<PointF>(currentPath) });
                                        currentPath.Clear();
                                        ObjectIndex.Add(index+1);
                                        break;
                                    }
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

        private static bool IsNextCurve(string[] lines, int index)
        {
            if (index + 1 == lines.Length)
                return false;
            var splitList = lines[index + 1].Split(' ');

            return splitList[splitList.Length - 1].Trim(' ') == "c" || splitList[splitList.Length - 1].Trim(' ') == "C";
        }

        private static bool canAddTag(int index, string[] lines)
        {
            if (index + 1 >= lines.Length)
                return true;
            return !IsGraphicsOperator(lines[index + 1]); 
        }

        private static bool IsGraphicsOperator(string cmd)
        {
            string[] operators = { "h", "S", "s" , "W", "n","W*"};
            foreach (var Operator in operators)
            {
                if (ContainsTag(Operator, cmd))
                    return true;
            }
            return false;
        }

        private static bool IsOperator(string cmd)
        {
            string[] operators = { "q", "Q", "cm", "m", "l", "c", "h", "S", "s" , "f" ,"f*", "re", "Do", "W","W*", "n","TJ","Tj" , "Tm","n" };
            foreach(var Operator in operators)
            {
                if (ContainsTag(Operator, cmd))
                    return true;
            }
            return false;
        }

        private static bool ContainsTag(string v, string line)
        {
            if (!line.Contains(v))
                return false;
            int index = line.IndexOf(v);
            if (index != 0 && line[index-1] != ' ')
                return false;
            index += v.Length;
            return index == line.Length || line[index] == ' ' || line[index] == '\r' || line[index] == '\n';
        }

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

        public static bool IsPointInsideOrNearPolygon(PointF point, List<PointF> polygon, float tolerance = 1,bool isFill = true)
        {
            
            if (IsPointInPolygon(point, polygon) && isFill)
                return true;

            // Check if point is near any edge or vertex (within tolerance)
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

        public static bool IsPointInPolygon(PointF point, List<PointF> polygon)
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

        public static float Distance(PointF p1, PointF p2)
        {
            float dx = p1.X - p2.X;
            float dy = p1.Y - p2.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static float DistanceToSegment(PointF p, PointF a, PointF b)
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
