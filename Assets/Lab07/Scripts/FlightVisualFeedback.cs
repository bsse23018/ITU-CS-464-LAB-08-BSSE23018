using UnityEngine;
using UnityEngine.UI;

namespace Lab07
{
    public class FlightVisualFeedback : MonoBehaviour
    {
        public FlightSession session;
        public RectTransform field;
        public Transform ship;
        public Image flash;

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

        void OnCollected(int score, Vector2 position) => Burst(position, Pal.Amber);
        void OnDamaged(int lives, Vector2 position)
        {
            Burst(position, Pal.Pink);
            Tw.To(k => flash.color = Pal.A(Pal.Pink, .28f * (1 - k)), .35f, Ease.OutQuad, 0f, null, this);
            if (GS.Shake) Tw.Shake(field, 16, .3f);
        }

        void OnShieldBroken(Vector2 position)
        {
            Burst(position, Pal.Cyan);
            Tw.ScaleFrom(ship, 1.18f, 1f, .35f, Ease.OutBack);
        }

        void Burst(Vector2 origin, Color color)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * .25f;
                var spark = UI.Icon(field, "spark", "sparkle", UI.TC, origin, 30, color);
                Vector2 end = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(78, 145);
                Tw.Move(spark.rectTransform, end, .42f, Ease.OutCubic);
                Tw.Alpha(UI.Group(spark.rectTransform), 0, .42f, Ease.InQuad, 0, () => Destroy(spark.gameObject));
            }
        }
    }
}
