using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace FlatEarth;

public class GraphWindow: Window
{
    #region Members

    private int[,] LocationNeighbors;

    private int NodeCount;

    private readonly Canvas GraphCanvas = new Canvas
    {
        Background = Brushes.Gray,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch
    };

    private List<DrawableNode> DrawableNodes = new List<DrawableNode>();
    private List<DrawableEdge> DrawableEdges = new List<DrawableEdge>();

    private LayoutMode CurrentLayoutMode = LayoutMode.Ellipse;

    private List<List<int>> AllPaths;

    private int CurrentPathIndex = 0;
    private int TotalPaths = 0;

    private TextBox PathNumberBox;
    private TextBlock TotalPathTextBlock;
    private Button FastBackButton;
    private Button BackButton;
    private Button ForwardButton;
    private Button FastForwardButton;
    private StackPanel PathNavigationPanel;

    private ContextMenu SharedContextMenu = new ContextMenu();

    #endregion


    #region CTOR

    public GraphWindow(int[,] adjacencyMatrix)
    {
        this.LocationNeighbors = adjacencyMatrix;
        this.MinHeight = 800;
        this.Title = "Graph View";
        this.Content = CreateLayout();

        this.GraphCanvas.Background = Brushes.LightGray;

        this.Opened += (sender, e) => this.RecomputeLayout();
        this.GraphCanvas.SizeChanged += (_, _) => this.RecomputeLayout();
        this.GraphCanvas.PointerPressed += (sender, e) => this.OnMouseClick(sender, e);
    }

    #endregion


    #region UI Runtime Layout

    private Control MakeLegendNodeItem(IBrush color, string description)
    {
        StackPanel panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(10, 5, 0, 5)
        };

        // Draw a colored square
        Control swatch = new Rectangle
        {
            Width = 24,
            Height = 24,
            Fill = color,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        TextBlock label = new TextBlock
        {
            Text = description,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16
        };

        panel.Children.Add(swatch);
        panel.Children.Add(label);
        return panel;
    }

    private Control MakeLegendEdgeItem(IBrush color, string description, double[]? dashPattern = null)
    {
        StackPanel panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(10, 5, 0, 5)
        };

        // Draw a horizontal line
        Control swatch = new Canvas
        {
            Width = 24,
            Height = 24,
            Children =
            {
                new Line
                {
                    StartPoint = new Point(0, 12),
                    EndPoint = new Point(24, 12),
                    Stroke = color,
                    StrokeThickness = 3,
                    StrokeDashArray = (dashPattern != null) ?
                        new AvaloniaList<double>(dashPattern) : null
                }
            }
        };

        TextBlock label = new TextBlock
        {
            Text = description,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16
        };

