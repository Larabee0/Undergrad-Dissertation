using System;
using System.Collections.Generic;
using System.Numerics;

namespace COMP302.Decimator
{
    public class BVHFaceAdapter : IBVHNodeAdapter<Face>
    {

        private BVH<Face> _bvh;
        private readonly Dictionary<Face, BVHNode<Face>> _gameObjectToLeafMap = [];
        private readonly Dictionary<Face, (Vector3 pos, float radius)> _boundingSphere = [];

        BVH<Face> IBVHNodeAdapter<Face>.BVH
        {
            get => _bvh;
            set => _bvh = value;
        }

        //TODO: this is not used?
        public void CheckMap(Face obj)
        {
            if (!_gameObjectToLeafMap.ContainsKey(obj))
            {
                throw new Exception("missing map for shuffled child");
            }
        }

        public BVHNode<Face> GetLeaf(Face obj)
        {
            return _gameObjectToLeafMap[obj];
        }

        public Vector3 GetObjectPos(Face obj)
        {
            return GetBoundingSphere(obj).pos;
        }

        public float GetRadius(Face obj)
        {
            return GetBoundingSphere(obj).radius;
        }

        public void MapObjectToBVHLeaf(Face obj, BVHNode<Face> leaf)
        {
            _gameObjectToLeafMap[obj] = leaf;
        }

        // this allows us to be notified when an object moves, so we can adjust the BVH
        public void OnPositionOrSizeChanged(Face changed)
        {
            var sphere = MakeMinimumBoundingSphere(changed.P(0), changed.P(1), changed.P(2));
            _boundingSphere[changed] = sphere;

            // the SSObject has changed, so notify the BVH leaf to refit for the object
            _gameObjectToLeafMap[changed].RefitObjectChanged(this);
        }

        public void UnmapObject(Face obj)
        {
            _gameObjectToLeafMap.Remove(obj);
        }

        private (Vector3 pos, float radius) GetBoundingSphere(Face obj)
        {
            if (_boundingSphere.TryGetValue(obj, out (Vector3 pos, float radius) sphere))
            {
                return sphere;
            }
            sphere = MakeMinimumBoundingSphere(obj.P(0), obj.P(1), obj.P(2));
            _boundingSphere[obj] = sphere;
            return sphere;
        }

        private static (Vector3, float) MakeMinimumBoundingSphere(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // Calculate relative distances
            float A = (p1 - p2).Length();
            float B = (p2 - p3).Length();
            float C = (p3 - p1).Length();

            // Re-orient triangle (make A longest side)
            Vector3 a = p3, b = p1, c = p2;
            if (B < C)
            {
                (B, C) = (C, B);
                (b, c) = (c, b);
            }
            if (A < B)
            {
                (A, B) = (B, A);
                (a, b) = (b, a);
            }

            Vector3 pos;
            float radius;
            // If obtuse, just use longest diameter, otherwise circumscribe
            if ((B * B) + (C * C) <= (A * A))
            {
                radius = A / 2;
                pos = (b + c) / 2;
            }
            else
            {
                // http://en.wikipedia.org/wiki/Circumscribed_circle
                Vector3 alpha = a - c, beta = b - c;
                Vector3 alphaCrossbeta = Vector3.Cross(alpha, beta);

                float sinC = (alphaCrossbeta.Length()) / (A * B);
                radius = (a - b).Length() / (2 * sinC);

                pos = c + Vector3.Cross(beta * alpha.LengthSquared() - alpha * beta.LengthSquared(), alphaCrossbeta) / (2 * alphaCrossbeta.LengthSquared());
            }
            return (pos, radius);
        }
    }
}
