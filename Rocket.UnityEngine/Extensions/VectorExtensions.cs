using UnityEngine;

namespace Rocket.UnityEngine.Extensions
{
    public static class VectorExtensions
    {
        public static System.Numerics.Vector2 ToRocketVector(this Vector2 vector)
        {
            return new System.Numerics.Vector2
            {
                X = vector.x,
                Y = vector.y
            };
        }

        public static Vector2 ToUnityVector(this System.Numerics.Vector2 vector)
        {
            return new Vector2
            {
                x = vector.X,
                y = vector.Y
            };
        }

        public static System.Numerics.Vector3 ToRocketVector(this Vector3 vector)
        {
            return new System.Numerics.Vector3
            {
                X = vector.x,
                Y = vector.y,
                Z = vector.z
            };
        }

        public static Vector3 ToUnityVector(this System.Numerics.Vector3 vector)
        {
            return new Vector3
            {
                x = vector.X,
                y = vector.Y,
                z = vector.Z
            };
        }

        public static System.Numerics.Vector4 ToRocketVector(this Vector4 vector)
        {
            return new System.Numerics.Vector4
            {
                X = vector.x,
                Y = vector.y,
                Z = vector.z,
                W = vector.w
            };
        }

        public static Vector4 ToUnityVector(this System.Numerics.Vector4 vector)
        {
            return new Vector4
            {
                x = vector.X,
                y = vector.Y,
                z = vector.Z,
                w = vector.W
            };
        }

    }
}