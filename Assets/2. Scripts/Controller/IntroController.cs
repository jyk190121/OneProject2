using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class IntroController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Button skipBtn;
    public Image backImg;

    void OnEnable()
    {
        videoPlayer.prepareCompleted += Prepare;
        // 영상 재생이 시작될 때 호출될 이벤트 연결
        //videoPlayer.started += OnVideoStarted;
        // 영상이 끝났을 때 실행될 이벤트 연결
        videoPlayer.loopPointReached += OnVideoFinished;
    }
    void Start()
    {
        backImg.gameObject.SetActive(true);
        skipBtn.gameObject.SetActive(false);
        skipBtn.onClick.AddListener(() => OnVideoFinished(videoPlayer));
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // 다음 씬으로 넘어가거나 UI를 전환합니다.
       //Debug.Log("인트로 끝! 메인 화면으로 이동합니다.");
       GameSceneManager.Instance.LoadScene("StartScene");
    }

    //void OnVideoStarted(VideoPlayer vp)
    //{
   
    //}

    void Prepare(VideoPlayer vp)
    {
        if (skipBtn != null)
        {
            backImg.gameObject.SetActive(false);
            skipBtn.gameObject.SetActive(true); // 버튼 보이기
        }
    }

    void OnDisable()
    {
        videoPlayer.prepareCompleted -= Prepare;
        //videoPlayer.started -= OnVideoStarted;
        videoPlayer.loopPointReached -= OnVideoFinished;
    }
}