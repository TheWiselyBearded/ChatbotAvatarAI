using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Net.Sockets;
using System.IO;
using System;

public class CameraStreamer : MonoBehaviour {
    public int port = 8080;
    public string host = "localhost";
    public float frameRate = 1f; // Desired frame rate
    public Camera cam;

    private Texture2D texture;
    private bool isStreaming = false;
    private TcpClient client;
    private NetworkStream stream;
    private StreamWriter writer;
    private float nextFrameTime = 0f; // Time of the next frame

    void Start() {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        // Start capturing video from the specified camera
        if (cam == null) {
            Debug.LogError("Camera reference is missing.");
            return;
        }

        cam.targetTexture = new RenderTexture(Screen.width, Screen.height, 24);
        RenderTexture.active = cam.targetTexture;

        texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        isStreaming = true;

        // Connect to the server
        client = new TcpClient(host, port);
        stream = client.GetStream();
        writer = new StreamWriter(stream);

        Debug.Log("Connected to " + host + ":" + port);

        // Set the initial next frame time
        nextFrameTime = Time.time;
    }

    private void OnDestroy() {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera) {
        if (camera.Equals(cam) && isStreaming && client != null && Time.time >= nextFrameTime) {
            // Set the time for the next frame
            nextFrameTime += 1f / frameRate;

            // Capture a frame from the camera
            RenderTexture.active = cam.targetTexture;
            cam.Render();
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();

            // Encode the frame as JPEG
            byte[] bytes = texture.EncodeToJPG();
            Debug.Log($"Transmitting {bytes.Length} bytes");

            // Send the frame to the server
            //writer.WriteLine(bytes.Length);
            //writer.Flush();
            //stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }
    }

    void OnApplicationQuit() {
        isStreaming = false;

        // Close the connection
        if (writer != null) {
            writer.Close();
        }
        if (stream != null) {
            stream.Close();
        }
        if (client != null) {
            client.Close();
        }
    }
}
