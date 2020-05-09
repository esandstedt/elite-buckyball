using EliteBuckyball.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EliteBuckyball.Application
{
    public class Vector
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector Add(Vector that)
        {
            return new Vector(
                this.X + that.X,
                this.Y + that.Y,
                this.Z + that.Z
            );
        }

        public Vector Subtract(Vector that)
        {
            return new Vector(
                this.X - that.X,
                this.Y - that.Y,
                this.Z - that.Z
            );
        }

        public Vector Multiply(double value)
        {
            return new Vector(
                value * this.X,
                value * this.Y,
                value * this.Z
            );
        }

        public double Dot(Vector that)
        {
            return this.X * that.X + this.Y * that.Y + this.Z * that.Z;
        }

        public double Magnitude()
        {
            return Math.Sqrt(Math.Pow(this.X, 2) + Math.Pow(this.Y, 2) + Math.Pow(this.Z, 2));
        }

        public double Distance(Vector that)
        {
            return this.Subtract(that).Magnitude();
        }

        public static explicit operator Vector(StarSystem system) => new Vector(system.X, system.Y, system.Z);

    }
}
