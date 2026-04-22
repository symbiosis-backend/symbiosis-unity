using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    private string baseUrl = "https://dlsymbiosis.com";

    public void Register(string email, string password, string nickname)
    {
        StartCoroutine(RegisterCoroutine(email, password, nickname));
    }

    private IEnumerator RegisterCoroutine(string email, string password, string nickname)
    {
        string json = JsonUtility.ToJson(new RegisterData
        {
            email = email,
            password = password,
            nickname = nickname
        });

        UnityWebRequest request = new UnityWebRequest(baseUrl + "/register", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        Debug.Log(request.downloadHandler.text);
    }

    [System.Serializable]
    public class RegisterData
    {
        public string email;
        public string password;
        public string nickname;
    }
}
