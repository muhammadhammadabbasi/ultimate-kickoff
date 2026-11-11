using UnityEngine;

/// <summary>Invisible trigger inside the goal. When the ball enters, the scoring team gets a goal.</summary>
[RequireComponent(typeof(Collider))]
public class GoalTrigger : MonoBehaviour
{
    public int scoringTeam;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<BallController>() != null && MatchManager.Instance != null)
            MatchManager.Instance.OnGoal(scoringTeam);
    }
}
