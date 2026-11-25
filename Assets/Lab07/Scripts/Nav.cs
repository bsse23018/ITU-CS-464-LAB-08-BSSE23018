using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lab07
{
    public enum Scr { Title, Main, Mode, Level, Loadout, Shop, Settings, Hud, Pause, Results, Credits }
    public enum Trans { Slide, Fade, Modal }

    public abstract class MenuScreen
    {
        public Nav nav; public RectTransform Root; public CanvasGroup Cg; public Scr Id; public Tappable Back;
        public bool Modal;
        public readonly Dictionary<string, RectTransform> Marks = new Dictionary<string, RectTransform>();
        public void Mark(string k, RectTransform rt) { Marks[k] = rt; }
        public abstract void Build();
        public virtual void OnShow(bool returning) { }
        public virtual void OnHide() { }
        /// <summary>Modal screens animate themselves out; Nav waits for the callback.</summary>
        public virtual void PlayOut(Action done) { done?.Invoke(); }
        public virtual void OnBackKey() { if (Back != null) Back.Click(); else nav.Back(); }
    }

    /// <summary>Screen stack + transitions. Every screen is reachable and every screen has a way back.</summary>
    public class Nav
    {
        public RectTransform Safe, Overlay; public Image Blocker;
        readonly Dictionary<Scr, MenuScreen> screens = new Dictionary<Scr, MenuScreen>();
        public readonly List<MenuScreen> Stack = new List<MenuScreen>();
        readonly List<Trans> enter = new List<Trans>();
        public bool Busy { get; private set; }
        public MenuScreen Current => Stack.Count > 0 ? Stack[Stack.Count - 1] : null;
        RectTransform toast; TextMeshProUGUI toastText; Image toastBg, toastIcon;

        public Nav(RectTransform safe, RectTransform overlay)
        {
            Safe = safe; Overlay = overlay;
            Blocker = UI.Box(overlay, "InputBlocker", UI.C, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0)); UI.Stretch(Blocker.rectTransform);
            Blocker.raycastTarget = true; Blocker.gameObject.SetActive(false);
            BuildToast();
        }

        MenuScreen Create(Scr id)
        {
            switch (id)
            {
                case Scr.Title: return new TitleScreen();
                case Scr.Main: return new MainMenuScreen();
                case Scr.Mode: return new ModeScreen();
                case Scr.Level: return new LevelScreen();
                case Scr.Loadout: return new LoadoutScreen();
                case Scr.Shop: return new ShopScreen();
                case Scr.Settings: return new SettingsScreen();
                case Scr.Hud: return new HudScreen();
                case Scr.Pause: return new PauseScreen();
                case Scr.Results: return new ResultsScreen();
                default: return new CreditsScreen();
            }
        }

        public MenuScreen Get(Scr id)
        {
            if (screens.TryGetValue(id, out var s)) return s;
            s = Create(id); s.nav = this; s.Id = id;
            s.Root = UI.Stretch(UI.Rect(Safe, id.ToString())); s.Cg = s.Root.gameObject.AddComponent<CanvasGroup>();
            s.Build(); s.Root.gameObject.SetActive(false); screens[id] = s; return s;
        }
        public T Get<T>(Scr id) where T : MenuScreen => (T)Get(id);
        public MenuScreen Peek(Scr id) { screens.TryGetValue(id, out var s); return s; }

        // ------------------------------------------------------------ navigation API
        public void Start(Scr id)
        {
            var s = Get(id); Stack.Clear(); enter.Clear(); Stack.Add(s); enter.Add(Trans.Fade);
            s.Root.gameObject.SetActive(true); s.Cg.alpha = 1; s.OnShow(false);
        }

        public void Push(Scr id, Trans t = Trans.Slide)
        {
            if (Busy) return; var next = Get(id); var cur = Current;
            if (next.Modal) t = Trans.Modal;
            Stack.Add(next); enter.Add(t); Run(cur, next, t, true, false);
        }

        public void Replace(Scr id, Trans t = Trans.Fade)
        {
            if (Busy) return; var next = Get(id); var cur = Current;
            Stack[Stack.Count - 1] = next; enter[enter.Count - 1] = t; Run(cur, next, t, true, true);
        }

        public void Back()
        {
            if (Busy || Stack.Count <= 1) return;
            GameAudio.UI(UiCue.Close);
            var cur = Current; var t = enter[enter.Count - 1];
            Stack.RemoveAt(Stack.Count - 1); enter.RemoveAt(enter.Count - 1);
            Run(cur, Current, t, false, false);
        }

        /// <summary>Go to a root screen (e.g. Home): stack becomes [Title, id].</summary>
        public void GoRoot(Scr id, Trans t = Trans.Fade)
        {
            if (Busy) return; var next = Get(id); var cur = Current; var title = Get(Scr.Title);
            if (next == cur) return;
            foreach (var screen in screens.Values)
            {
                if (screen == cur || screen == next || !screen.Root.gameObject.activeSelf) continue;
                screen.Root.gameObject.SetActive(false);
                screen.OnHide();
            }
            if (id == Scr.Main)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
                App.I.flight.ReturnToMenu();
                GameAudio.Instance.Music(false);
            }
            Stack.Clear(); enter.Clear(); if (id != Scr.Title) { Stack.Add(title); enter.Add(Trans.Fade); }
            Stack.Add(next); enter.Add(t);
            Run(cur, next, t, true, true);
        }

        /// <summary>Instant jump used by the screenshot tool.</summary>
        public void Jump(Scr id)
        {
            foreach (var s in screens.Values) { s.Root.gameObject.SetActive(false); Tw.Kill(s.Root); }
            Stack.Clear(); enter.Clear(); Busy = false; Blocker.gameObject.SetActive(false);
            var n = Get(id); Stack.Add(n); enter.Add(Trans.Fade);
            n.Root.anchoredPosition = Vector2.zero; n.Cg.alpha = 1; n.Root.gameObject.SetActive(true); n.Root.SetAsLastSibling(); n.OnShow(false);
        }

        void Run(MenuScreen outS, MenuScreen inS, Trans t, bool forward, bool replace)
        {
            Busy = true;
            Overlay.SetAsLastSibling();
            Blocker.transform.SetAsLastSibling();
            Blocker.gameObject.SetActive(true);
            if (forward && (inS.Id == Scr.Pause || inS.Id == Scr.Settings || inS.Id == Scr.Shop || inS.Id == Scr.Loadout || inS.Id == Scr.Credits))
                GameAudio.UI(UiCue.Open);
            float W = Mathf.Max(1080f, Safe.rect.width);
            float dur = t == Trans.Slide ? .4f : t == Trans.Modal ? .38f : .42f;

            // z-order: forward -> entering screen on top; backward -> leaving screen on top
            inS.Root.gameObject.SetActive(true); inS.Root.anchoredPosition = Vector2.zero;
            if (forward) inS.Root.SetAsLastSibling(); else { inS.Root.SetAsLastSibling(); outS.Root.SetAsLastSibling(); }
            Overlay.SetAsLastSibling();
            if (inS.Modal && !forward) { }

            Action finish = () =>
            {
                bool outStays = (t == Trans.Modal && forward);
                if (!outStays)
                {
                    outS.Root.gameObject.SetActive(false); outS.Root.anchoredPosition = Vector2.zero; outS.Cg.alpha = 1; outS.Cg.interactable = true; outS.Root.localScale = Vector3.one; outS.OnHide();
                    // hide anything left underneath a modal that has been replaced/escaped
                    if (!inS.Modal) foreach (var s in Stack) if (s != inS && s.Root.gameObject.activeSelf) { s.Root.gameObject.SetActive(false); s.OnHide(); }
                }
                inS.Root.anchoredPosition = Vector2.zero; inS.Cg.alpha = 1; inS.Root.localScale = Vector3.one;
                Busy = false; Blocker.gameObject.SetActive(false);
            };

            // a modal that is being restored needs the screen under it visible again
            if (inS.Modal && forward == false) { }
            if (!forward && inS.Modal) { var under = Stack.Count >= 2 ? Stack[Stack.Count - 2] : null; if (under != null) { under.Root.gameObject.SetActive(true); } }

            if (t == Trans.Slide)
            {
                                if (forward)
                {
                    outS.Cg.interactable = false;
                    Tw.Move(outS.Root, new Vector2(-W * .28f, 0), dur, Ease.OutCubic); Tw.Alpha(outS.Cg, 0, dur * .9f, Ease.OutQuad);
                    Tw.MoveFrom(inS.Root, new Vector2(W, 0), dur, Ease.OutCubic); Tw.AlphaFrom(inS.Cg, 0, 1, dur * .6f, Ease.OutQuad);
                    inS.OnShow(false);
                }
                else
                {
                    Tw.Move(outS.Root, new Vector2(W, 0), dur * .9f, Ease.OutCubic); Tw.Alpha(outS.Cg, 0, dur * .8f, Ease.OutQuad);
                    Tw.MoveFrom(inS.Root, new Vector2(-W * .28f, 0), dur, Ease.OutCubic); Tw.AlphaFrom(inS.Cg, 0, 1, dur * .7f, Ease.OutQuad);
                    inS.OnShow(true);
                }
            }
            else if (t == Trans.Fade)
            {
                Tw.Alpha(outS.Cg, 0, dur * .55f, Ease.OutQuad);
                Tw.ScaleFrom(inS.Root, 1.05f, 1f, dur, Ease.OutCubic); Tw.AlphaFrom(inS.Cg, 0, 1, dur * .8f, Ease.OutQuad, dur * .25f);
                inS.OnShow(!forward);
            }
            else // modal
            {
                if (forward) { inS.Cg.alpha = 1; inS.OnShow(false); }
                else { inS.OnShow(true); }
            }

            if (t == Trans.Modal && !forward)
            {
                // leaving modal animates itself (card scales out, dim fades) then hands control back
                outS.PlayOut(() => Tw.To(k => { }, .02f, Ease.Linear, 0f, finish));
            }
            else Tw.To(k => { }, dur + .04f, Ease.Linear, 0f, finish);
        }

        // ------------------------------------------------------------ toast
        void BuildToast()
        {
            toast = UI.Rect(Overlay, "Toast"); UI.Place(toast, UI.BC, new Vector2(0, 140), new Vector2(900, 120));
            toastBg = toast.gameObject.AddComponent<Image>(); toastBg.sprite = Sprites.Round(60); toastBg.type = Image.Type.Sliced; toastBg.color = Pal.White; toastBg.raycastTarget = false;
            toastIcon = UI.Icon(toast, "icon", "info", UI.ML, new Vector2(40, 0), 64, Pal.Ink);
            toastText = UI.Text(toast, "text", "", 42, Pal.Ink, UI.C, new Vector2(40, 0), new Vector2(740, 110));
            toast.gameObject.SetActive(false);
        }

        public void Toast(string msg, bool good = true)
        {
            GameAudio.UI(good ? UiCue.Success : UiCue.Error);
            toast.gameObject.SetActive(true); toast.SetAsLastSibling();
            toastText.text = msg; toastBg.color = good ? Pal.Cyan : Pal.Pink; toastIcon.sprite = Sprites.Icon(good ? "check" : "close");
            Tw.Kill((toast, "pos")); Tw.Kill((toast, "hide"));
            toast.anchoredPosition = new Vector2(0, 40);
            Tw.Move(toast, new Vector2(0, 150), .38f, Ease.OutBack);
            Tw.To(k => { }, 1.8f, Ease.Linear, 0f, () => Tw.Move(toast, new Vector2(0, 20), .28f, Ease.InBack, 0f, () => toast.gameObject.SetActive(false)), (toast, "hide"));
                    }

        public void HideToastNow() { Tw.Kill((toast, "pos")); Tw.Kill((toast, "hide")); toast.gameObject.SetActive(false); }
    }
}
