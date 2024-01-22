using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.UI; // 需要导入这个命名空间
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class LoginManager : MonoBehaviour
{
    public InputField userName;
    public InputField passWord;
    public Button btn;

    private const string loginUrl = "http://127.0.0.1:5000/login";

    private void Start()
    {
        // 示例使用 application/x-www-form-urlencoded 格式

        // 如果你的服务器端期望 JSON 格式，使用下面的示例
        // StartCoroutine(LoginRequestJson("123", "123"));

        btn.onClick.AddListener(() =>
        {
            StartCoroutine(LoginRequest());
        });
    }

    private IEnumerator LoginRequest()
    {
        string url = "http://127.0.0.1:5000/auth/login";

        // Create login request data as JSON
        JObject obj = new JObject();
        obj.Add("username", userName.text);
        obj.Add("password", passWord.text);
        //string jsonData = "{\"username\":\"12323\",\"password\":\"123\"}";
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(obj.ToString());

        UnityWebRequest request = UnityWebRequest.Post(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(responseJson);

            if (loginResponse != null)
            {
                if (loginResponse.success)
                {
                    Debug.Log("Login Success: " + loginResponse.message);

                    string token = loginResponse.token;

                    Debug.Log("token: " + loginResponse.token);
                }
                else
                {
                    Debug.Log("Login Failed: " + loginResponse.message);
                }
            }
            else
            {
                Debug.LogError("Error parsing JSON response.");
            }
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }
}

[System.Serializable]
public class LoginResponse
{
    public bool success;
    public string message;
    public string token;
}