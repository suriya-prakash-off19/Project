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
        private PdfObjectHandler objectManipulator;
        private PdfViewer pdfViewer;
        private PdfDocument pdfDocument;
        private string currentPdfPath;
        private string tempPath;

        public PDFViewerForm()
        {
            InitializeComponent();
            InitializePDFViewer();
            tempPath = @"D:\PDFRemoval\Demo.pdf";
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
            pdfDocument?.Dispose();
            byte[] memoryByte =  objectManipulator.RemoveObject(pdfX, pdfY, selectedPage+1);
            LoadAndReloadPdf(memoryByte);
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
            pdfViewer.Document?.Dispose();
            pdfDocument?.Dispose();
            
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF Files|*.pdf";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    currentPdfPath = openFileDialog.FileName;
                    var temp = File.ReadAllBytes(currentPdfPath);
                    string destinPath = currentPdfPath;
                    pdfDocument?.Dispose();
                    objectManipulator = new PdfObjectHandler(temp);
                    LoadAndReloadPdf(temp);
                }
            }
            
        }

        private void LoadAndReloadPdf(byte[] temp)
        {
            var dispRectangle = pdfViewer.Renderer.DisplayRectangle;
            Point ZoomFactor = new Point((int)dispRectangle.X, (int)dispRectangle.Y);
            if (pdfDocument != null)
            {
                pdfViewer.Document?.Dispose();
                pdfDocument?.Dispose();
            }
            pdfDocument = PdfDocument.Load(new MemoryStream(temp));
            pdfViewer.Document = pdfDocument;
            pdfViewer.Renderer.SetBounds(dispRectangle.X, dispRectangle.Y, dispRectangle.Width, dispRectangle.Height);
            pdfViewer.Renderer.SetDisplayRectLocation(ZoomFactor);

        }

        private void ResetBtnClicked(object sender, EventArgs e)
        {
            pdfDocument?.Dispose();
            objectManipulator?.ResetDocument();
            //LoadAndReloadPdf();
        }
    }
}
