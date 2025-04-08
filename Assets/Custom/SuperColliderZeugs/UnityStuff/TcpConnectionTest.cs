using System;
using System.Collections;
using System.Collections.Generic;
using InternetTime.Custom.SuperColliderZeugs;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TcpConnectionTest : MonoBehaviour {
    public GameObject canvas;
    public RectTransform buttonPrefab;
    
    private GameNetwork network;
    private volatile bool changeScenes;
    private List<string> currentHosts = new();
    private List<string> oldHosts = new();
    private readonly object lockObj = new object();
    
    void Start() {
        this.network = new GameNetwork();
        network.OnHostsChanged += OnHostsChanged;
        network.OnInfoSent += ChangeScene;
        Debug.Log("Application started!");
    }

    private void Update() {
        RefreshCanvas();
        lock (lockObj) {
            if (changeScenes) {
                //SceneManager.LoadScene("SuperColliderTestScene");
                SceneManager.LoadScene("SheetMusicTest");
            }
        }
    }

    private void OnHostsChanged(List<string> availableLobbies) {
        this.currentHosts = new List<string>(availableLobbies);
        Debug.Log("Host change received");
        Debug.Log("Hosts: ");
        foreach (string host in currentHosts) {
            Debug.Log(host);
        }
    }

    private void RefreshCanvas() {
        if (currentHosts.Count == oldHosts.Count) return;
        
        for (int i = 0; i < currentHosts.Count; i++) {
            string lobbyName = currentHosts[i];
            //Vector3 buttonPos = new Vector3((100+ (100 * i)), 100, 0);
            //Vector3 buttonPos = new Vector3((600 + (100 * i)), 400, 0);
            Vector3 buttonPos = new Vector3((0 + (100 * i)), 0, 0);

            RectTransform createdButtonObj = Instantiate(buttonPrefab, canvas.transform);
            TextMeshProUGUI createdButtonText = createdButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            Button createdButton = createdButtonObj.GetComponent<Button>();
            
            createdButtonText.text = lobbyName;
            createdButtonObj.anchoredPosition = buttonPos;
            
            createdButton.onClick.AddListener(() => network.SendJoinReq(lobbyName));
        }

        oldHosts = new List<string>(currentHosts);
    }
    
    private void ChangeScene() {
        lock (lockObj) {
            NetworkRegistry.Network = network;
            changeScenes = true;
        }
        
    }

    private void OnDestroy() {
        if (NetworkRegistry.Network == null) {
            network.Stop();
        }
    }
}
