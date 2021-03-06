﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine;

public enum Lane { D, F, Space, J, K, UnInit };
public enum BeatType { Hit, Held, UnInit };
public enum HitType { Great, Good, Bad, Miss, UnInit };

public class Onload : MonoBehaviour {

    public static Onload instance = null;

    /* MEMBER VARIABLES */

    public const float LOOKAHEAD_INTERVAL = SongPosition.TIME_BEFORE_AUDIO_START*1000;

    //score trackers
    private int totalGreats;
    private int totalGoods;
    private int totalBads;
    private int totalMisses;

    private bool beatsDone;

    //our ordered map, which will be populated during preprocessing
    SortedDictionary<int, List<BeatInfo>> upcomingBeats;
    //enumerator for this dictionary that will progressively advance.
    SortedDictionary<int, List<BeatInfo>>.Enumerator upcomingBeatsEnumerator;

    private int totalPossibleScore;

    //score and health display
    public static ScoreDisplay score;
    public static ScoreDisplay combo;
    public static HealthBar health;
    //public static HitTypeDisplay HitTypeD;
    //public static HitTypeDisplay HitTypeF;
    //public static HitTypeDisplay HitTypeSpace;
    //public static HitTypeDisplay HitTypeJ;
    //public static HitTypeDisplay HitTypeK;

    /* "GETTER" Functions */

    public int GetGreats() { return totalGreats; }
    public int GetGoods() { return totalGoods; }
    public int GetBads() { return totalBads; }
    public int GetMisses() { return totalMisses; }

    /* "SETTER" Functions
     * NOTE: Update() functions of Unity objects are run in SEQUENCE on a single core unless multithreading is explicitly stated.
     * Thus there is no need for mutexes or locks.
     */

    public void AddGreat() { totalGreats++; }
    public void AddGood() { totalGoods++; }
    public void AddBad() { totalBads++; }
    public void AddMiss() { totalMisses++; }

    // Populate static script with result data
    private void populate(bool calculateGrade)
    {
        // Call this function with FALSE argument if the player died
        // Otherwise, call with TRUE (player reached end of song)
        if (calculateGrade)
        {
            // calculate accurate grade
            int calculatedScore = health.getGreat()*InputReaction.SCORE_GREAT + health.getGood()*InputReaction.SCORE_GOOD 
                + health.getBad() * InputReaction.SCORE_BAD;
            if (totalPossibleScore != 0)
            {
                float ratio = (float)calculatedScore / (float)totalPossibleScore * 100;
                
                //A
                if(ratio > 80)
                {
                    ResultStats.Grade = "A";
                }
                //B
                else if (ratio > 60)
                {
                    ResultStats.Grade = "B";
                }
                //C
                else if (ratio > 40)
                {
                    ResultStats.Grade = "C";
                }
                //D
                else
                {
                    ResultStats.Grade = "D";
                }
            }
            else ResultStats.Grade = "A";
        } else { ResultStats.Grade = "F"; }

        ResultStats.Score = score.getScore();
        ResultStats.MaxCombo = score.getMaxCombo();
        ResultStats.Great = health.getGreat();
        ResultStats.Good = health.getGood();
        ResultStats.Bad = health.getBad();
        ResultStats.Miss = health.getMiss();
    }

    /* UNITY FUNCTIONS */

    //enforce singleton
    private void Awake()
    {
        //Check if instance already exists
        if (instance == null)
            //if not, set instance to this
            instance = this;

        //If instance already exists and it's not this:
        else if (instance != this)
            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);

