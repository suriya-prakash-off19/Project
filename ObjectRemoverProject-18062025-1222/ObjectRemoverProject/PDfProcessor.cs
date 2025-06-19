using HalconDotNet;
using iText.Kernel.Pdf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ObjectRemoverProject
{
    [ComVisible(true)]
    public static class PdfProcessor
    {
        static PdfProcessor()
        {
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
        }
        private static readonly string tempFolder = "Temp";
        private static readonly string tempFileName = Path.Combine(tempFolder, "PdfToConvert.pdf");
        private static readonly string outputFileName = Path.Combine(tempFolder, "ConvertedImage.png");
        

        public enum RenderTool
        {
            Mutool = 1,
            ImageMagic = 2,
            GhostScript = 3
        }

        public enum LayerManipulationTool
        {
            iText7 = 1,
            PdfClown = 2
        }

        public enum ColorSeparationTool
        {
            iText7 = 1,
            PdfClown = 2
        }

        /// <summary>
        /// Check whether the PDF is password protected or not
        /// </summary>
        /// <param name="pdfFileByte"></param>
        /// <returns>bool</returns>
        public static bool IsPasswordProtected(byte[] pdfFileByte, ref string errorMsg)
        {
            bool isProtected = false;

            using (MemoryStream memoryStream = new MemoryStream(pdfFileByte))
            {
                try
                {
                    // Attempt to open the PDF without a password
                    using (iText.Kernel.Pdf.PdfReader pdfReader = new iText.Kernel.Pdf.PdfReader(memoryStream))
                    {
                        using (iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader))
                        {
                            Console.WriteLine("The PDF is not password-protected.");
                        }
                    }
                }
                catch (iText.Kernel.Exceptions.BadPasswordException)
                {
                    isProtected = true;
                    errorMsg = "The PDF is password-protected.";
                    Console.WriteLine(errorMsg);
                }
                catch (Exception ex)
                {
                    errorMsg = $"An error occurred: {ex.Message}";
                    Console.WriteLine(errorMsg);
                }
            }
            return isProtected;
        }

        /// <summary>
        /// Validates given password with PDF
        /// </summary>
        /// <param name="pdfFileByte"></param>
        /// <param name="password"></param>
        /// <param name="decryptedPdfMemory"></param>
        /// <param name="errorMsg"></param>
        /// <returns>DecryptedPdfMemory</returns>
        public static bool ValidatePassword(byte[] pdfFileByte, string password, out byte[] decryptedPdfMemory, ref string errorMsg)
        {
            decryptedPdfMemory = null;
            try
            {
                using (MemoryStream inputStream = new MemoryStream(pdfFileByte))
                {
                    // Step 1: Open the password-protected PDF
                    iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(inputStream, new iText.Kernel.Pdf.ReaderProperties().SetPassword(System.Text.Encoding.UTF8.GetBytes(password)));
                    iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader);

                    // Step 2: Write the decrypted PDF to a MemoryStream
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(outputStream);
                        iText.Kernel.Pdf.PdfDocument decryptedPdfDoc = new iText.Kernel.Pdf.PdfDocument(writer);

                        // Copy the decrypted content to the new PDF document
                        pdfDoc.CopyPagesTo(1, pdfDoc.GetNumberOfPages(), decryptedPdfDoc);

                        // Close the documents
                        decryptedPdfDoc.Close();
                        pdfDoc.Close();

                        // Step 3: Convert the MemoryStream to a byte[]
                        decryptedPdfMemory = outputStream.ToArray();

                        // Output the result (for demonstration)
                        Console.WriteLine("Decrypted PDF size: " + decryptedPdfMemory.Length + " bytes");
                    }
                }
                return true;
            }
            catch (iText.Kernel.Exceptions.BadPasswordException ex)
            {
                Console.WriteLine("Incorrect password or the PDF is not password-protected.");
                errorMsg = ex.Message;
                decryptedPdfMemory = null;
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                errorMsg = ex.Message;
                decryptedPdfMemory = null;
                return false;
            }
        }

        public static Dictionary<string, string> GetPdfInfo(byte[] pdfMemory)
        {
            //string pdfInfo = "";
            Dictionary<string, string> pdfInfo = new Dictionary<string, string>();

            using (MemoryStream memoryStream = new MemoryStream(pdfMemory))
            {
                using (iText.Kernel.Pdf.PdfReader pdfReader = new iText.Kernel.Pdf.PdfReader(memoryStream))
                {
                    using (iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader))
                    {

                        // Get document information
                        var info = pdfDocument.GetDocumentInfo();

                        // Extract Creator
                        if (!string.IsNullOrEmpty(info.GetCreator()))
                        {
                            pdfInfo["Creator"] = info.GetCreator().Split(',')[0];
                        }


                        // Extract Producer
                        if (!string.IsNullOrEmpty(info.GetProducer()))
                        {
                            pdfInfo["Producer"] = info.GetProducer().Split(',')[0];
                        }

                        // Extract PDF version
                        if (!string.IsNullOrEmpty(pdfDocument.GetPdfVersion().ToString()))
                        {
                            pdfInfo["PDF Version"] = "PDF Version: 1." + pdfDocument.GetPdfVersion();
                        }

                        // Display the information
                        Console.WriteLine(pdfInfo);
                    }
                }
            }
            return pdfInfo;
        }

        /// <summary>
        /// Gets the number of pages in the PDF.
        /// </summary>
        /// <param name="pdfMemory"></param>
        /// <param name="NoOfPages"></param>
        /// <param name="errorMsg"></param>
        /// <returns>NoOfPages</returns>
        public static bool GetPages(byte[] pdfMemory, ref int NoOfPages, ref string errorMsg)
        {
            try
            {
                // Read PDF from byte[]
                using (MemoryStream memoryStream = new MemoryStream(pdfMemory))
                {
                    using (iText.Kernel.Pdf.PdfReader pdfReader = new iText.Kernel.Pdf.PdfReader(memoryStream))
                    {
                        using (iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader))
                        {
                            // Access PDF contents
                            NoOfPages = pdfDocument.GetNumberOfPages();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                return false;
            }
        }

        public static bool GetPdfImagePath(byte[] pdfMemory, int dpi, RenderTool renderTool, bool isMultiPage, int pageNumber, ref string outputImagePath, ref string errorMsg)
        {
            bool result = false;
            HImage[] hImageArr = null;
            bool isGetImagePath = true;
            WritePdfFile(tempFileName, pdfMemory, ref errorMsg);
            result = GetPdfImageFromGhostscript(tempFileName, dpi, pageNumber, isMultiPage, isGetImagePath, ref outputImagePath, ref hImageArr, ref errorMsg);
            DeletePdfFile(tempFileName, ref errorMsg);
            return result;
        }

        public static bool GetPdfImage(byte[] pdfMemory, int dpi, RenderTool renderTool, bool isMultiPage, int pageNumber, ref HImage[] hImageArr, ref string errorMsg)
        {
            bool result = false;
            string empty = "";
            bool isGetImagePath = false;

           
            WritePdfFile(tempFileName, pdfMemory, ref errorMsg);
            result = GetPdfImageFromGhostscript(tempFileName, dpi, pageNumber, isMultiPage, isGetImagePath, ref empty, ref hImageArr, ref errorMsg);
            DeletePdfFile(tempFileName, ref errorMsg);
            
            return result;
        }
        private static bool GetPdfImageFromGhostscript(string PdfPath, int dpi, int PageNumber, bool isMultiPage, bool isGetImagePath, ref string outputImagePath, ref HImage[] hImageArr, ref string errorMsg)
        {
            // Path to Ghostscript executable
            string ghostscriptPath = @"gswin64c.exe";

            // Input PDF file path
            string inputFilePath = PdfPath;

            // Ghostscript arguments
            string arguments = "";
            if (isMultiPage)
            {
                string outputFile = Path.Combine(tempFolder, "ConvertedImage%d.png");
                arguments = $"-sDEVICE=png16m -dBATCH -dNOPAUSE -r{dpi} -dTextAlphaBits=4 -dGraphicsAlphaBits=4 -sOutputFile=\"{outputFile}\" \"{inputFilePath}\"";
            }
            else
            {
                arguments = $"-sDEVICE=png16m -dBATCH -dNOPAUSE -r{dpi} -dTextAlphaBits=4 -dGraphicsAlphaBits=4  -dFirstPage={PageNumber} -dLastPage={PageNumber} -sOutputFile=\"{outputFileName}\" \"{inputFilePath}\"";
            }
            try
            {
                // Configure and start the process
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = ghostscriptPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = processInfo;

                    // Start the process
                    process.Start();

                    // Read the output and error streams
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Wait for the process to exit
                    process.WaitForExit();

                    // Display the results
                    Console.WriteLine("Output:");
                    Console.WriteLine(output);

                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine("Error:");
                        Console.WriteLine(error);
                        errorMsg = error;
                    }

                    if (isMultiPage)
                    {
                        int pageCounter = 1;
                        List<HImage> images = new List<HImage>();
                        while (true)
                        {
                            string filePath = Path.Combine(tempFolder, $"ConvertedImage{pageCounter}.png");
                            if (System.IO.File.Exists(filePath))
                            {
                                if (isGetImagePath)
                                {
                                    if (outputImagePath == "")
                                    {
                                        outputImagePath = Path.GetFullPath(filePath);
                                    }
                                    else
                                    {
                                        outputImagePath = outputImagePath + "|" + Path.GetFullPath(filePath);
                                    }
                                }
                                else
                                {
                                    images.Add(new HImage(filePath));
                                    System.IO.File.Delete(filePath);
                                }
                                pageCounter++;
                            }
                            else
                            {
                                if (images.Count == 0) errorMsg = "Output file not found";
                                break;
                            }
                        }
                        hImageArr = images.Select(img => img.Clone()).ToArray();
                        DisposeHImages(images);
                    }
                    else
                    {
                        if (System.IO.File.Exists(outputFileName))
                        {
                            if (isGetImagePath)
                            {
                                outputImagePath = Path.GetFullPath(outputFileName);
                                return true;
                            }
                            hImageArr = new HImage[1];
                            hImageArr[0] = new HImage(outputFileName);
                            System.IO.File.Delete(outputFileName);
                        }
                        else
                        {
                            errorMsg = "Output file not found";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
            return true;

        }

        private static bool DisposeHImages(List<HImage> images)
        {
            try
            {
                foreach (var img in images)
                {
                    img.Dispose();
                }
                images.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
            return true;
        }

        private static bool WritePdfFile(string outputFilePath, byte[] pdfMemory, ref string errorMsg)
        {
            try
            {
                System.IO.File.WriteAllBytes(outputFilePath, pdfMemory);
            }
            catch (IOException ex)
            {
                errorMsg = "Failed to write pdf file: " + ex.Message;
                Console.WriteLine(errorMsg);
                return false;
            }
            return true;
        }

        private static bool DeletePdfFile(string filePath, ref string errorMsg)
        {
            // Wait or ensure the file is not locked before deleting
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath); // Safely delete the file
                    Console.WriteLine("Temporary PDF file deleted successfully.");
                }
            }
            catch (IOException ex)
            {
                errorMsg = "Failed to delete pdf file: " + ex.Message;
                Console.WriteLine(errorMsg);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the layers and its visibility present in the PDF file
        /// </summary>
        /// <param name="pdfMemory"></param>
        /// <param name="layerTool"></param>
        /// <param name="layerArr"></param>
        /// <param name="layerVisibility"></param>
        /// <param name="errorMsg"></param>
        /// <returns>LayerArray, LayerVisibilityArray</returns>
        //public static bool GetLayers(byte[] pdfMemory, LayerManipulationTool layerTool, ref string[] layerArr, ref bool[] layerVisibility, ref string errorMsg)
        //{
        //    bool result = false;
        //    if (layerTool == LayerManipulationTool.iText7)
        //    {
        //        result = GetLayersByiText7(pdfMemory, ref layerArr, ref layerVisibility, ref errorMsg);
        //    }
        //    else if (layerTool == LayerManipulationTool.PdfClown)
        //    {
        //        result = GetLayersByPdfClown(pdfMemory, ref layerArr, ref layerVisibility, ref errorMsg);
        //    }
        //    return result;
        //}

        private static bool GetLayersByiText7(byte[] pdfMemory, ref string[] layerArr, ref bool[] layerVisibility, ref string errorMsg)
        {
            try
            {
                //itext7
                using (MemoryStream outputstream = new MemoryStream())
                using (MemoryStream inputstream = new MemoryStream(pdfMemory))
                using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(inputstream))
                {
                    reader.SetUnethicalReading(true);
                    using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(outputstream))
                    using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, writer))
                    {
                        iText.Kernel.Pdf.Layer.PdfOCProperties ocProperties = pdfDoc.GetCatalog().GetOCProperties(false);
                        if (ocProperties != null)
                        {
                            IList<iText.Kernel.Pdf.Layer.PdfLayer> layers = ocProperties.GetLayers();


                            Dictionary<string, bool> ocgs = new Dictionary<string, bool>();
                            foreach (iText.Kernel.Pdf.Layer.PdfLayer layer in layers)
                            {
                                if (layer.IsOnPanel())
                                {
                                    iText.Kernel.Pdf.PdfObject Name = layer.GetPdfObject().Get(iText.Kernel.Pdf.PdfName.Name);
                                    if (Name != null)
                                    {
                                        string checkLayerName = System.Text.RegularExpressions.Regex.Replace(Name.ToString(), "[^A-Za-z0-9 ]", "");
                                        if (Name != null && !ocgs.ContainsKey(checkLayerName))
                                            ocgs.Add(checkLayerName, layer.IsOn());
                                    }
                                }
                            }

                            if (layerArr == null) layerArr = new string[ocgs.Count];
                            if (layerVisibility == null) layerVisibility = new bool[ocgs.Count];

                            int i = 0;
                            foreach (KeyValuePair<string, bool> ocg in ocgs)
                            {
                                layerArr[i] = ocg.Key;
                                layerVisibility[i] = ocg.Value;
                                i++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                return false;
            }
            return true;
        }
        private static HashSet<string> names = new HashSet<string>();
        private static readonly List<string> removables = new List<string> { " K", " k", " rg", " RG", " g", " G"/*, " scn", " SCN"*//*, " cs ", " SC ", " sc "*/ };
        private static List<int> CmykRemoveIndex = new List<int>();
        private static List<string> RemoveSpotColors = new List<string>();

        public static Dictionary<string, string> SpotColorKeys = new Dictionary<string, string>();
        public static Dictionary<string, List<string>> DeviceNColor = new Dictionary<string, List<string>>();

        private static bool isCymkAdded = false;
        public static bool ClearImageData { get; set; }
        public static bool IsCmykLayer { get; set; }

        private static void GetColorFromByte(byte[] byteArr)
        {
            foreach (string removable in removables)
            {
                if (isCymkAdded)
                    break;

                byte[] removableArray = Encoding.UTF8.GetBytes(removable);

                var indexes = GetSubarrayIndexes(byteArr, removableArray);

                if ((removable == " RG" || removable == " rg") && indexes.Count > 0 && !isCymkAdded)
                {
                    names.Add("Black");
                    names.Add("Yellow");
                    names.Add("Magenta");
                    names.Add("Cyan");

                    isCymkAdded = true;
                }
                if ((removable == " K" || removable == " k") && indexes.Count > 0 && !isCymkAdded)
                {
                    names.Add("Black");
                    names.Add("Yellow");
                    names.Add("Magenta");
                    names.Add("Cyan");

                    isCymkAdded = true;
                }
            }
        }

      
        // to get rgb value from removing the c/m/y/k channels of original passed rgb value.
        private static List<double> GetCMYKfromRGB(double R, double G, double B, bool cState, bool mState, bool yState, bool kState)
        {

            List<double> alteredRGBValues = new List<double>();

            double c = (1f - R);
            double m = (1f - G);
            double y = (1f - B);

            double min = Math.Min(Math.Min(c, m), y);
            double k = min;
            if (!cState) c = 0;
            //else c = 1;
            if (!yState) y = 0;
            //  else y = 1;
            if (!mState) m = 0;
            //  else m = 1;
            if (!kState) k = 0;
            // else k = 1;


            //double r = (1f - c) * (1f - k);
            //double g = (1f - m) * (1f - k);
            //double b = (1f - y) * (1f - k);

            double r = (1f - c);
            double g = (1f - m);
            double b = (1f - y);

            alteredRGBValues.Add(r);
            alteredRGBValues.Add(g);
            alteredRGBValues.Add(b);

            return alteredRGBValues;
        }
        public static bool UpdateSpotKeys(Dictionary<string, string> Spotcolor)
        {
            try
            {
                SpotColorKeys.Clear();
                SpotColorKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(Spotcolor));
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        private static int GetByteIndex(byte[] arr, int startIndex)
        {
            int lastIndex = startIndex;
            for (int i = startIndex; i < arr.Length; i++)
            {
                byte currentByte = arr[i];
                if ((arr[i] == 115 || arr[i] == 83) && (arr[i + 1] == 67 || arr[i + 1] == 99) && (arr[i + 2] == 78 || arr[i + 2] == 110))
                {
                    return lastIndex;
                }
                lastIndex = i;
            }
            return lastIndex;
        }

        private static int GetByteIndexReverse(byte[] arr, int startIndex)
        {
            int lastIndex = startIndex;
            for (int i = startIndex; i >= 0; i--)
            {
                byte currentByte = arr[i];
                if (currentByte == 10)
                {
                    return lastIndex;
                }
                lastIndex = i;
            }
            return lastIndex;
        }

        private static int GetByteIndexrev(byte[] arr, int startIndex)
        {
            int lastIndex = startIndex;
            for (int i = startIndex; i >= 0; i--)
            {
                byte currentByte = arr[i];
                if (currentByte == 32 || currentByte == 10)
                {
                    return lastIndex;
                }
                lastIndex = i;
            }
            return lastIndex;
        }

        private static List<int> GetSubarrayIndexes(byte[] largerArray, byte[] subarray)
        {
            List<int> indexes = new List<int>();

            for (int i = 0; i <= largerArray.Length - subarray.Length; i++)
            {
                bool found = true;

                for (int j = 0; j < subarray.Length; j++)
                {
                    if (largerArray[i + j] != subarray[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    indexes.Add(i);
                }
            }
            return indexes;
        }

        /// <summary>
        /// GetColorNames using iText7
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="NPages"></param>
        /// <returns></returns>
        /// 
        private static bool GetColorNamesByitext7(byte[] FileByte, ref string[] colorNames, ref string errorMsg)
        {
            try
            {
                SpotColorKeys.Clear();
                List<string> spotcolornames = new List<string>();
                using (PdfReader reader = new PdfReader(new MemoryStream(FileByte)))
                {
                    using (PdfDocument pdfDoc = new PdfDocument(reader))
                    {
                        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                        {
                            PdfPage page = pdfDoc.GetPage(i);
                            PdfResources resources = page.GetResources();
                            PdfDictionary colorSpaces = resources.GetResource(PdfName.ColorSpace) as PdfDictionary;
                            if (colorSpaces == null) continue;
                            foreach (PdfName key in colorSpaces.KeySet())
                            {
                                PdfObject colorSpaceObj = colorSpaces.Get(key);
                                if (colorSpaceObj is PdfArray colorSpaceArray)
                                {
                                    if (colorSpaceArray.Get(0).ToString() == "/Separation")
                                    {
                                        PdfObject tintcolorname = colorSpaceArray.Get(1);
                                        if (tintcolorname is PdfName tintName)
                                        {
                                            string name = tintName.GetValue();
                                            if (!spotcolornames.Contains(name))
                                                spotcolornames.Add(name);
                                            if (!SpotColorKeys.ContainsKey(name))
                                                SpotColorKeys.Add(name, key.GetValue());
                                        }
                                    }
                                    else if (colorSpaceArray.Get(0).ToString() == "/DeviceN")
                                    {
                                        PdfArray deviceNarray = colorSpaceArray.Get(1) as PdfArray;
                                        foreach (var colorname in deviceNarray)
                                        {
                                            PdfObject tintObj = colorname;
                                            if (tintObj is PdfName tintName)
                                            {
                                                string name = tintName.GetValue();
                                                if (!spotcolornames.Contains(name))
                                                    spotcolornames.Add(name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        for (int j = 1; j < pdfDoc.GetNumberOfPdfObjects(); j++)
                        {
                            try
                            {
                                PdfObject obj = pdfDoc.GetPdfObject(j);
                                if (obj is PdfArray pdfarray)
                                {
                                    if (pdfarray.Count() > 0 && pdfarray.Get(0) is PdfName pname && pname.GetValue() == "Separation")
                                    {
                                        PdfName pdfname = pdfarray.Get(1) as PdfName;
                                        if (!spotcolornames.Contains(pdfname.GetValue()))
                                            spotcolornames.Add(pdfname.GetValue());
                                    }
                                }
                            }
                            catch
                            {

                            }
                        }
                    }
                }
                spotcolornames = AddCMYK(spotcolornames, "Cyan");
                spotcolornames = AddCMYK(spotcolornames, "Magenta");
                spotcolornames = AddCMYK(spotcolornames, "Yellow");
                spotcolornames = AddCMYK(spotcolornames, "Black");
                colorNames = spotcolornames.ToArray();
                return colorNames.Length > 0 ? true : false;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                Console.WriteLine(errorMsg);
                return false;
            }
        }

        private static List<string> AddCMYK(List<string> spotcolornames, string name)
        {
            if (!spotcolornames.Contains(name))
            {
                spotcolornames.Add(name);
            }
            return spotcolornames;
        }


        private static PdfStream UpdateStreamUsingKey(PdfStream contentStream, List<ColorKeyIndex> colorspaceKeys)
        {
            byte[] contentBytes = contentStream.GetBytes();
            string content = Encoding.UTF8.GetString(contentBytes);
            for (int i = 0; i < colorspaceKeys.Count; i++)
            {
                string str = colorspaceKeys[i].ColorKey + " ";
                byte[] removableArray = Encoding.UTF8.GetBytes(str);
                var indexes = GetSubarrayIndexes(contentBytes, removableArray);

                while (indexes.Count > 0)
                {
                    int colorindex = 0;
                    int a = GetByteIndex(contentBytes, indexes[0] + str.Length + 3);
                    for (int j = indexes[0] + str.Length + 3; j <= a; j++)
                    {
                        if (contentBytes[j] >= 49 && contentBytes[j] <= 57 && colorspaceKeys[i].Index == colorindex)
                        {
                            contentBytes[j] = 48;
                        }
                        else if (contentBytes[j] == 32)
                        {
                            colorindex++;
                        }
                    }
                    indexes.RemoveAt(0);
                }
            }
            contentStream.SetData(contentBytes);
            return contentStream;
        }
        private static PdfArray UpdatePdfarray(PdfArray arr, PdfArray colorSpaceDict, List<int> CmykRemoveIndex, List<String> colornames = null)
        {
            List<string> colorname = new List<string>();
            if (colorSpaceDict != null)
            {
                if (colorSpaceDict.Get(1) is PdfName colorspacename && colorspacename != null)
                {
                    colorname.Add(colorspacename.GetValue());
                }
                else if (colorSpaceDict.Get(1) is PdfArray colorspacearray && colorspacearray.Count() > 0)
                {
                    foreach (var cname in colorspacearray)
                    {
                        if (cname is PdfName name)
                            colorname.Add(name.GetValue());
                    }
                }
            }
            int rgbvalue = 1;
            for (int i = 0; i < colorname.Count; i++)
            {
                if (colorname[i] == "Black" && CmykRemoveIndex.Contains(0))
                    arr.Set(i, new PdfNumber(0));
                else if (colorname[i] == "Yellow" && CmykRemoveIndex.Contains(1))
                    arr.Set(i, new PdfNumber(0));
                else if (colorname[i] == "Magenta" && CmykRemoveIndex.Contains(2))
                    arr.Set(i, new PdfNumber(0));
                else if (colorname[i] == "Cyan" && CmykRemoveIndex.Contains(3))
                    arr.Set(i, new PdfNumber(0));
                else if (colorname[i] == "Red" && colornames != null && colornames.Contains(colorname[i]))
                    arr.Set(i, new PdfNumber(rgbvalue));
                else if (colorname[i] == "Green" && colornames != null && colornames.Contains(colorname[i]))
                    arr.Set(i, new PdfNumber(rgbvalue));
                else if (colorname[i] == "Blue" && colornames != null && colornames.Contains(colorname[i]))
                    arr.Set(i, new PdfNumber(rgbvalue));
                else if (colornames != null && colornames.Contains(colorname[i]))
                    arr.Set(i, new PdfNumber(0));
            }
            return arr;
        }

        public class ColorKeyIndex
        {
            public int Index { get; set; }
            public string ColorKey { get; set; }
        }
    }

}