        panel.Children.Add(swatch);
        panel.Children.Add(label);
        return panel;
    }

    private Control CreateLayout()
    {
        this.NodeCount = this.LocationNeighbors.GetLength(0);
        List<int>? bridgeNodes = GraphAlgorithms.FindBridgeNodes(this.LocationNeighbors);

        Grid layoutGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto), // Legend
                new ColumnDefinition(GridLength.Star)  // Graph
            }
        };

        // === Left Panel: Legend ===
        DockPanel legendPanel = new DockPanel
        {
            //Height = 700,
            Width = 280,
            Background = new SolidColorBrush(Color.Parse("#444444")),
            Margin = new Thickness(20)
        };

        // Content stack panel (everything else)
        StackPanel contentPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Top
        };

        contentPanel.Children.Add(new TextBlock
        {
            Text = "Node Types",
            FontWeight = FontWeight.Bold,
            FontSize = 18,
            Margin = new Thickness(10, 10, 0, 10)
        });

        contentPanel.Children.Add(MakeLegendNodeItem(Brushes.DarkGreen, "Single In-Degree"));
        contentPanel.Children.Add(MakeLegendNodeItem(Brushes.Red, "Zero In-Degree"));
        contentPanel.Children.Add(MakeLegendNodeItem(Brushes.Orange, "Bridge Node"));
        contentPanel.Children.Add(MakeLegendNodeItem(Brushes.Brown, "Dead-End"));
        contentPanel.Children.Add(MakeLegendNodeItem(Brushes.Black, "Default Node"));

        contentPanel.Children.Add(new Separator());

        contentPanel.Children.Add(new TextBlock
        {
            Text = "Edge Types",
            FontWeight = FontWeight.Bold,
            FontSize = 18,
            Margin = new Thickness(10, 10, 0, 10)
        });

        contentPanel.Children.Add(MakeLegendEdgeItem(Brushes.Gray, "Default Edge"));
        contentPanel.Children.Add(MakeLegendEdgeItem(Brushes.Blue, "Bidirectional Edge"));
        contentPanel.Children.Add(MakeLegendEdgeItem(
            Brushes.OrangeRed,
            "Redundant Edge",
            dashPattern: new double[] { 4, 4 }));
        contentPanel.Children.Add(MakeLegendEdgeItem(Brushes.Red, "From Zero In-Degree"));

        contentPanel.Children.Add(new Separator());

        contentPanel.Children.Add(new TextBlock
        {
            Text = "Layout Method",
            FontWeight = FontWeight.Bold,
            FontSize = 18,
            Margin = new Thickness(10, 10, 0, 10)
        });

        StackPanel radioPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(10, 0, 0, 0)
        };

        RadioButton ellipseRadio = new RadioButton
        {
            Content = "Ellipse Layout",
            GroupName = "LayoutGroup",
            IsChecked = true,
            Foreground = Brushes.White,
            FontSize = 16
        };

        ellipseRadio.Checked += (_, _) =>
        {
            this.CurrentLayoutMode = LayoutMode.Ellipse;
            this.RecomputeLayout();
        };

        RadioButton kkRadio = new RadioButton
        {
            Content = "Kamada-Kawai Layout",
            GroupName = "LayoutGroup",
            IsChecked = false,
            Foreground = Brushes.White,
            FontSize = 16
        };

        kkRadio.Checked += (_, _) =>
        {
            this.CurrentLayoutMode = LayoutMode.KamadaKawai;
            this.RecomputeLayout();
        };

        RadioButton forceDirectedRadio = new RadioButton
        {
            Content = "Force Directed Layout",
            GroupName = "LayoutGroup",
            IsChecked = false,
            Foreground = Brushes.White,
            FontSize = 16
        };

        forceDirectedRadio.Checked += (_, _) =>
        {
            this.CurrentLayoutMode = LayoutMode.ForceDirected;
            this.RecomputeLayout();
        };

        radioPanel.Children.Add(ellipseRadio);
        radioPanel.Children.Add(kkRadio);
        radioPanel.Children.Add(forceDirectedRadio);
        contentPanel.Children.Add(radioPanel);

        contentPanel.Children.Add(new Separator());

        this.PathNavigationPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(10),
            IsEnabled = false
        };

        TextBlock titleTextBlock = new TextBlock
        {
            Text = "All Paths",
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        };

        StackPanel navPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5
        };

        this.FastBackButton = new Button
        {
            Height = 40,
            Width = 30,
            Content = new Image
            {
                Source = new Bitmap("8665026_backward_fast_icon.png"),
                Width = 24,
                Height = 24
            }
        };
        this.FastBackButton.Click += (_, _) => this.GoToPath(0);

        this.BackButton = new Button
        {
            Height = 40,
            Width = 30,
            Content = new Image
            {
                Source = new Bitmap("8664982_backward_step_icon.png"),
                Width = 24,
                Height = 24
            }
        };
        this.BackButton.Click += (_, _) => this.GoToPath(this.CurrentPathIndex - 1);

        this.PathNumberBox = new TextBox
        {
            Width = 50,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = "1"
        };
        this.PathNumberBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(this.PathNumberBox.Text, out int val))
                {
                    this.GoToPath(val - 1);
                }
            }
        };
        this.PathNumberBox.LostFocus += (s, e) =>
        {
            if (int.TryParse(this.PathNumberBox.Text, out int val))
            {
                this.GoToPath(val - 1);
            }
        };

        this.ForwardButton = new Button
        {
            Height = 40,
            Width = 30,
            Content = new Image
            {
                Source = new Bitmap("8665527_forward_step_icon.png"),
                Width = 24,
                Height = 24
            }
        };
        this.ForwardButton.Click += (_, _) => this.GoToPath(this.CurrentPathIndex + 1);

        this.FastForwardButton = new Button
        {
            Height = 40,
            Width = 30,
            Content = new Image
            {
                Source = new Bitmap("8541927_fast_forward_icon.png"),
                Width = 24,
                Height = 24
            }
        };
        this.FastForwardButton.Click += (_, _) => this.GoToPath(this.AllPaths.Count - 1);

        this.TotalPathTextBlock = new TextBlock
        {
            Text = "/ 0",
            VerticalAlignment = VerticalAlignment.Center
        };

        navPanel.Children.Add(this.FastBackButton);
        navPanel.Children.Add(this.BackButton);
        navPanel.Children.Add(this.PathNumberBox);
        navPanel.Children.Add(this.TotalPathTextBlock);
        navPanel.Children.Add(this.ForwardButton);
        navPanel.Children.Add(this.FastForwardButton);

        this.PathNavigationPanel.Children.Add(titleTextBlock);
        this.PathNavigationPanel.Children.Add(navPanel);

        contentPanel.Children.Add(this.PathNavigationPanel);

        legendPanel.Children.Add(contentPanel);

        Grid.SetColumn(legendPanel, 0);
        layoutGrid.Children.Add(legendPanel);

        // === Right Panel: Graph Canvas ===
        this.GraphCanvas.Background = Brushes.Gray;
        Grid canvasWrapper = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        canvasWrapper.Children.Add(this.GraphCanvas);
        Grid.SetColumn(canvasWrapper, 1);
        layoutGrid.Children.Add(canvasWrapper);

        this.GraphCanvas.InvalidateVisual();

        return layoutGrid;
    }

    #endregion


    #region Helpers

    private void RecomputeLayout()
    {
        this.ClassifyNodesAndEdges();
        this.DrawNodesAndEdges();
    }

    private bool TryFindNode(Point point, out int depotIndex)
    {
        depotIndex = -1;
        bool foundNode = false;
        for (int i = 0; i < this.NodeCount; i++)
        {
            if (this.DrawableNodes[i].Contains(point.X, point.Y))
            {
                foundNode = true;
                depotIndex = i;
                break;
            }
        }

        return foundNode;
    }

    private bool CanCoverAllNodes()
    {
        int zeroInCount = this.DrawableNodes.Count(node => node.IsZeroInDegree);
        int zeroOutCount = this.DrawableNodes.Count(node => node.IsDeadEnd);

        if ((zeroInCount > 1) || (zeroOutCount > 1))
        {
            this.MessageBox(
                "Error",
                "Graph has too many start or end points.\n\n" +
                    "Please ensure at most one start node (zero in-degree)\n" +
                    "and one end node (zero out-degree).");

            return false;
        }

        return true;
    }

    private long[,] ConvertToDistanceMatrix(int[,] locationNeighbors)
    {
        long[,] distMatrix = new long[this.NodeCount, this.NodeCount];

        Enumerable.Range(0, this.NodeCount).ToList().ForEach(i =>
            Enumerable.Range(0, this.NodeCount).ToList().ForEach(j =>
                distMatrix[i, j] = locationNeighbors[i, j] == 0 ? long.MaxValue : 1
            )
        );

        return distMatrix;
    }

    private bool IsValidRoute(IEnumerable<int> route)
    {
        if ((route == null) || (route.Count() != this.NodeCount))
        {
            this.MessageBox(
                "Error",
                "Impossible to cover all nodes starting this node!");

            return false;
        }

        for (int i = 0; i < this.NodeCount - 1; ++i)
        {
            if (this.LocationNeighbors[route.ElementAt(i), route.ElementAt(i + 1)] == 0)
            {
                this.MessageBox(
                    "Error",
                    "Impossible to cover all nodes starting this node!");

                return false;
            }
        }

        return true;
    }

    private bool AreValidRoutes(IEnumerable<IEnumerable<int>> routes)
    {
        if ((routes == null) || (routes.Count() == 0))
        {
            this.MessageBox(
                "Error",
                "Impossible to cover all nodes!");

            return false;
        }

        foreach (IEnumerable<int> route in routes)
        {
            if (!this.IsValidRoute(route))
            {
                return false;
            }
        }

        return true;
    }

    private void ConvertZeroInNode(int nodeIndex)
    {
        if (this.DrawableNodes[nodeIndex].IsZeroInDegree)
        {
            for (int k = 0; k < this.NodeCount; k++)
            {
                if (this.LocationNeighbors[nodeIndex, k] == 1)
                {
                    this.LocationNeighbors[k, nodeIndex] = 1;
                }
            }
        }
    }

    private void ConvertZeroInNodes()
    {
        for (int i = 0; i < this.NodeCount; ++i)
        {
            this.ConvertZeroInNode(i);
        }
    }

    #endregion


    #region Classification

    private void ClassifyNodesAndEdges()
    {
        this.ClassifyNodes();
        this.ClassifyEdges();
    }

    private void ClassifyNodes()
    {
        this.DrawableNodes.Clear();

        (double X, double Y)[] positions = this.PositionByLayoutMode();

        List<int>? bridgeNodes = GraphAlgorithms.FindBridgeNodes(this.LocationNeighbors);
        for (int i = 0; i < this.NodeCount; i++)
        {
            DrawableNodes.Add(new DrawableNode()
            {
                ID = i + 1,
                Position = positions[i],
                IsBridge = bridgeNodes != null && bridgeNodes.Contains(i),

                IsZeroInDegree = !Enumerable
                    .Range(0, this.NodeCount)
                    .Any(row => (row != i) && (this.LocationNeighbors[row, i] == 1)),

                IsSingleInDegree = Enumerable
                    .Range(0, this.NodeCount)
                    .Count(row => (row != i) && (this.LocationNeighbors[row, i] == 1)) == 1,

                IsDeadEnd = !Enumerable
                    .Range(0, this.NodeCount)
                    .Any(col => (col != i) && (this.LocationNeighbors[i, col] == 1))
            });
        }
    }


    private void ClassifyEdges()
    {
        this.DrawableEdges.Clear();

        bool[,] redundantEdges = GraphAlgorithms.FindRedundantEdges(this.LocationNeighbors);

        for (int i = 0; i < this.NodeCount; i++)
        {
            for (int j = 0; j < this.NodeCount; j++)
            {
                if (this.LocationNeighbors[i, j] == 1)
                {
                    this.ClassifyEdge(i, j);
                    this.DrawableEdges[DrawableEdges.Count - 1].IsRedundant = redundantEdges[i, j];
                }
            }
        }
    }

    private void ClassifyEdge(int i, int j)
    {
        (double X, double Y) start = this.DrawableNodes[i].Position;
        (double X, double Y) end = this.DrawableNodes[j].Position;

        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        double unitX = dx / length;
        double unitY = dy / length;

        double offset = DrawableNode.NodeRadius;
        Point adjustedStart = new Point(start.X + unitX * offset, start.Y + unitY * offset);
        Point adjustedEnd = new Point(end.X - unitX * offset, end.Y - unitY * offset);

        double arrowSize = 15.0;
        double arrowAngle = Math.PI / 8;
        double angle = Math.Atan2(dy, dx);

        Point leftWing = new Point(
            adjustedEnd.X - arrowSize * Math.Cos(angle - arrowAngle),
            adjustedEnd.Y - arrowSize * Math.Sin(angle - arrowAngle)
        );
        Point rightWing = new Point(
            adjustedEnd.X - arrowSize * Math.Cos(angle + arrowAngle),
            adjustedEnd.Y - arrowSize * Math.Sin(angle + arrowAngle)
        );

        Polygon arrowHead = new Polygon
        {
            Points = new Points { adjustedEnd, leftWing, rightWing }
        };

        this.DrawableEdges.Add(new DrawableEdge()
        {
            Start = adjustedStart,
            End = adjustedEnd,
            ArrowHead = arrowHead,
            IsBidirectional = (this.LocationNeighbors[j, i] == 1),
            IsFromZeroInDegree = this.DrawableNodes[i].IsZeroInDegree
        });
    }

    #endregion


    #region Positioning

    private (double X, double Y)[] PositionByLayoutMode()
    {
        (double X, double Y)[] positions = null;
        switch (this.CurrentLayoutMode)
        {
            case LayoutMode.Ellipse:
                positions = this.PositionsByEllipse();
                break;

            case LayoutMode.KamadaKawai:
                positions = this.PositionsByKamadaKawai();
                break;

            case LayoutMode.ForceDirected:
                positions = this.PositionsByForceDirected();
                break;

            default:
                positions = this.PositionsByEllipse();
                break;
        }

        return positions;
    }

    private (double X, double Y)[] PositionsByKamadaKawai()
    {
        double[,] shortestPaths = GraphAlgorithms.ComputeShortestPaths(this.LocationNeighbors);

        double width = GraphCanvas.Bounds.Width;
        double height = GraphCanvas.Bounds.Height;

        (double X, double Y)[] positions = GraphAlgorithms.ComputeKamadaKawaiPositions(
            shortestPaths, (int)width, (int)height);

        return positions;
    }

    private (double X, double Y)[] PositionsByEllipse()
    {
        (double X, double Y)[] positions = new (double X, double Y)[this.NodeCount];

        // Circular Layout
        double width = GraphCanvas.Bounds.Width;
        double height = GraphCanvas.Bounds.Height;

        double centerX = width / 2;
        double centerY = height / 2;
        double radius = Math.Min(width, height) / 2.7;

        for (int i = 0; i < this.NodeCount; i++)
        {
            double angle = 2 * Math.PI * i / this.NodeCount;
            positions[i].X = centerX + radius * 1.2 * Math.Cos(angle);
            positions[i].Y = centerY + radius * 0.8 * Math.Sin(angle);
        }

        return positions;
    }

    private (double X, double Y)[] PositionsByForceDirected()
    {
        const double padding = 40; // Space around borders to avoid clipping

        int width = (int)this.GraphCanvas.Bounds.Width;
        int height = (int)this.GraphCanvas.Bounds.Height;

        Random rand = new Random();

        // Initial random positions
        (double X, double Y)[] positions = new (double X, double Y)[NodeCount];
        for (int i = 0; i < NodeCount; i++)
        {
            positions[i] = (rand.NextDouble() * width, rand.NextDouble() * height);
        }

        double area = width * height;
        double k = Math.Sqrt(area / NodeCount);
        double temperature = width / 10;
        int iterations = 100;

        for (int iter = 0; iter < iterations; iter++)
        {
            // Repulsive forces
            (double X, double Y)[] disp = new (double X, double Y)[NodeCount];
            for (int i = 0; i < NodeCount; i++)
            {
                disp[i] = (0, 0);
                for (int j = 0; j < NodeCount; j++)
                {
                    if (i == j)
                        continue;

                    double dx = positions[i].X - positions[j].X;
                    double dy = positions[i].Y - positions[j].Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy) + 0.01;
                    double repulse = k * k / dist;

                    disp[i].X += (dx / dist) * repulse;
                    disp[i].Y += (dy / dist) * repulse;
                }
            }

            // Attractive forces
            for (int i = 0; i < NodeCount; i++)
            {
                for (int j = 0; j < NodeCount; j++)
                {
                    if (this.LocationNeighbors[i, j] == 1 || this.LocationNeighbors[j, i] == 1)
                    {
                        double dx = positions[i].X - positions[j].X;
                        double dy = positions[i].Y - positions[j].Y;
                        double dist = Math.Sqrt(dx * dx + dy * dy) + 0.01;
                        double attract = (dist * dist) / k;

                        double fx = (dx / dist) * attract;
                        double fy = (dy / dist) * attract;

                        disp[i].X -= fx;
                        disp[i].Y -= fy;
                        disp[j].X += fx;
                        disp[j].Y += fy;
                    }
                }
            }

            // Apply displacement and cool down
            for (int i = 0; i < NodeCount; i++)
            {
                double dx = disp[i].X;
                double dy = disp[i].Y;
                double dispLength = Math.Sqrt(dx * dx + dy * dy);
                if (dispLength > 0)
                {
                    dx = dx / dispLength * Math.Min(dispLength, temperature);
                    dy = dy / dispLength * Math.Min(dispLength, temperature);
                }

                positions[i].X = Math.Clamp(positions[i].X + dx, padding, width - padding);
                positions[i].Y = Math.Clamp(positions[i].Y + dy, padding, height - padding);
            }

            temperature *= 0.95;
        }

        return positions;
    }

    #endregion


    #region Draw

    private void DrawNodesAndEdges()
    {
        this.GraphCanvas.Children.Clear();
        this.DrawEdges();
        this.DrawNodes();
    }

    private void DrawNodes()
    {
        for (int i = 0; i < this.NodeCount; i++)
        {
            Ellipse circle = new Ellipse
            {
                Width = DrawableNode.NodeRadius * 2,
                Height = DrawableNode.NodeRadius * 2,
                Fill = this.DrawableNodes[i].Color(),
                Stroke = Brushes.Black,
                StrokeThickness = 4
            };
            Canvas.SetLeft(circle, this.DrawableNodes[i].Position.X - DrawableNode.NodeRadius);
            Canvas.SetTop(circle, this.DrawableNodes[i].Position.Y - DrawableNode.NodeRadius);
            this.GraphCanvas.Children.Add(circle);

            TextBlock label = new TextBlock
            {
                Text = DrawableNodes[i].ID.ToString(),
                Foreground = Brushes.White,
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(label, DrawableNodes[i].Position.X - DrawableNode.NodeRadius / 2);
            Canvas.SetTop(label, DrawableNodes[i].Position.Y - DrawableNode.NodeRadius / 2);
            this.GraphCanvas.Children.Add(label);
        }
    }

    private void DrawEdges()
    {
        for (int i = 0; i < this.DrawableEdges.Count; i++)
        {
            DrawableEdge edge = this.DrawableEdges[i];
            Line line = new Line
            {
                StartPoint = edge.Start,
                EndPoint = edge.End,
                Stroke = edge.IsRedundant ? Brushes.OrangeRed :
                    (edge.IsBidirectional ? Brushes.Blue :
                    (edge.IsFromZeroInDegree ? Brushes.Red :
                    Brushes.Gray)),

                StrokeThickness = edge.IsRedundant ? 2 : (edge.IsBidirectional ? 4 : 2),
                StrokeDashArray = edge.IsRedundant ? new AvaloniaList<double> { 4, 4 } : null
            };
            this.GraphCanvas.Children.Add(line);

            // Draw arrowhead
            Polygon arrowHead = edge.ArrowHead;
            arrowHead.Fill = edge.IsBidirectional ? Brushes.Blue : Brushes.Gray;
            this.GraphCanvas.Children.Add(arrowHead);
        }
    }

    private async void ShowBestRouteFromNode(int depotIndex)
    {
        if (!this.CanCoverAllNodes())
            return;

        long[,] distanceMatrix = this.ConvertToDistanceMatrix(this.LocationNeighbors);
        IEnumerable<int> route =
            await new GoogleOrToolsRouteExplorer().GetRouteToCoverAll(distanceMatrix, depotIndex);

        if (!this.IsValidRoute(route))
            return;

        this.DrawableEdges.Clear();
        foreach (var (from, to) in route.Zip(route.Skip(1)))
            this.ClassifyEdge(from, to);

        this.DrawNodesAndEdges();
    }

    private async void ShowAllRoutes(int? depotIndex = null)
    {
        if (!this.CanCoverAllNodes())
            return;

        this.AllPaths = GraphAlgorithms.GetAllPaths(
            this.LocationNeighbors,
            this.NodeCount,
            depotIndex);

        if (!this.AreValidRoutes(this.AllPaths))
            return;

        this.TotalPaths = this.AllPaths.Count;
        this.TotalPathTextBlock.Text = $"/ {this.TotalPaths}";
        this.PathNavigationPanel.IsEnabled = this.TotalPaths > 0;

        this.GoToPath(0);
    }

    private void GoToPath(int newIndex)
    {
        if (this.TotalPaths == 0)
            return;

        newIndex = Math.Clamp(newIndex, 0, this.TotalPaths - 1);
        this.CurrentPathIndex = newIndex;

        this.PathNumberBox.Text = (this.CurrentPathIndex + 1).ToString();

        this.DrawableEdges.Clear();
        IEnumerable<int> route = this.AllPaths[this.CurrentPathIndex];
        foreach (var (from, to) in route.Zip(route.Skip(1)))
            this.ClassifyEdge(from, to);

        this.DrawNodesAndEdges();
    }

    #endregion


    #region Events

    private async void OnMouseClick(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(GraphCanvas).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed)
            return;

        Point point = e.GetPosition(GraphCanvas);

        Border anchor = new Border
        {
            Width = 1,
            Height = 1,
            Background = Brushes.Transparent // Invisible but hit-testable
        };

        Canvas.SetLeft(anchor, point.X);
        Canvas.SetTop(anchor, point.Y);
        GraphCanvas.Children.Add(anchor); // Add it temporarily

        if (this.SharedContextMenu.IsOpen)
        {
            this.SharedContextMenu.Close();
        }

        this.SharedContextMenu.Items.Clear();

        int depotIndex;
        if (this.TryFindNode(point, out depotIndex))
        {
            //this.SharedContextMenu.Items.Add(new MenuItem
            //{
            //    Header = $"Best Route from {depotIndex + 1}",
            //    Command = new DelegateCommand(_ =>
            //        this.ShowBestRouteFromNode(depotIndex))
            //});

            this.SharedContextMenu.Items.Add(new MenuItem
            {
                Header = $"All Routes from {depotIndex + 1}",
                Command = new DelegateCommand(_ => this.ShowAllRoutes(depotIndex))
            });

            if (this.DrawableNodes[depotIndex].IsZeroInDegree)
            {
                this.SharedContextMenu.Items.Add(new MenuItem
                {
                    Header = $"Convert zero-in node {depotIndex + 1}",
                    Command = new DelegateCommand(_ =>
                    {
                        this.ConvertZeroInNode(depotIndex);
                        this.RecomputeLayout();
                    })
                });
            }
        }
        else
        {
            this.SharedContextMenu.Items.Add(new MenuItem
            {
                Header = "All Possible Routes",
                Command = new DelegateCommand(_ => this.ShowAllRoutes())
            });

            this.SharedContextMenu.Items.Add(new MenuItem
            {
                Header = "Convert zero-in nodes",
                Command = new DelegateCommand(_ =>
                {
                    this.ConvertZeroInNodes();
                    this.RecomputeLayout();
                })
            });
        }

        this.SharedContextMenu.PlacementTarget = anchor;
        this.SharedContextMenu.PlacementMode = PlacementMode.Pointer;
        this.SharedContextMenu.Closed += (_, __) => GraphCanvas.Children.Remove(anchor);

        await Task.Delay(1);
        this.SharedContextMenu.Open(anchor);
    }

    #endregion
}
