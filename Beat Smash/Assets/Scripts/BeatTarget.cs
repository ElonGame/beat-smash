﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* WARNING: DO NOT INSTANTIATE THIS PREFAB BY HAND IN UNITY!!!!!! Unless if you like Null Pointer Exceptions */
public abstract class BeatTarget : MonoBehaviour {

    public static float onscreenInterval = SongPosition.TIME_BEFORE_AUDIO_START*1000;
    public const float OFFSCREEN_Y = 100;
    public const float TOP_Y = 6;
    public const float GOAL_Y = -2.5f;
    public const float BOTTOM_Y = GOAL_Y - (TOP_Y - GOAL_Y);

    private BeatInfo m_beatinfo = null;
    private float m_x;

    private bool isBeginLerp = true;

    /* SETTERS */
    public void SetBeatInfo(BeatInfo beatinfo) { m_beatinfo = beatinfo; }
    public void SetXPos(float x) { m_x = x; }
    public void SetLerp(bool lerp) { isBeginLerp = lerp; }

    /* GETTERS */
    public BeatInfo GetBeatInfo() { return m_beatinfo; }
    public float GetXPos() { return m_x; }
    public bool GetLerp() { return isBeginLerp; }
}
