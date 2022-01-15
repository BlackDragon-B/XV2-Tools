﻿using System;

namespace LB_Common.Numbers
{
    [Serializable]
    public class CustomVector4
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public CustomVector4() { }

        public CustomVector4(float value)
        {
            X = Y = Z = W = value;
        }

        public CustomVector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float GetValue(int idx)
        {
            switch (idx)
            {
                case 0:
                    return X;
                case 1:
                    return Y;
                case 2:
                    return Z;
                case 3:
                    return W;
            }
            return 0;
        }

        #region Operators
        public override bool Equals(object obj)
        {
            return this == (CustomVector4)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash1 = 2256559;
                const int hash2 = 5023567;
                hash1 *= hash2 ^ X.GetHashCode();
                hash1 *= hash2 ^ Y.GetHashCode();
                hash1 *= hash2 ^ Z.GetHashCode();
                hash1 *= hash2 ^ W.GetHashCode();
                return hash1;
            }
        }

        public static bool operator ==(CustomVector4 a, CustomVector4 b)
        {
            return a?.X == b?.X && a?.Y == b?.Y && a?.Z == b?.Z && a?.W == b?.W;
        }

        public static bool operator !=(CustomVector4 a, CustomVector4 b)
        {
            return (a == b) ? false : true;
        }

        #endregion
    }
}
