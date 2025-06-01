using System.Collections.Generic;
using System.Threading.Tasks;


namespace FlatEarth;

public interface IRouteExplorer
{
    Task<IEnumerable<int>> GetRouteToCoverAll(long[,] distanceMatrix, int startAt);
}
