using System.Numerics;

namespace COMP302
{
    /// <summary>
    /// C++ Source: https://meshdev.sourceforge.net/
    /// LICENSE GNU General Public License
    /// Translated to C# by William Vickers Hastings
    /// </summary>
    public class Neighborhood
    {
        private Neighbor neighbors;

        // Distance to given point
        private float distance;


        public Neighborhood()
        {
            distance = float.MaxValue;
        }


        public void NewVertex(float dist, Vector3 coord, int vertex)
        {
            Reset();
            neighbors = new()
            {
                c = coord,
                v = vertex
            };
            distance = dist;
        }

        public void NewFace(float dist, Vector3 coord, int face, float ratio1, float ratio2)
        {
            Reset();
            neighbors = new()
            {
                c = coord,
                v = -1,
                f = face,
                e = -1,
                r1 = ratio1,
                r2 = ratio2
            };
            distance = dist;
        }

        public void NewEdge(float dist, Vector3 coord, int face, int edge, float ratio)
        {
            Reset();
            neighbors = new()
            {
                c = coord,
                v = -1,
                f = face,
                e = edge,
                r1 = ratio
            };
            distance = dist;
        }

        // Add a neighbor in the list
        public void AddVertex(Vector3 coord, int vertex)
        {

            Neighbor temp = neighbors;
            if (temp.c == coord) return; // Already registered ?
            while (temp.next != null)
            {
                temp = temp.next;
                if (temp.c == coord) return; // Already registered ?
            }
            // Add neighbor to the end of list
            temp.next = new()
            {

                c = coord,
                v = vertex
            };
        }

        public void AddFace(Vector3 coord, int face, float ratio1, float ratio2)
        {
            Neighbor temp = neighbors;
            if (temp.c == coord) return; // Already registered ?
            while (temp.next != null)
            {
                temp = temp.next;
                if (temp.c == coord) return; // Already registered ?
            }
            // Add neighbor to the end of list
            temp.next = new()
            {
                c = coord,
                v = -1,
                f = face,
                e = -1,
                r1 = ratio1,
                r2 = ratio2
            };
        }

        public void AddEdge(Vector3 coord, int face, int edge, float ratio)
        {
            Neighbor temp = neighbors;
            if (temp.c == coord) return; // Already registered ?
            while (temp.next != null)
            {
                temp = temp.next;
                if (temp.c == coord) return; // Already registered ?
            }
            // Add neighbor to the end of list
            temp.next = new()
            {
                c = coord,
                v = -1,
                f = face,
                e = edge,
                r1 = ratio
            };
        }

        // Return neighbors distance
        public float Distance()
        {
            return distance;
        }

        // Return neighbors
        public Neighbor Neighbors() => neighbors;

        // Delete every registered neighbors
        public void Reset()
        {
            neighbors = null;
            distance = float.MaxValue;
        }
    }
}