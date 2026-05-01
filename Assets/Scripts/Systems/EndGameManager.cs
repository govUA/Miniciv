using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;

public class EndGameManager : MonoBehaviour
{
    [Header("Video")] public VideoPlayer videoPlayer;
    public VideoClip victoryClip;
    public VideoClip defeatClip;

    [Header("Interface (UI)")] public GameObject resultsPanel;
    public TMP_Text resultTitleText;
    public TMP_Text scoreText;

    private void Start()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.clip = GameSettings.DidPlayerWin ? victoryClip : defeatClip;

            videoPlayer.loopPointReached += OnVideoFinished;

            videoPlayer.Play();
        }
        else
        {
            ShowResults();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        ShowResults();
    }

    private void ShowResults()
    {
        if (videoPlayer != null)
        {
            videoPlayer.gameObject.SetActive(false);
        }

        if (resultsPanel != null)
            resultsPanel.SetActive(true);

        if (resultTitleText != null)
        {
            resultTitleText.text = GameSettings.DidPlayerWin ? "VICTORY" : "DEFEAT";
            resultTitleText.color = GameSettings.DidPlayerWin ? Color.green : Color.red;
        }

        if (scoreText != null)
        {
            scoreText.text = $"Score: {GameSettings.FinalScore:F2}";
        }
    }

    public void OnPlayAgainClicked()
    {
        SceneManager.LoadScene("StartScene");
    }
}