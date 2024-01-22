using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.UI; // 需要导入这个命名空间
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections.LowLevel.Unsafe;

public class LoginManager : MonoBehaviour
{
    public static string TOKEN = "";

    public InputField userName;
    public InputField passWord;
    public Button btn;
    public Button btn_LoginOut;
    private const string loginUrl = "http://127.0.0.1:5000/Auth/login";

    private string loginOutUrl = "http://127.0.0.1:5000/Auth/logout";

    private void Start()
    {
        btn.onClick.AddListener(() =>
        {
            StartCoroutine(LoginRequest());
        });

        btn_LoginOut.onClick.AddListener(() =>
        {
            StartCoroutine(LogoutRequest());
        });
    }

    private IEnumerator LoginRequest()
    {
        // Construct JSON data for login request
        JObject json = new JObject();
        json.Add("Username", userName.text);
        json.Add("Password", passWord.text);

        // Send POST request to the login endpoint
        using (UnityWebRequest request = new UnityWebRequest(loginUrl, "POST"))
        {
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(json.ToString());
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Request successful, parse server response
                string responseJson = request.downloadHandler.text;
                LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseJson);

                if (loginResponse != null && loginResponse.Success)
                {
                    Debug.Log("Login Successful! Token: " + loginResponse.Token);

                    TOKEN = loginResponse.Token;
                    // Include the token in subsequent requests to secure endpoints
                    StartCoroutine(SecureEndpointRequest(loginResponse.Token));
                }
                else
                {
                    Debug.Log("Login Failed: " + loginResponse.Message);
                }
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }

    private IEnumerator LogoutRequest()
    {
        UnityWebRequest request = UnityWebRequest.Post(loginOutUrl, "");
        request.SetRequestHeader("Authorization", "Bearer " + LoginManager.TOKEN);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Logout successful");
            // 在这里处理退出登录成功的逻辑，例如清除本地保存的Token等
            LoginManager.TOKEN = "";
        }
        else
        {
            Debug.LogError("Logout failed: " + request.error);
            // 在这里处理退出登录失败的逻辑
        }
    }

    private IEnumerator SecureEndpointRequest(string token)
    {
        // Make a request to the secure endpoint with the JWT token
        string secureEndpointUrl = "http://127.0.0.1:5000/Auth/secure";

        using (UnityWebRequest request = new UnityWebRequest(secureEndpointUrl, "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Request to secure endpoint successful
                string responseJson = request.downloadHandler.text;
                Debug.Log("Secure Endpoint Response: " + responseJson);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }
}

[System.Serializable]
public class LoginResponse
{
    public bool Success;
    public string Message;
    public string Token;
}