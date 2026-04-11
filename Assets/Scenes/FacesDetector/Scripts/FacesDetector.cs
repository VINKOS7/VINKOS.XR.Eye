using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Meta.XR;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Scenes.FacesDetector
{
    public class FacesDetector : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private Text m_debugText;
        [SerializeField] private RawImage m_image;
        [SerializeField] private int m_minNeighbors = 3;
        [SerializeField] private double m_scaleFactor = 1.15;
        [SerializeField] private EyesStreamer m_eyesStreamer;

        private const int TARGET_WIDTH = 640;
        private const int TARGET_HEIGHT = 480;

        private Texture2D m_cpuTexture;
        private RenderTexture m_gpuTexture;
        private byte[] m_managedBuffer;
        private bool m_isReadyToProcess = false;
        private bool m_isModelReady = false;
        private bool m_isProcessingFrame = false; // Флаг для асинхронности
        private string m_finalCascadePath;
        private float m_nextFrameTime = 0f;
        private float m_frameInterval = 1f / 30f; // 30 FPS

        private async void Start()
        {
            await PrepareCascadeModelAsync();

            while (!m_cameraAccess.IsPlaying) await Task.Yield();

            var rawTex = m_cameraAccess.GetTexture();

            if (rawTex is RenderTexture rt)
            {
                m_gpuTexture = rt;

                // Создаем CPU текстуру сразу в низком разрешении
                m_cpuTexture = new Texture2D(TARGET_WIDTH, TARGET_HEIGHT, TextureFormat.RGBA32, false);
                m_image.texture = m_cpuTexture;

                Debug.Log($"[DeHazeEyes] Инициализировано. Камера: {rt.width}x{rt.height}, Детектор: {TARGET_WIDTH}x{TARGET_HEIGHT}");

                for (var i = 0; i < 10; i++) await Task.Yield();

                m_isReadyToProcess = true;
            }
        }

        private void Update()
        {
            if (m_debugText != null)
            {
                m_debugText.text = OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess)
                    ? $"Camera Access: OK; minNeighbors={m_minNeighbors}; scaleFactor={m_scaleFactor}" : "No Permission";
            }

            if (m_isReadyToProcess && m_cameraAccess.IsPlaying && !m_isProcessingFrame)
            {
                if (Time.time >= m_nextFrameTime)
                {
                    ProcessAsync();
                    m_nextFrameTime = Time.time + m_frameInterval;
                }
            }
        }

        private async void ProcessAsync()
        {
            m_isProcessingFrame = true;

            var tempRT = RenderTexture.GetTemporary(TARGET_WIDTH, TARGET_HEIGHT, 0);

            Graphics.Blit(m_gpuTexture, tempRT);

            var activeRT = RenderTexture.active;

            RenderTexture.active = tempRT;

            m_cpuTexture.ReadPixels(new Rect(0, 0, TARGET_WIDTH, TARGET_HEIGHT), 0, 0);

            RenderTexture.active = activeRT;

            RenderTexture.ReleaseTemporary(tempRT);

            var rawData = m_cpuTexture.GetRawTextureData<byte>();

            byte[] jpgData = m_cpuTexture.EncodeToJPG(75);

            if (m_managedBuffer == null || m_managedBuffer.Length != rawData.Length) m_managedBuffer = new byte[rawData.Length];

            rawData.CopyTo(m_managedBuffer);

            await Task.Run(() =>
            {
                try
                {
                    m_eyesStreamer.PushFrame(jpgData);

                    unsafe
                    {
                        fixed (byte* ptr = m_managedBuffer)
                        {
                            if (m_isModelReady)
                            {
                                OpenCVFacade.DetectFaces((IntPtr)ptr, TARGET_WIDTH, TARGET_HEIGHT, m_finalCascadePath, m_minNeighbors, m_scaleFactor);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[OpenCV] Native Error: {e.Message}");
                }
            });

            m_cpuTexture.LoadRawTextureData(m_managedBuffer);
            m_cpuTexture.Apply();

            m_isProcessingFrame = false;
        }

        private async Task PrepareCascadeModelAsync()
        {
            var sourcePath = Path.Combine(Application.streamingAssetsPath, "haarcascade_frontalface_default.xml");

            m_finalCascadePath = Path.Combine(Application.persistentDataPath, "faces_model.xml");

            if (!File.Exists(m_finalCascadePath))
            {
                using var request = UnityWebRequest.Get(sourcePath);

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    File.WriteAllBytes(m_finalCascadePath, request.downloadHandler.data);

                    m_isModelReady = true;
                }
            }
            else m_isModelReady = true;
        }
    }
}