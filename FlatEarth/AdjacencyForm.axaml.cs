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

public partial class AdjacencyForm : Window
{
    private DataGrid adjacencyGrid;
    private ObservableCollection<LocationNeighbor> locationNeighbors { get; set; }
    private int[,] adjacencyMatrix;

    public AdjacencyForm() : this(new List<Location>()) { }

    public AdjacencyForm(List<Location> selectedLocations)
    {
        InitializeComponent();

        locationNeighbors = new ObservableCollection<LocationNeighbor>();
        adjacencyMatrix = new int[selectedLocations.Count, selectedLocations.Count];

        for (int i = 0; i < selectedLocations.Count; i++)
        {
            locationNeighbors.Add(new LocationNeighbor(selectedLocations.Count)
            {
                Index = i,
                Number = i + 1,
                Name = selectedLocations[i].Title
            });
        }

        DataContext = this;
        if (adjacencyGrid != null)
        {
            adjacencyGrid.ItemsSource = locationNeighbors;
            adjacencyGrid.CurrentCellChanged += AdjacencyGrid_CurrentCellChanged;
        }

        GenerateMatrixGrid(selectedLocations.Count);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        adjacencyGrid = this.FindControl<DataGrid>("AdjacencyGrid");
        this.FindControl<Button>("BackButton").Click += OnBackButtonClick;
        this.FindControl<Button>("DrawButton").Click += OnDrawButtonClick;
    }

    private void AdjacencyGrid_CurrentCellChanged(object? sender, EventArgs e)
    {
        if (adjacencyGrid.CurrentColumn is DataGridColumn column &&
            adjacencyGrid.SelectedItem is LocationNeighbor selectedRow)
        {
            int rowIndex = locationNeighbors.IndexOf(selectedRow);
            int columnIndex = adjacencyGrid.Columns.IndexOf(column) - 1; // offset by 1 due to label column

            if (columnIndex >= 0 && rowIndex == columnIndex)
            {
                selectedRow.Neighbors[columnIndex] = false;
                adjacencyGrid.ItemsSource = null;
                adjacencyGrid.ItemsSource = locationNeighbors;
            }
        }
    }

    private void GenerateMatrixGrid(int size)
    {
        adjacencyGrid.Columns.Clear();

        // First column for row labels
        DataGridTemplateColumn labelColumn = new DataGridTemplateColumn
        {
            Header = "",
            Width = new DataGridLength(200), // Fixed or max width for wrapping
            CellTemplate = new FuncDataTemplate<LocationNeighbor>((row, _) =>
            {
                var numberText = new TextBlock
                {
                    Text = row.Number.ToString(),
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Margin = new Thickness(5, 0, 5, 0),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var nameText = new TextBlock
                {
                    Text = row.Name,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 150,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var textPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Children = { numberText, nameText }
                };

                return new Grid
                {
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Children = { textPanel }
                };
            })
        };
        adjacencyGrid.Columns.Add(labelColumn);


        for (int i = 0; i < size; i++)
        {
            int columnIndex = i; // capture loop var
            var column = new DataGridTemplateColumn
            {
                Header = (i + 1).ToString(),
                CellTemplate = new FuncDataTemplate<LocationNeighbor>((row, _) =>
                {
                    int rowIndex = row.Index;

                    var checkBox = new CheckBox
                    {
                        IsChecked = row.Neighbors[columnIndex],
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
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

            adjacencyGrid.Columns.Add(column);
        }
    }

    private void OnBackButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OnDrawButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        for (int i = 0; i < locationNeighbors.Count; i++)
        {
            LocationNeighbor neighbor = locationNeighbors[i];
            for (int j = 0; j < neighbor.Neighbors.Count; j++)
            {
                adjacencyMatrix[i, j] = neighbor.Neighbors[j] ? 1 : 0;
            }
        }

        GraphWindow graphWindow = new GraphWindow(adjacencyMatrix);
        graphWindow.ShowDialog(this);
    }
}
