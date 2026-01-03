using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Util
{
    public static class Extension
    {
        public static Vector2 ToXZ(this Vector3 vector3)
        {
            return new Vector2(vector3.x, vector3.z);
        }

        public static T RandomPick<T>(this List<T> list)
        {
            var randomI = Random.Range(0, list.Count);
            return list[randomI];
        }

        public static T RandomPick<T>(this IEnumerable<T> enumerable)
        {
            var list = enumerable.ToList();
            var randomI = Random.Range(0, list.Count);

            return list[randomI];
        }
    }
}