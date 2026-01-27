using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.InputSystem; // Обязательно для InputActionProperty
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class Eye : MonoBehaviour
{
    const string DLL_NAME = "EyeBridge";

    // Твой мост с 4 аргументами (включая bool)
    [DllImport(DLL_NAME)]
    private static extern void ProcessEyeFrame(IntPtr rawData, int width, int height);

    [Header("Настройки AR")]
    public ARCameraManager cameraManager;
    public Renderer quadRenderer;

    [Header("Управление (Input System)")]
    // Сюда в инспекторе мы назначим кнопку X левого контроллера
    public InputActionProperty toggleButton;

    private Texture2D _texture;
    private bool _isGrayscale = false;

    void Start()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.Log($"[Eye] Разрешение к камерам нет, выполняем запрос...");

            Permission.RequestUserPermission(Permission.Camera);

            Debug.Log($"[Eye] Разрешение к камерам получено");
        }
        else Debug.Log($"[Eye] Разрешение к камерам есть изначально");
    }

    void OnEnable()
    {
        Debug.Log($"[Eye] += OnFrameReceived");

        cameraManager.frameReceived += OnFrameReceived;

        // Включаем отслеживание кнопки
        if (toggleButton.action != null) toggleButton.action.Enable();
    }

    void OnDisable()
    {
        Debug.Log($"[Eye] -= OnFrameReceived");

        cameraManager.frameReceived -= OnFrameReceived;

        // Выключаем, чтобы не тратить ресурсы
        if (toggleButton.action != null) toggleButton.action.Disable();
    }

    void Update()
    {
        // ИДЕАЛЬНАЯ ЛОГИКА НАЖАТИЯ
        // WasPressedThisFrame вернет true только в тот кадр, когда ты нажал кнопку.
        // Зажатие или дребезг контактов не повлияют.
        if (toggleButton.action != null && toggleButton.action.WasPressedThisFrame())
        {
            _isGrayscale = !_isGrayscale;
            Debug.Log($"[Eye] Режим переключен: {(_isGrayscale ? "ЧБ" : "Цвет")}");
        }
    }

    void OnFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        Debug.Log($"[Eye] OnFrameReceived");
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return;

        if (!_isGrayscale) return;

        using (image)
        {
            if (_texture == null || _texture.width != image.width)
            {
                _texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
                quadRenderer.material.mainTexture = _texture;
            }

            var conversionParams = new XRCpuImage.ConversionParams(image, TextureFormat.RGBA32);
            var rawTextureData = _texture.GetRawTextureData<byte>();

            unsafe
            {
                IntPtr ptr = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(rawTextureData);
                image.Convert(conversionParams, ptr, rawTextureData.Length);

                // Передаем флаг в C++
                ProcessEyeFrame(ptr, image.width, image.height);
            }

            _texture.Apply();
        }
    }
}