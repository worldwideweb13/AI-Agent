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
    /// 表記揺れをさせたくないGeminiへの指示プロンプトは定数として定義
    /// </summary>
    private const string Emotion = "Emotion";
    private const string Content = "Content";

    /// <summary>
    /// Geminiの返答テキストの受取用変数
    /// </summary>
    private string geminiAnswer = string.Empty;

    public enum Emotions
    {
        Idle,
        Joy,
        Anger,
        Sadness,
        Pleasure
    }

    [SerializeField]
    private AvatorController _avatorController;


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
        AddChatHistory
        (
            "user",
            $"あなたは喜怒哀楽が激しく、ユーザーの対話内容によって回答の口調の節々に現在の感情が表れる裏表のない性格の女性です。" +
            $"あなたは{Emotion}のパラメータとして{Emotions.Joy},{Emotions.Anger},{Emotions.Sadness},{Emotions.Pleasure}の4つの感情パラメーターを持つかのように、振る舞うものとします。" +
            $"各感情パラメーターは会話を通じて変動するものとします。現在の感情パラメーターの値を反映するように、あなたの返答のトーンや発言は変化します。"
        );

        // リクエストを送信（会話履歴全体をリクエストに含める）
        StartCoroutine(triggerApiRequest());
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
        StartCoroutine(triggerApiRequest());
    }


    private IEnumerator triggerApiRequest()
    {
        // 最新の会話履歴全体をAPIリクエストとして送信
        yield return StartCoroutine(SendRequestData(true, (string _geminiAnswer) =>
        {
            geminiAnswer = _geminiAnswer;
            // 会話履歴にGeminiの回答結果を表示
            AddChatHistory("model", _geminiAnswer);
            AppendChatLog("model", _geminiAnswer);
        }));

        yield return StartCoroutine(SendRequestData(false, (string emotion) =>
        {
            // AIの応答を会話履歴に追加し、UIにも反映
            _avatorController.ChangeAnimation(emotion);
        }));
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
    /// Geminiから受け取ったテキストを加工する関数
    /// </summary>
    /// <param name="GeminiText"></param>
    /// <returns></returns>
    private (string emotion, string message) OrganizeText(string aiResponse)
    {
        string[] messageArr = aiResponse.Split("\n", StringSplitOptions.None);
        string emotion = messageArr[0].Replace(Emotion, "");
        Debug.Log(messageArr[1]);
        string message = messageArr[1].Replace(Content, "");

        return (emotion, message);
    }


    /// <summary>
    /// Gemini APIへ会話履歴全体をリクエストとして送信し、レスポンスを処理するコルーチン
    /// </summary>
    private IEnumerator SendRequestData(bool FirstCall, Action<string> onResponse)
    {

        ChatRequest requestData;

        if (FirstCall)
        {
            // 現在の会話履歴をリクエストデータとして設定
            requestData = new ChatRequest { contents = chatHistory };
        }
        else
        {
            string EmotionPrompt = $"次の文章の感情を判定してください。\nテキスト: {geminiAnswer}\n" +
                                    $"出力形式: 感情は{Emotions.Joy},{Emotions.Anger},{Emotions.Sadness},{Emotions.Pleasure}のいずれか一つで返答してください。" +
                                    $"回答例は次のようになります。\n" +
                                    $"Fun";                            
            requestData = new ChatRequest
            {
                contents = new List<ContentMessage>
                {
                    new ContentMessage
                    {
                        role = "user",
                        parts = new List<Part> { new Part { text = EmotionPrompt } }
                    }
                }
            };
        }

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

                // レスポンスJSONをChatResponseオブジェクトに変換
                ChatResponse responseData = JsonUtility.FromJson<ChatResponse>(responseText);
                if (responseData != null &&
                    responseData.candidates != null &&
                    responseData.candidates.Count > 0)
                {
                    // 最初の候補のテキストを取得し、末尾の改行を除去
                    string aiResponse = responseData.candidates[0].content.parts[0].text.TrimEnd('\r', '\n');
                    Debug.Log($"API成功: {aiResponse}");
                    // string role = responseData.candidates[0].content.role;

                    // テキストからアバターの感情表現変数とテキスト本文を取り出し
                    // (string emotion, string message) = OrganizeText(aiResponse);

                    // APIの受け取り結果を受けて、callback関数を実行
                    onResponse?.Invoke(aiResponse);

                }
                else
                {
                    Debug.LogWarning("想定したレスポンス形式ではありません。");
                }
            }
        }
    }
    



}

