using System;
using UnityEngine;

namespace Lab07
{
    // Owned by the Flight scene. The HUD supplies collision candidates and renders the state.
    public class FlightSession : MonoBehaviour
    {
        public event Action Launched;
        public event Action<int, Vector2> Collected;
        public event Action<int, Vector2> Damaged;
        public event Action<Vector2> ShieldBroken;
        public event Action<bool> Finished;

        public int Score { get; private set; }
        public int Coins { get; private set; }
        public int Lives { get; private set; }
        public bool Shield { get; private set; }
        public bool Magnet { get; private set; }
        public bool Running { get; private set; }
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public string BoostLabel { get; private set; }

        [SerializeField] float campaignSeconds = 25f;
        [SerializeField] float timeAttackSeconds = 45f;
        int multiplier;
        int offered;
        float immunity;

        public void Prepare(float durationOverride)
        {
            Duration = durationOverride > 0 ? durationOverride :
                GS.Session.Mode == 0 ? campaignSeconds : GS.Session.Mode == 2 ? timeAttackSeconds : 0f;
            Remaining = Duration;
            Score = Coins = offered = 0;
            Lives = 3;
            immunity = 0;
            Running = false;
            var boost = GS.Boosts[GS.BoostIndex];
            bool equipped = GS.BoostCount(boost.id) > 0;
            if (equipped) GS.AddBoost(boost.id, -1);
            Shield = equipped && boost.id == "shield";
            Magnet = equipped && boost.id == "magnet";
            multiplier = equipped && boost.id == "x2" ? 2 : 1;
            BoostLabel = equipped ? boost.name.ToUpperInvariant() + " ON" : "";
            if (GS.BoostCount("life") > 0)
            {
                Lives++;
                GS.AddBoost("life", -1);
                BoostLabel += (equipped ? "  ·  " : "") + "EXTRA LIFE";
            }
        }

        public void Launch()
        {
            Running = true;
            Launched?.Invoke();
        }

        public void Advance(float dt)
        {
            if (!Running) return;
            immunity = Mathf.Max(0, immunity - dt);
            if (Duration <= 0) return;
            Remaining = Mathf.Max(0, Remaining - dt);
            if (Remaining == 0) End(true);
        }

        public void OfferCoin() => offered++;

        public void Collect(Vector2 position)
        {
            if (!Running) return;
            Coins++;
            Score += 100 * multiplier;
            Collected?.Invoke(Score, position);
        }

        public void Hit(Vector2 position)
        {
            if (!Running || immunity > 0) return;
            immunity = .65f;
            if (Shield)
            {
                Shield = false;
                BoostLabel = Lives > 3 ? "EXTRA LIFE" : "";
                ShieldBroken?.Invoke(position);
                return;
            }

            Lives--;
            Damaged?.Invoke(Lives, position);
            if (Lives == 0) End(GS.Session.Mode == 1);
        }

        void End(bool win)
        {
            if (!Running) return;
            Running = false;
            int stars = win ? Mathf.Clamp(GS.Session.Mode == 0 ? Lives : Score > 1800 ? 3 : Score > 900 ? 2 : 1, 1, 3) : 0;
            GS.Session.Win = win;
            GS.Session.Score = Score;
            GS.Session.CoinsEarned = Coins;
            GS.Session.Stars = stars;
            GS.Session.Accuracy = offered > 0 ? (float)Coins / offered : 0;
            GS.Session.NewBest = Score > GS.BestScore;
            if (GS.Session.Mode == 0 && win) GS.Submit(GS.Session.Level, stars, Score);
            else GS.RecordBestScore(Score);
            GS.AddCoins(Coins);
            GS.AddXp(Mathf.Max(5, Score / 100));
            PlayerPrefs.Save();
            Finished?.Invoke(win);
        }

    }
}
