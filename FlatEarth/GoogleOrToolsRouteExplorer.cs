using Google.OrTools.ConstraintSolver;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace FlatEarth;

public class GoogleOrToolsRouteExplorer: IRouteExplorer
{
    public async Task<IEnumerable<int>> GetRouteToCoverAll(long[,] distanceMatrix, int startAt)
    {
        int n = distanceMatrix.GetLength(0);

        RoutingIndexManager manager = new RoutingIndexManager(n, 1, startAt);
        RoutingModel routing = new RoutingModel(manager);

        int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
        {
            int fromNode = manager.IndexToNode(fromIndex);
            int toNode = manager.IndexToNode(toIndex);
            return distanceMatrix[fromNode, toNode];
        });

        routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

        RoutingSearchParameters searchParameters =
            operations_research_constraint_solver.DefaultRoutingSearchParameters();

        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.Automatic;

        Assignment solution = routing.SolveWithParameters(searchParameters);

        List<int> result = new List<int>();
        if (solution != null)
        {
            long index = routing.Start(0);
            while (!routing.IsEnd(index))
            {
                result.Add(manager.IndexToNode(index));
                index = solution.Value(routing.NextVar(index));
            }
        }

        return result;
    }
}
