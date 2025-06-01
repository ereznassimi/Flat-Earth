using System.Collections.ObjectModel;
using System.ComponentModel;


namespace FlatEarth;

public class LocationNeighbor: INotifyPropertyChanged
{
    public int Index { get; set; }
    public int Number { get; set; }
    public string Name { get; set; }

    private ObservableCollection<bool> neighbors;
    public ObservableCollection<bool> Neighbors
    {
        get => this.neighbors;
        set
        {
            if (this.neighbors != value)
            {
                this.neighbors = value;
                OnPropertyChanged(nameof(Neighbors));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public LocationNeighbor(int size)
    {
        this.neighbors = new ObservableCollection<bool>(new bool[size]);
    }
}
