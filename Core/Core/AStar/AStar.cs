using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.AStar
{
    public static class AStar
    {
        /// <summary>
        /// A* Pathfinding.
        /// </summary>
        /// <typeparam name="Node"></typeparam>
        /// <param name="start"></param>
        /// <param name="destination"></param>
        /// <param name="distance"></param>
        /// <param name="estimate"></param>
        /// <returns></returns>
        static public Path<Node> FindPath<Node>(Node start, Node destination, Func<Node, Node, double> distance, Func<Node, Node, double> estimate)
            where Node : IHasNeighbours<Node>
        {
            var closed = new HashSet<Node>();
            var queue = new PriorityQueue<double, Path<Node>>();
            queue.Enqueue(0, new Path<Node>(start));
            while (!queue.IsEmpty)
            {
                var path = queue.DequeueValue();
                if (closed.Contains(path.LastStep))
                    continue;
                if (path.LastStep.Equals(destination))
                    return path;
                closed.Add(path.LastStep);
                foreach (Node n in path.LastStep.Neighbours)
                {
                    double d = distance(path.LastStep, n);
                    var newPath = path.AddStep(n, d);
                    queue.Enqueue(newPath.TotalCost + estimate(n, destination), newPath);
                }
            }
            return null;
        }
    }
}
