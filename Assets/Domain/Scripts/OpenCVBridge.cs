using System.Runtime.InteropServices;

public static class OpenCVBridge
{
    private delegate TOutput Lambda<TInput, TOutput>(TInput item);

    [DllImport("opencv_world", EntryPoint = "OpenCV_GetVersion")]
    private static extern double OpenCV_GetVersion();
    public static double GetVersion() => OpenCV_GetVersion();


    [DllImport("opencv_world", EntryPoint = "cvGetTickFrequency")]
    private static extern double cvGetTickFrequency();
    public static double GetTickFrequency() => cvGetTickFrequency();
}
