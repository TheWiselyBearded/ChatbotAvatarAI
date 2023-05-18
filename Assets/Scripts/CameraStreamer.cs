using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Net.Sockets;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

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

    private Thread receiveThread;

    private Queue<string> identifiedObjectsQueue = new Queue<string>();
    private object queueLock = new object();

    //protected StreamReader reader;

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

        // Start a separate thread to receive JSON data
        //reader = new StreamReader(stream, Encoding.UTF8);
        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();
    }

    
    void ReceiveData() {
        BinaryReader reader = new BinaryReader(stream, Encoding.UTF8);
        while (isStreaming) {
            try {
                Debug.Log("Attempt to readline");
                //string jsonData = reader.ReadLine();
                // Read the size of the JSON data as an integer
                int dataSize = reader.ReadInt32();
                Debug.Log("Received data size: " + dataSize + " bytes");

                // Read the JSON data as bytes
                byte[] jsonData = reader.ReadBytes(dataSize);

                // Convert the bytes to a string
                string jsonString = Encoding.UTF8.GetString(jsonData);
                Debug.Log("Received JSON data: " + jsonString);
                Debug.Log($"Received total of {jsonData.Length} bytes");
                /*if (!string.IsNullOrEmpty(jsonData)) {
                    lock (queueLock) {
                        identifiedObjectsQueue.Enqueue(jsonData);
                    }

                    // Decode the JSON data
                    List<Dictionary<string, object>> objectsData = JsonUtility.FromJson<List<Dictionary<string, object>>>(jsonData);

                    // Process the identified objects
                    foreach (Dictionary<string, object> objectData in objectsData) {
                        int labelId = Convert.ToInt32(objectData["label_id"]);
                        string labelName = objectData["label_name"].ToString();
                        float confidence = Convert.ToSingle(objectData["confidence"]);
                        //List<float> bbox = objectData["bbox"] as List<float>;

                        // Print the values for now
                        Debug.Log("Label ID: " + labelId);
                        Debug.Log("Label Name: " + labelName);
                        Debug.Log("Confidence: " + confidence);
                        //Debug.Log("Bounding Box: " + string.Join(", ", bbox));
                        Debug.Log("--");
                    }
                }*/
            } catch (Exception e) {
                Debug.Log("Error receiving data: " + e.Message);
                break;
            }
        }

        reader.Close();
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
            //cam.Render();
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();

            // Encode the frame as JPEG
            byte[] bytes = texture.EncodeToJPG();
            Debug.Log($"Transmitting {bytes.Length} bytes");

            // Send the frame to the server
            //writer.WriteLine(bytes.Length);
            stream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
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
        // Stop and join the receive thread
        if (receiveThread != null && receiveThread.IsAlive) {
            receiveThread.Join();
        }
    }
}
