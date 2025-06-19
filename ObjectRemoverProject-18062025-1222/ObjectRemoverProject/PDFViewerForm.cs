using PdfiumViewer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ObjectRemoverProject
{
    public partial class PDFViewerForm : Form
    {
        private ObjectManipulator objectManipulator;
        private PdfViewer pdfViewer;
        private PdfDocument pdfDocument;
        private string currentPdfPath;
        private string pdfPathForViewer;
        private string pdfPathForManipulator;

        public PDFViewerForm()
        {
            InitializeComponent();
            InitializePDFViewer();
            pdfPathForViewer = @"D:\PDFRemoval\DemoForViewer.pdf";
            pdfPathForManipulator = @"D:\PDFRemoval\DemoForManipulator.pdf";
            CheckAndCreateDirectory();
        }

        private void CheckAndCreateDirectory()
        {
            string primaryDict = string.Join("\\", pdfPathForViewer.Split('\\').Take(pdfPathForViewer.Split('\\').Length - 1));
            if(!Directory.Exists(primaryDict))
            {
                Directory.CreateDirectory(primaryDict);
            }
            primaryDict += "\\Temp";
            if (!Directory.Exists(primaryDict))
            {
                Directory.CreateDirectory(primaryDict);
            }
            

        }

        private void InitializePDFViewer()
        {
            pdfViewer = new PdfViewer()
            {
                Dock = DockStyle.Fill
            };
            
            pdfViewer.Renderer.MouseClick += RendererMouseClicked;
            ContainerPanel.Controls.Add(pdfViewer);
        }

        private void RendererMouseClicked(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left && RemoveObjectCheck.Checked)
            {
                float zoomFactor = (float)pdfViewer.Renderer.Zoom + 0f;
                var renderer = pdfViewer.Renderer;
                int pageNo = renderer.Page;
                var point = DeviceToPdfPoint( pageNo, e.Location);
                RemoveObjectAtCoordinates(currentPdfPath, point.X, point.Y, pageNo);
            }
        }

        private void RemoveObjectAtCoordinates(string inputPath, float pdfX, float pdfY, int selectedPage)
        {
            string temp = @"D:\PDFRemoval\Temp\Demo2.pdf";
            objectManipulator.RemoveObject(pdfX, pdfY, temp, selectedPage+1);

            File.WriteAllBytes(pdfPathForManipulator, File.ReadAllBytes(temp));
            LoadAndReloadPdf(pdfPathForViewer);
            objectManipulator.Dispose();
            objectManipulator = new ObjectManipulator(pdfPathForManipulator);
        }

        private PointF DeviceToPdfPoint( int pageIndex, Point devicePoint)
        {
            var pageSize = pdfViewer.Document.PageSizes[pageIndex];
            var renderSize = pdfViewer.Renderer.GetOuterBounds(pageIndex).Size;

            Point scrollOffset = pdfViewer.Renderer.AutoScrollOffset;

            double zoom = pdfViewer.Renderer.Zoom;

            float adjustedX = (devicePoint.X + scrollOffset.X) / (float)zoom;
            float adjustedY = (devicePoint.Y + scrollOffset.Y) / (float)zoom;

            Point scroll = pdfViewer.Renderer.AutoScrollOffset;


            PointF relativePoint = new PointF(
                devicePoint.X + scroll.X - pdfViewer.Renderer.GetOuterBounds(pageIndex).Right,
                devicePoint.Y + scroll.Y - pdfViewer.Renderer.GetOuterBounds(pageIndex).Top);


            float scaleX = pageSize.Width / (renderSize.Width * (float)zoom);
            float scaleY = pageSize.Height / (renderSize.Height * (float)zoom);


            PdfPoint PDFpoint = pdfViewer.Renderer.PointToPdf(devicePoint);

            float pdfX = PDFpoint.Location.X;
            float pdfY = PDFpoint.Location.Y;
            
            return new PointF(pdfX, pdfY);
        }


        private void LoadPdfBtnClicked(object sender, EventArgs e)
        {
            string currentPathCpy = currentPdfPath;
            if (pdfDocument != null)
            {
                pdfViewer.Document?.Dispose();
                pdfDocument?.Dispose();
                File.WriteAllBytes(pdfPathForViewer, File.ReadAllBytes(pdfPathForViewer));
            }
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF Files|*.pdf";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    currentPdfPath = openFileDialog.FileName;
                    if (File.Exists(pdfPathForViewer))
                        File.Delete(pdfPathForViewer);
                    File.Copy(currentPdfPath, pdfPathForViewer);
                    if (File.Exists(pdfPathForManipulator))
                        File.Delete(pdfPathForManipulator);
                    File.Copy(currentPdfPath, pdfPathForManipulator);
                    LoadAndReloadPdf(pdfPathForViewer);
                    objectManipulator?.Dispose();
                    objectManipulator = new ObjectManipulator(pdfPathForManipulator);
                }
            }

            //if (currentPdfPath == currentPathCpy)
            //    return;

            
        }

        private void LoadAndReloadPdf(string pdfPathForViewer)
        {
            if (pdfDocument != null)
            {
                pdfViewer.Document?.Dispose();
                pdfDocument?.Dispose();
                File.WriteAllBytes(pdfPathForViewer, File.ReadAllBytes(pdfPathForManipulator));
            }
            pdfDocument = PdfDocument.Load(pdfPathForViewer);
            pdfViewer.Document = pdfDocument;
        }
    }
}
