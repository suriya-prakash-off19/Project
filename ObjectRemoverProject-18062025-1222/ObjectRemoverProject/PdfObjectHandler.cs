using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ObjectRemoverProject
{
    public class ObjectData
    {
        public Rectangle Rectangle;

        public GraphicsBlock Block;

        public int From;

        public int ID;

        public int To;
    }

    public class PdfObjectHandler
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputFileBytes">Byte array of the original PDF</param>
        public PdfObjectHandler(byte[] inputFileBytes)
        {
            orgFileBytes = inputFileBytes;
            ReadAndWriteMemory(orgFileBytes);
            ResetAndLoadData();
        }


        private byte[] orgFileBytes;
        
        private Stream tempStream;

        private List<ObjectData> objectDatas;

        private Dictionary<int, ObjectData> objectDataDict;

        private List<GraphicsBlock> globalGraphicsBlock;

        private int ID;


        /// <summary>
        /// Reset the PDF Document to it's original state
        /// </summary>
        public void ResetDocument()
        {
            ReadAndWriteMemory(orgFileBytes);
            ResetAndLoadData();
        }
        
        /// <summary>
        /// Remove the top most object from the PDF 
        /// </summary>
        /// <param name="x">X coordinate based on the PDF Document</param>
        /// <param name="y">Y coordinate based on the PDF Document</param>
        /// <param name="pageNo">Page Number that the coordinates belong to</param>
        public byte[] RemoveObject(float x, float y, int pageNo)
        {
            tempStream.Position = 0;
            byte[] buffer = new byte[tempStream.Length];
            tempStream.Read(buffer, 0, buffer.Length);
            tempStream.Position = 0;

            using (MemoryStream inputStream = new MemoryStream(buffer))
            {
                using(MemoryStream outputStream = new MemoryStream())
                {
                    using (PdfReader reader = new PdfReader(inputStream))
                    using (PdfWriter writer = new PdfWriter(outputStream))
                    using (PdfDocument doc = new PdfDocument(reader, writer))
                    {
                        int pgNumber = pageNo;
                        PdfPage page = doc.GetPage(pgNumber);
                        PdfDictionary pageDict = page.GetPdfObject();
                        PdfObject contentobj = pageDict.Get(PdfName.Contents);
                        string fullContent = GetFullContent(contentobj, out int[] array);
                        #region Using Stored Rectangle
                        Rectangle clickedArea = new Rectangle(x - 2, y - 2, 4, 4);
                        bool isChanged = false;
                        foreach (var objectData in objectDatas)
                        {
                            #region Remove Text
                            var path = string.Join("\n", objectData.Block.GetOwnString());
                            var tempLines = objectData.Block.Lines.ToList();
                            float x1 = float.MaxValue;
                            float y1 = float.MaxValue;
                            PdfFont font = null;
                            float fontSize = 1;
                            float prevX = 0, prevY = 0;
                            if (ContainsTag("BT", path))
                            {
                                bool canRemove = false;
                                Matrix3x3 currentMatrix = new Matrix3x3();
                                for (int i = 0; i < tempLines.Count; i++)
                                {
                                    if (ContainsTag("Tm", tempLines[i]) || ContainsTag("TM", tempLines[i]) || ContainsTag("TD", tempLines[i]) || ContainsTag("Td", tempLines[i]) || ContainsTag("'", tempLines[i]))
                                    {
                                        canRemove = false;
                                        Stack<float> tempList = new Stack<float>();
                                        foreach (var val in tempLines[i].Split(' '))
                                        {
                                            if (float.TryParse(val, out float result))
                                            {
                                                tempList.Push(result);
                                            }
                                            else if (val == "Tm" || val == "TM")
                                            {
                                                float f = tempList.Pop();
                                                float e = tempList.Pop();
                                                float d = tempList.Pop();
                                                float c = tempList.Pop();
                                                float b = tempList.Pop();
                                                float a = tempList.Pop();

                                                currentMatrix = new Matrix3x3(a, b, c, d, e, f);
                                                x1 = e;
                                                y1 = f;
                                            }

                                            else if (val == "Td" || val == "TD")
                                            {
                                                float f = tempList.Pop();
                                                float e = tempList.Pop();
                                                (e, f) = ((float, float))currentMatrix.Transform(e, f);
                                                if (val == "TD")
                                                {
                                                    prevX = e;
                                                    prevY = f;
                                                }
                                                currentMatrix.E = e;
                                                currentMatrix.F = f;
                                                x1 = e;
                                                y1 = f;

                                            }


                                        }

                                    }
                                    else if (ContainsTag("T*", tempLines[i]))
                                    {
                                        float e = prevX;
                                        float f = prevY;
                                        (e, f) = ((float, float))currentMatrix.Transform(e, f);
                                        x1 = e;
                                        y1 = f;
                                    }
                                    else if (ContainsTag("Tf", tempLines[i]))
                                    {
                                        PdfDictionary fonts = page.GetResources().GetResource(PdfName.Font);
                                        var split = tempLines[i].Split(' ');
                                        string fontFamily = split[0].Trim('/');
                                        fontSize = float.Parse(split[1]);
                                        PdfName fontKey = new PdfName(fontFamily);
                                        var fontDict = fonts.GetAsDictionary(fontKey);
                                        if(fontDict != null)
                                        {
                                            font = PdfFontFactory.CreateFont(fontDict);
                                        }
                                    }

                                    else if ((ContainsTag("Tj", tempLines[i]) || ContainsTag("TJ", tempLines[i])))
                                    {
                                        float width = ExtractTextFromTj(tempLines[i]).Length;
                                        float height = 5;
                                        if (font != null)
                                        {
                                            width = font != null ? font.GetWidth(ExtractTextFromTj(tempLines[i]), fontSize) : 30;
                                            var bbox = font.GetFontProgram().GetFontMetrics().GetBbox();
                                            height = (bbox[3] - bbox[1]) * fontSize / 1000f;
                                        }
                                        (float x2, float y2) = ((float, float))currentMatrix.Transform(width, height);
                                        Rectangle tempRect1 = new Rectangle(x - 1, y - 1, 2, 2);
                                        Rectangle tempRect2 = new Rectangle(Math.Min(x1, x2) - 1, Math.Min(y1, y2) - 1, Math.Abs(x2 - x1), Math.Abs(y2 - y1));
                                        canRemove = tempRect1.Overlaps(tempRect2);
                                        if (canRemove)
                                        {
                                            tempLines[i] = "";
                                            isChanged = true;
                                            break;
                                        }
                                    }
                                }
                                objectData.Block.Lines = tempLines;
                            }
                            #endregion

                            if (objectData.Rectangle.Overlaps(clickedArea) && objectData.Block.PageNo == pageNo && !isChanged)
                            {
                                isChanged = RemoveObjectUsingPolygen(objectData, x, y, page);
                            }
                            if (isChanged)
                                break;
                        }
                        #endregion

                        SetFullContent(contentobj, array, pageNo);
                    }

                    buffer = outputStream.ToArray();
                }
            }
            
            ReadAndWriteMemory(buffer);
            ResetAndLoadData();
            return buffer;
        }

        /// <summary>
        /// Create new instances of collections and Load all Object Data
        /// </summary>
        private void ResetAndLoadData()
        {
            objectDatas = new List<ObjectData>();
            globalGraphicsBlock = new List<GraphicsBlock>();
            objectDataDict = new Dictionary<int, ObjectData>();
            GetAllObjectData();
        }

        /// <summary>
        /// Write the temporary PDF with original PDF's Memory 
        /// </summary>
        private void ReadAndWriteMemory(byte[] bufferCpy)
        {
            tempStream = new MemoryStream(bufferCpy);
        }

        /// <summary>
        /// Create Object Data for rectangles from the Content Stream
        /// </summary>
        private void GetAllObjectData()
        {
            ID = 0;
            using (PdfDocument doc = new PdfDocument(new PdfReader(tempStream)))
            {
                for (int pageNo = 1; pageNo <= doc.GetNumberOfPages(); pageNo++)
                {
                    PdfPage page = doc.GetPage(pageNo);
                    PdfDictionary pageDict = page.GetPdfObject();
                    PdfObject contentobj = pageDict.Get(PdfName.Contents);

                    if (contentobj is PdfArray contentArray)
                    {
                        string fullContent = "";
                        for (int i = 0; i < contentArray.Size(); i++)
                        {
                            PdfStream stream = contentArray.GetAsStream(i);
                            fullContent += Encoding.ASCII.GetString(stream.GetBytes());
                        }
                        var temp = GetBlocks(pageNo, fullContent);
                        globalGraphicsBlock.AddRange(new List<GraphicsBlock>(temp));
                        GetAllObjectRectangles(temp, page);
                    }
                    else if (contentobj is PdfStream stream)
                    {
                        string content = Encoding.ASCII.GetString(stream.GetBytes());
                        globalGraphicsBlock.AddRange(GetBlocks(pageNo, content));
                        GetAllObjectRectangles(globalGraphicsBlock, page);
                        var x = string.Join("\n", globalGraphicsBlock.Select(X => X.GetFormattedString()));
                    }
                }
            }
            objectDatas = objectDatas.OrderBy(x => x.Rectangle.GetWidth() * x.Rectangle.GetHeight()).ToList();
        }

        /// <summary>
        /// Creates the collection of GraphicsBlock from the given Content Stream
        /// </summary>
        /// <param name="pageNo">Page Number that the Content Stream belong to</param>
        /// <param name="contentStream">Content Stream that should be processed</param>
        /// <returns>collection of Graphics Block that are present in the Content Stream</returns>
        private List<GraphicsBlock> GetBlocks(int pageNo, string contentStream)
        {
            #region block Separation using q to Q tags
            string[] lines = contentStream.Split('\n');
            List<GraphicsBlock> rootBlocks = new List<GraphicsBlock>();
            Stack<GraphicsBlock> stack = new Stack<GraphicsBlock>();
            List<string> looseblocks = new List<string>();
            int index = 0;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim('\r');

                if (ContainsTag("q", line))
                {
                    GraphicsBlock newBlock = new GraphicsBlock()
                    {
                        Index = index,
                        PageNo = pageNo
                    };
                    newBlock.Lines.Add(line);
                    if (looseblocks.Count > 0)
                    {
                        GraphicsBlock looseBlock = new GraphicsBlock();
                        looseBlock.PageNo = pageNo;
                        looseBlock.Lines = new List<string>(looseblocks);
                        looseblocks.Clear();
                        if (stack.Count > 0)
                        {
                            looseBlock.Parent = stack.Peek();
                            stack.Peek().Children.Add(looseBlock);
                        }
                        else
                        {
                            rootBlocks.Add(looseBlock);
                        }
                    }
                    if (stack.Count > 0)
                    {
                        newBlock.Parent = stack.Peek();
                        stack.Peek().Children.Add(newBlock);
                    }
                    else
                    {
                        rootBlocks.Add(newBlock);
                    }

                    stack.Push(newBlock);
                }
                else if (ContainsTag("Q", line))
                {
                    if (stack.Count > 0)
                    {
                        GraphicsBlock block = stack.Pop();
                        block.Lines.Add(line);
                    }
                    else
                    {
                        looseblocks.Add(line);
                    }
                }
                else
                {
                    if (stack.Count > 0)
                    {
                        stack.Peek().Lines.Add(line);
                    }
                    else
                    {
                        looseblocks.Add(line);
                    }
                }

                index++;
            }

            if (looseblocks.Count > 0)
            {
                GraphicsBlock looseBlock = new GraphicsBlock()
                {
                    PageNo = pageNo,
                    Lines = new List<string>(looseblocks)
                };
                looseblocks.Clear();
                if (stack.Count > 0)
                {
                    stack.Peek().Children.Add(looseBlock);
                }
                else
                {
                    rootBlocks.Add(looseBlock);
                }
            }

            return rootBlocks;
            #endregion

            #region Block seperation using q to Q and h tags
            /*Not working*/
            //string[] lines = contentStream.Split('\n');
            //List<GraphicsBlock> rootBlocks = new List<GraphicsBlock>();
            //Stack<GraphicsBlock> stack = new Stack<GraphicsBlock>();
            //List<string> looseblocks = new List<string>();
            //int index = 0;

            //foreach (string rawLine in lines)
            //{
            //    string line = rawLine.TrimEnd('\r');

            //    if (ContainsTag("q", line))
            //    {
            //        GraphicsBlock newBlock = new GraphicsBlock(true);
            //        newBlock.Index = index;
            //        newBlock.Lines.Add(line);

            //        if (looseblocks.Count > 0)
            //        {
            //            GraphicsBlock looseBlock = new GraphicsBlock(false);
            //            looseBlock.Lines = new List<string>(looseblocks);
            //            looseblocks.Clear();
            //            if (stack.Count > 0)
            //            {
            //                stack.Peek().Children.Add(looseBlock);
            //            }
            //            else
            //            {
            //                rootBlocks.Add(looseBlock);
            //            }
            //        }

            //        if (stack.Count > 0)
            //        {
            //            stack.Peek().Children.Add(newBlock);
            //        }
            //        else
            //        {
            //            rootBlocks.Add(newBlock);
            //        }

            //        stack.Push(newBlock);
            //    }
            //    else if (ContainsTag("Q", line))
            //    {
            //        if (stack.Count > 0)
            //        {
            //            GraphicsBlock block = stack.Pop();
            //            block.Lines.Add(line);
            //        }
            //        else
            //        {
            //            looseblocks.Add(line);
            //        }
            //    }
            //    else if (ContainsTag("h", line))
            //    {
            //        if (stack.Count > 0)
            //        {
            //            GraphicsBlock block = stack.Pop();
            //            block.Lines.Add(line);
            //        }
            //        else
            //        {
            //            looseblocks.Add(line);
            //        }
            //    }
            //    else
            //    {
            //        if (stack.Count > 0)
            //        {
            //            stack.Peek().Lines.Add(line);
            //        }
            //        else
            //        {
            //            looseblocks.Add(line);
            //        }
            //    }

            //    index++;
            //}

            //if (looseblocks.Count > 0)
            //{
            //    GraphicsBlock looseBlock = new GraphicsBlock(false);
            //    looseBlock.Lines = new List<string>(looseblocks);
            //    looseblocks.Clear();
            //    if (stack.Count > 0)
            //    {
            //        stack.Peek().Children.Add(looseBlock);
            //    }
            //    else
            //    {
            //        rootBlocks.Add(looseBlock);
            //    }
            //}

            //return rootBlocks;

            #endregion
        }
        
        /// <summary>
        /// Create ObjectDatas based on the Rectangle created.
        /// </summary>
        /// <param name="tokens">Collection of Graphics Blocks</param>
        /// <param name="page">PdfPage from the iText7</param>
        /// <returns>Collection of Unique ID's of the childern</returns>
        private List<int> GetAllObjectRectangles(List<GraphicsBlock> tokens, PdfPage page)
        {
            List<int> childIndices = new List<int>();
            if (tokens.Count == 0)
                return childIndices;

            foreach (GraphicsBlock token in tokens)
            {
                token.ChildrenIds = (GetAllObjectRectangles(token.Children, page));
                List<string> tempLines = token.Lines.ToList();
                var XobjectName = GetResourceName(token.GetOwnString());
                PdfDictionary objarray = page.GetResources().GetResource(PdfName.XObject) as PdfDictionary;
                if (objarray != null && XobjectName != null)
                {
                    int index = 0;
                    for (; index < tempLines.Count; index++)
                    {
                        if (tempLines[index].Contains(XobjectName))
                        {
                            break;
                        }
                    }
                    foreach (PdfName key in objarray.KeySet())
                    {
                        if (XobjectName != null && key.ToString().Contains(XobjectName))
                        {
                            PdfStream contentStream = objarray.Get(key) as PdfStream;
                            var subType = contentStream.GetAsName(PdfName.Subtype);
                            byte[] contentBytes = contentStream.GetBytes();
                            if (!subType.Equals(PdfName.Image))
                            {
                                tempLines[index] = Encoding.UTF8.GetString(contentBytes);
                            }
                            else
                            {
                                if (token.Parent != null)
                                    tempLines = token.Parent.Lines?.ToList();
                            }
                            break;
                        }

                    }
                }

                string convertorString = string.Join("\n", tempLines);

                var rectangle = CalculateRectangle.CalculateBoundingBoxFromContentStream(convertorString);
                var rectangle3 = CalculateRectangle.GetRectangle(convertorString, out int[] indices);
                var lines = string.Join("\n", token.Lines);
                if (rectangle3 != null && rectangle3.Count != 0)
                {
                    for (int i = 0; i < rectangle3.Count; i++)
                    {
                        var rect = rectangle3[i];
                        if (rect != null)
                        {
                            var obj = new ObjectData()
                            {
                                Rectangle = rect,
                                Block = token,
                                From = indices[i],
                                To = indices[i + 1]
                            };
                            objectDatas.Add(obj);
                            objectDataDict.Add(ID, obj);
                            childIndices.Add(ID);
                            ID++;
                        }
                    }
                }
                if (rectangle != null && rectangle.GetWidth() != double.PositiveInfinity)
                {
                    var obj = new ObjectData()
                    {
                        Rectangle = rectangle,
                        Block = token,
                        From = 0,
                        To = token.Lines.Count,
                        ID = ID
                    };
                    objectDatas.Add(obj);
                    objectDataDict.Add(ID, obj);
                    childIndices.Add(ID);
                    ID++;
                }

            }

            return childIndices;
        }

        /// <summary>
        /// Check if the given line has the tag
        /// </summary>
        /// <param name="tag">Tag that used to check if present</param>
        /// <param name="line">Line that should be checked if the tag present</param>
        /// <returns>Returns true if the tag present, otherwise false</returns>
        private bool ContainsTag(string tag, string line)
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
        /// Get the Name object from the Content Stream given
        /// </summary>
        /// <param name="contentStream">Content Stream to be checked</param>
        /// <returns>return name of the resource if exists otherwise Null</returns>
        private string GetResourceName(string contentStream)
        {
            int startIndex = -1;
            if (contentStream.Contains("Fm"))
                startIndex = contentStream.IndexOf("/F");
            else if (contentStream.Contains("Im"))
                startIndex = contentStream.IndexOf("/I");
            int endIndex = startIndex + string.Join("", contentStream.Skip(startIndex)).IndexOf("Do");
            if (contentStream.Contains("Sh"))
            {
                startIndex = contentStream.IndexOf("/S");
                endIndex = startIndex + string.Join("", contentStream.Skip(startIndex)).IndexOf("sh");
            }
            if (startIndex == -1)
                return null;
            try
            {
                return contentStream.Substring(startIndex, endIndex - startIndex - 1).Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Remove object from the object data based on Polygen Points
        /// </summary>
        /// <param name="objData">Object Data that should be Checked</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="page">PDfPage to get the resources</param>
        /// <returns>return true if any object is removed from the object Data</returns>
        private bool RemoveObjectUsingPolygen(ObjectData objData, float x, float y, PdfPage page)
        {
            GraphicsBlock block = objData.Block;
            var tempLines = block.Lines.ToList();
            bool isChanged = false;
            List<int> objectIndex = new List<int>();
            string requiredString = string.Join("\n", block.Lines.Skip(objData.From).Take(objData.To - objData.From + 1));
            var polygenPoints = CalculatePolygen.ParseContentStream(requiredString, out List<(int, int)> objIndex, page);
            SortPolygenPoints(polygenPoints, objIndex);
            int index = 0;
            bool isfill = ContainsTag("f", requiredString) || ContainsTag("f*", requiredString) || ContainsTag("W", requiredString) || ContainsTag("W*", requiredString);
            foreach (var polygen in polygenPoints)
            {
                string blockData = string.Join("\n", requiredString.Split('\n').Skip(objIndex[index].Item1).Take(objIndex[index].Item2 - objIndex[index].Item1));

                if (CalculatePolygen.IsPointInsideOrNearPolygon(new System.Drawing.PointF(x, y), polygen.Points, isFill: isfill))
                {
                    foreach (int id in objData.Block.ChildrenIds)
                    {
                        ObjectData data = objectDataDict[id];

                        if (RemoveObjectUsingPolygen(data, x, y, page))
                        {
                            isChanged = true;
                            break;
                        }
                    }
                    if (isChanged)
                        break;
                    if (ContainsTag("W n", blockData) || ContainsTag("W*", blockData) || ContainsTag("W", blockData))
                    {
                        bool isChildRemoved = false;
                        if (isChildRemoved)
                            break;
                        foreach (var child in block.Children)
                        {
                            string childXobjectName = GetResourceName(child.GetOwnString())?.Trim('/');
                            var isXobject = page.GetResources().GetResource(PdfName.XObject)?.GetAsStream(new PdfName(childXobjectName == null ? "" : childXobjectName))?.GetAsName(PdfName.Subtype);
                            if (childXobjectName != null && (isXobject == null || !isXobject.Equals(PdfName.Form)))
                            {
                                for (int i = 0; i < child.Lines.Count; i++)
                                {
                                    if (ContainsTag("Do", child.Lines[i]) || ContainsTag("sh", child.Lines[i]))
                                    {
                                        string tempLine = child.Lines[i];
                                        if (ContainsTag("q", tempLine))
                                        {
                                            child.Lines[i] = "q";
                                            isChildRemoved = true;
                                        }
                                        else if (ContainsTag("Q", tempLine))
                                        {
                                            child.Lines[i] = "Q";
                                            isChildRemoved = true;
                                        }
                                        else
                                        {
                                            child.Lines[i] = "";
                                            isChildRemoved = true;
                                        }

                                    }
                                }
                                break;
                            }
                        }
                        if (!isChildRemoved)
                        {
                            for (int i = objData.From + objIndex[index].Item1; i < objData.From + objIndex[index].Item2; i++)
                            {

                                if (IsOnlyObjectMarking(tempLines[i]))
                                    tempLines[i] = "";

                            }
                        }
                        isChanged = true;
                        break;
                    }
                    else
                    {
                        for (int i = objData.From + objIndex[index].Item1; i < objData.From + objIndex[index].Item2; i++)
                        {
                            if (IsOnlyObjectMarking(tempLines[i]))
                                tempLines[i] = "";
                        }
                        isChanged = true;
                        break;
                    }
                }
                index++;
            }
            block.Lines = tempLines;
            return isChanged;
        }

        /// <summary>
        /// Check the line if it contains any graphical based tags
        /// </summary>
        /// <param name="line">Line That should be Checked</param>
        /// <returns>return true if it contains Graphics related Tag</returns>
        private bool IsOnlyObjectMarking(string line)
        {
            List<string> checkList = new List<string>
            {
                "m","l","c","re","Do","v","y","h","H","Sh","sh"
            };
            foreach (var check in checkList)
            {
                if (ContainsTag(check, line))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Sort the polygen points based on the Area created by the PDFPolygen
        /// </summary>
        /// <param name="polygenPoints"></param>
        /// <param name="objectIndex"></param>
        private void SortPolygenPoints(List<PdfPolygon> polygenPoints, List<(int, int)> objectIndex)
        {
            List<dynamic> datas = new List<dynamic>();

            for (int i = 0; i < polygenPoints.Count; i++)
            {
                var poly = polygenPoints[i].Points;
                datas.Add(new
                {
                    polygen = polygenPoints[i],
                    index = objectIndex[i],
                    angle = SignedArea(poly)
                });
            }

            datas = datas.OrderBy(x => x.angle).ToList();

            for (int i = 0; i < polygenPoints.Count; i++)
            {
                polygenPoints[i] = datas[i].polygen;
                objectIndex[i] = datas[i].index;
            }
        }

        /// <summary>
        /// Calculate the Area of the Polygen Points
        /// </summary>
        /// <param name="points">Points that should be calculated</param>
        /// <returns></returns>
        private float SignedArea(List<System.Drawing.PointF> points)
        {
            float area = 0;
            int n = points.Count;
            for (int i = 0; i < n; i++)
            {
                System.Drawing.PointF p1 = points[i];
                System.Drawing.PointF p2 = points[(i + 1) % n]; // wrap around
                area += (p1.X * p2.Y) - (p2.X * p1.Y);
            }
            return Math.Abs(area) / 2.0f;
        }

        /// <summary>
        /// Get the Text that present in the Tj Array
        /// </summary>
        /// <param name="tjArray"></param>
        /// <returns></returns>
        private string ExtractTextFromTj(string tjArray)
        {
            MatchCollection matches = Regex.Matches(tjArray, @"\((.*?(?<!\\))\)|<([0-9A-Fa-f\s]*)>");

            var builder = new StringBuilder();

            foreach (Match match in matches)
            {
                string raw = match.Groups[1].Success
                                ? match.Groups[1].Value
                                : match.Groups[2].Value;
                builder.Append(DecodePdfString(raw));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Extract the original string from the encoded string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string DecodePdfString(string input)
        {
            var output = new StringBuilder();
            for (int i = 0; i < input.Length;)
            {
                if (input[i] == '\\')
                {
                    if (i + 1 >= input.Length)
                        break;

                    char next = input[i + 1];
                    switch (next)
                    {
                        case 'n': output.Append('\n'); i += 2; break;
                        case 'r': output.Append('\r'); i += 2; break;
                        case 't': output.Append('\t'); i += 2; break;
                        case 'b': output.Append('\b'); i += 2; break;
                        case 'f': output.Append('\f'); i += 2; break;
                        case '(': output.Append('('); i += 2; break;
                        case ')': output.Append(')'); i += 2; break;
                        case '\\': output.Append('\\'); i += 2; break;
                        default:
                            if (char.IsDigit(next))
                            {
                                int len = 1;
                                while (len < 3 && i + 1 + len < input.Length && char.IsDigit(input[i + 1 + len]))
                                    len++;

                                string octal = input.Substring(i + 1, len);
                                try
                                {
                                    int val = Convert.ToInt32(octal, 8);
                                    output.Append((char)val);
                                }
                                catch { }

                                i += 1 + len;
                            }
                            else
                            {
                                // Unknown escape — treat as literal
                                output.Append(next);
                                i += 2;
                            }
                            break;
                    }
                }
                else
                {
                    output.Append(input[i]);
                    i++;
                }
            }

            return output.ToString();
        }

        /// <summary>
        /// Set the modified contentto the Pdf Stream
        /// </summary>
        /// <param name="contentobj">Stream(s) that to be Modified</param>
        /// <param name="array">Array of indices where the total content stream should be divided if we have multiple streams</param>
        /// <param name="pageNo">Page Number that the content is modified</param>
        private void SetFullContent(PdfObject contentobj, int[] array, int pageNo)
        {
            var totalContentStream = "";
            var graphicsBlocks = globalGraphicsBlock;
            if (contentobj is PdfArray contentarray)
            {
                PdfStream stream;
                string content;
                foreach (var graphicsBlock in graphicsBlocks)
                {
                    if (graphicsBlock.PageNo == pageNo)
                        totalContentStream += graphicsBlock.GetFormattedString() + "\n";
                }

                var totalContentSplit = totalContentStream.Split('\n').ToList();
                for (int i = 0; i < contentarray.Size(); i++)
                {
                    stream = contentarray.GetAsStream(i);
                    content = string.Join("\n", totalContentSplit.Skip(array[i]).Take(array[i + 1] - array[i])) + "\n";
                    byte[] encodeArray = Encoding.ASCII.GetBytes(content);
                    stream.SetData(encodeArray);
                }
            }
            else if (contentobj is PdfStream stream)
            {
                foreach (var graphicsBlock in graphicsBlocks)
                {
                    if (graphicsBlock.PageNo == pageNo)
                        totalContentStream += graphicsBlock.GetFormattedString() + "\n";
                }
                byte[] encodeArray = Encoding.ASCII.GetBytes(totalContentStream);
                stream.SetData(encodeArray);
            }
        }

        /// <summary>
        /// Get the full Content from the given Pdf Object
        /// </summary>
        /// <param name="contentobj">Stream(s) where we take the contents</param>
        /// <param name="array">Contains of indices of the content that should be split if we have multiple streams</param>
        /// <returns>Content that are present in the given stream(s)</returns>
        private string GetFullContent(PdfObject contentobj, out int[] array)
        {
            array = new int[1];
            if (contentobj is PdfArray contentArray)
            {
                string fullContentStream = "";
                array = new int[contentArray.Size() + 1];
                int prev = 0;

                for (int i = 0; i < contentArray.Size(); i++)
                {
                    PdfStream stream = contentArray.GetAsStream(i);
                    string content = Encoding.ASCII.GetString(stream.GetBytes());
                    fullContentStream += content;
                    array[i] = prev;
                    prev = fullContentStream.Split('\n').Length - 1;
                }
                array[contentArray.Size()] = prev;
                return fullContentStream;
            }
            else if (contentobj is PdfStream stream)
            {
                return Encoding.ASCII.GetString(stream.GetBytes());
            }
            else
            {
                return null;
            }

        }

    }
}
