using System;
using System.Collections.Generic;
using System.Numerics;

namespace COMP302.Decimator
{
    public sealed class EdgeCollapseParameter : ICloneable
    {

        public sealed class PropertySetting
        {

            /// <summary>
            /// Additional weight for property
            /// </summary>
            public float ExtraWeight;

            /// <summary>
            /// Use the adjacent face when position is outside of triangle
            /// </summary>
            public bool InterpolateWithAdjacentFace;

            /// <summary>
            /// Clamp the interpolation when position is outside of triangle
            /// </summary>
            public bool InterpolateClamped;

            /// <summary>
            /// Function used to calculate value (default is property value)
            /// </summary>
            public Func<Vector4, Vector4> SampleFunc;

            /// <summary>
            /// Max squared distance to seem as same value
            /// </summary>
            public float SqrDistanceThreshold;

            public PropertySetting()
            {
                ExtraWeight = 0;
                InterpolateWithAdjacentFace = true;
                InterpolateClamped = true;
                SampleFunc = null;
                SqrDistanceThreshold = 0.003f;
            }
        }

        public class VertexPropertySetting : Dictionary<VertexProperty, PropertySetting> { }

        /// <summary>
        /// Used properties for quadric
        /// </summary>
        public VertexProperty UsedProperty;

        /// <summary>
        /// Weight when edge is a boundary
        /// </summary>
        public double BoundaryWeight;

        /// <summary>
        /// Enable check for normal change
        /// </summary>
        public bool NormalCheck;

        /// <summary>
        /// The threshold for normal change
        /// </summary>
        public double NormalCosineThr;

        /// <summary>
        /// Enable finding the best new position
        /// </summary>
        public bool OptimalPlacement;

        /// <summary>
        /// The sample count when can't find a optimal position
        /// </summary>
        public int OptimalSampleCount;

        /// <summary>
        /// Enable fixed position for boundary vertex
        /// </summary>
        public bool PreserveBoundary;

        /// <summary>
        /// The minimum quadric error
        /// </summary>
        public double QuadricEpsilon;

        /// <summary>
        /// The threshold for triangle quality (larger than this will cost no penalty)
        /// </summary>
        public double QualityThr;

        /// <summary>
        /// Addition quality quadric
        /// </summary>
        public bool QualityQuadric;

        /// <summary>
        /// Prevent intersection between faces
        /// </summary>
        public bool PreventIntersection;

        /// <summary>
        /// Settings for all properties
        /// </summary>
        private VertexPropertySetting PropertySettings;

        public EdgeCollapseParameter()
        {
            SetDefaultParams();
        }

        public void SetDefaultParams()
        {
            UsedProperty = VertexProperty.UV0;
            BoundaryWeight = 0.5;
            NormalCheck = false;
            NormalCosineThr = Math.Cos(Math.PI / 2);
            OptimalPlacement = true;
            OptimalSampleCount = 1;
            PreserveBoundary = false;
            QuadricEpsilon = 1e-15;
            QualityThr = 0.1;
            QualityQuadric = false;
            PreventIntersection = false;
            PropertySettings = [];
        }

        public PropertySetting GetPropertySetting(VertexProperty property)
        {
            if (!PropertySettings.TryGetValue(property, out PropertySetting setting))
            {
                setting = new PropertySetting();
                PropertySettings[property] = setting;
            }
            return setting;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
