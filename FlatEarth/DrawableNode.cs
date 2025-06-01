using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;


namespace FlatEarth;

public class DrawableNode
{
    public static readonly int NodeRadius = 26;

    public int ID { get; set; }
    public (double X, double Y) Position { get; set; }
    public bool IsZeroInDegree { get; set; } = false;
    public bool IsSingleInDegree { get; set; } = false;
    public bool IsBridge { get; set; } = false;
    public bool IsDeadEnd { get; set; } = false;
    public Ellipse? Visual { get; set; }
    public TextBlock? Label { get; set; }
    
    public IBrush Color()
    {
        if (this.IsSingleInDegree)
            return Brushes.DarkGreen;

        if (this.IsZeroInDegree)
            return Brushes.Red;

        if (this.IsBridge)
            return Brushes.Orange;

        if (this.IsDeadEnd)
            return Brushes.Brown;

        return Brushes.Black;
    }
    
    public bool Contains(double x, double y)
    {
        return x >= Position.X - DrawableNode.NodeRadius && x <= Position.X + DrawableNode.NodeRadius &&
               y >= Position.Y - DrawableNode.NodeRadius && y <= Position.Y + DrawableNode.NodeRadius;
    }
}
