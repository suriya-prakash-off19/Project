using System.Collections.Generic;

namespace ObjectRemoverProject
{
    public class PDFContents
    {
        public int ContentArrayIndex { get; private set; }

        public List<string> GraphicsStateList { get; private set; }

        public PDFContents(int index)
        {
            ContentArrayIndex = index;
            GraphicsStateList = new List<string>();
        }
    }
}
