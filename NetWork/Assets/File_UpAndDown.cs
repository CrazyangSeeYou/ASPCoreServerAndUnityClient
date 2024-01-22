using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class File_UpAndDown : MonoBehaviour
{
    public Button btn_Up;
    public Button btn_Down;

    public string userName = "XXX";

    // Start is called before the first frame update
    private void Start()
    {
        btn_Up.onClick.AddListener(() =>
        {
            StartCoroutine(UploadFile());
        });

        btn_Down.onClick.AddListener(() =>
        {
            StartCoroutine(DownloadFile());
        });
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private IEnumerator UploadFile()
    {
        string apiUrl = "http://127.0.0.1:5000/File/upload";
        string filePath = "E:\\GitHub_Project\\NetWork\\File\\UpLoad\\file.txt";

        byte[] fileData = File.ReadAllBytes(filePath);

        var fileRequest = new
        {
            UserId = userName,
            FileName = "file.txt",
            FileData = fileData
        };

        string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(fileRequest);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonRequest);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("File uploaded successfully.");
            }
            else
            {
                Debug.LogError($"Error uploading file: {request.error}");
            }
        }
    }

    private IEnumerator DownloadFile()
    {
        string apiUrl = "http://127.0.0.1:5000/File/download"; // �滻����ķ�������ؽӿڵ�ַ
        string userId = userName; // �滻��ʵ���û�ID
        string fileName = "file.txt"; // �滻��ʵ���ļ���

        // ������������URL
        string downloadUrl = $"{apiUrl}?userId={userId}&fileName={fileName}";

        // ����UnityWebRequest
        using (UnityWebRequest request = UnityWebRequest.Get(downloadUrl))
        {
            // ��������
            yield return request.SendWebRequest();

            // ������Ӧ
            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] fileData = request.downloadHandler.data;

                // �����ļ�������
                File.WriteAllBytes($"E:\\NetWork\\NetWork\\File\\DownLoad\\{fileName}", fileData); // �滻��ʵ�ʱ���·��

                Debug.Log("File downloaded successfully.");
            }
            else
            {
                Debug.LogError($"Error downloading file: {request.error}");
            }
        }
    }
}