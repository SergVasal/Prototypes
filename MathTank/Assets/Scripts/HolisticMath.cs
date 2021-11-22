﻿using UnityEngine;

namespace DefaultNamespace
{
    public class HolisticMath
    {
        public static Coords GetNormal(Coords vector)
        {
            var distance = Distance(new Coords(0f, 0f, 0f), vector);
            return new Coords(vector.x / distance, vector.y / distance, vector.z / distance);
        }

        public static float Distance(Coords point1, Coords point2)
        {
            var xSquared = Square(point1.x - point2.x);
            var ySquared = Square(point1.y - point2.y);
            var zSquared = Square(point1.z - point2.z);
            return Mathf.Sqrt(xSquared + ySquared + zSquared);
        }

        public static float Square(float value)
        {
            return value * value;
        }

        public static float Dot(Coords vector1, Coords vector2)
        {
            return vector1.x * vector2.x + vector1.y * vector2.y + vector1.z * vector2.z;
        }

        public static float Angle(Coords vector1, Coords vector2)
        {
            var dot = Dot(vector1, vector2);
            var distancesMultiplied = Distance(new Coords(0f, 0f, 0f), vector1) * Distance(new Coords(0f, 0f, 0f), vector2);
            return Mathf.Acos(dot / distancesMultiplied);
        }
    }
}