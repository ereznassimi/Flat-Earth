// AdjacencyForm.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace FlatEarth;

public partial class AdjacencyForm: Window
{
    private DataGrid AdjacencyGrid;
    private ObservableCollection<LocationNeighbor> LocationNeighbors { get; set; }
    private int[,] AdjacencyMatrix;

    public AdjacencyForm(): this(new List<Location>()) { }

    public AdjacencyForm(List<Location> selectedLocations)
    {
        InitializeComponent();

        this.LocationNeighbors = new ObservableCollection<LocationNeighbor>();
        this.AdjacencyMatrix = new int[selectedLocations.Count, selectedLocations.Count];

        for (int i = 0; i < selectedLocations.Count; ++i)
        {
            this.LocationNeighbors.Add(new LocationNeighbor(selectedLocations.Count)
            {
                Index = i,
                Number = i + 1,
                Name = selectedLocations[i].Title
            });
        }

        this.DataContext = this;
        if (this.AdjacencyGrid != null)
        {
            this.AdjacencyGrid.ItemsSource = this.LocationNeighbors;
            this.AdjacencyGrid.CurrentCellChanged += AdjacencyGrid_CurrentCellChanged;
        }

        this.GenerateMatrixGrid(selectedLocations.Count);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        this.AdjacencyGrid = this.FindControl<DataGrid>("CheckboxesGrid");
        this.FindControl<Button>("BackButton").Click += OnBackButtonClick;
        this.FindControl<Button>("DrawButton").Click += OnDrawButtonClick;
    }

    private void AdjacencyGrid_CurrentCellChanged(object? sender, EventArgs e)
    {
        if (this.AdjacencyGrid.CurrentColumn is DataGridColumn column &&
            this.AdjacencyGrid.SelectedItem is LocationNeighbor selectedRow)
        {
            int rowIndex = this.LocationNeighbors.IndexOf(selectedRow);
            int columnIndex = this.AdjacencyGrid.Columns.IndexOf(column) - 1; // offset by 1 due to label column

            if (columnIndex >= 0 && rowIndex == columnIndex)
            {
                selectedRow.Neighbors[columnIndex] = false;
                this.AdjacencyGrid.ItemsSource = this.LocationNeighbors;
            }
        }
    }

    private void GenerateMatrixGrid(int size)
    {
        this.AdjacencyGrid.Columns.Clear();

        // First column for row labels
        DataGridTemplateColumn labelColumn = new DataGridTemplateColumn
        {
            Header = "",
            Width = new DataGridLength(200), // Fixed or max width for wrapping
            CellTemplate = new FuncDataTemplate<LocationNeighbor>((row, _) =>
            {
                TextBlock numberText = new TextBlock
                {
                    Text = row.Number.ToString(),
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(5, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock nameText = new TextBlock
                {
                    Text = row.Name,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 150,
                    VerticalAlignment = VerticalAlignment.Center
                };

                StackPanel textPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = { numberText, nameText }
                };

                return new Grid
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = { textPanel }
                };
            })
        };
        this.AdjacencyGrid.Columns.Add(labelColumn);

        for (int i = 0; i < size; i++)
        {
            int columnIndex = i;
            DataGridTemplateColumn column = new DataGridTemplateColumn
            {
                Header = (i + 1).ToString(),
                CellTemplate = new FuncDataTemplate<LocationNeighbor>((row, _) =>
                {
                    int rowIndex = row.Index;

                    CheckBox checkBox = new CheckBox
                    {
                        IsChecked = row.Neighbors[columnIndex],
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    checkBox.Click += (s, e) =>
                    {
                        if (rowIndex == columnIndex)
                        {
                            checkBox.IsChecked = false;
                        }
                        else
                        {
                            row.Neighbors[columnIndex] = checkBox.IsChecked == true;
                        }
                    };

                    return new Grid { Children = { checkBox } };
                })
            };

            this.AdjacencyGrid.Columns.Add(column);
        }
    }

    private void OnBackButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }

    private void OnDrawButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        for (int i = 0; i < this.LocationNeighbors.Count; i++)
        {
            LocationNeighbor neighbor = this.LocationNeighbors[i];
            for (int j = 0; j < neighbor.Neighbors.Count; j++)
            {
                this.AdjacencyMatrix[i, j] = neighbor.Neighbors[j] ? 1 : 0;
            }
        }

        GraphWindow graphWindow = new GraphWindow(this.AdjacencyMatrix);
        graphWindow.ShowDialog(this);
    }
}
