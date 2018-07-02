using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace Extensions.Model.Simulations.DifferentialGrowth
{
    public struct Force
    {
        public Vector3d vector;
        public double weight;
        private Object thisLock;

        public Force(Vector3d vector, double weight)
        {
            this.thisLock = new Object();
            this.vector = vector;
            this.weight = weight;
        }

        public void Add(Vector3d vector, double weight)
        {
            lock (thisLock)
            {
                this.vector += vector;
                this.weight += weight;
            }
        }

        public void Add(Force other)
        {
            this.vector += other.vector;
            this.weight += other.weight;
        }

        public void SetZero()
        {
            vector = Vector3d.Zero;
            weight = 0.0;
        }
    }
}
