using HalconRegionEditor;
using System;
using System.Windows.Forms;
using HalconDotNet;
using System.IO;
using static ObjectRemoverProject.PdfProcessor;
using System.Drawing;
using System.Threading;
using HalconEditorTools;
using System.Collections.Generic;

namespace ObjectRemoverProject
{
    public partial class ObjectRemovalForm : Form
    {
        private string selectedPdfPath;
        string basePath = Path.Combine(@"D:\", "PdfLibraryTestReport");
        private string tempFolder = "Temp";
        private string outfileName = "";
        private string pdfPath = @"D:\Demo2.pdf";
        private string pdfDemoPath = @"D:\Demo.pdf";
        private int pgNo = 1;
        private int selectedPage = 1;
        private int noOfRectangle;
        private HImage displayImage;
        private ObjectManipulator objectManipulator;
        HImage[] hImageArr = null;
        public ObjectRemovalForm()
        {
            noOfRectangle = 0;
            InitializeComponent();
            tempFolder = @"D:\PDF";
            ImageDisplayer = new HWindowControl()
            {
                Dock = DockStyle.Fill,
            };
            regionEditor = new RegionEditor((HWindowControl)ImageDisplayer)
            {
                TrackerIsVisible = false
            };

            this.MainMenuStrip = null;
            this.ContextMenuStrip = null;
            ControlContainer.Controls.Add(ImageDisplayer);
            ImageDisplayer.HMouseDown += ImageDisplayer_HMouseDown;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }



        private void DrawRectangle(List<iText.Kernel.Geom.Rectangle> temp)
        {
            displayImage.GetImageSize(out HTuple imageWidth, out HTuple imageHeight);
            objectManipulator.GetHeightAndWidthOfPage(pgNo, out float pageWidth, out float pageHeight);
            Random random = new Random();
            int count = 0;
            foreach (var rect in temp)
            {
                count++;
                double top = imageHeight - imageHeight *(rect.GetTop()/pageHeight);
                double bottom =imageHeight -  imageHeight*(rect.GetBottom()/pageHeight);
                double left = imageWidth*(rect.GetLeft()/pageWidth);
                double rigth = imageWidth*(rect.GetRight()/ pageWidth);
                noOfRectangle++;
                regionEditor.AddRegion("Rect", new NormalRectangle()
                {
                    ID = noOfRectangle.ToString(),
                    IsEditable = false,
                    RegionColor = Color.FromArgb(random.Next(20,200),random.Next(20,200),random.Next(20,200)),
                    TopLeftPoint = new GoLibrary.PointRC(top, left),
                    BottomRightPoint = new GoLibrary.PointRC(bottom, rigth)
                });
            }
            
        }

        private void ImageDisplayer_HMouseDown(object sender, HMouseEventArgs e)
        {
            if (RemoveObj.Checked)
            {
                Thread.Sleep(100);
                displayImage.GetImageSize(out HTuple imageWidth, out HTuple imageHeight);
                objectManipulator.GetHeightAndWidthOfPage(pgNo, out float pageWidth, out float pageHeight);
                float pointX = (float)e.X;
                float pointY = (float)e.Y;
                float pagePointX = (float)((pageWidth) * (pointX / imageWidth.D));
                float pagePointY = pageHeight - (float)((pageHeight) * (pointY / imageHeight.D));
                objectManipulator.RemoveObject(pagePointX, pagePointY, pdfDemoPath, selectedPage);
                byte[] pdfMemory = File.ReadAllBytes(pdfDemoPath);
                selectedPdfPath = pdfPath;
                if (File.Exists(pdfPath))
                    File.Delete(pdfPath);
                File.Copy(pdfDemoPath,pdfPath);
                objectManipulator = new ObjectManipulator(pdfPath);
                RenderPDFtoImage(pdfMemory);
            }
           
        }

        private HWindowControl ImageDisplayer;
        private RegionEditor regionEditor;

        private void selectbtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "PDF Files|*.pdf";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                selectedPdfPath = openFileDialog.FileName;

                objectManipulator = new ObjectManipulator(selectedPdfPath);
                byte[] pdfMemory = File.ReadAllBytes(selectedPdfPath);
                RemoveAllRectangle();
                RenderPDFtoImage(pdfMemory);

            }
        }

        private void RenderPDFtoImage(byte[] pdfMemory, int dpi = 600, int pgNo = 1, RenderTool renderTool = RenderTool.GhostScript)
        {
            outfileName = Path.Combine(tempFolder, "RenderedOutput_" + dpi.ToString() + "dpi.png");
            string errorMsg = "";
            string outputPath = "";
            try
            {
                Directory.CreateDirectory(@"D:\PDF");
                double startTime = HSystem.CountSeconds();

                GetPdfImagePath(pdfMemory, dpi, renderTool, true, pgNo, ref outputPath, ref errorMsg);
                string[] pagePaths = outputPath.Split('|');
                displayImage = new HImage(pagePaths[selectedPage - 1]);
                ImageDisplayer.HalconWindow.DispImage(displayImage);
                displayImage.WriteImage("png", 0, outfileName);


                GetPdfImage(pdfMemory, dpi, renderTool, true, pgNo, ref hImageArr, ref errorMsg);
                displayImage = hImageArr[selectedPage - 1];

                double endTime = HSystem.CountSeconds();


                if (displayImage != null)
                {
                    displayImage.WriteImage("png", 0, outfileName);
                    HImage image = new HImage(outfileName);
                    regionEditor.defaultHandler.DisplayImage(image);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Library error: " + errorMsg);
                Console.WriteLine(ex.Message);
            }
        }

        private void ControlContainer_Paint(object sender, EventArgs e)
        {
            objectManipulator.RemoveAllObjects(@"D:\Demo.pdf");
            byte[] pdfMemory = File.ReadAllBytes(@"D:\Demo.pdf");
            if (File.Exists(@"D:\Demo2.pdf"))
                File.Delete(@"D:\Demo2.pdf");
            File.Copy(@"D:\Demo.pdf", @"D:\Demo2.pdf");
            List<iText.Kernel.Geom.Rectangle> temp = objectManipulator.SelectedRectangles;
            objectManipulator = new ObjectManipulator(@"D:\Demo2.pdf");
            RenderPDFtoImage(pdfMemory);
            RemoveAllRectangle();
            DrawRectangle(temp);
        }

        private void RemoveAllRectangle()
        {
            regionEditor.ClearLists("Rect");
            noOfRectangle = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            objectManipulator.FetchAllObjects();
            List<iText.Kernel.Geom.Rectangle> temp = objectManipulator.SelectedRectangles;
            byte[] pdfMemory = File.ReadAllBytes(selectedPdfPath);
            RenderPDFtoImage(pdfMemory);
            RemoveAllRectangle();
            DrawRectangle(temp);
        }
    }
}
