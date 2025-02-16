using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using VECS;

namespace COMP302.Decimator
{
    public class BVHNode<T>
    {
        public Bounds Box;

        public BVHNode<T> Parent;
        public BVHNode<T> Left;
        public BVHNode<T> Right;

        public int Depth;
        public int NodeNumber; // for debugging

        public List<T> GObjects;  // only populated in leaf nodes

        public override string ToString()
        {
            return string.Format("BVHNode<{0}>:{1}", typeof(T), NodeNumber);
        }

        public bool IsLeaf
        {
            get
            {
                bool isLeaf = (GObjects != null);
                // if we're a leaf, then both left and right should be null..
                if (isLeaf && ((Right != null) || (Left != null)))
                {
                    throw new Exception("BVH Leaf has objects and left/right pointers!");
                }
                return isLeaf;

            }
        }

        public void RefitObjectChanged(IBVHNodeAdapter<T> nAda)
        {
            if (GObjects == null)
            {
                throw new Exception("dangling leaf!");
            }
            if (RefitVolume(nAda))
            {
                // add our parent to the optimize list...
                if (Parent != null)
                {
                    nAda.BVH.refitNodes.Add(Parent);

                    // you can force an optimize every time something moves, but it's not very efficient
                    // instead we do this per-frame after a bunch of updates.
                    // nAda.BVH.Optimize();                    
                }
            }
        }

        private void ExpandVolume(IBVHNodeAdapter<T> nAda, Vector3 objectpos, float radius)
        {
            bool expanded = false;

            // test Min X and Max X against the current bounding volume
            if ((objectpos.X - radius) < Box.Min.X)
            {
                Box.Min = new Vector3(objectpos.X - radius, Box.Min.Y, Box.Min.Z);
                expanded = true;
            }
            if ((objectpos.X + radius) > Box.Max.X)
            {
                Box.Max = new Vector3(objectpos.X + radius, Box.Max.Y, Box.Max.Z);
                expanded = true;
            }
            // test Min Y and Max Y against the current bounding volume
            if ((objectpos.Y - radius) < Box.Min.Y)
            {
                Box.Min = new Vector3(Box.Min.X, (objectpos.Y - radius), Box.Min.Z);
                expanded = true;
            }
            if ((objectpos.Y + radius) > Box.Max.Y)
            {
                Box.Max = new Vector3(Box.Max.X, (objectpos.Y + radius), Box.Max.Z);
                expanded = true;
            }
            // test Min Z and Max Z against the current bounding volume
            if ((objectpos.Z - radius) < Box.Min.Z)
            {
                Box.Min = new Vector3(Box.Min.X, Box.Min.Y, (objectpos.Z - radius));
                expanded = true;
            }
            if ((objectpos.Z + radius) > Box.Max.Z)
            {
                Box.Max = new Vector3(Box.Max.X, Box.Max.Y, (objectpos.Z + radius));
                expanded = true;
            }

            if (expanded && Parent != null)
            {
                Parent.ChildExpanded(nAda, this);
            }
        }

        private void AssignVolume(Vector3 objectpos, float radius)
        {
            Box.Min = new Vector3(objectpos.X - radius, objectpos.Y - radius, objectpos.Z - radius);
            Box.Max = new Vector3(objectpos.X + radius, objectpos.Y + radius, objectpos.Z + radius);
        }

        internal void ComputeVolume(IBVHNodeAdapter<T> nAda)
        {
            AssignVolume(nAda.GetObjectPos(GObjects[0]), nAda.GetRadius(GObjects[0]));
            for (int i = 1; i < GObjects.Count; i++)
            {
                ExpandVolume(nAda, nAda.GetObjectPos(GObjects[i]), nAda.GetRadius(GObjects[i]));
            }
        }

