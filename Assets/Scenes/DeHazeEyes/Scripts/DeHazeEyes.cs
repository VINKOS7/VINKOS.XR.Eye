using System;
using System.Collections;
using System.IO;
using Meta.XR;
using Meta.XR.Samples;

using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Scenes.DeHazeEyes
{
    [MetaCodeSample("Scene-DeHazeEyes")]
    public class DeHazeEyes : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private Text m_debugText;
        [SerializeField] private RawImage m_image;

        private Texture2D m_cpuTexture;
        private RenderTexture m_gpuTexture;
        private byte[] m_managedBuffer; // Наш "безопасный" буфер

        // НОВОЕ: флаг готовности, чтобы не спамить ReadPixels раньше времени
        private bool m_isReadyToProcess = false;

        private IEnumerator Start()
        {
            // Ждем запуска камеры
            while (!m_cameraAccess.IsPlaying) yield return null;

            var rawTex = m_cameraAccess.GetTexture();

            if (rawTex is RenderTexture rt)
            {
                m_gpuTexture = rt;
                m_cpuTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                m_image.texture = m_cpuTexture;

                Debug.Log($"[DeHazeEyes] Инициализировано: {rt.width}x{rt.height}");

                // НОВОЕ: Даем драйверу SyncBoss 10 кадров, чтобы очередь BufferQueue наполнилась
                // Это должно убрать ошибки "abandoned"
                for (int i = 0; i < 10; i++)
                {
                    yield return new WaitForEndOfFrame();
                }

                m_isReadyToProcess = true;
                Debug.Log("[DeHazeEyes] Система готова к обработке.");
            }
            else
            {
                Debug.LogError("[DeHazeEyes] Текстура камеры не является RenderTexture!");
            }
        }

        private void Update()
        {
            m_debugText.text = OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess)
                ? "Permission granted."
                : "No permission granted.";

            // НОВОЕ: Добавлена проверка m_isReadyToProcess
            if (m_isReadyToProcess && m_cameraAccess.IsPlaying && m_cpuTexture != null)
            {
                SyncAndProcess2();
            }
        }

        private void SyncAndProcess2()
        {
            // 1. Копируем данные через Graphics.Blit (это надежнее для защищенных текстур)
            // Создаем временный буфер
            RenderTexture tempRT = RenderTexture.GetTemporary(m_gpuTexture.width, m_gpuTexture.height, 0);

            // Копируем из камеры в буфер
            Graphics.Blit(m_gpuTexture, tempRT);

            // 2. Читаем из буфера в нашу Texture2D
            var activeRT = RenderTexture.active;
            RenderTexture.active = tempRT;
            m_cpuTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
            m_cpuTexture.Apply(); // Сначала просто выведем без обработки

            RenderTexture.active = activeRT;
            RenderTexture.ReleaseTemporary(tempRT);

            // 3. Если картинка появилась, раскомментируй OpenCV ниже:
            var rawData = m_cpuTexture.GetRawTextureData<byte>();

            // Инициализируем наш массив один раз
            if (m_managedBuffer == null || m_managedBuffer.Length != rawData.Length)
            {
                m_managedBuffer = new byte[rawData.Length];
            }

            // ШАГ 1: Копируем данные из защищенной памяти в обычный массив
            rawData.CopyTo(m_managedBuffer);

            try
            {
                unsafe
                {
                    fixed (byte* ptr = m_managedBuffer)
                    {
                        OpenCVFacade.EnhanceContrast((IntPtr)ptr, m_cpuTexture.width, m_cpuTexture.height, 20);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[OpenCV] Ошибка при вызове функции: {e.Message}");
            }

            // ШАГ 3: Возвращаем измененные данные в текстуру
            m_cpuTexture.LoadRawTextureData(m_managedBuffer);
            m_cpuTexture.Apply();

        }

        private void SyncAndProcess()
        {
            // Сохраняем активный RT, чтобы не сломать рендер Unity
            var activeRT = RenderTexture.active;
            RenderTexture.active = m_gpuTexture;

            // Копируем данные из GPU (Passthrough) в нашу Texture2D (RAM)
            m_cpuTexture.ReadPixels(new Rect(0, 0, m_gpuTexture.width, m_gpuTexture.height), 0, 0);

            // НОВОЕ: Apply(false, false) быстрее, так как не обновляет мип-мапы и не выгружает из RAM
            // Но нам нужно сначала обработать через OpenCV

            RenderTexture.active = activeRT;

            // 2. Обработка через OpenCV
            var w = m_cpuTexture.width;
            var h = m_cpuTexture.height;
            //var rawData = m_cpuTexture.GetRawTextureData<byte>();

            //unsafe
            //{
            //    var ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(rawData);
            //    OpenCVFacade.ToNegative(ptr, w, h);
            //    //OpenCVFacade.EnhanceContrast(ptr, w, h, 2.0f, 8);
            //}

            //// 3. Отправляем на экран
            //m_cpuTexture.Apply(false);
        }
    }
}