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

    public class BVHHelper
    {
        public static NodeTraversalTest RadialNodeTraversalTest(Vector3 center, float radius)
        {
            return (Bounds bounds) =>
            {
                //find the closest point inside the bounds
                //Then get the difference between the point and the circle center
                float deltaX = center.X - MathF.Max(bounds.Min.X, MathF.Min(center.X, bounds.Max.X));
                float deltaY = center.Y - MathF.Max(bounds.Min.Y, MathF.Min(center.Y, bounds.Max.Y));
                float deltaZ = center.Z - MathF.Max(bounds.Min.Z, MathF.Min(center.Z, bounds.Max.Z));

                //sqr magnitude < sqr radius = inside bounds!
                return (deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ) < (radius * radius);
            };
        }
    }


    public class BVH<T>
    {
        private Material _debugRenderMaterial = null;

        public BVHNode<T> rootBVH;
        public IBVHNodeAdapter<T> nAda;
        public readonly int LEAF_OBJ_MAX;
        public int nodeCount = 0;
        public int maxDepth = 0;

        public HashSet<BVHNode<T>> refitNodes = new HashSet<BVHNode<T>>();

        // internal functional traversal...
        private void _traverse(BVHNode<T> curNode, NodeTraversalTest hitTest, List<BVHNode<T>> hitlist)
        {
            if (curNode == null) { return; }
            if (hitTest(curNode.Box))
            {
                hitlist.Add(curNode);
                _traverse(curNode.Left, hitTest, hitlist);
                _traverse(curNode.Right, hitTest, hitlist);
            }
        }

        // public interface to traversal..
        public List<BVHNode<T>> Traverse(NodeTraversalTest hitTest)
        {
            var hits = new List<BVHNode<T>>();
            this._traverse(rootBVH, hitTest, hits);
            return hits;
        }

        /*	
        public List<BVHNode<T> Traverse(Ray ray)
		{
			float tnear = 0f, tfar = 0f;

			return Traverse(box => OpenTKHelper.intersectRayAABox1(ray, box, ref tnear, ref tfar));
		}
		public List<BVHNode<T>> Traverse(Bounds volume)
		{
			return Traverse(box => box.IntersectsAABB(volume));
		}
		*/

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

        public void Add(T newOb)
        {
            Bounds box = BoundsFromSphere(nAda.GetObjectPos(newOb), nAda.GetRadius(newOb));
            float boxSAH = BVHNode<T>.SA(ref box);
            rootBVH.Add(nAda, newOb, ref box, boxSAH);
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

        public int CountBVHNodes()
        {
            return rootBVH.CountBVHNodes();
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
            this.nAda = nodeAdaptor;

            if (objects.Count > 0)
            {
                rootBVH = new BVHNode<T>(this, objects);
            }
            else
            {
                rootBVH = new BVHNode<T>(this);
                rootBVH.GObjects = new List<T>(); // it's a leaf, so give it an empty object list
            }
        }

    }
}
