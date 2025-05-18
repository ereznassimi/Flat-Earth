using Avalonia.Controls.Shapes;


namespace FlatEarth
{
    public class DrawableEdge
    {
        public Avalonia.Point Start { get; set; }
        public Avalonia.Point End { get; set; }
        public Polygon ArrowHead { get; set; }
        public bool IsBidirectional { get; set; }
        public bool IsRedundant { get; set; }
        public bool IsFromZeroInDegree { get; set; }
    }
}
