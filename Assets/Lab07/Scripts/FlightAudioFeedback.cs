using UnityEngine;

namespace Lab07
{
    public class FlightAudioFeedback : MonoBehaviour
    {
        public FlightSession session;
        GameAudio Audio => GameAudio.Instance;

        void OnEnable()
        {
            session.Launched += OnLaunched;
            session.Collected += OnCollected;
            session.Damaged += OnDamaged;
            session.ShieldBroken += OnShieldBroken;
            session.Finished += OnFinished;
        }

        void OnDisable()
        {
            session.Launched -= OnLaunched;
            session.Collected -= OnCollected;
            session.Damaged -= OnDamaged;
            session.ShieldBroken -= OnShieldBroken;
            session.Finished -= OnFinished;
        }

        void OnLaunched()
        {
            Audio.Music(true);
            Audio.Flight(FlightCue.Launch);
        }

        void OnCollected(int score, Vector2 position) => Audio.Flight(FlightCue.Coin);
        void OnDamaged(int lives, Vector2 position) => Audio.Flight(FlightCue.Hit);
        void OnShieldBroken(Vector2 position) => Audio.Flight(FlightCue.Shield);

        void OnFinished(bool won)
        {
            Audio.Flight(won ? FlightCue.Win : FlightCue.Lose);
            Audio.Music(false);
        }
    }
}
