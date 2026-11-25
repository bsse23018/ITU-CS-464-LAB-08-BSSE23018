using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lab07
{
    public class FlightHudFeedback : MonoBehaviour
    {
        public FlightSession session;
        public TMP_Text score, boost;
        public Image[] hearts;
        public Image shield;

        void OnEnable()
        {
            session.Collected += OnCollected;
            session.Damaged += OnDamaged;
            session.ShieldBroken += OnShieldBroken;
        }

        void OnDisable()
        {
            session.Collected -= OnCollected;
            session.Damaged -= OnDamaged;
            session.ShieldBroken -= OnShieldBroken;
        }

        void OnCollected(int total, Vector2 position)
        {
            score.text = total.ToString("N0");
            Tw.ScaleFrom(score.transform, 1.18f, 1f, .3f, Ease.OutBack);
        }

        void OnDamaged(int lives, Vector2 position)
        {
            if (lives >= hearts.Length) return;
            hearts[lives].color = Pal.Mute;
            Tw.ScaleFrom(hearts[lives].transform, 1.5f, 1f, .4f, Ease.OutBack);
        }

        void OnShieldBroken(Vector2 position)
        {
            shield.gameObject.SetActive(false);
            boost.text = session.BoostLabel;
        }
    }
}
