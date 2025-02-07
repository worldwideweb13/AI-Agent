using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class GeminiManager : MonoBehaviour
{
    private string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

    /// <summary>
    /// --- リクエスト用クラス ---
    ///  API に送信するデータの形式を定義
    /// </summary>
    [Serializable]
    public class ChatRequest
    {
        public List<Message> contents;
    }

    /// <summary>
    /// API疎通用の型定義。以下の形式を定義
    /// 
    /// </summary>
    [Serializable]
    public class Message
    {
        public string role;
        public List<Part> parts;
    }

    [Serializable]

    public class Part
    {
        public string text;
    }

    // Start is called before the first frame update
    void Start()
    {
        ChatRequest requestData = new ChatRequest
        {
            contents = new List<Message>
            {
                new Message {
                    role = "user",
                    parts = new List<Part> { new Part { text = "こんにちは、Gemini 1.5 Flash APIさん！" } }
                }
            }
        };

        StartCoroutine(SendChatRequest(requestData));
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator SendChatRequest(ChatRequest requestData)
    {
        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log("送信するJSON" + jsonData);
        // JSON をバイト配列に変換してアップロードハンドラーにセット
        byte[] postData = Encoding.UTF8.GetBytes(jsonData);
        using (UnityWebRequest request = new UnityWebRequest(API_URL + "?key=" + Keys.GEMINI_KEY, "POST"))
        {
            Debug.Log("送信するURL" + API_URL + "?key=" + Keys.GEMINI_KEY);

            request.uploadHandler = new UploadHandlerRaw(postData);
            // ダウンロードハンドラーを設定してレスポンスを取得
            request.downloadHandler = new DownloadHandlerBuffer();

            // 3. リクエストヘッダーの設定
            request.SetRequestHeader("Content-Type", "application/json");

            // 4. API へリクエストを送信し、レスポンスを待機
            yield return request.SendWebRequest();

            // 5. エラー処理
            if(request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("API通信エラー" + request.error);
            }
            else
            {
                // レスポンスのJSON取得
                string requestText = request.downloadHandler.text;
                Debug.Log("API成功: " + requestText);

                // 6. JSON を ChatResponse オブジェクトに変換
            //     ChatResponse chatResponse = JsonUtility.FromJson<ChatResponse>(requestText);

            //     if(
            //         chatResponse != null &&
            //         chatResponse.candidates != null &&
            //         chatResponse.candidates.Count > 0
            //     )
            //     {
            //         string aiResponse = chatResponse.candidates[0].content.parts[0].text;
            //         Debug.Log("AIからの返事: " + aiResponse);
            //     }
            //     else
            //     {
            //         Debug.LogWarning("想定したレスポンス形式ではありません。");
            //     }

            }

        }
    }
}
