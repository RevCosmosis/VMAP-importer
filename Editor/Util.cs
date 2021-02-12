using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;
using Datamodel;
using DM = Datamodel.Datamodel;
using VMAP;

namespace VMAP
{
    public static class Util {
        public static int[] ConvertIntArray (Array<int> array) {
            int[] output = new int[array.Count];
            for (int i = 0; i < array.Count; i++) { output[i] = array[i]; }
            return output;
        }

        public static string[] ConvertStringArray (Array<string> array) {
            string[] output = new string[array.Count];
            for (int i = 0; i < array.Count; i++) { output[i] = array[i]; }
            return output;
        }

        public static Vector2[] ConvertV2Array (Array<System.Numerics.Vector2> array) {
            Vector2[] output = new Vector2[array.Count];
            for (int i = 0; i < array.Count; i++) { output[i] = new Vector2(array[i].X, array[i].Y); }
            return output;
        }

        public static Vector3[] ConvertV3Array (Array<System.Numerics.Vector3> array) {
            Vector3[] output = new Vector3[array.Count];
            for (int i = 0; i < array.Count; i++) { output[i] = new Vector3(array[i].X, array[i].Y, array[i].Z); }
            return output;
        }

        public static Vector4[] ConvertV4Array (Array<System.Numerics.Vector4> array) {
            Vector4[] output = new Vector4[array.Count];
            for (int i = 0; i < array.Count; i++) { output[i] = new Vector4(array[i].W, array[i].X, array[i].Y, array[i].Z); }
            return output;
        }
    }
}
