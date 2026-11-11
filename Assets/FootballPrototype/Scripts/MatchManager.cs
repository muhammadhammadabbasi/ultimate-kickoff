using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Score, match clock, kick-offs and the HUD.</summary>
public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("Pitch (meters)")]
    public float halfLength = 30f;
    public float halfWidth = 20f;
    public float goalHalfWidth = 3f;
    public float goalHeight = 2.2f;

    [Header("Match")]
    public float matchMinutes = 3f;
    public string[] teamNames = { "BLU", "RED" };

    [Header("References")]
    public BallController ball;
    public Goalkeeper[] keepers;

    [Header("UI")]
    public Text scoreText;
    public Text messageText;
    public Image shotPowerFill;

    public bool Playing { get; private set; }

    readonly int[] score = new int[2];
    float clock;

    public Vector3 AttackGoal(int team) => new Vector3(0f, 0f, team == 0 ? halfLength : -halfLength);
    public Vector3 DefendGoal(int team) => AttackGoal(1 - team);

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    void Start()
    {
        StartCoroutine(KickoffRoutine(0, "KICK OFF"));
    }

    void Update()
    {
        if (Playing)
        {
            clock += Time.deltaTime;
            if (clock >= matchMinutes * 60f)
            {
                clock = matchMinutes * 60f;
                StartCoroutine(FullTimeRoutine());
            }
        }
        UpdateUI();
    }

    public void OnGoal(int scoringTeam)
    {
        if (!Playing) return;
        score[scoringTeam]++;
        StartCoroutine(GoalRoutine(scoringTeam));
    }

    IEnumerator GoalRoutine(int scoringTeam)
    {
        Playing = false;
        ShowMessage("GOAL!\n" + teamNames[scoringTeam]);
        yield return new WaitForSeconds(2.5f);
        yield return KickoffRoutine(1 - scoringTeam, null);
    }

    IEnumerator FullTimeRoutine()
    {
        Playing = false;
        ShowMessage($"FULL TIME\n{teamNames[0]} {score[0]} - {score[1]} {teamNames[1]}");
        yield return new WaitForSeconds(5f);
        score[0] = score[1] = 0;
        clock = 0f;
        yield return KickoffRoutine(0, "KICK OFF");
    }

    IEnumerator KickoffRoutine(int kickoffTeam, string msg)
    {
        Playing = false;
        ResetPositions(kickoffTeam);
        if (msg != null) ShowMessage(msg);
        yield return new WaitForSeconds(msg != null ? 1.2f : 0.5f);
        ShowMessage("");
        Playing = true;
    }

    void ResetPositions(int kickoffTeam)
    {
        if (ball != null) ball.ResetTo(new Vector3(0f, ball.radius, 0f));

        FootballPlayer striker = null;
        float best = float.MaxValue;
        foreach (var p in FootballPlayer.All)
        {
            Quaternion rot = Quaternion.LookRotation(p.team == 0 ? Vector3.forward : Vector3.back);
            p.Teleport(p.HomePosition, rot);
            if (p.team == kickoffTeam)
            {
                float d = p.HomePosition.sqrMagnitude;
                if (d < best) { best = d; striker = p; }
            }
        }

        if (striker != null)
        {
            Vector3 kp = new Vector3(0f, 0f, kickoffTeam == 0 ? -1.2f : 1.2f);
            striker.Teleport(kp, striker.transform.rotation);
        }

        FootballPlayer user = null;
        best = float.MaxValue;
        foreach (var p in FootballPlayer.All)
        {
            if (p.team != 0) continue;
            float d = p.transform.position.sqrMagnitude;
            if (d < best) { best = d; user = p; }
        }
        if (user != null) FootballPlayer.SetUserControlled(user);

        if (keepers != null)
            foreach (var k in keepers)
                if (k != null) k.ResetToHome();
    }

    void ShowMessage(string s)
    {
        if (messageText != null) messageText.text = s;
    }

    void UpdateUI()
    {
        if (scoreText != null)
        {
            int secs = Mathf.FloorToInt(clock);
            scoreText.text = $"{teamNames[0]}  {score[0]} - {score[1]}  {teamNames[1]}      {secs / 60:00}:{secs % 60:00}";
        }

        if (shotPowerFill != null)
        {
            float charge = 0f;
            foreach (var p in FootballPlayer.All)
                if (p.team == 0 && p.userControlled) { charge = p.ShotCharge; break; }
            shotPowerFill.fillAmount = charge;
        }
    }
}