        //Sets this to not be destroyed when reloading scene
        //DontDestroyOnLoad(gameObject);
    }

    // Use this for initialization
    void Start () {
        //member variable initialization
        beatsDone = false;
        totalGreats = totalGoods = totalBads = totalMisses = 0;

        // Change key from int to float -> desired? 
        // Reason: m_offset is a float and that is the same as beat timestamp
        upcomingBeats = new SortedDictionary<int, List<BeatInfo>>();

        score = GameObject.FindWithTag("score").GetComponent<ScoreDisplay>();
        combo = GameObject.FindWithTag("combo").GetComponent<ScoreDisplay>();
        health = GameObject.FindWithTag("health").GetComponent<HealthBar>();

        SongInfo cur_song = SongToBePlayed.songInfo;
        /* FOR MICHELLE AND OTHER PREPROCESSORS
         * Insert preprocessing script here.
         * INPUT: Read the beatmap file in (using System.IO.File?) 
         *          access path to beatmap (string) using SongToBePlayed.songInfo.m_pathToBeatmap;
         * OUTPUT: Using an ordered map (implemented as a red-black tree, so O(logn) access, search, insertion, deletion)
         * of type < int, List<BeatInfo> > where int corresponds to the time offset (in ms) and BeatInfo is a class containing target info, 
         * process the beatmap file and consolidate beats with the same ms offset into the List of each key-value pair
         */

        /* Current - load file from a set path
          TO DO: 
            - Load file from a directory; user specified file name
            - Grab path to file from the specified SongToBePlayed object
            OR 
            - Load file from any directory that the user specifies
       */

        if (SongToBePlayed.songInfo == null)
        {
            Debug.Log("You tried to play a game with no song specified.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif

            return;
        }

        string btmp_path = SongToBePlayed.songInfo.m_pathToBeatmap;

        TextAsset btmp_file = Resources.Load(btmp_path) as TextAsset;
        var btmp_raw = btmp_file.text.Split('\n');

        int firstLine = 0;
        int line_count = 0;
        foreach(var line in btmp_raw)
        {        
            line_count++;

            /** THE BEATMAP FILE FORMAT IS: 
                    bpm 
                    timestamp, lane, beat type, duration
                    values[0], [1],  [2],       [3]
            **/
            // If the firstLine has already been read, start processing values
            if (firstLine == 1 && line.Length > 0)
            {

                // Get values from each line (beat)
                var values = line.Split(',');

                // ** Beat Timestamp ** 
                int beat_timestamp = -1;
                if (int.TryParse(values[0], out beat_timestamp))
                {
                }
                else
                    Debug.Log("beat_timestamp could not be parsed: " + beat_timestamp);

                float beat_offset = -1;
                if (float.TryParse(values[0], out beat_offset))
                {
                }
                else
                    Debug.Log("beat_offset could not be parsed.");

                // ** Beat Lane **     
                Lane beat_lane = Lane.UnInit;
                string raw_beat_lane = values[1];
                switch (raw_beat_lane)
                {
                    case "D":
                        beat_lane = Lane.D;
                        break;
                    case "F":
                        beat_lane = Lane.F;
                        break;
                    case "Space":
                        beat_lane = Lane.Space;
                        break;
                    case "J":
                        beat_lane = Lane.J;
                        break;
                    case "K":
                        beat_lane = Lane.K;
                        break;
                    default:
                        Debug.Log("No such key lane: " + beat_lane);
                        break;
                }


                // ** Beat Type ** 
                BeatType beat_type = BeatType.UnInit;
                char raw_beat_type = values[2][0];

                switch (raw_beat_type)
                {
                    case '0':
                        beat_type = BeatType.Hit;
                        break;
                    case '1':
                        beat_type = BeatType.Held;
                        break;
                    default:
                        Debug.Log("No such beat type: " + raw_beat_type);
                        break;
                }

                var newBeat = new BeatInfo(beat_lane, beat_type, beat_offset);

                // ** Beat Duration ** 
                int beat_duration = -1;
                if (values.Length > 3)
                {
                    if (int.TryParse(values[3], out beat_duration))
                    {
                        var tmpBeat = new BeatInfo(beat_lane, beat_type, beat_offset, beat_duration);
                        newBeat = tmpBeat;
                    }
                    else
                        Debug.Log("beat_duration could not be parsed.");
                }


                //the set of beats for each time stamp
                List<BeatInfo> beatSet = new List<BeatInfo>();
                if (upcomingBeats.ContainsKey(beat_timestamp))
                {

                    if (upcomingBeats.TryGetValue(beat_timestamp, out beatSet))
                    {
                        beatSet.Add(newBeat);
                        upcomingBeats[beat_timestamp] = beatSet;
                        // Maybe all of this (within TryGetValue if statement)
                        // can be shortened down to just: 
                        // upcomingBeats[beat_timestamp].Add(newBeat);
                    }
                    else
                    {
                        Debug.Log("Value is not found.");
                    }
                }
                else
                {
                    beatSet.Add(newBeat);
                    upcomingBeats.Add(beat_timestamp, beatSet);
                }
            } // end firstLine if statement
            else { firstLine = 1; }//Debug.Log("First line skipped"); }

        } // end of btmp_raw foreach loop

        //NO BPM PROVIDED
        if(line_count < 1)
        {
            Debug.Log("The beatmap file is malformed.");
            totalPossibleScore = 0;
        }
        else { totalPossibleScore = InputReaction.SCORE_GREAT * (line_count-1); }

        upcomingBeatsEnumerator = upcomingBeats.GetEnumerator();
        //move to first position
        if (!upcomingBeatsEnumerator.MoveNext()) beatsDone = true;

    } // end of Start()

    // Update is called once per frame
    void Update () {
        float songPos = SongPosition.instance.getSongPos();
        if (!beatsDone)
        {
            if (upcomingBeatsEnumerator.Current.Key - songPos < LOOKAHEAD_INTERVAL)
            {
                foreach (BeatInfo bi in upcomingBeatsEnumerator.Current.Value)
                {
                    bi.CreateBeatTarget();
                }
                if (!upcomingBeatsEnumerator.MoveNext())
                {
                    beatsDone = true;
                }
            }
        }
        if (health.isDead())
        {
            // TODO: Death animation?
            populate(false);
            SceneManager.LoadScene("resultscreen");
        }
        if (songPos >= SongPosition.instance.songLength + LOOKAHEAD_INTERVAL)
        {
            populate(true);
            SceneManager.LoadScene("resultscreen");
        }

    }
}
