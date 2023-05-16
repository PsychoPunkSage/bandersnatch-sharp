// Copyright 2022 Demerzel Solutions Limited
// Licensed under Apache-2.0.For full terms, see LICENSE in the project root.

using Nethermind.Verkle.Fields.FpEElement;
using Nethermind.Verkle.Fields.FrEElement;

namespace Nethermind.Verkle.Curve
{
    public readonly struct AffinePoint
    {
        /// <summary>
        /// serialization constants
        /// </summary>
        private const byte MCompressedNegative = 128;

        private const byte MCompressedPositive = 0;

        public readonly FpE X;
        public readonly FpE Y;

        public AffinePoint(FpE x, FpE y)
        {
            X = x;
            Y = y;
        }

        private static FpE A => CurveParams.A;
        private static FpE D => CurveParams.D;

        public static AffinePoint Generator()
        {
            return new AffinePoint(CurveParams.XTe, CurveParams.YTe);
        }

        public static AffinePoint Neg(AffinePoint p)
        {
            return new AffinePoint(p.X.Negative(), p.Y);
        }

        public static AffinePoint Add(AffinePoint p, AffinePoint q)
        {
            FpE x1Y2 = p.X * q.Y;
            FpE y1X2 = p.Y * q.X;
            FpE x1X2A = p.X * q.X * A;
            FpE y1Y2 = p.Y * q.Y;

            FpE x1X2Y1Y2D = x1Y2 * y1X2 * D;

            FpE xNum = x1Y2 + y1X2;

            FpE xDen = FpE.One + x1X2Y1Y2D;

            FpE yNum = y1Y2 - x1X2A;

            FpE yDen = FpE.One - x1X2Y1Y2D;

            FpE x = xNum / xDen;

            FpE y = yNum / yDen;

            return new AffinePoint(x, y);
        }

        public static AffinePoint Sub(AffinePoint p, AffinePoint q)
        {
            AffinePoint negQ = Neg(q);
            return Add(p, negQ);
        }

        public static AffinePoint Double(AffinePoint p)
        {
            FpE.MultiplyMod(p.X, p.X, out FpE xSq);
            FpE.MultiplyMod(p.Y, p.Y, out FpE ySq);

            FpE xY = p.X * p.Y;
            FpE xSqA = xSq * A;

            FpE xSqYSqD = xSq * ySq * D;

            FpE xNum = xY + xY;

            FpE xDen = FpE.One + xSqYSqD;

            FpE yNum = ySq - xSqA;

            FpE yDen = FpE.One - xSqYSqD;

            FpE.Divide(xNum, xDen, out FpE x);
            FpE.Divide(yNum, yDen, out FpE y);

            return new AffinePoint(x, y);
        }

        public bool IsOnCurve()
        {
            FpE xSq = X * X;
            FpE ySq = Y * Y;

            FpE dxySq = xSq * ySq * D;
            FpE aXSq = A * xSq;

            FpE one = FpE.One;

            FpE rhs = one + dxySq;
            FpE lhs = aXSq + ySq;

            return lhs.Equals(rhs);
        }

        public byte[] ToBytes()
        {
            // This is here to test that we have the correct generator element
            // banderwagon uses a different serialisation algorithm
            byte mask = MCompressedPositive;
            if (Y.LexicographicallyLargest())
                mask = MCompressedNegative;

            byte[] xBytes = X.ToBytes().ToArray();
            xBytes[31] |= mask;
            return xBytes;
        }

        public static AffinePoint ScalarMultiplication(AffinePoint point, FrE scalar)
        {
            AffinePoint result = Identity();
            byte[] bytes = scalar.ToBytes().ToArray();

            // using double and add : https://en.wikipedia.org/wiki/Elliptic_curve_point_multiplication#Double-and-add
            foreach (byte idx in bytes)
            {
                string? binaryString = Convert.ToString(bytes[idx], 2);
                for (int i = 0; i < 8; i++)
                {
                    if (i < binaryString.Length && binaryString[i] == '1')
                    {
                        result = Add(result, point);
                    }

                    point = Double(point);
                }
            }

            return new AffinePoint(result.X, result.Y);
        }

        public static AffinePoint Identity()
        {
            return new AffinePoint(FpE.Zero, FpE.One);
        }

        public static FpE? GetYCoordinate(FpE x, bool returnPositiveY)
        {
            FpE one = FpE.One;
            FpE num = x * x;
            FpE den = num * D - one;
            num = num * A - one;

            FpE? y = num / den;

            if (y is null)
                return null;

            if (!FpE.Sqrt(y.Value, out FpE z))
                return null;

            bool isLargest = z.LexicographicallyLargest();

            return isLargest == returnPositiveY ? z : z.Negative();
        }

        public static AffinePoint operator +(in AffinePoint a, in AffinePoint b)
        {
            return Add(a, b);
        }

        public static AffinePoint operator -(in AffinePoint a, in AffinePoint b)
        {
            return Sub(a, b);
        }

        public static AffinePoint operator *(in AffinePoint a, in FrE b)
        {
            return ScalarMultiplication(a, b);
        }

        public static AffinePoint operator *(in FrE a, in AffinePoint b)
        {
            return ScalarMultiplication(b, a);
        }

        public static bool operator ==(in AffinePoint a, in AffinePoint b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(in AffinePoint a, in AffinePoint b)
        {
            return !(a == b);
        }

        private bool Equals(AffinePoint a)
        {
            return X.Equals(a.X) && Y.Equals(a.Y);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj.GetType() == GetType() && Equals((AffinePoint)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}
