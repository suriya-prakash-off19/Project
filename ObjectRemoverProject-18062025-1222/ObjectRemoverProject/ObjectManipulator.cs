using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Layer;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Filespec;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser.Filter;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using System.IO;

namespace ObjectRemoverProject
{
    public class ObjectData
    {
        public Rectangle Rectangle;

        public GraphicsBlock Block;

        public int From;

        public int To;
    }
    public class ObjectManipulator : IDisposable
    {
        private string filePath;

        private List<ObjectData> ObjectDatas;

        private List<GraphicsBlock> globalGraphicsBlock;

        public List<Rectangle> SelectedRectangles { get; private set; }

        public List<Dictionary<string, (int start, int end)>> locationSave;

        public ObjectManipulator(string path)
        {
            locationSave = new List<Dictionary<string, (int start, int end)>>();
            filePath = path;
            ObjectDatas = new List<ObjectData>();
            SelectedRectangles = new List<Rectangle>();
            globalGraphicsBlock = new List<GraphicsBlock>();
            GetAllObjectData();
        }

        //Object Removal Form and PDFViewerForm
        private void GetAllObjectData()
        {
            using (PdfDocument doc = new PdfDocument(new PdfReader(filePath)))
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
                        var temp = GetTokens(pageNo, fullContent);
                        globalGraphicsBlock.AddRange(new List<GraphicsBlock>(temp));
                        GetAllObjectRectangles(temp, page);
                    }
                    else if (contentobj is PdfStream stream)
                    {
                        string content = Encoding.ASCII.GetString(stream.GetBytes());
                        globalGraphicsBlock.AddRange(GetTokens(pageNo, content));
                        GetAllObjectRectangles(globalGraphicsBlock, page);
                        var x = string.Join("\n", globalGraphicsBlock.Select(X => X.GetFormattedString()));
                    }
                }
            }
            ObjectDatas = ObjectDatas.OrderBy(x => x.Rectangle.GetWidth() * x.Rectangle.GetHeight()).ToList();
        }

        //Object Removal Form and PDFViewerForm
        private void GetAllObjectRectangles(List<GraphicsBlock> tokens, PdfPage page)
        {
            if (tokens.Count == 0)
                return;

            foreach (GraphicsBlock token in tokens)
            {
                GetAllObjectRectangles(token.Children, page);
                List<string> tempLines = token.Lines.ToList();
                var XobjectName = GetXobjectName(token.GetOwnString());
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
                                break;
                            }
                        }

                    }
                }

                string convertorString = string.Join("\n", tempLines);
                var rectangle = CalculateRectangle.CalculateBoundingBoxFromContentStream(convertorString);
                var rectangle2 = CalculateRectangle.CalculateRectangleFromXObject(page, convertorString);
                var rectangle3 = CalculateRectangle.GetRectangle(convertorString, out int[] indices);

                var lines = string.Join("\n", token.Lines);
                if (rectangle == null || rectangle.GetWidth() == double.PositiveInfinity)
                    rectangle = rectangle2;
                //if (rectangle == null || rectangle.GetWidth() == double.PositiveInfinity)
                //{
                if (rectangle3 != null)
                {
                    for (int i = 0; i < rectangle3.Count; i++)
                    {
                        var rect = rectangle3[i];
                        if (rect != null)
                            ObjectDatas.Add(new ObjectData()
                            {
                                Rectangle = rect,
                                Block = token,
                                From = indices[i],
                                To = indices[i + 1]
                            });
                    }
                }
                //}
                if (rectangle != null && rectangle.GetWidth() != double.PositiveInfinity)
                    ObjectDatas.Add(new ObjectData()
                    {
                        Rectangle = rectangle,
                        Block = token,
                        From = 0,
                        To = token.Lines.Count
                    });

            }
        }

        //Object Removal Form
        public void FetchAllObjects()
        {

            SelectedRectangles.Clear();
            locationSave.Clear();
            PdfDictionary extGState = new PdfDictionary();
            extGState.Put(PdfName.Type, PdfName.ExtGState);
            extGState.Put(PdfName.CA, new PdfNumber(0.0f));
            extGState.Put(PdfName.ca, new PdfNumber(0.0f));
            PdfExtGState pdfExtGState = new PdfExtGState(extGState);

            using (PdfReader reader = new PdfReader(filePath))
            using (PdfDocument doc = new PdfDocument(reader))
            {
                int numPages = doc.GetNumberOfPages();
                int pgNumber = 1;

                PdfPage page = doc.GetPage(pgNumber);
                PdfDictionary pageDict = page.GetPdfObject();
                PdfObject contentobj = pageDict.Get(PdfName.Contents);
                PdfStream stream;
                string content;
                if (contentobj is PdfArray contentarray)
                {
                    string fullContent = "";
                    for (int j = 0; j < contentarray.Size(); j++)
                    {
                        stream = contentarray.GetAsStream(j);
                        content = Encoding.ASCII.GetString(stream.GetBytes());
                        fullContent += content;
                        var tokensBlock = GetTokens(pgNumber, content);

                        foreach (GraphicsBlock block in tokensBlock)
                        {
                            SelectedRectangles.AddRange(block.GetAllRectangles(page));
                        }
                        #region Using Old Token Generator
                        //string[] tokensQ = GetTokens(content, j);
                        //for (int f = 0; f < tokensQ.Length; f++)
                        //{
                        //    string[] tokens = tokensQ[f].Split('\n');
                        //    var aa = CalculateRectangle.GetRectangle(tokensQ[f]);
                        //    Rectangle aa2 = CalculateRectangle.CalculateBoundingBoxFromContentStream(tokensQ[f]);
                        //    if (aa == null)
                        //        aa = aa2;
                        //    if (tokensQ[f].Contains("Fm"))
                        //    {
                        //        PdfDictionary resourcesTemp = page.GetResources().GetResource(PdfName.XObject);
                        //        var getExtXobject = GetXobjectName(tokensQ[f]).Trim('/');
                        //        PdfObject obj = resourcesTemp.Get(new PdfName(getExtXobject));

                        //        if (obj is PdfStream streamTemp)
                        //        {
                        //            PdfName subtype = streamTemp.GetAsName(PdfName.Subtype);
                        //            if (PdfName.Form.Equals(subtype))
                        //            {
                        //                var temp1 = streamTemp.GetAsArray(PdfName.BBox)?.ToArray().Select(l => (float)double.Parse(l.ToString())).ToArray();
                        //                aa2 = new Rectangle(Math.Min(temp1[0], temp1[2]), Math.Min(temp1[1], temp1[3]), Math.Abs(temp1[0] - temp1[2]), Math.Abs(temp1[1] - temp1[3]));
                        //            }
                        //        }

                        //    }
                        //    if (aa != null && aa.GetWidth() != double.PositiveInfinity && IsValidRectangle(aa))
                        //    {
                        //        rectangleStream.Add(aa, tokensQ[f]);
                        //        SelectedRectangles.Add(aa);
                        //        count++;
                        //    }
                        //    if (aa2 != null && aa2.GetWidth() != double.PositiveInfinity && IsValidRectangle(aa2))
                        //    {
                        //        rectangleStream.Add(aa2, tokensQ[f]);
                        //        SelectedRectangles.Add(aa2);
                        //        count++;
                        //    }
                        //    //for (int i = 0; i < tokens.Length; i++)
                        //    //{
                        //    //    if (tokens[i].Contains("gs"))
                        //    //    {

                        //    //        if (aa.GetWidth() != double.PositiveInfinity)
                        //    //        {
                        //    //            SelectedRectangles.Add(aa);

                        //    //        }
                        //    //        if (aa2 != null && aa2.GetWidth() != double.PositiveInfinity)
                        //    //        {
                        //    //            SelectedRectangles.Add(aa2);
                        //    //        }
                        //    //    }
                        //    //}
                        //    tokensQ[f] = string.Join("\n", tokens); 
                        //}
                        #endregion
                        //string outstr = string.Join("\n", tokensQ.Select(p => p).ToArray());
                        //byte[] outbytes = Encoding.ASCII.GetBytes(outstr);
                        //stream.SetData(outbytes);
                    }


                }

                else if (contentobj is PdfStream streamer)
                {
                    content = Encoding.ASCII.GetString(streamer.GetBytes());

                    string[] tokensQ = GetTokens(content, 0);
                    for (int f = 0; f < tokensQ.Length; f++)
                    {
                        string[] tokens = tokensQ[f].Split('\n');
                        //var aa = CalculateRectangle.GetRectangle(tokensQ[f]);
                        Rectangle aa2 = CalculateRectangle.CalculateBoundingBoxFromContentStream(tokensQ[f]);
                        //if (aa2 != null)
                        //    aa = aa2;
                        if (tokensQ[f].Contains("Fm"))
                        {
                            PdfDictionary resourcesTemp = page.GetResources().GetResource(PdfName.XObject);
                            var getExtXobject = GetXobjectName(tokensQ[f]).Trim('/');
                            PdfObject obj = resourcesTemp.Get(new PdfName(getExtXobject));

                            if (obj is PdfStream streamTemp)
                            {
                                PdfName subtype = streamTemp.GetAsName(PdfName.Subtype);
                                if (PdfName.Form.Equals(subtype))
                                {
                                    var temp1 = streamTemp.GetAsArray(PdfName.BBox)?.ToArray().Select(l => (float)double.Parse(l.ToString())).ToArray();
                                    aa2 = new Rectangle(Math.Min(temp1[0], temp1[2]), Math.Min(temp1[1], temp1[3]), Math.Abs(temp1[0] - temp1[2]), Math.Abs(temp1[1] - temp1[3]));
                                }
                            }

                        }
                        if (aa2 != null && aa2.GetWidth() != double.PositiveInfinity)
                        {
                            SelectedRectangles.Add(aa2);

                        }
                        else if (aa2 != null && aa2.GetWidth() != double.PositiveInfinity)
                        {
                            SelectedRectangles.Add(aa2);
                        }
                    }
                }

            }
        }

        //Object Removal Form
        public void GetHeightAndWidthOfPage(int pageNo, out float width, out float height)
        {
            width = 0;
            height = 0;
            using (PdfDocument doc = new PdfDocument(new PdfReader(filePath)))
            {
                PdfPage page = doc.GetPage(pageNo);
                Rectangle rect = page.GetPageSize();
                width = rect.GetWidth();
                height = rect.GetHeight();
            }
        }

        //Object Removal Form
        public void RemoveAllObjects(string OutputPath)
        {
            SelectedRectangles.Clear();
            locationSave.Clear();
            PdfDictionary extGState = new PdfDictionary();
            extGState.Put(PdfName.Type, PdfName.ExtGState);
            extGState.Put(PdfName.CA, new PdfNumber(0.0f));
            extGState.Put(PdfName.ca, new PdfNumber(0.0f));
            PdfExtGState pdfExtGState = new PdfExtGState(extGState);

            using (PdfReader reader = new PdfReader(filePath))
            using (PdfWriter writer = new PdfWriter(OutputPath))
            using (PdfDocument doc = new PdfDocument(reader, writer))
            {
                int numPages = doc.GetNumberOfPages();
                int pgNumber = 1;

                PdfPage page = doc.GetPage(pgNumber);
                PdfDictionary pageDict = page.GetPdfObject();
                PdfName gsName = new PdfName("CusGS1");
                PdfDictionary resources = page.GetResources().GetPdfObject();
                PdfDictionary extGStates = resources.GetAsDictionary(PdfName.ExtGState);
                if (extGStates == null)
                {
                    resources.Put(PdfName.ExtGState, new PdfDictionary());
                    extGStates = resources.GetAsDictionary(PdfName.ExtGState);
                }
                if (!extGStates.ContainsKey(gsName))
                    extGStates.Put(gsName, extGState);
                PdfObject contentobj = pageDict.Get(PdfName.Contents);
                PdfStream stream;
                string content;
                if (contentobj is PdfArray contentarray)
                {
                    //Loop through content
                    for (int j = 0; j < contentarray.Size(); j++)
                    {
                        stream = contentarray.GetAsStream(j);
                        content = Encoding.ASCII.GetString(stream.GetBytes());

                        string[] tokensQ = GetTokens(content, j);
                        for (int f = 0; f < tokensQ.Length; f++)
                        {
                            string[] tokens = tokensQ[f].Split('\n');
                            var aa = CalculateRectangle.GetRectangle(tokensQ[f], out int[] indices);
                            Rectangle aa2 = CalculateRectangle.CalculateBoundingBoxFromContentStream(tokensQ[f]);
                            //if (aa == null)
                            //    aa = aa2;
                            if (tokensQ[f].Contains("Fm"))
                            {
                                PdfDictionary resourcesTemp = page.GetResources().GetResource(PdfName.XObject);
                                var getExtXobject = GetXobjectName(tokensQ[f]).Trim('/');
                                PdfObject obj = resourcesTemp.Get(new PdfName(getExtXobject));

                                if (obj is PdfStream streamTemp)
                                {
                                    PdfName subtype = streamTemp.GetAsName(PdfName.Subtype);
                                    if (PdfName.Form.Equals(subtype))
                                    {
                                        var temp1 = streamTemp.GetAsArray(PdfName.BBox)?.ToArray().Select(l => (float)double.Parse(l.ToString())).ToArray();
                                        aa2 = new Rectangle(Math.Min(temp1[0], temp1[2]), Math.Min(temp1[1], temp1[3]), Math.Abs(temp1[0] - temp1[2]), Math.Abs(temp1[1] - temp1[3]));
                                    }
                                }

                            }
                            for (int i = 0; i < tokens.Length; i++)
                            {
                                if (tokens[i].Contains("gs"))
                                {
                                    tokens[i] = @"/CusGS1 gs";
                                }
                            }
                            tokensQ[f] = string.Join("\n", tokens);
                        }
                        string outstr = string.Join("\n", tokensQ.Select(p => p).ToArray());
                        byte[] outbytes = Encoding.ASCII.GetBytes(outstr);
                        stream.SetData(outbytes);
                    }
                }

                else if (contentobj is PdfStream streamer)
                {
                    content = Encoding.ASCII.GetString(streamer.GetBytes());
                    string[] tokens = GetTokens(content, 0);
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        if (tokens[i].Contains("gs"))
                        {
                            tokens[i] = @"/CusGS1 gs";
                        }
                    }

                    string outstr = string.Join("\n", tokens.Select(p => p).ToArray());
                    byte[] outbytes = Encoding.ASCII.GetBytes(outstr);
                    streamer.SetData(outbytes);
                }

            }
        }

        //Object Removal Form and PDFViewerForm
        public void RemoveObject(float x, float y, string OutputPath, int pageNo)
        {
            locationSave.Clear();

            #region x

            //int x = 1;
            //using (PdfReader reader = new PdfReader(filePath))
            //using (PdfWriter writer = new PdfWriter(@"D:\Demo.pdf"))
            //using (PdfDocument doc = new PdfDocument(reader, writer))
            //{
            //    extGState.MakeIndirect(doc);
            //    PdfIndirectReference gsRef = extGState.GetIndirectReference();
            //    List<PdfObject> objs = new List<PdfObject>();
            //    PdfPage pdfPage = doc.GetPage(1);
            //    PdfName gsName = new PdfName("CusGS1");
            //    PdfDictionary resources = pdfPage.GetResources().GetPdfObject();
            //    PdfDictionary extGStates = resources.GetAsDictionary(PdfName.ExtGState);
            //    if (extGStates == null)
            //    {

            //        extGStates = new PdfDictionary();
            //        resources.Put(PdfName.ExtGState, extGStates);
            //    }
            //    extGStates.Put(gsName, pdfExtGState.GetPdfObject());
            //    PdfCanvas canvas = new PdfCanvas(pdfPage);
            //    canvas.SetExtGState(extGState);
            //    var dict = pdfPage.GetResources().GetResource(PdfName.XObject) as PdfDictionary;
            //    foreach (var key in dict.KeySet())
            //    {
            //        objs.Add(dict.Get(key));
            //    }
            //    List<PdfObject> pdfObjectList = new List<PdfObject>();
            //    for (int i = 1; i <= doc.GetNumberOfPdfObjects(); i++)
            //    {
            //        pdfObjectList.Add(doc.GetPdfObject(i));
            //    }
            //    int c = 0;
            //    foreach (PdfObject t in pdfObjectList)
            //    {
            //        x++;
            //        if (t is PdfDictionary temp)
            //        {
            //            try
            //            {
            //                string objsReference = temp.GetIndirectReference().ToString();
            //                var dicttemp = temp.GetAsDictionary(PdfName.Resources)?.GetAsDictionary(PdfName.ExtGState);
            //                defaultExtGState.Add(objsReference, new List<(string, string)>());
            //                foreach (PdfName name in dicttemp.KeySet())
            //                {
            //                    defaultExtGState[objsReference].Add((name.ToString(), dicttemp.Get(name).GetIndirectReference().ToString()));
            //                }

            //            }
            //            catch (Exception e)
            //            { }
            //            finally
            //            {
            //                var loc = temp.GetAsDictionary(PdfName.Resources)?.GetAsDictionary(PdfName.ExtGState);
            //                if (temp.GetAsDictionary(PdfName.Resources) != null)
            //                {
            //                    if (loc == null)
            //                        temp.GetAsDictionary(PdfName.Resources).Put(PdfName.ExtGState, new PdfDictionary());
            //                    loc?.Clear();
            //                    temp.GetAsDictionary(PdfName.Resources)?.GetAsDictionary(PdfName.ExtGState)?.Put(new PdfName("CusGS1"), gsRef);
            //                    temp.SetModified();
            //                    var tem = temp.Values();
            //                    c++;
            //                }
            //                else
            //                {
            //                    if (temp is PdfStream)
            //                    {
            //                        temp.Put(PdfName.ColorSpace,gsRef);
            //                        //temp.GetAsDictionary(PdfName.ExtGState).Put(new PdfName("CusGS1"), gsRef);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    ;
            //    doc.Close();
            //}
            #endregion

            using (PdfReader reader = new PdfReader(filePath))
            using (PdfWriter writer = new PdfWriter(OutputPath))
            using (PdfDocument doc = new PdfDocument(reader, writer))
            {
                int pgNumber = pageNo;
                PdfPage page = doc.GetPage(pgNumber);
                PdfDictionary pageDict = page.GetPdfObject();
                PdfObject contentobj = pageDict.Get(PdfName.Contents);
                string fullContent = GetFullContent(contentobj, out int[] array);

                #region Remove all objects in the area
                //content = Encoding.ASCII.GetString(streamer.GetBytes());
                //var graphicsBlocks = GetTokens(content);
                //var totalContentStream = "";
                //foreach (var graphicsBlock in graphicsBlocks)
                //{
                //    totalContentStream += graphicsBlock.ToFormattedString() + "\n";
                //}


                //ChangeIfTheBlockContains(graphicsBlocks, x, y, pageNo, doc);

                //string contentCopy = "";

                //foreach (var graphicsBlock in graphicsBlocks)
                //{
                //    contentCopy += graphicsBlock.ToFormattedString() + "\n";
                //}

                //byte[] outbytes = Encoding.ASCII.GetBytes(contentCopy);
                //streamer.SetData(outbytes);

                #endregion

                #region Use content Stream combined 
                //var graphicsBlocks = GetTokens(content);
                //Rectangle rect = new Rectangle(x - 2, y - 2, 4, 4);
                //bool isChanged = false;
                //foreach (var val in ObjectDatas)
                //{
                //    if (val.Rectangle.Overlaps(rect))
                //    {
                //        var rectx = CalculatePolygen.ParseContentStream(string.Join("\n", val.Block.Lines), out List<int> HIndex,page);
                //        int HCount = 1;
                //        var tempLines = val.Block.Lines;
                //        foreach (var polygen in rectx)
                //        {
                //            if (CalculatePolygen.IsPointInsideOrNearPolygon(new System.Drawing.PointF(x, y), polygen.Points))
                //            {
                //                //string path = string.Join("\n", tempLines.Skip(HIndex[HCount - 1]).Take(HIndex[HCount] - HIndex[HCount - 1] - 1));
                //                //if (ContainsTag("gs", path))
                //                //{
                //                //    for (int i = HIndex[HCount - 1]; i < HIndex[HCount]; i++)
                //                //    {
                //                //        if (ContainsTag("gs", tempLines[i]))
                //                //        {
                //                //            string gState = @"/Cu1 gs";
                //                //            if (tempLines[i].Contains('\r'))
                //                //                gState += '\r';
                //                //            tempLines[i] = gState;
                //                //            isChanged = true;
                //                //            break;
                //                //        }
                //                //    }
                //                //}
                //                //else
                //                //{
                //                for (int i = HIndex[HCount - 1]; i < HIndex[HCount]; i++)
                //                {
                //                    tempLines[i] = "";
                //                }
                //                //tempLines[HIndex[HCount-1]] += "/Cu1 gs";
                //                isChanged = true;
                //                break;
                //                //}
                //            }
                //            HCount++;
                //        }
                //        val.Block.Lines = tempLines;
                //        if (isChanged)
                //            break;
                //    }
                //}

                //string contentCopy = "";

                //foreach (var graphicsBlock in globalGraphicsBlock)
                //{
                //    contentCopy += graphicsBlock.ToFormattedString() + "\n";
                //}

                //byte[] outbytes = Encoding.ASCII.GetBytes(contentCopy);
                //streamer.SetData(outbytes);
                #endregion

                #region Using Stored Rectangle
                Rectangle clickedArea = new Rectangle(x - 2, y - 2, 4, 4);
                bool isChanged = false;
                foreach (var objectData in ObjectDatas)
                {
                    if (objectData.Rectangle.Overlaps(clickedArea) && objectData.Block.PageNo == pageNo)
                    {

                        #region Remove based on the Parent
                        //bool isParentModified = false;
                        //bool isInsideParent = false;
                        //if(isParentContainer(val.Block.Parent))
                        //{
                        //    isParentModified = isChanged = RemoveObjectUsingPolygen(val.Block.Parent,x,y,page,out isInsideParent);
                        //}
                        //if(isInsideParent || !isParentModified)
                        //{
                        //    isChanged = RemoveObjectUsingPolygen(val.Block,x,y,page,out isInsideParent);
                        //} 
                        #endregion

                        isChanged = RemoveObjectUsingPolygen(objectData, x, y, page, out bool isInsideParent);

                        #region TODO:Remove Text
                        var path = string.Join("\n", objectData.Block.GetFormattedString());
                        var tempLines = objectData.Block.Lines.ToList();
                        float X = 0;
                        float Y = 0;
                        if (ContainsTag("BT", path))
                        {
                            bool canRemove = false;
                            for (int i = 0; i < tempLines.Count; i++)
                            {
                                if (ContainsTag("Tm", tempLines[i]) || ContainsTag("Td", tempLines[i]))
                                {
                                    canRemove = false;
                                    Stack<float> tempList = new Stack<float>();
                                    foreach (var val in tempLines[i].Split(' '))
                                    {
                                        if (float.TryParse(val, out float result))
                                        {
                                            tempList.Push(result);
                                        }
                                        else if (val == "Tm" || val == "Td")
                                        {
                                            float tempY = tempList.Pop();
                                            float tempX = tempList.Pop();
                                            if (val == "Td")
                                            {
                                                X += tempX;
                                                Y += tempY;
                                            }
                                            else
                                            {
                                                Y = tempY;
                                                X = tempX;
                                            }
                                            Rectangle tempRect1 = new Rectangle(x - 2, y - 2, 4, 4);
                                            Rectangle tempRect2 = new Rectangle(X - 4, Y - 4, 10, 10);
                                            canRemove = tempRect1.Overlaps(tempRect2);
                                        }

                                    }

                                }
                                if (canRemove)
                                {
                                    if ((ContainsTag("Tj", tempLines[i]) || ContainsTag("TJ", tempLines[i])))
                                        tempLines[i] = "";
                                    isChanged = true;
                                }
                            }
                        }
                        objectData.Block.Lines = tempLines;
                        #endregion

                        if (isChanged)
                            break;
                    }
                }


                #endregion

                SetFullContent(contentobj, array, pageNo);

            }
        }

        //Object Removal Form and PDFViewerForm
        private bool RemoveObjectUsingPolygen(ObjectData objData, float x, float y, PdfPage page, out bool isInsideParent)
        {
            GraphicsBlock block = objData.Block;
            if (block == null)
                isInsideParent = true;
            var tempLines = block.Lines.ToList();
            bool isChanged = false;
            List<int> objectIndex = new List<int>();
            string requiredString = string.Join("\n", block.Lines.Skip(objData.From).Take(objData.To - objData.From + 1));
            var polygenPoints = CalculatePolygen.ParseContentStream(requiredString, out List<(int, int)> objIndex, page);
            SortPolygenPoints(polygenPoints, objIndex);
            int index = 0;
            isInsideParent = false;
            bool isfill = ContainsTag("f", requiredString) || ContainsTag("f*", requiredString) || ContainsTag("W", requiredString) || ContainsTag("W*", requiredString);
            foreach (var polygen in polygenPoints)
            {
                string blockData = string.Join("\n", requiredString.Split('\n').Skip(objIndex[index].Item1).Take(objIndex[index].Item2 - objIndex[index].Item1));
                
                if (CalculatePolygen.IsPointInsideOrNearPolygon(new System.Drawing.PointF(x, y), polygen.Points, isFill: isfill))
                {
                    if (ContainsTag("W n", blockData) || ContainsTag("W*", blockData) || ContainsTag("W", blockData))
                    {
                        isInsideParent = true;
                        bool isChildRemoved = false;
                        foreach (var child in block.Children)
                        {
                            string childXobjectName = GetXobjectName(child.GetOwnString())?.Trim('/');
                            var isXobject = page.GetResources().GetResource(PdfName.XObject)?.GetAsStream(new PdfName(childXobjectName==null?"":childXobjectName));
                            if (childXobjectName != null && isXobject == null)
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
                            foreach (var child in block.Children)
                            {
                                child.Parent = null;
                            }
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
                        //isInsideParent = DoesClickIsInParent(objData, x, y, page);
                        //if (isInsideParent)
                        //{
                        for (int i = objData.From + objIndex[index].Item1; i < objData.From + objIndex[index].Item2; i++)
                        {
                            if (IsOnlyObjectMarking(tempLines[i]))
                                tempLines[i] = "";
                        }
                        isChanged = true;
                        break;
                        //}
                    }
                }
                index++;
            }
            block.Lines = tempLines;
            return isChanged;
        }

        private bool DoesClickIsInParent(ObjectData objData, float x, float y, PdfPage page)
        {
            GraphicsBlock block = objData.Block;

            if (block.Parent == null)
                return true;

            var polygon = CalculatePolygen.ParseContentStream(objData.Block.Parent.GetOwnString(), out List<(int, int)> objIndex, page);

            var rect = CalculateRectangle.GetRectangle(objData.Block.Parent.GetOwnString(), out int[] index);

            foreach (var poly in polygon)
            {
                if (CalculatePolygen.IsPointInPolygon(new System.Drawing.PointF(x, y), poly.Points))
                {
                    return true;
                }
            }

            return polygon.Count == 0;
        }

        private void SortPolygenPoints(List<CalculatePolygen.PdfPolygon> polygenPoints, List<(int, int)> objectIndex)
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

        //Object Removal Form and PDFViewerForm
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

        //Object Removal Form and PDFViewerForm
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

        //Object Removal Form and PDFViewerForm
        private bool IsOnlyObjectMarking(string line)
        {
            List<string> checkList = new List<string>
            {
                "m","l","c","re","Do","v","y","f","f*","F","F*"
            };
            foreach (var check in checkList)
            {
                if (ContainsTag(check, line))
                    return true;
            }
            return false;
        }

        //Object Removal Form and PDFViewerForm
        private void ChangeIfTheBlockContains(List<GraphicsBlock> graphicsBlocks, float x, float y, int pageNo, PdfDocument doc)
        {
            foreach (var graphicsBlock in graphicsBlocks)
            {
                ChangeIfTheBlockContains(graphicsBlock.Children, x, y, pageNo, doc);

                #region Remove Region based on Graphics
                var rect1 = CalculateRectangle.CalculateBoundingBoxFromContentStream(string.Join("\n", graphicsBlock.Lines));
                List<int> HIndex = new List<int>();
                var rectx = CalculatePolygen.ParseContentStream(string.Join("\n", graphicsBlock.Lines), out List<(int, int)> index, doc.GetPage(pageNo));
                var rect2 = CalculateRectangle.CalculateRectangleFromXObject(doc.GetPage(pageNo), string.Join("\n", graphicsBlock.Lines));
                if (rect1 == null || rect1.GetRight() == double.PositiveInfinity)
                    rect1 = rect2;

                int HCount = 1;
                var tempLines = graphicsBlock.Lines;
                foreach (var polygen in rectx)
                {
                    if (CalculatePolygen.IsPointInsideOrNearPolygon(new System.Drawing.PointF(x, y), polygen.Points))
                    {
                        bool isContainGraphics = false;

                        //for (int i = 0; i < tempLines.Count; i++)
                        //{
                        //    if (tempLines[i].Contains("gs"))
                        //    {
                        //        string gState = @"/Cu1 gs";
                        //        if (tempLines[i].Contains('\r'))
                        //            gState += '\r';
                        //        tempLines[i] = gState;
                        //        isContainGraphics = true;
                        //    }
                        //}

                        if (!isContainGraphics)
                        {
                            for (int i = HIndex[HCount - 1]; i < HIndex[HCount]; i++)
                            {
                                tempLines[i] = "";
                            }
                        }

                    }
                    HCount++;
                }
                graphicsBlock.Lines = tempLines;
                //if (rect1 != null && DoesContains(x, y, rect1) && !isPageSize(rect1, pageNo))
                //{
                //    CalculatePolygen.IsPointInPolygon(new System.Drawing.PointF(x, y), rectx[0].Points);
                //    bool isContainGraphics = false;
                //    var tempLines = graphicsBlock.Lines;
                //    for (int i = 0; i < tempLines.Count; i++)
                //    {
                //        if (tempLines[i].Contains("gs"))
                //        {
                //            string gState = @"/Cu1 gs";
                //            if (tempLines[i].Contains('\r'))
                //                gState += '\r';
                //            tempLines[i] = gState;
                //            isContainGraphics = true;
                //        }
                //    }

                //    if (!isContainGraphics)
                //    {
                //        tempLines[0] = "q";
                //        for (int i = 1; i < tempLines.Count - 1; i++)
                //        {
                //            tempLines[i] = "";
                //        }
                //        tempLines[tempLines.Count - 1] = "Q";
                //    }
                //    graphicsBlock.Lines = tempLines;
                //}
                #endregion

            }
        }

        //Object Removal Form and PDFViewerForm
        private List<GraphicsBlock> GetTokens(int pageNo, string contentStream)
        {
            #region block Separation using q to Q tags
            string[] lines = contentStream.Split('\n');
            List<GraphicsBlock> rootBlocks = new List<GraphicsBlock>();
            Stack<GraphicsBlock> stack = new Stack<GraphicsBlock>();
            List<string> looseblocks = new List<string>();
            int index = 0;

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd('\r');

                if (ContainsTag("q", line))
                {
                    GraphicsBlock newBlock = new GraphicsBlock(true)
                    {
                        Index = index,
                        PageNo = pageNo
                    };
                    newBlock.Lines.Add(line);
                    if (looseblocks.Count > 0)
                    {
                        GraphicsBlock looseBlock = new GraphicsBlock(false);
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
                GraphicsBlock looseBlock = new GraphicsBlock(false)
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

        //Object Removal Form
        private string[] GetTokens(string contentStream, int contentIndex)
        {
            locationSave.Add(new Dictionary<string, (int start, int end)>());
            var blocks = new List<string>();
            var lines = contentStream.Split('\n');
            Stack<int> startIndex = new Stack<int>();
            int depth = 0;
            Stack<List<string>> stack = new Stack<List<string>>();
            List<string> looseContent = new List<string>();
            int index = 0;
            int lineIndex = 0;
            startIndex.Push(0);
            foreach (var rawLine in lines)
            {
                string line = rawLine.TrimEnd('\r'); // Handle CRLF or LF

                if (ContainsTag("q", line))
                {
                    if (depth == 0 && looseContent.Count > 0)
                    {
                        int temp = index + rawLine.Length + 1;
                        int length = looseContent.Sum(x => x.Length);
                        blocks.Add(string.Join("\n", looseContent));
                        if (!locationSave[contentIndex].ContainsKey(string.Join("\n", string.Join("\n", looseContent))))
                            locationSave[contentIndex].Add(string.Join("\n", looseContent), (temp - length, index));
                        //startIndex.Push(temp);
                        looseContent.Clear();
                    }

                    var newBlock = new List<string> { line };
                    startIndex.Push(index + rawLine.Length + 1);
                    stack.Push(newBlock);
                    depth++;
                }
                else if (ContainsTag("Q", line))
                {
                    if (stack.Count > 0)
                    {
                        if (startIndex.Count == 0)
                            startIndex.Pop();
                        var completedBlock = stack.Pop();
                        int temp = index + rawLine.Length + 1;
                        completedBlock.Add(line);
                        if (!locationSave[contentIndex].ContainsKey(string.Join("\n", completedBlock)))
                            locationSave[contentIndex].Add(string.Join("\n", completedBlock), (startIndex.Pop(), temp));
                        //startIndex.Push(temp);
                        blocks.Add(string.Join("\n", completedBlock));
                    }
                    else
                    {
                        looseContent.Add(line);
                    }

                    depth = Math.Max(0, depth - 1);
                }
                else
                {
                    if (depth > 0 && stack.Count > 0)
                    {
                        stack.Peek().Add(line);
                    }
                    else
                    {
                        looseContent.Add(line);
                    }
                }
                index += rawLine.Length + 1;
                lineIndex += 1;
            }

            if (looseContent.Count > 0)
            {
                blocks.Add(string.Join("\n", looseContent));
            }


            return blocks.ToArray();

        }

        //Object Removal Form and PDFViewerForm
        private bool ContainsTag(string v, string line)
        {
            if (!line.Contains(v))
                return false;
            int index = line.IndexOf(v);
            if (index != 0 && (line[index - 1] != ' ' && line[index - 1] != '\n' && line[index - 1] != '\r' && line[index - 1] != ']'))
                return false;

            index += v.Length;
            return index == line.Length || line[index] == ' ' || line[index] == '\r' || line[index] == '\n';
        }

        //Object Removal Form and PDFViewerForm
        private string GetXobjectName(string contentStream)
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
            return contentStream.Substring(startIndex, endIndex - startIndex - 1).Trim();
        }

        //Object Removal Form and PDFViewerForm
        private bool isParentContainer(GraphicsBlock parent)
        {
            if (parent == null)
                return false;
            string parentGraphics = parent.GetOwnString();
            return ContainsTag("W", parentGraphics) || ContainsTag("W*", parentGraphics);
        }

        public void Dispose()
        {
            ObjectDatas.Clear();
            SelectedRectangles.Clear();
            globalGraphicsBlock.Clear();
            foreach (var location in locationSave)
            {
                location.Clear();
            }

            locationSave.Clear();
        }

        private bool IsOperator(string cmd)
        {
            string[] operators = { "q", "Q", "cm", "m", "l", "c", "h", "S", "s", "f", "f*", "re", "Do", "W", "W*", "n", "TJ", "Tj", "Tm", "n" };
            foreach (var Operator in operators)
            {
                if (ContainsTag(Operator, cmd))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class GraphicsBlock
    {
        public List<string> Lines { get; set; }
        public List<GraphicsBlock> Children { get; set; }
        public GraphicsBlock Parent { get; set; }
        public bool IsWrapper { get; set; }
        public int Index;
        public int PageNo;

        public GraphicsBlock(bool isWrapper = false)
        {
            Lines = new List<string>();
            Children = new List<GraphicsBlock>();
            IsWrapper = isWrapper;
            Index = -1;
        }

        public int GetTotalLines()
        {
            int length = Lines.Count;
            foreach (var child in Children)
            {
                length += child.GetTotalLines();
            }
            return length;
        }

        public void ProcessForRectangle()
        {
            foreach (string line in Lines)
            {
                if (line.Contains("re"))
                {
                    Console.WriteLine("Found rectangle in: " + line);
                }
            }

            foreach (GraphicsBlock child in Children)
            {
                child.ProcessForRectangle();
            }
        }

        public string GetFormattedString()
        {
            List<string> result = new List<string>();

            if (Lines.Count == 0)
            {
                result.Add("q");
                foreach (var child in Children)
                {
                    result.Add(child.GetFormattedString());
                }
                result.Add("Q");
            }
            int currentIndex = 0;
            for (int i = 0; i < Lines.Count; i++)
            {
                var childInsert = Children.Where(x => x.Index - Index == currentIndex).ToList();
                while (childInsert.Count != 0)
                {
                    foreach (var child in childInsert)
                    {
                        var childLines = child.GetFormattedString();
                        result.Add(childLines);
                        currentIndex += child.GetTotalLines();
                    }

                    childInsert = Children.Where(x => x.Index - Index == currentIndex).ToList();
                }
                result.Add(Lines[i]);
                currentIndex += 1;
            }

            return string.Join("\n", result.ToArray());
        }

        public List<Rectangle> GetAllRectangles(PdfPage page)
        {
            List<Rectangle> tempRectArray = new List<Rectangle>();

            foreach (var block in Children)
            {
                tempRectArray.AddRange(block.GetAllRectangles(page));
            }

            var rect1 = CalculateRectangle.CalculateBoundingBoxFromContentStream(string.Join("\n", this.Lines));
            var rect2 = CalculateRectangle.CalculateRectangleFromXObject(page, string.Join("\n", this.Lines));
            if (rect1 == null || rect1.GetWidth() == double.PositiveInfinity)
                rect1 = rect2;
            if (rect1 != null && rect1.GetWidth() != double.PositiveInfinity && rect1.GetWidth() != 0)
                tempRectArray.Add(rect1);



            return tempRectArray;
        }

        public string GetOwnString()
        {
            return string.Join("\n", Lines);
        }
    }
}
