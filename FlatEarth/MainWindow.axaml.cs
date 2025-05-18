using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;


namespace FlatEarth;


public partial class MainWindow: Window
{
    public ObservableCollection<Location> Locations { get; set; } = new();

    public MainWindow()
    {
        this.InitializeComponent();
        this.DataContext = this;
        this.LoadLocations();
        this.Icon = new WindowIcon("102558_map_continent_earth_globe_world_icon_trR_icon.ico");
    }

    private void LoadLocations()
    {
        if (File.Exists("Locations.txt"))
        {
            string[] lines = File.ReadAllLines("Locations.txt");
            int id = 1;
            foreach (string? line in lines)
            {
                Locations.Add(new Location { Id = id++, Title = line, IsSelected = false });
            }
        }
    }

    private void OnNextButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        List<Location> selectedItems = Locations.Where(l => l.IsSelected).ToList();
        if (selectedItems.Any())
        {
            AdjacencyForm adjacencyForm = new AdjacencyForm(selectedItems);
            adjacencyForm.ShowDialog(this);
        }
    }
}