using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Core.AStar
{
    /// <summary>
    /// A Path of Nodes, as used for graph-searching.
    /// </summary>
    /// <typeparam name="Node"></typeparam>
    public class Path<Node> : IEnumerable<Node>
    {
        public Node LastStep { get; private set; }
        public Path<Node> PreviousSteps { get; private set; }
        public double TotalCost { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="lastStep"></param>
        /// <param name="previousSteps"></param>
        /// <param name="totalCost"></param>
        private Path(Node lastStep, Path<Node> previousSteps, double totalCost)
        {
            LastStep = lastStep;
            PreviousSteps = previousSteps;
            TotalCost = totalCost;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="start"></param>
        public Path(Node start) : this(start, null, 0) { }

        /// <summary>
        /// Adds a step to the end of this path.
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepCost"></param>
        /// <returns></returns>
        public Path<Node> AddStep(Node step, double stepCost)
        {
            return new Path<Node>(step, this, TotalCost + stepCost);
        }

        /// <summary>
        /// IEnumerable implementation.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Node> GetEnumerator()
        {
            for (Path<Node> p = this; p != null; p = p.PreviousSteps)
                yield return p.LastStep;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
