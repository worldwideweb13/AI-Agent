using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AutoSizeAdjuster : MonoBehaviour
{

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnRectTransformDimensionsChange()
    {
        // Debug.Log("スクリーンのサイズは" + Screen.height);        
        AdjustHeight();
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void AdjustHeight()
    {
        // 万が一 rectTransform がまだ取得されていなければここで取得
        if (rectTransform == null) return; // ここは基本的には発生しないが安全策

        // RectTransform のサイズを変更（Anchorの設定に注意）
        Vector2 sizeDelta = rectTransform.sizeDelta;
        float ScreenHight = Screen.height;
        // Debug.Log(Screen.height);
        if (ScreenHight >= 1300)
        {
            sizeDelta.y = Screen.height * 0.5f;
        }
        else if (ScreenHight >= 1100)
        {
            sizeDelta.y = Screen.height * 0.7f;
        }
        else
        {
            sizeDelta.y = Screen.height * 0.85f;
        }
            rectTransform.sizeDelta = sizeDelta;
    }
}
