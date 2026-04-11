using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class OpenCVFacade : MonoBehaviour
{
    private const string libName = "opencv_facade";

    [DllImport(libName, EntryPoint = "GetOpenCVVersion")]
    public static extern IntPtr GetOpenCVVersion();

    [DllImport(libName, EntryPoint = "IsGPUAvailable")]
    public static extern bool IsGPUAvailable();

    [DllImport(libName, EntryPoint = "InitDetector")]
    public static extern bool InitDetector(string cascadePath);

    [DllImport(libName, EntryPoint = "ToGray")]
    public static extern void ToGray(IntPtr data, int width, int height);

    [DllImport(libName, EntryPoint = "ToNegative")]
    public static extern void ToNegative(IntPtr data, int width, int height);

    [DllImport(libName, EntryPoint = "SimpleTest")]
    public static extern void SimpleTest(IntPtr data, int width, int height);

    [DllImport(libName, EntryPoint = "DetectFaces")]
    public static extern void DetectFaces(IntPtr data, int width, int height, string path, int minNeighbors = 6, double scaleFactor = 1.15);

    [DllImport(libName, EntryPoint = "EnhanceContrast")]
    public static extern void EnhanceContrast(IntPtr data, int width, int height, float clipLimit = 2.0f, int gridSize = 8);

    [DllImport(libName, EntryPoint = "OffsetView")]
    public static extern void OffsetView(IntPtr data, int width, int height, int offsetX, int offsetY);

    void Start()
    {
        var versionPtr = GetOpenCVVersion();
        var version = Marshal.PtrToStringAnsi(versionPtr);

        Debug.Log($"[OpenCVFacade] OpenCV Version: {version}");
        Debug.Log($"[OpenCVFacade] GPU Support: {IsGPUAvailable()}");
    }
}