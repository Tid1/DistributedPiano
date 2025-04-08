using System.Collections;
using System.Collections.Generic;
using InternetTime.Custom.SuperColliderZeugs;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour {
    [SerializeField] private TMP_Text achievedScoreText;
    [SerializeField] private TMP_Text maximumScoreText;
    [SerializeField] private Button mainMenuButton;
    

    void Start() {
        achievedScoreText.text = ScoringRegistry.achievedScore + "";
        maximumScoreText.text = ScoringRegistry.maximumScore + "";
        mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene("TestConnection"));
    }
    
}
