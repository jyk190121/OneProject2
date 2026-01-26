using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class UIToolkitManager : MonoBehaviour
{
    UIDocument document;
    VisualElement panel;

    Button startBtn;

    [Header("Fade In 시간")]
    float fadeDuration = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        document = GetComponent<UIDocument>();
        panel = document.rootVisualElement;

        startBtn = panel.Q<Button>("PlayBtn");

        startBtn.clickable.clicked += GameStart;

        //스타트 씬 Fadein 효과
        StartCoroutine(FadeInUi());
    }

    IEnumerator FadeInUi()
    {
        //시작 시 UI의 투명도를 0으로 설정
        panel.style.opacity = 0;

        yield return null;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panel.style.opacity = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        panel.style.opacity = 1;
    }

    void GameStart()
    {
        GameSceneManager.Instance.LoadScene("Map");
    }
}
