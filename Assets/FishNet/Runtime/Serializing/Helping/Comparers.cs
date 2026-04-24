using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace FishNet.Serializing.Helping
{

    public class Comparers
    {
        /// <summary>
        /// Returns if A equals B using EqualityCompare.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool EqualityCompare<T>(T a, T b)
        {
            return (EqualityComparer<T>.Default.Equals(a, b));
        }

        public static bool IsDefault<T>(T t)
        {
            return t.Equals(default(T));
        }

    }


    internal class SceneComparer : IEqualityComparer<Scene>
    {
        public bool Equals(Scene a, Scene b)
        {
            if (!a.IsValid() || !b.IsValid())
                return false;

            ulong aHandle = a.handle.GetRawData();
            ulong bHandle = b.handle.GetRawData();
            if (aHandle != 0 || bHandle != 0)
                return (aHandle == bHandle);

            return (a.name == b.name);
        }

        public int GetHashCode(Scene obj)
        {
            return obj.GetHashCode();
        }
    }

}
