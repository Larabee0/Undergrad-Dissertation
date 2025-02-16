using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using VECS;

namespace COMP302.Decimator
{
    public enum Axis
    {
        X, Y, Z,
    }

    public delegate bool NodeTraversalTest(Bounds box);

    public class BVH<T>
    {
        public BVHNode<T> rootBVH;
        public IBVHNodeAdapter<T> nAda;
        public readonly int LEAF_OBJ_MAX;
        public int nodeCount = 0;
        public int maxDepth = 0;

        public HashSet<BVHNode<T>> refitNodes = [];

        // internal functional traversal...
        private static void Traverse(BVHNode<T> curNode, NodeTraversalTest hitTest, List<BVHNode<T>> hitlist)
        {
            if (curNode == null) { return; }
            if (hitTest(curNode.Box))
            {
                hitlist.Add(curNode);
                Traverse(curNode.Left, hitTest, hitlist);
                Traverse(curNode.Right, hitTest, hitlist);
            }
        }

        // public interface to traversal..
        public List<BVHNode<T>> Traverse(NodeTraversalTest hitTest)
        {
            var hits = new List<BVHNode<T>>();
            Traverse(rootBVH, hitTest, hits);
            return hits;
        }

        /// <summary>
        /// Call this to batch-optimize any object-changes notified through 
        /// ssBVHNode.refit_ObjectChanged(..). For example, in a game-loop, 
        /// call this once per frame.
        /// </summary>
        public void Optimize()
        {
            if (LEAF_OBJ_MAX != 1)
            {
                throw new Exception("In order to use optimize, you must set LEAF_OBJ_MAX=1");
            }

            while (refitNodes.Count > 0)
            {
                int maxdepth = refitNodes.Max(n => n.Depth);

                var sweepNodes = refitNodes.Where(n => n.Depth == maxdepth).ToList();
                sweepNodes.ForEach(n => refitNodes.Remove(n));

                sweepNodes.ForEach(n => n.TryRotate(this));
            }
        }

        /// <summary>
        /// Call this when you wish to update an object. This does not update straight away, but marks it for update when Optimize() is called
        /// </summary>
        /// <param name="toUpdate"></param>
        public void MarkForUpdate(T toUpdate)
        {
            nAda.OnPositionOrSizeChanged(toUpdate);
        }

        //Modified from https://github.com/jeske/SimpleScene/blob/master/SimpleScene/Core/SSAABB.cs
        public static Bounds BoundsFromSphere(Vector3 pos, float radius)
        {
            Bounds bounds =  Bounds.FromMinMax
            (
                new Vector3(pos.X - radius, pos.Y - radius, pos.Z - radius),
                new Vector3(pos.X + radius, pos.Y + radius, pos.Z + radius)
            );
            return bounds;
        }

        public void Remove(T newObj)
        {
            var leaf = nAda.GetLeaf(newObj);
            leaf.Remove(nAda, newObj);
        }

        /// <summary>
        /// initializes a BVH with a given nodeAdaptor, and object list.
        /// </summary>
        /// <param name="nodeAdaptor"></param>
        /// <param name="objects"></param>
        /// <param name="LEAF_OBJ_MAX">WARNING! currently this must be 1 to use dynamic BVH updates</param>
        public BVH(IBVHNodeAdapter<T> nodeAdaptor, List<T> objects, int LEAF_OBJ_MAX = 1)
        {
            this.LEAF_OBJ_MAX = LEAF_OBJ_MAX;
            nodeAdaptor.BVH = this;
            nAda = nodeAdaptor;

            rootBVH = objects.Count > 0
                ? new BVHNode<T>(this, objects)
                : new BVHNode<T>(this)
                {
                    GObjects = [] // it's a leaf, so give it an empty object list
                };
        }

    }
}
