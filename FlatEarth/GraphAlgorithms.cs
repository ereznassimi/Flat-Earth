using System;
using System.Collections.Generic;


namespace FlatEarth;


public static class GraphAlgorithms
{
    public static List<int> FindBridgeNodes(int[,] adjacencyMatrix)
    {
        int nodeCount = adjacencyMatrix.GetLength(0);
        Dictionary<int, HashSet<int>> fullReachability = new Dictionary<int, HashSet<int>>();

        // Step 1: Precompute full reachability from each node
        for (int i = 0; i < nodeCount; i++)
        {
            fullReachability[i] = DFS(adjacencyMatrix, i);
        }

        List<int> bridgeNodes = new List<int>();

        // Step 2: For each node, simulate removal and check reachability
        for (int remove = 0; remove < nodeCount; remove++)
        {
            bool isBridge = false;

            for (int i = 0; i < nodeCount; i++)
            {
                if (i == remove)
                    continue;

                HashSet<int>? reachable = DFS(adjacencyMatrix, i, new HashSet<int> { remove });

                int expected = fullReachability[i].Count;
                if (fullReachability[i].Contains(remove))
                    expected -= 1;

                if (reachable.Count < expected)
                {
                    isBridge = true;
                    break;
                }
            }

            if (isBridge)
            {
                bridgeNodes.Add(remove); // 0-based index
            }
        }

        return bridgeNodes;
    }
    
    private static HashSet<int> DFS(int[,] matrix, int start, HashSet<int>? blocked = null)
    {
        int count = matrix.GetLength(0);
        HashSet<int> visited = new HashSet<int>();
        Stack<int> stack = new Stack<int>();
        stack.Push(start);

        // Mark the starting node as visited
        while (stack.Count > 0)
        {
            // Pop the top element from the stack
            int current = stack.Pop();

            // If the current node is blocked, skip it
            if (blocked != null && blocked.Contains(current))
                continue;

            // If the current node has already been visited, skip it
            if (!visited.Add(current))
                continue;
            
            // Check all neighbors of the current node
            for (int neighbor = 0; neighbor < count; neighbor++)
            {
                // If there is an edge to the neighbor and it's not blocked
                if (matrix[current, neighbor] == 1 &&
                    (blocked == null || !blocked.Contains(neighbor)))
                {
                    // Push the neighbor onto the stack
                    stack.Push(neighbor);
                }
            }
        }

        return visited;
    }

    public static bool[,] FindRedundantEdges(int[,] adjacencyMatrix)
    {
        int n = adjacencyMatrix.GetLength(0);
        bool[,] redundant = new bool[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (adjacencyMatrix[i, j] == 1)
                {
                    // Temporarily remove edge (i, j)
                    adjacencyMatrix[i, j] = 0;

                    // Check if there is still a path from i to j
                    if (HasPath(adjacencyMatrix, i, j))
                    {
                        redundant[i, j] = true;
                    }

                    // Restore the edge
                    adjacencyMatrix[i, j] = 1;
                }
            }
        }

        return redundant;
    }

    private static bool HasPath(int[,] matrix, int start, int end)
    {
        int n = matrix.GetLength(0);
        bool[] visited = new bool[n];
        Queue<int> queue = new Queue<int>();
        queue.Enqueue(start);
        visited[start] = true;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == end)
                return true;

            for (int neighbor = 0; neighbor < n; neighbor++)
            {
                if (matrix[current, neighbor] == 1 && !visited[neighbor])
                {
                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return false;
    }

    public static double[,] ComputeShortestPaths(int[,] adjacencyMatrix)
    {
        int n = adjacencyMatrix.GetLength(0);
        double[,] dist = new double[n, n];
        double INF = double.PositiveInfinity;

        // Initialize distances
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i == j) dist[i, j] = 0;
                else if (adjacencyMatrix[i, j] == 1) dist[i, j] = 1;
                else dist[i, j] = INF;
            }
        }

        // Floyd-Warshall algorithm
        for (int k = 0; k < n; k++)
        {
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (dist[i, k] + dist[k, j] < dist[i, j])
                        dist[i, j] = dist[i, k] + dist[k, j];
                }
            }
        }

        return dist;
    }

    public static (double X, double Y)[] ComputeKamadaKawaiPositions(double[,] shortestPaths, int width, int height)
    {
        int n = shortestPaths.GetLength(0);
        double K = 1.0; // Spring constant
        double L0 = 400.0; // Ideal edge length
        double EPSILON = 1e-2;

        // Build L matrix and K matrix
        double[,] L = new double[n, n];
        double[,] Kmat = new double[n, n];
        double maxD = 0;

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (shortestPaths[i, j] != double.PositiveInfinity && i != j)
                {
                    maxD = Math.Max(maxD, shortestPaths[i, j]);
                }
            }
        }

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i != j && shortestPaths[i, j] != double.PositiveInfinity)
                {
                    L[i, j] = L0 * shortestPaths[i, j] / maxD;
                    Kmat[i, j] = K / (shortestPaths[i, j] * shortestPaths[i, j]);
                }
            }
        }

        // Initialize node positions in a circle
        (double X, double Y)[] positions = new (double X, double Y)[n];
        double cx = width / 2, cy = height / 2, radius = Math.Min(width, height) / 3;
        for (int i = 0; i < n; i++)
        {
            double angle = 2 * Math.PI * i / n;
            positions[i] = (cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
        }

        // Simple gradient descent loop (limited iterations)
        for (int iteration = 0; iteration < 500; iteration++)
        {
            double maxDelta = 0;

            for (int i = 0; i < n; i++)
            {
                double dx = 0, dy = 0;

                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;

                    double dxij = positions[i].X - positions[j].X;
                    double dyij = positions[i].Y - positions[j].Y;
                    double dist = Math.Sqrt(dxij * dxij + dyij * dyij);
                    if (dist < 1e-4) dist = 1e-4; // avoid division by 0

                    double delta = Kmat[i, j] * (dist - L[i, j]);
                    dx += delta * dxij / dist;
                    dy += delta * dyij / dist;
                }

                positions[i].X -= dx * 0.01;
                positions[i].Y -= dy * 0.01;
                maxDelta = Math.Max(maxDelta, Math.Sqrt(dx * dx + dy * dy));
            }

            if (maxDelta < EPSILON)
                break;
        }

        return positions;
    }
}
