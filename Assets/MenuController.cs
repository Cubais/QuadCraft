using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public Button continueGame;    
    
    void Start()
    {
        continueGame.enabled = SaveManager.instance.SaveGameExists();
        continueGame.interactable = continueGame.enabled;
    }
    
    public void OnStartGame(bool loadGame)
    {        
        PlayerPrefs.SetInt("LoadGame", (loadGame) ? 1 : 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene("GameScene");
    }

    public void OnQuit()
    {
        Application.Quit();
    }
}
