using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class IntroController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Button skipBtn;

    void OnEnable()
    {
        // 영상이 끝났을 때 실행될 이벤트 연결
        videoPlayer.loopPointReached += OnVideoFinished;
    }
    void Start()
    {
        skipBtn.onClick.AddListener(() => OnVideoFinished(videoPlayer));
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // 다음 씬으로 넘어가거나 UI를 전환합니다.
       //Debug.Log("인트로 끝! 메인 화면으로 이동합니다.");
       GameSceneManager.Instance.LoadScene("StartScene");
    }

    void OnDisable()
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
    }
}