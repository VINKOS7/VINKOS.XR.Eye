using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class EyesStreamer : MonoBehaviour
{
    [SerializeField] private string m_serverIp = "192.168.137.1";
    [SerializeField] private int m_port = 1984;

    private TcpClient m_client;
    private NetworkStream m_stream;
    private Texture2D m_encodeTexture;

    private byte[] m_rawBuffer; // Промежуточный буфер для байтов
    private byte[] m_jpgToSend; // Готовый JPG
    private readonly object m_lock = new();
    private bool m_isRunning = true;
    private bool m_hasNewRawData = false;

    private void Start()
    {
        Thread netThread = new Thread(NetworkLoop) { IsBackground = true };
        netThread.Start();
    }

    // БЕЗОПАСНЫЙ метод: только копирует данные
    public void PushFrame(byte[] jpgData)
    {
        lock (m_lock)
        {
            m_jpgToSend = jpgData; // Просто сохраняем массив для отправки в NetworkLoop
        }
    }

    private void Update()
    {
        // Вся работа с текстурами — ТОЛЬКО ТУТ (Main Thread)
        byte[] dataToEncode = null;
        lock (m_lock)
        {
            if (!m_hasNewRawData) return;
            dataToEncode = m_rawBuffer;
            m_hasNewRawData = false;
        }

        if (dataToEncode != null)
        {
            ProcessTexture(dataToEncode);
        }
    }

    private void ProcessTexture(byte[] rawData)
    {
        // Инициализация текстуры 640x480 (или твое разрешение)
        if (m_encodeTexture == null) m_encodeTexture = new Texture2D(640, 480, TextureFormat.RGBA32, false);

        m_encodeTexture.LoadRawTextureData(rawData);
        m_encodeTexture.Apply();

        byte[] jpg = m_encodeTexture.EncodeToJPG(75);

        lock (m_lock)
        {
            m_jpgToSend = jpg;
        }
    }

    private void NetworkLoop()
    {
        while (m_isRunning)
        {
            try
            {
                // 1. ПОДКЛЮЧЕНИЕ: Только если клиент реально мертв
                if (m_client == null || !m_client.Connected)
                {
                    Debug.Log($"[EyesStreamer] Connecting to {m_serverIp}:{m_port}...");
                    m_client = new TcpClient(m_serverIp, m_port);
                    m_stream = m_client.GetStream();
                    Debug.Log("[EyesStreamer] CONNECTED!");
                }

                // 2. ОТПРАВКА: Если подключены и есть свежий кадр
                byte[] toSend = null;
                lock (m_lock)
                {
                    if (m_jpgToSend != null)
                    {
                        toSend = m_jpgToSend;
                        m_jpgToSend = null; // Забираем кадр, чтобы не слать дубли
                    }
                }

                if (toSend != null && m_stream != null)
                {
                    m_stream.Write(toSend, 0, toSend.Length);
                    // m_stream.Flush(); // Можно не флушить каждый раз для экономии ресурсов
                }

                // 3. ОТДЫХ: Обязательно спим, чтобы не спамить впустую
                // 15ms ~= 60fps. Если кадров нет, спим чуть дольше.
                Thread.Sleep(toSend != null ? 15 : 30);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EyesStreamer] Socket Error: {e.Message}");
                m_client?.Close();
                m_client = null;
                Thread.Sleep(2000); // При ошибке ждем 2 сек перед реконнектом
            }
        }
    }
    private void OnDestroy()
    {
        m_isRunning = false;
        m_client?.Close();
    }
}