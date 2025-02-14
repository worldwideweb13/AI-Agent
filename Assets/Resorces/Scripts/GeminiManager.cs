using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class GeminiManager : MonoBehaviour
{
    /// <summary>
    /// Gemini APIのエンドポイントURL (APIキーは末尾に付与)
    /// </summary>
    private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

    /// <summary>
    /// AIとの会話履歴を表示するTextMeshProテキストボックス
    /// </summary>
    [SerializeField] private TMP_Text aiTextBox;

    /// <summary>
    /// ユーザー入力を受け付けるInputField
    /// </summary>
    [SerializeField] private TMP_InputField userInputField;

    /// <summary>
    /// ユーザーとAIの会話履歴を保持するリスト
    /// </summary>
    private readonly List<ContentMessage> chatHistory = new List<ContentMessage>();

    #region データクラス

    /// <summary>
    /// APIリクエスト用のデータクラス
    /// </summary>
    [Serializable]
    public class ChatRequest
    {
        public List<ContentMessage> contents;
    }

    /// <summary>
    /// 1件の会話（ユーザーまたはAI）を表すクラス
    /// </summary>
    [Serializable]
    public class ContentMessage
    {
        public string role;
        public List<Part> parts;
    }

    /// <summary>
    /// 会話内容のパートを表すクラス
    /// </summary>
    [Serializable]
    public class Part
    {
        public string text;
    }

    /// <summary>
    /// APIレスポンスを受け取るためのデータクラス
    /// </summary>
    [Serializable]
    public class ChatResponse
    {
        public List<Candidate> candidates;
    }

    /// <summary>
    /// APIレスポンス内の候補データを表すクラス
    /// </summary>
    [Serializable]
    public class Candidate
    {
        public ContentMessage content;
    }

    #endregion

    /// <summary>
    /// 初回処理
    /// </summary>
    private void Start()
    {
        // 初期の会話履歴として、初回メッセージを追加
        AddChatHistory("user", "こんにちは、Gemini 1.5 Flash APIさん！");

        // 初回リクエストを送信（会話履歴全体をリクエストに含める）
        StartCoroutine(SendRequestData());
    }

    /// <summary>
    /// UIの送信ボタンなどから呼び出される、ユーザー入力送信イベントハンドラ
    /// </summary>
    public void OnSendUserInput()
    {
        string userInputText = userInputField.text.Trim();
        if (string.IsNullOrEmpty(userInputText))
        {
            // 入力が空の場合は何もしない
            return;
        }

        // ユーザーのメッセージを履歴に追加し、UIにも表示
        AddChatHistory("user", userInputText);
        AppendChatLog("user", userInputText);

        // 入力欄をクリア
        userInputField.text = "";

        // 最新の会話履歴全体をAPIリクエストとして送信
        StartCoroutine(SendRequestData());
    }

    /// <summary>
    /// 会話履歴に新しいメッセージを追加する
    /// </summary>
    /// <param name="role">発言者の役割 ("user" や "model" など)</param>
    /// <param name="message">発言内容</param>
    private void AddChatHistory(string role, string message)
    {
        ContentMessage newMessage = new ContentMessage
        {
            role = role,
            parts = new List<Part> { new Part { text = message } }
        };
        chatHistory.Add(newMessage);
    }

    /// <summary>
    /// 会話ログのUIテキストに新しいメッセージを追加して表示する
    /// </summary>
    /// <param name="role">発言者の役割</param>
    /// <param name="message">メッセージ内容</param>
    private void AppendChatLog(string role, string message)
    {
        aiTextBox.text += $"{role}: {message}\n";
    }

    /// <summary>
    /// Gemini APIへ会話履歴全体をリクエストとして送信し、レスポンスを処理するコルーチン
    /// </summary>
    private IEnumerator SendRequestData()
    {
        // 現在の会話履歴をリクエストデータとして設定
        ChatRequest requestData = new ChatRequest { contents = chatHistory };

        // リクエストデータをJSON文字列に変換
        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log($"送信するJSON: {jsonData}");

        // JSON文字列をバイト配列に変換
        byte[] postData = Encoding.UTF8.GetBytes(jsonData);

        // APIエンドポイントにAPIキーを付与してUnityWebRequestを作成
        using (UnityWebRequest request = new UnityWebRequest($"{API_URL}?key={Keys.GEMINI_KEY}", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"送信するURL: {API_URL}?key={Keys.GEMINI_KEY}");

            // APIリクエスト送信
            yield return request.SendWebRequest();

            // エラー処理
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API通信エラー: {request.error}");
            }
            else
            {
                // レスポンスのJSON文字列を取得
                string responseText = request.downloadHandler.text;
                Debug.Log($"API成功: {responseText}");

                // レスポンスJSONをChatResponseオブジェクトに変換
                ChatResponse responseData = JsonUtility.FromJson<ChatResponse>(responseText);
                if (responseData != null &&
                    responseData.candidates != null &&
                    responseData.candidates.Count > 0)
                {
                    // 最初の候補のテキストを取得し、末尾の改行を除去
                    string aiResponse = responseData.candidates[0].content.parts[0].text.TrimEnd('\r', '\n');
                    string role = responseData.candidates[0].content.role;

                    // AIの応答を会話履歴に追加し、UIにも反映
                    AddChatHistory(role, aiResponse);
                    AppendChatLog(role, aiResponse);
                }
                else
                {
                    Debug.LogWarning("想定したレスポンス形式ではありません。");
                }
            }
        }
    }
}