        internal bool RefitVolume(IBVHNodeAdapter<T> nAda)
        {
            if (GObjects.Count == 0)
            {
                // TODO: fix .. we should never get called in this case...
                throw new NotImplementedException();
            }

            Bounds oldbox = Box;

            ComputeVolume(nAda);
            if (!Box.Equals(oldbox))
            {
                Parent?.ChildRefit();
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static float SA(Bounds box)
        {
            float x_size = box.Max.X - box.Min.X;
            float y_size = box.Max.Y - box.Min.Y;
            float z_size = box.Max.Z - box.Min.Z;

            return 2.0f * ((x_size * y_size) + (x_size * z_size) + (y_size * z_size));

        }

        internal static float SA(ref Bounds box)
        {
            float x_size = box.Max.X - box.Min.X;
            float y_size = box.Max.Y - box.Min.Y;
            float z_size = box.Max.Z - box.Min.Z;

            return 2.0f * ((x_size * y_size) + (x_size * z_size) + (y_size * z_size));

        }

        internal static float SA(BVHNode<T> node)
        {
            float x_size = node.Box.Max.X - node.Box.Min.X;
            float y_size = node.Box.Max.Y - node.Box.Min.Y;
            float z_size = node.Box.Max.Z - node.Box.Min.Z;

            return 2.0f * ((x_size * y_size) + (x_size * z_size) + (y_size * z_size));
        }

        internal static Bounds AABBofPair(BVHNode<T> nodea, BVHNode<T> nodeb)
        {
            Bounds box = nodea.Box;
            box.Encapsulate(nodeb.Box);
            return box;
        }

        internal static Bounds AABBofOBJ(IBVHNodeAdapter<T> nAda, T obj)
        {
            float radius = nAda.GetRadius(obj);
            return Bounds.FromMinMax(new Vector3(-radius, -radius, -radius), new Vector3(radius, radius, radius));
        }

        internal static float SAofList(IBVHNodeAdapter<T> nAda, List<T> list)
        {
            var box = AABBofOBJ(nAda, list[0]);

            list.ToList<T>().GetRange(1, list.Count - 1).ForEach(obj =>
            {
                var newbox = AABBofOBJ(nAda, obj);
                box.Encapsulate(newbox);
            });
            return SA(box);
        }

        // The list of all candidate rotations, from "Fast, Effective BVH Updates for Animated Scenes", Figure 1.
        internal enum Rot
        {
            NONE, L_RL, L_RR, R_LL, R_LR, LL_RR, LL_RL,
        }

        internal class RotOpt : IComparable<RotOpt>
        {  // rotation option
            public float SAH;
            public Rot Rot;

            internal RotOpt(float SAH, Rot rot)
            {
                this.SAH = SAH;
                Rot = rot;
            }

            public int CompareTo(RotOpt other)
            {
                return SAH.CompareTo(other.SAH);
            }
        }

        private static List<Rot> EachRot => [.. Enum.GetValues<Rot>()];

        /// <summary>
        /// tryRotate looks at all candidate rotations, and executes the rotation with the best resulting SAH (if any)
        /// </summary>
        /// <param name="bvh"></param>
        internal void TryRotate(BVH<T> bvh)
        {
            IBVHNodeAdapter<T> nAda = bvh.nAda;

            // if we are not a grandparent, then we can't rotate, so queue our parent and bail out
            if (Left.IsLeaf && Right.IsLeaf)
            {
                if (Parent != null)
                {
                    bvh.refitNodes.Add(Parent);
                    return;
                }
            }

            // for each rotation, check that there are grandchildren as necessary (aka not a leaf)
            // then compute total SAH cost of our branches after the rotation.

            float mySA = SA(Left) + SA(Right);

            RotOpt bestRot = EachRot.Min((rot) =>
            {
                switch (rot)
                {
                    case Rot.NONE: return new RotOpt(mySA, Rot.NONE);
                    // child to grandchild rotations
                    case Rot.L_RL:
                        if (Right.IsLeaf) return new RotOpt(float.MaxValue, Rot.NONE);
                        else return new RotOpt(SA(Right.Left) + SA(AABBofPair(Left, Right.Right)), rot);
                    case Rot.L_RR:
                        if (Right.IsLeaf) return new RotOpt(float.MaxValue, Rot.NONE);
                        else return new RotOpt(SA(Right.Right) + SA(AABBofPair(Left, Right.Left)), rot);
                    case Rot.R_LL:
                        if (Left.IsLeaf) return new RotOpt(float.MaxValue, Rot.NONE);
                        else return new RotOpt(SA(AABBofPair(Right, Left.Right)) + SA(Left.Left), rot);
                    case Rot.R_LR:
                        if (Left.IsLeaf) return new RotOpt(float.MaxValue, Rot.NONE);
                        else return new RotOpt(SA(AABBofPair(Right, Left.Left)) + SA(Left.Right), rot);
                    // grandchild to grandchild rotations
                    case Rot.LL_RR:
                        if (Left.IsLeaf || Right.IsLeaf) return new RotOpt(float.MaxValue, Rot.NONE);
                        else return new RotOpt(SA(AABBofPair(Right.Right, Left.Right)) + SA(AABBofPair(Right.Left, Left.Left)), rot);
                    case Rot.LL_RL:
                        if (Left.IsLeaf || Right.IsLeaf) return new RotOpt(float.MaxValue, Rot.NONE);
                        else return new RotOpt(SA(AABBofPair(Right.Left, Left.Right)) + SA(AABBofPair(Left.Left, Right.Right)), rot);
                    // unknown...
                    default: throw new NotImplementedException("missing implementation for BVH Rotation SAH Computation .. " + rot.ToString());
                }
            });

            // perform the best rotation...            
            if (bestRot.Rot != Rot.NONE)
            {
                // if the best rotation is no-rotation... we check our parents anyhow..                
                if (Parent != null)
                {
                    // but only do it some random percentage of the time.
                    if ((DateTime.UtcNow.Ticks % 100) < 2)
                    {
                        bvh.refitNodes.Add(Parent);
                    }
                }
            }
            else
            {

                if (Parent != null) { bvh.refitNodes.Add(Parent); }

                if (((mySA - bestRot.SAH) / mySA) < 0.3f)
                {
                    return; // the benefit is not worth the cost
                }
                Console.WriteLine("BVH swap {0} from {1} to {2}", bestRot.Rot.ToString(), mySA, bestRot.SAH);

                // in order to swap we need to:
                //  1. swap the node locations
                //  2. update the depth (if child-to-grandchild)
                //  3. update the parent pointers
                //  4. refit the boundary box
                BVHNode<T> swap = null;
                switch (bestRot.Rot)
                {
                    case Rot.NONE: break;
                    // child to grandchild rotations
                    case Rot.L_RL: swap = Left; Left = Right.Left; Left.Parent = this; Right.Left = swap; swap.Parent = Right; Right.ChildRefit(false); break;
                    case Rot.L_RR: swap = Left; Left = Right.Right; Left.Parent = this; Right.Right = swap; swap.Parent = Right; Right.ChildRefit(false); break;
                    case Rot.R_LL: swap = Right; Right = Left.Left; Right.Parent = this; Left.Left = swap; swap.Parent = Left; Left.ChildRefit(false); break;
                    case Rot.R_LR: swap = Right; Right = Left.Right; Right.Parent = this; Left.Right = swap; swap.Parent = Left; Left.ChildRefit(false); break;

                    // grandchild to grandchild rotations
                    case Rot.LL_RR: swap = Left.Left; Left.Left = Right.Right; Right.Right = swap; Left.Left.Parent = Left; swap.Parent = Right; Left.ChildRefit(false); Right.ChildRefit(false); break;
                    case Rot.LL_RL: swap = Left.Left; Left.Left = Right.Left; Right.Left = swap; Left.Left.Parent = Left; swap.Parent = Right; Left.ChildRefit(false); Right.ChildRefit(false); break;

                    // unknown...
                    default: throw new NotImplementedException("missing implementation for BVH Rotation .. " + bestRot.Rot.ToString());
                }

                // fix the depths if necessary....
                switch (bestRot.Rot)
                {
                    case Rot.L_RL:
                    case Rot.L_RR:
                    case Rot.R_LL:
                    case Rot.R_LR:
                        SetDepth(nAda, Depth);
                        break;
                }
            }
        }

        private static List<Axis> EachAxis => [.. Enum.GetValues<Axis>()];

        internal class SplitAxisOpt<GO> : IComparable<SplitAxisOpt<GO>>
        {  // split Axis option
            public float SAH;
            public Axis Axis;
            public List<GO> Left, Right;

            internal SplitAxisOpt(float SAH, Axis axis, List<GO> left, List<GO> right)
            {
                this.SAH = SAH;
                Axis = axis;
                Left = left;
                Right = right;
            }

            public int CompareTo(SplitAxisOpt<GO> other)
            {
                return SAH.CompareTo(other.SAH);
            }
        }

        internal void SplitNode(IBVHNodeAdapter<T> nAda)
        {
            // second, decide which axis to split on, and sort..
            List<T> splitlist = GObjects;
            splitlist.ForEach(nAda.UnmapObject);
            int center = splitlist.Count / 2; // find the center object

            SplitAxisOpt<T> bestSplit = EachAxis.Min((axis) =>
            {
                var orderedlist = new List<T>(splitlist);
                switch (axis)
                {
                    case Axis.X:
                        orderedlist.Sort(delegate (T go1, T go2) { return nAda.GetObjectPos(go1).X.CompareTo(nAda.GetObjectPos(go2).X); });
                        break;
                    case Axis.Y:
                        orderedlist.Sort(delegate (T go1, T go2) { return nAda.GetObjectPos(go1).Y.CompareTo(nAda.GetObjectPos(go2).Y); });
                        break;
                    case Axis.Z:
                        orderedlist.Sort(delegate (T go1, T go2) { return nAda.GetObjectPos(go1).Z.CompareTo(nAda.GetObjectPos(go2).Z); });
                        break;
                    default:
                        throw new NotImplementedException("unknown split axis: " + axis.ToString());
                }

                var left_s = orderedlist.GetRange(0, center);
                var right_s = orderedlist.GetRange(center, splitlist.Count - center);

                float SAH = SAofList(nAda, left_s) * left_s.Count + SAofList(nAda, right_s) * right_s.Count;
                return new SplitAxisOpt<T>(SAH, axis, left_s, right_s);
            });

            // perform the split
            GObjects = null;
            Left = new BVHNode<T>(nAda.BVH, this, bestSplit.Left, Depth + 1); // Split the Hierarchy to the left
            Right = new BVHNode<T>(nAda.BVH, this, bestSplit.Right, Depth + 1); // Split the Hierarchy to the right                                
        }

        internal void SplitIfNecessary(IBVHNodeAdapter<T> nAda)
        {
            if (GObjects.Count > nAda.BVH.LEAF_OBJ_MAX)
            {
                SplitNode(nAda);
            }
        }

        internal void Add(IBVHNodeAdapter<T> nAda, T newOb, ref Bounds newObBox, float newObSAH)
        {
            Add(nAda, this, newOb, ref newObBox, newObSAH);
        }

        internal static void AddObjectPushdown(IBVHNodeAdapter<T> nAda, BVHNode<T> curNode, T newOb)
        {
            var left = curNode.Left;
            var right = curNode.Right;

            // merge and pushdown left and right as a new node..
            var mergedSubnode = new BVHNode<T>(nAda.BVH)
            {
                Left = left,
                Right = right,
                Parent = curNode,
                GObjects = null // we need to be an interior node... so null out our object list..
            };
            left.Parent = mergedSubnode;
            right.Parent = mergedSubnode;
            mergedSubnode.ChildRefit(false);

            // make new subnode for obj
            var newSubnode = new BVHNode<T>(nAda.BVH)
            {
                Parent = curNode,
                GObjects = [newOb]
            };
            nAda.MapObjectToBVHLeaf(newOb, newSubnode);
            newSubnode.ComputeVolume(nAda);

            // make assignments..
            curNode.Left = mergedSubnode;
            curNode.Right = newSubnode;
            curNode.SetDepth(nAda, curNode.Depth); // propagate new depths to our children.
            curNode.ChildRefit();
        }

        internal static void Add(IBVHNodeAdapter<T> nAda, BVHNode<T> curNode, T newOb, ref Bounds newObBox, float newObSAH)
        {
            // 1. first we traverse the node looking for the best leaf
            while (curNode.GObjects == null)
            {
                // find the best way to add this object.. 3 options..
                // 1. send to left node  (L+N,R)
                // 2. send to right node (L,R+N)
                // 3. merge and pushdown left-and-right node (L+R,N)

                var left = curNode.Left;
                var right = curNode.Right;

                float leftSAH = SA(left);
                float rightSAH = SA(right);

                //Create new bounds to avoid modifying originals when using encapsulate
                Bounds leftExpanded = Bounds.FromMinMax(left.Box.Min, left.Box.Max);

                Bounds rightExpanded = Bounds.FromMinMax(right.Box.Min, right.Box.Max);

                leftExpanded.Encapsulate(newObBox);
                rightExpanded.Encapsulate(newObBox);

                float sendLeftSAH = rightSAH + SA(leftExpanded);    // (L+N,R)
                float sendRightSAH = leftSAH + SA(rightExpanded);   // (L,R+N)
                float mergedLeftAndRightSAH = SA(AABBofPair(left, right)) + newObSAH; // (L+R,N)

                // Doing a merge-and-pushdown can be expensive, so we only do it if it's notably better
                const float MERGE_DISCOUNT = 0.3f;

                if (mergedLeftAndRightSAH < (Math.Min(sendLeftSAH, sendRightSAH)) * MERGE_DISCOUNT)
                {
                    AddObjectPushdown(nAda, curNode, newOb);
                    return;
                }
                else
                {
                    if (sendLeftSAH < sendRightSAH)
                    {
                        curNode = left;
                    }
                    else
                    {
                        curNode = right;
                    }
                }
            }

            // 2. then we add the object and map it to our leaf
            curNode.GObjects.Add(newOb);
            nAda.MapObjectToBVHLeaf(newOb, curNode);
            curNode.RefitVolume(nAda);
            // split if necessary...
            curNode.SplitIfNecessary(nAda);
        }

        internal int CountBVHNodes() => GObjects != null ? 1 : Left.CountBVHNodes() + Right.CountBVHNodes();

        internal void Remove(IBVHNodeAdapter<T> nAda, T newOb)
        {
            if (GObjects == null) { throw new Exception("removeObject() called on nonLeaf!"); }

            nAda.UnmapObject(newOb);
            GObjects.Remove(newOb);
            if (GObjects.Count > 0)
            {
                RefitVolume(nAda);
            }
            else
            {
                // our leaf is empty, so collapse it if we are not the root...
                if (Parent != null)
                {
                    GObjects = null;
                    Parent.RemoveLeaf(nAda, this);
                    Parent = null;
                }
            }
        }

        void SetDepth(IBVHNodeAdapter<T> nAda, int newdepth)
        {
            Depth = newdepth;
            if (newdepth > nAda.BVH.maxDepth)
            {
                nAda.BVH.maxDepth = newdepth;
            }
            if (GObjects == null)
            {
                Left.SetDepth(nAda, newdepth + 1);
                Right.SetDepth(nAda, newdepth + 1);
            }
        }

        internal void RemoveLeaf(IBVHNodeAdapter<T> nAda, BVHNode<T> removeLeaf)
        {
            if (Left == null || Right == null) { throw new Exception("bad intermediate node"); }
            BVHNode<T> keepLeaf;

            if (removeLeaf == Left)
            {
                keepLeaf = Right;
            }
            else if (removeLeaf == Right)
            {
                keepLeaf = Left;
            }
            else
            {
                throw new Exception("removeLeaf doesn't match any leaf!");
            }

            // "become" the leaf we are keeping.
            Box = keepLeaf.Box;
            Left = keepLeaf.Left; Right = keepLeaf.Right; GObjects = keepLeaf.GObjects;
            // clear the leaf..
            // keepLeaf.left = null; keepLeaf.right = null; keepLeaf.gobjects = null; keepLeaf.parent = null; 

            if (GObjects == null)
            {
                Left.Parent = this; Right.Parent = this;  // reassign child parents..
                SetDepth(nAda, Depth); // this reassigns depth for our children
            }
            else
            {
                // map the objects we adopted to us...                                                
                GObjects.ForEach(o => { nAda.MapObjectToBVHLeaf(o, this); });
            }

            // propagate our new volume..
            Parent?.ChildRefit();
        }

        internal void FindOverlappingLeaves(IBVHNodeAdapter<T> nAda, Vector3 origin, float radius, List<BVHNode<T>> overlapList)
        {
            if (BoundsIntersectsSphere(ToBounds(), origin, radius))
            {
                if (GObjects != null)
                {
                    overlapList.Add(this);
                }
                else
                {
                    Left.FindOverlappingLeaves(nAda, origin, radius, overlapList);
                    Right.FindOverlappingLeaves(nAda, origin, radius, overlapList);
                }
            }
        }

        //Modified from https://github.com/jeske/SimpleScene/blob/master/SimpleScene/Core/SSAABB.cs
        private static bool BoundsIntersectsSphere(Bounds bounds, Vector3 origin, float radius)
        {
            if (
                (origin.X + radius < bounds.Min.X) ||
                (origin.Y + radius < bounds.Min.Y) ||
                (origin.Z + radius < bounds.Min.Z) ||
                (origin.X - radius > bounds.Max.X) ||
                (origin.Y - radius > bounds.Max.Y) ||
                (origin.Z - radius > bounds.Max.Z)
               )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal void FindOverlappingLeaves(IBVHNodeAdapter<T> nAda, Bounds aabb, List<BVHNode<T>> overlapList)
        {
            if (ToBounds().Intersects(aabb))
            {
                if (GObjects != null)
                {
                    overlapList.Add(this);
                }
                else
                {
                    Left.FindOverlappingLeaves(nAda, aabb, overlapList);
                    Right.FindOverlappingLeaves(nAda, aabb, overlapList);
                }
            }
        }

        internal Bounds ToBounds()
        {
            return Bounds.FromMinMax(new Vector3(Box.Min.X, Box.Min.Y, Box.Min.Z), new Vector3(Box.Max.X, Box.Max.Y, Box.Max.Z));
        }

        internal void ChildExpanded(IBVHNodeAdapter<T> nAda, BVHNode<T> child)
        {
            bool expanded = false;

            if (child.Box.Min.X < Box.Min.X)
            {
                Box.Min = new Vector3(child.Box.Min.X, Box.Min.Y, Box.Min.Z);
                expanded = true;
            }
            if (child.Box.Max.X > Box.Max.X)
            {
                Box.Max = new Vector3(child.Box.Max.X, Box.Max.Y, Box.Max.Z);
                expanded = true;
            }
            if (child.Box.Min.Y < Box.Min.Y)
            {
                Box.Min = new Vector3(Box.Min.X, child.Box.Min.Y, Box.Min.Z);
                expanded = true;
            }
            if (child.Box.Max.Y > Box.Max.Y)
            {
                Box.Max = new Vector3(Box.Max.X, child.Box.Max.Y, Box.Max.Z);
                expanded = true;
            }
            if (child.Box.Min.Z < Box.Min.Z)
            {
                Box.Min = new Vector3(Box.Min.X, Box.Min.Y, child.Box.Min.Z);
                expanded = true;
            }
            if (child.Box.Max.Z > Box.Max.Z)
            {
                Box.Max = new Vector3(Box.Max.X, Box.Max.Y, child.Box.Max.Z);
                expanded = true;
            }

            if (expanded && Parent != null)
            {
                Parent.ChildExpanded(nAda, this);
            }
        }

        internal void ChildRefit(bool propagate = true)
        {
            ChildRefit(this, propagate);
        }

        internal static void ChildRefit(BVHNode<T> curNode, bool propagate = true)
        {
            do
            {
                BVHNode<T> left = curNode.Left;
                BVHNode<T> right = curNode.Right;

                // start with the left box
                Bounds newBox = left.Box;

                // expand any dimension bigger in the right node
                if (right.Box.Min.X < newBox.Min.X)
                {
                    newBox.Min = new Vector3(right.Box.Min.X, newBox.Min.Y, newBox.Min.Z);
                }
                if (right.Box.Min.Y < newBox.Min.Y)
                {
                    newBox.Min = new Vector3(newBox.Min.X, right.Box.Min.Y, newBox.Min.Z);
                }
                if (right.Box.Min.Z < newBox.Min.Z)
                {
                    newBox.Min = new Vector3(newBox.Min.X, newBox.Min.Y, right.Box.Min.Z);
                }

                if (right.Box.Max.X > newBox.Max.X)
                {
                    newBox.Max = new Vector3(right.Box.Max.X, newBox.Max.Y, newBox.Max.Z);
                }
                if (right.Box.Max.Y > newBox.Max.Y)
                {
                    newBox.Max = new Vector3(newBox.Max.X, right.Box.Max.Y, newBox.Max.Z);
                }
                if (right.Box.Max.Z > newBox.Max.Z)
                {
                    newBox.Max = new Vector3(newBox.Max.X, newBox.Max.Y, right.Box.Max.Z);
                }

                // now set our box to the newly created box
                curNode.Box = newBox;

                // and walk up the tree
                curNode = curNode.Parent;
            } while (propagate && curNode != null);
        }

        internal BVHNode(BVH<T> bvh)
        {
            GObjects = [];
            Left = Right = null;
            Parent = null;
            NodeNumber = bvh.nodeCount++;
        }

        internal BVHNode(BVH<T> bvh, List<T> gobjectlist) : this(bvh, null, gobjectlist,0)
        {

        }

        private BVHNode(BVH<T> bvh, BVHNode<T> lparent, List<T> gobjectlist, int curdepth)
        {
            IBVHNodeAdapter<T> nAda = bvh.nAda;
            NodeNumber = bvh.nodeCount++;

            Parent = lparent; // save off the parent BVHGObj Node
            Depth = curdepth;

            if (bvh.maxDepth < curdepth)
            {
                bvh.maxDepth = curdepth;
            }

            // Early out check due to bad data
            // If the list is empty then we have no BVHGObj, or invalid parameters are passed in
            if (gobjectlist == null || gobjectlist.Count < 1)
            {
                throw new Exception("ssBVHNode constructed with invalid paramaters");
            }

            // Check if we’re at our LEAF node, and if so, save the objects and stop recursing.  Also store the Min/Max for the leaf node and update the parent appropriately
            if (gobjectlist.Count <= bvh.LEAF_OBJ_MAX)
            {
                // once we reach the leaf node, we must set prev/next to null to signify the end
                Left = null;
                Right = null;
                // at the leaf node we store the remaining objects, so initialize a list
                GObjects = gobjectlist;
                GObjects.ForEach(o => nAda.MapObjectToBVHLeaf(o, this));
                ComputeVolume(nAda);
                SplitIfNecessary(nAda);
            }
            else
            {
                // --------------------------------------------------------------------------------------------
                // if we have more than (bvh.LEAF_OBJECT_COUNT) objects, then compute the volume and split
                GObjects = gobjectlist;
                ComputeVolume(nAda);
                SplitNode(nAda);
                ChildRefit(false);
            }
        }

    }
}