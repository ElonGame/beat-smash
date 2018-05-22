﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SongButton : MonoBehaviour {

    public Button button;
    public Text TitleText;

    private SongInfo m_songInfo;
    

	// Use this for initialization
	void Start () {
        button.onClick.AddListener(HandleClick);
	}
	
	// Update is called once per frame
	void Update () {
	}

    public void Setup(SongInfo currInfo)
    {
        m_songInfo = currInfo;
        TitleText.text = m_songInfo.m_title;
    }

    public void HandleClick()
    {
        //SceneManager.LoadScene("gamescreen");
        Text title = GameObject.Find("TitleText").GetComponent<Text>();
        title.text = "TITLE\n" + m_songInfo.m_title;
        Text length = GameObject.Find("LengthText").GetComponent<Text>();
        length.text = "LENGTH\n" + m_songInfo.m_length;
        Text notice = GameObject.Find("NoticeText").GetComponent<Text>();
        notice.text = "Press ENTER to play";
    }

}
