using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;


namespace FlatEarth;

public static class Extensions
{
    public static Task MessageBox(this Window owner, string title, string message)
    {
        Window window = new Window
        {
            Background = Brushes.White,
            Foreground = Brushes.Black,
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Width = 500,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Title = title,
            Content = message
        };

        return window.ShowDialog(owner);
    }
}
