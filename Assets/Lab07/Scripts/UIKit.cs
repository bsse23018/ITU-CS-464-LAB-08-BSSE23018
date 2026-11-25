using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lab07
{
    public static class UI
    {
        public static readonly Vector2 TL = new Vector2(0, 1), TC = new Vector2(.5f, 1), TR = new Vector2(1, 1),
            ML = new Vector2(0, .5f), C = new Vector2(.5f, .5f), MR = new Vector2(1, .5f),
            BL = new Vector2(0, 0), BC = new Vector2(.5f, 0), BR = new Vector2(1, 0);

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false); return rt;
        }
        public static RectTransform Place(RectTransform rt, Vector2 a, Vector2 pos, Vector2 size)
        { rt.anchorMin = rt.anchorMax = rt.pivot = a; rt.anchoredPosition = pos; rt.sizeDelta = size; return rt; }
        public static RectTransform Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f, .5f); rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t); return rt; }

        public static Image Box(Transform p, string n, Vector2 a, Vector2 pos, Vector2 size, Color c, Sprite sp = null)
        {
            var rt = Place(Rect(p, n), a, pos, size); var im = rt.gameObject.AddComponent<Image>();
            im.sprite = sp; im.color = c; im.raycastTarget = false;
            im.type = (sp != null && sp.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            return im;
        }
        public static Image Rounded(Transform p, string n, Vector2 a, Vector2 pos, Vector2 size, Color c, int radius) => Box(p, n, a, pos, size, c, Sprites.Round(radius));
        public static Image Outline(Transform p, string n, Vector2 size, Color c, int radius, int thick = 4)
        {   // stretched ring that follows its parent
            var im = Box(p, n, C, Vector2.zero, size, c, Sprites.Ring(radius, thick)); Stretch(im.rectTransform); return im;
        }
        public static Image Icon(Transform p, string n, string icon, Vector2 a, Vector2 pos, float size, Color c)
        { var im = Box(p, n, a, pos, new Vector2(size, size), c, Sprites.Icon(icon)); im.preserveAspect = true; return im; }

        public static TextMeshProUGUI Text(Transform p, string n, string s, float size, Color c, Vector2 a, Vector2 pos, Vector2 box,
            TextAlignmentOptions al = TextAlignmentOptions.Center, bool bold = true, float spacing = 0)
        {
            var rt = Place(Rect(p, n), a, pos, box); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = s; t.fontSize = size; t.color = c; t.alignment = al; t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.raycastTarget = false; t.characterSpacing = spacing; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        public static CanvasGroup Group(RectTransform rt) { var g = rt.GetComponent<CanvasGroup>(); return g ? g : rt.gameObject.AddComponent<CanvasGroup>(); }

        /// <summary>Standard pill/rounded button: fill + label (+ optional icon) + optional outline. Min touch target 120 canvas px.</summary>
        public static Tappable Button(Transform p, string key, Vector2 a, Vector2 pos, Vector2 size, Color fill, Color labelColor,
            string label, float fontSize, string icon = null, Color? outline = null, int? radius = null)
        {
            int r = radius ?? Mathf.RoundToInt(Mathf.Min(size.x, size.y) / 2f);
            var bg = Rounded(p, key, a, pos, size, fill, r); bg.raycastTarget = true;
            var tap = bg.gameObject.AddComponent<Tappable>(); tap.key = key; tap.bg = bg; tap.fill = fill; tap.labelColor = labelColor;
            if (outline.HasValue) tap.ring = Outline(bg.transform, "ring", size, outline.Value, r);
            var t = Text(bg.transform, "label", label, fontSize, labelColor, C, Vector2.zero, new Vector2(size.x - 40, size.y)); tap.label = t;
            if (icon != null)
            {
                float w = t.GetPreferredValues(label).x, isz = fontSize * 1.05f;
                t.rectTransform.anchoredPosition = new Vector2((isz + 16) / 2f, 0);
                tap.icon = Icon(bg.transform, "icon", icon, C, new Vector2(-w / 2f - 8, 0), isz, labelColor);
            }
            return tap;
        }

        public static Tappable IconButton(Transform p, string key, string icon, Vector2 a, Vector2 pos, float size, Color fill, Color iconColor, float iconSize = 0, Color? outline = null)
        {
            var bg = Rounded(p, key, a, pos, new Vector2(size, size), fill, Mathf.RoundToInt(size * .3f)); bg.raycastTarget = true;
            var tap = bg.gameObject.AddComponent<Tappable>(); tap.key = key; tap.bg = bg; tap.fill = fill; tap.labelColor = iconColor;
            if (outline.HasValue) tap.ring = Outline(bg.transform, "ring", new Vector2(size, size), outline.Value, Mathf.RoundToInt(size * .3f));
            tap.icon = Icon(bg.transform, "icon", icon, C, Vector2.zero, iconSize > 0 ? iconSize : size * .55f, iconColor);
            return tap;
        }

        /// <summary>Header used on every sub-screen: Back (always top-left), title, coin balance (top-right).</summary>
        public static RectTransform Header(MenuScreen s, string title, bool back = true, bool coins = true)
        {
            var h = Rect(s.Root, "Header"); Place(h, TC, Vector2.zero, new Vector2(1080, 190)); h.anchorMin = new Vector2(0, 1); h.anchorMax = new Vector2(1, 1); h.sizeDelta = new Vector2(0, 190);
            if (back)
            {
                var b = IconButton(h, "back", "back", TL, new Vector2(40, -34), 132, Pal.PanelHi, Pal.White, 80, Pal.Mute);
                b.onClick = () => { s.nav.Back(); };
                s.Mark("back", b.rectTransform); s.Back = b;
            }
            var t = Text(h, "title", title, 62, Pal.White, TL, new Vector2(214, -50), new Vector2(520, 110), TextAlignmentOptions.MidlineLeft, true, 2);
            s.Mark("title", t.rectTransform);
            if (coins) { var chip = CoinChip.Create(h, TR, new Vector2(-40, -46)); s.Mark("coins", chip.rectTransform); }
            return h;
        }

        public static RectTransform Card(Transform p, string n, Vector2 a, Vector2 pos, Vector2 size, Color fill, int radius = 44, Color? outline = null)
        {
            var im = Rounded(p, n, a, pos, size, fill, radius);
            if (outline.HasValue) Outline(im.transform, "ring", size, outline.Value, radius, 3);
            return im.rectTransform;
        }
    }

    /// <summary>Touch control with press/release feedback, disabled state and a deny shake.</summary>
    public class Tappable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerExitHandler, IPointerEnterHandler, ISelectHandler, ISubmitHandler
    {
        public static readonly Dictionary<string, Tappable> All = new Dictionary<string, Tappable>();
        public string key; public Action onClick; public Action onDenied; public Image bg, ring, icon; public TextMeshProUGUI label;
        public Color fill, labelColor; public bool interactable = true; 
        public RectTransform rectTransform => (RectTransform)transform;

        void OnEnable() { if (!string.IsNullOrEmpty(key)) All[key] = this; }
        void OnDestroy() { if (!string.IsNullOrEmpty(key) && All.TryGetValue(key, out var t) && t == this) All.Remove(key); }

        public void SetInteractable(bool on, Color? disabledFill = null)
        {
            interactable = on;
            if (bg) bg.color = on ? fill : (disabledFill ?? Pal.Locked);
            if (label) label.color = on ? labelColor : Pal.Mute;
            if (icon) icon.color = on ? labelColor : Pal.Mute;
        }
        public void SetFill(Color f) { fill = f; if (interactable && bg) bg.color = f; }
        public void SetLabel(string s) { if (label) label.text = s; }

        public void OnPointerEnter(PointerEventData e)
        {
            if (interactable && Input.touchCount == 0) GameAudio.UI(UiCue.Select);
        }

        public void OnSelect(BaseEventData e)
        {
            if (interactable) GameAudio.UI(UiCue.Select);
        }

        public void OnSubmit(BaseEventData e) => Click();
        public void OnPointerDown(PointerEventData e) { if (!interactable) return; Tw.Scale(transform, .94f, .08f, Ease.OutQuad); }
        public void OnPointerExit(PointerEventData e) { Tw.Scale(transform, 1f, .18f, Ease.OutBack); }
        public void OnPointerUp(PointerEventData e) { Tw.Scale(transform, 1f, .28f, Ease.OutBack); }
        public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) Click(); }

        public void Click()
        {
            if (App.I.flight.Loading || App.I.nav.Busy) return;
            if (!interactable)
            {
                if (onDenied == null) GameAudio.UI(UiCue.Error);
                Tw.Shake(rectTransform); onDenied?.Invoke(); return;
            }
            GameAudio.UI(key == "back" ? UiCue.Back : UiCue.Press);
            onClick?.Invoke();
        }
    }

    /// <summary>Currency chip bound to GS. Counts up/down (OutQuart) instead of snapping, so the player sees the effect of a purchase.</summary>
    public class CoinChip : MonoBehaviour
    {
        TextMeshProUGUI txt; int shown; Image pulse;
        public RectTransform rectTransform => (RectTransform)transform;

        public static CoinChip Create(Transform parent, Vector2 anchor, Vector2 pos)
        {
            var bg = UI.Rounded(parent, "CoinChip", anchor, pos, new Vector2(290, 100), Pal.PanelLo, 50);
            UI.Outline(bg.transform, "ring", new Vector2(290, 100), Pal.Mute, 50, 3);
            UI.Icon(bg.transform, "icon", "coin", UI.ML, new Vector2(26, 0), 62, Pal.Amber);
            var c = bg.gameObject.AddComponent<CoinChip>();
            c.txt = UI.Text(bg.transform, "n", "0", 46, Pal.White, UI.MR, new Vector2(-24, 0), new Vector2(180, 90), TextAlignmentOptions.MidlineRight);
            c.shown = GS.Coins; c.txt.text = GS.Coins.ToString("N0");
            return c;
        }
        void OnEnable() { GS.Changed += Refresh; shown = GS.Coins; if (txt) txt.text = shown.ToString("N0"); }
        void OnDisable() { GS.Changed -= Refresh; }
        void Refresh()
        {
            if (!txt || shown == GS.Coins) return;
            Tw.Count(v => { shown = v; txt.text = v.ToString("N0"); }, shown, GS.Coins, .7f, Ease.OutQuart, 0f, (this, "count"));
            Tw.ScaleFrom(transform, 1.12f, 1f, .4f, Ease.OutBack);
        }
    }

    /// <summary>Horizontal drag slider (0..1).</summary>
    public class USlider : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<float> onChanged; public float value; RectTransform fillRt, knob; RectTransform track; TextMeshProUGUI valueText;
        public static USlider Create(Transform p, string name, Vector2 a, Vector2 pos, float width, float initial, Action<float> changed, TextMeshProUGUI valueLabel = null)
        {
            var tr = UI.Rounded(p, name, a, pos, new Vector2(width, 120), new Color(0, 0, 0, 0), 20); tr.raycastTarget = true; // generous hit area
            var s = tr.gameObject.AddComponent<USlider>(); s.track = tr.rectTransform; s.onChanged = changed; s.valueText = valueLabel;
            var rail = UI.Rounded(tr.transform, "rail", UI.C, Vector2.zero, new Vector2(width, 28), Pal.PanelLo, 14);
            var fill = UI.Rounded(tr.transform, "fill", UI.ML, Vector2.zero, new Vector2(width, 28), Pal.Cyan, 14); s.fillRt = fill.rectTransform;
            var k = UI.Box(tr.transform, "knob", UI.ML, Vector2.zero, new Vector2(64, 64), Pal.White, Sprites.Circle()); s.knob = k.rectTransform; s.knob.pivot = new Vector2(.5f, .5f);
            UI.Box(k.transform, "dot", UI.C, Vector2.zero, new Vector2(26, 26), Pal.Cyan, Sprites.Circle());
            s.Set(initial, false); return s;
        }
        public void Set(float v, bool fire = true)
        {
            value = Mathf.Clamp01(v); float w = track.sizeDelta.x;
            fillRt.sizeDelta = new Vector2(Mathf.Max(28, w * value), 28); knob.anchoredPosition = new Vector2(Mathf.Clamp(w * value, 32, w - 32), 0);
            if (valueText) valueText.text = Mathf.RoundToInt(value * 100) + "%";
            if (fire) onChanged?.Invoke(value);
        }
        void FromEvent(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(track, e.position, e.pressEventCamera, out var lp);
            Set((lp.x + track.sizeDelta.x * track.pivot.x) / track.sizeDelta.x);
        }
        public void OnPointerDown(PointerEventData e) { FromEvent(e); Tw.Scale(knob, 1.25f, .15f, Ease.OutBack); }
        public void OnDrag(PointerEventData e) { FromEvent(e); }
        public void OnPointerUp(PointerEventData e) { Tw.Scale(knob, 1f, .2f, Ease.OutBack); PlayerPrefs.Save(); }
    }

    /// <summary>ON/OFF switch: knob slides (OutCubic) and the track changes value, plus an ON/OFF word so state never relies on colour alone.</summary>
    public class UToggle : MonoBehaviour
    {
        public bool on; public Action<bool> onChanged; Image track; RectTransform knob; TextMeshProUGUI word; Image knobIcon; public Tappable tap;
        public static UToggle Create(Transform p, string name, Vector2 a, Vector2 pos, bool initial, Action<bool> changed)
        {
            var tap = UI.Button(p, name, a, pos, new Vector2(200, 100), Pal.PanelLo, Pal.White, "", 1, null, Pal.Mute);
            var t = tap.gameObject.AddComponent<UToggle>(); t.tap = tap; t.track = tap.bg; t.onChanged = changed;
            t.word = UI.Text(tap.transform, "word", "ON", 34, Pal.Ink, UI.C, new Vector2(-34, 0), new Vector2(90, 60));
            t.knob = UI.Box(tap.transform, "knob", UI.C, Vector2.zero, new Vector2(76, 76), Pal.White, Sprites.Circle()).rectTransform;
            tap.onClick = () => t.Set(!t.on, true, true);
            t.Set(initial, false, false); return t;
        }
        public void Set(bool v, bool animate, bool fire = false)
        {
            on = v;
            var kp = new Vector2(on ? 50 : -50, 0); var col = on ? Pal.Cyan : Pal.PanelLo;
            word.text = on ? "ON" : "OFF"; word.color = on ? Pal.Ink : Pal.Dim;
            word.rectTransform.anchoredPosition = new Vector2(on ? -34 : 34, 0);
            if (animate) { Tw.Move(knob, kp, .22f, Ease.OutCubic); Tw.Tint(track, col, .22f); }
            else { knob.anchoredPosition = kp; track.color = col; }
            tap.fill = col; if (fire) onChanged?.Invoke(on);
        }
    }

    /// <summary>Segmented control with a sliding selection pill (OutBack).</summary>
    public class USegment : MonoBehaviour
    {
        public int index; public Action<int> onChanged; RectTransform pill; Tappable[] items; string[] names; float w, h; float pad = 8;
        public static USegment Create(Transform p, string name, Vector2 a, Vector2 pos, Vector2 size, string[] labels, int initial, Action<int> changed, float fontSize = 42)
        {
            var bg = UI.Rounded(p, name, a, pos, size, Pal.PanelLo, Mathf.RoundToInt(size.y / 2)); UI.Outline(bg.transform, "ring", size, Pal.Mute, Mathf.RoundToInt(size.y / 2), 3);
            var s = bg.gameObject.AddComponent<USegment>(); s.onChanged = changed; s.names = labels; s.w = (size.x - 16) / labels.Length; s.h = size.y - 16;
            s.pill = UI.Rounded(bg.transform, "pill", UI.ML, Vector2.zero, new Vector2(s.w, s.h), Pal.Cyan, Mathf.RoundToInt(s.h / 2)).rectTransform;
            s.items = new Tappable[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var tap = UI.Button(bg.transform, name + "." + labels[i], UI.ML, new Vector2(8 + s.w * i, 0), new Vector2(s.w, s.h), new Color(0, 0, 0, 0), Pal.Dim, labels[i], fontSize, null, null, Mathf.RoundToInt(s.h / 2));
                tap.onClick = () => { s.Select(idx, true, true); };
                tap.rectTransform.pivot = tap.rectTransform.anchorMin; s.items[i] = tap;
            }
            s.Select(initial, false, false); return s;
        }
        public void Select(int i, bool animate, bool fire)
        {
            index = i; var target = new Vector2(8 + w * i, 0);
            if (animate) Tw.Move(pill, target, .32f, Ease.OutBack); else pill.anchoredPosition = target;
            for (int k = 0; k < items.Length; k++) { items[k].label.color = k == i ? Pal.Ink : Pal.Dim; items[k].labelColor = items[k].label.color; }
            if (fire) onChanged?.Invoke(i);
        }
    }

    public class SafeArea : MonoBehaviour
    {
        RectTransform rt; Rect last; Camera cam;
        void Awake() { rt = (RectTransform)transform; }
        void LateUpdate()
        {
            if (!cam) cam = GetComponentInParent<Canvas>()?.worldCamera;
            Rect sa = (cam && cam.targetTexture) ? new Rect(0, 0, Screen.width, Screen.height) : Screen.safeArea;
            if (sa == last) return; last = sa;
            float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            rt.anchorMin = new Vector2(sa.xMin / w, sa.yMin / h); rt.anchorMax = new Vector2(sa.xMax / w, sa.yMax / h); rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
