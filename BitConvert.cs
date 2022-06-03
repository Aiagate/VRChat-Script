
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

public class BitConvert : UdonSharpBehaviour
{
    void Start()
    {
        sbyte x = (sbyte)127;
        sbyte y = (sbyte)0;
        sbyte z = (sbyte)-128;

        Debug.Log($"{AxisToColor(x,y,z)}");
        Debug.Log($"{ColorToAxis(AxisToColor(x,y,z))}");
        int i = -1;
        Debug.Log($"{i:d4}");
        // Debug.Log($"{(byte)color.r}");
        // Debug.Log($"{(byte)color.g}");
        // Debug.Log($"{(byte)color.b}");
        // Debug.Log($"{Convert.ToSByte(Convert.ToString(color.r, 2), 2)}");
        // Debug.Log($"{Convert.ToSByte(Convert.ToString(color.g, 2), 2)}");
        // Debug.Log($"{Convert.ToSByte(Convert.ToString(color.b, 2), 2)}");


        // byte x = (byte)(((byte)1   ^ 0xFF) + 1);
        // byte y = (byte)(((byte)16  ^ 0xFF) + 1);
        // byte z = (byte)(((byte)128 ^ 0xFF) + 1);
        // Debug.Log($"{x}");
        // Debug.Log($"{y}");
        // Debug.Log($"{z}");
        // Debug.Log($"{Convert.ToSByte(Convert.ToString(x, 2), 2)}");
        // Debug.Log($"{Convert.ToSByte(Convert.ToString(y, 2), 2)}");
        // Debug.Log($"{Convert.ToSByte(Convert.ToString(z, 2), 2)}");
    }

    public Color32 AxisToColor(sbyte x, sbyte y, sbyte z)
    {
        Color32 color = new Color32((byte)0,(byte)0,(byte)0,(byte)0);
        color.r = x >= 0 ? (byte)x : (byte)((Math.Abs((int)x) ^ 0xFF) + 1);
        color.g = y >= 0 ? (byte)y : (byte)((Math.Abs((int)y) ^ 0xFF) + 1);
        color.b = z >= 0 ? (byte)z : (byte)((Math.Abs((int)z) ^ 0xFF) + 1);
        return color;
    }

    public Vector3Int ColorToAxis(Color32 color)
    {
        Vector3Int axis = Vector3Int.zero;
        axis.x = Convert.ToSByte(Convert.ToString(color.r, 2), 2);
        axis.y = Convert.ToSByte(Convert.ToString(color.g, 2), 2);
        axis.z = Convert.ToSByte(Convert.ToString(color.b, 2), 2);
        return axis;
    }
}
