using UnityEngine;

namespace Rocket.UnityEngine.Extensions
{
    public static class VectorExtensions
    {
        public static API.Math.Vector3 ToRocketVector(this Vector3 vector)
        {
            return new API.Math.Vector3
            {
                X = vector.x,
                Y = vector.y,
                Z = vector.z
            };
        }

        public static Vector3 ToUnityVector(this API.Math.Vector3 vector)
        {
            return new Vector3
            {
                x = vector.X,
                y = vector.Y,
                z = vector.Z
            };
        }
    }
}