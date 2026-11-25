using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lab07
{
    // =====================================================================  1. TITLE
    public class TitleScreen : MenuScreen
    {
        Tappable cta, catcher; RectTransform ship, nova, rush, tag, loadGroup, barFill; TextMeshProUGUI loadTxt; bool ready; Image glow;
        const float BarW = 640f;

        public override void Build()
        {
            catcher = UI.Button(Root, "title.catcher", UI.C, Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0), Pal.White, "", 1);
            catcher.onClick = () => { if (ready) Go(); };
            glow = UI.Box(Root, "glow", UI.C, new Vector2(0, 440), new Vector2(820, 820), Pal.A(Pal.Cyan, .30f), Sprites.Glow());
            ship = UI.Icon(Root, "ship", "ship1", UI.C, new Vector2(0, 440), 320, Pal.Cyan).rectTransform; Mark("ship", ship);
            nova = UI.Text(Root, "nova", "NOVA", 200, Pal.Cyan, UI.C, new Vector2(0, 110), new Vector2(1000, 220), TextAlignmentOptions.Center, true, 26).rectTransform;
            rush = UI.Text(Root, "rush", "RUSH", 200, Pal.Amber, UI.C, new Vector2(0, -95), new Vector2(1000, 220), TextAlignmentOptions.Center, true, 26).rectTransform;
            Mark("logo", UI.Place(UI.Rect(Root, "logoMark"), UI.C, new Vector2(0, 10), new Vector2(860, 470)));
            tag = UI.Text(Root, "tag", "Dodge the dark. Chase the light.", 46, Pal.Dim, UI.C, new Vector2(0, -255), new Vector2(940, 80), TextAlignmentOptions.Center, false).rectTransform;

            loadGroup = UI.Rect(Root, "loader"); UI.Place(loadGroup, UI.BC, new Vector2(0, 360), new Vector2(BarW, 140)); Mark("loader", loadGroup);
            UI.Rounded(loadGroup, "track", UI.TC, Vector2.zero, new Vector2(BarW, 30), Pal.PanelLo, 15);
            UI.Outline(loadGroup.Find("track"), "ring", new Vector2(BarW, 30), Pal.Mute, 15, 3);
            barFill = UI.Rounded(loadGroup, "fill", UI.TL, Vector2.zero, new Vector2(30, 30), Pal.Cyan, 15).rectTransform;
            loadTxt = UI.Text(loadGroup, "txt", "Loading… 0%", 38, Pal.Dim, UI.BC, Vector2.zero, new Vector2(BarW, 70), TextAlignmentOptions.Center, false);

            cta = UI.Button(Root, "title.start", UI.BC, new Vector2(0, 330), new Vector2(680, 170), Pal.Cyan, Pal.Ink, "TAP TO START", 60, "play");
            cta.onClick = () => { if (ready) Go(); }; Mark("cta", cta.rectTransform);
            var foot = UI.Text(Root, "foot", "ITU CS-464 · Lab 07 · v1.0", 32, Pal.Mute, UI.BC, new Vector2(0, 70), new Vector2(900, 60), TextAlignmentOptions.Center, false);
            Mark("footer", foot.rectTransform);
        }

        void Go() { nav.Push(Scr.Main, Trans.Fade); }

        public override void OnShow(bool returning)
        {
            ready = false; Tw.Kill((this, "pulse")); Tw.Kill((this, "bob"));
            cta.gameObject.SetActive(false); loadGroup.gameObject.SetActive(true); UI.Group(loadGroup).alpha = 1;
            barFill.sizeDelta = new Vector2(30, 30); loadTxt.text = "Loading… 0%";
            var r = Rest.Of(ship);
            // hierarchy of entrance: ship (hero) -> NOVA -> RUSH -> tagline
            Tw.ScaleFrom(ship, .3f, 1f, .7f, Ease.OutBack); Intro.Rise(nova, 1, 90, .12f, .55f); Intro.Rise(rush, 2, 90, .12f, .55f); Intro.Rise(tag, 3, 40, .12f, .6f);
            Tw.Loop(k => ship.anchoredPosition = r.pos + new Vector2(0, (k - .5f) * 36f), 1.8f, Ease.InOutSine, true, (this, "bob"));
            Tw.Loop(k => glow.color = Pal.A(Pal.Cyan, .22f + .16f * k), 1.8f, Ease.InOutSine, true, (this, "glow"));
            float dur = returning ? .45f : 1.9f;
            Tw.To(k =>
            {
                barFill.sizeDelta = new Vector2(Mathf.Max(30, BarW * k), 30); loadTxt.text = "Loading… " + Mathf.RoundToInt(k * 100) + "%";
            }, dur, Ease.InOutQuad, .35f, ShowReady, (this, "load"));
        }

        void ShowReady()
        {
            ready = true;
            Tw.Alpha(UI.Group(loadGroup), 0, .25f, Ease.OutQuad, 0f, () => loadGroup.gameObject.SetActive(false));
            cta.gameObject.SetActive(true); Tw.ScaleFrom(cta.transform, .7f, 1f, .45f, Ease.OutBack); Tw.AlphaFrom(UI.Group(cta.rectTransform), 0, 1, .25f);
                        // attract motion: a ping halo, NOT a scale on the button itself (so press feedback stays readable)
            var halo = cta.rectTransform.Find("halo");
            if (!halo) { var h = UI.Rounded(cta.rectTransform, "halo", UI.C, Vector2.zero, new Vector2(680, 170), Pal.A(Pal.Cyan, .5f), 85); h.transform.SetAsFirstSibling(); halo = h.transform; }
            var hi = halo.GetComponent<Image>(); var hrt = (RectTransform)halo;
            Tw.Loop(k => { hrt.localScale = Vector3.one * (1f + .16f * k); hi.color = Pal.A(Pal.Cyan, .5f * (1 - k)); }, 1.3f, Ease.OutQuad, false, (this, "pulse"));
        }
        public override void OnBackKey() { }
    }

    // =====================================================================  2. MAIN MENU
    public class MainMenuScreen : MenuScreen
    {
        RectTransform hero, playHalo, chips; Image heroImg, heroGlow; TextMeshProUGUI shipLabel, lvl; Image xpFill;
        readonly List<RectTransform> tiles = new List<RectTransform>();

        public override void Build()
        {
            // profile chip (identity) + coin chip (resource)
            var prof = UI.Rounded(Root, "profile", UI.TL, new Vector2(40, -40), new Vector2(560, 120), Pal.PanelLo, 60); UI.Outline(prof.transform, "ring", new Vector2(560, 120), Pal.Mute, 60, 3);
            UI.Box(prof.transform, "avatar", UI.ML, new Vector2(14, 0), new Vector2(92, 92), Pal.Cyan, Sprites.Circle());
            UI.Icon(prof.transform, "av", "user", UI.ML, new Vector2(30, -2), 60, Pal.Ink);
            UI.Text(prof.transform, "name", "Pilot Nova", 40, Pal.White, UI.TL, new Vector2(124, -14), new Vector2(300, 52), TextAlignmentOptions.MidlineLeft);
            lvl = UI.Text(prof.transform, "lvl", "Lv 7", 34, Pal.Amber, UI.TR, new Vector2(-28, -16), new Vector2(110, 46), TextAlignmentOptions.MidlineRight);
            UI.Rounded(prof.transform, "xpTrack", UI.BL, new Vector2(124, 24), new Vector2(410, 16), Pal.Panel, 8);
            xpFill = UI.Rounded(prof.transform, "xpFill", UI.BL, new Vector2(124, 24), new Vector2(60, 16), Pal.Cyan, 8);
            Mark("profile", prof.rectTransform);
            var coin = CoinChip.Create(Root, UI.TR, new Vector2(-40, -50)); Mark("coins", coin.rectTransform);

            // hero
            heroGlow = UI.Box(Root, "glow", UI.C, new Vector2(0, 380), new Vector2(760, 760), Pal.A(Pal.Cyan, .25f), Sprites.Glow());
            heroImg = UI.Icon(Root, "hero", "ship1", UI.C, new Vector2(0, 380), 340, Pal.Cyan); hero = heroImg.rectTransform; Mark("hero", hero);
            shipLabel = UI.Text(Root, "shipLabel", "COMET", 44, Pal.Dim, UI.C, new Vector2(0, 150), new Vector2(600, 60), TextAlignmentOptions.Center, true, 10); Mark("shipLabel", shipLabel.rectTransform);

            // primary action - largest, brightest, centred: strongest hierarchy on the page
            playHalo = UI.Rounded(Root, "halo", UI.C, new Vector2(0, -60), new Vector2(860, 200), Pal.A(Pal.Cyan, .5f), 100).rectTransform;
            var play = UI.Button(Root, "main.play", UI.C, new Vector2(0, -60), new Vector2(860, 200), Pal.Cyan, Pal.Ink, "PLAY", 92, "play");
            play.onClick = () => nav.Push(Scr.Mode); Mark("play", play.rectTransform);

            // secondary actions - equal-sized tiles grouped in one row (proximity + repetition)
            tiles.Clear();
            Tile("main.hangar", -300, "ship1", "HANGAR", () => nav.Push(Scr.Loadout), 0);
            var shop = Tile("main.shop", 0, "bag", "SHOP", () => nav.Push(Scr.Shop), 1);
            Tile("main.settings", 300, "gear", "SETTINGS", () => nav.Push(Scr.Settings), 2);
            var badge = UI.Box(shop.transform, "badge", UI.TR, new Vector2(-14, -14), new Vector2(70, 70), Pal.Pink, Sprites.Circle());
            UI.Text(badge.transform, "n", "2", 38, Pal.Ink, UI.C, Vector2.zero, new Vector2(70, 70)); Mark("badge", badge.rectTransform);
            Mark("tiles", UI.Place(UI.Rect(Root, "tilesMark"), UI.C, new Vector2(0, -400), new Vector2(880, 290)));

            // tertiary
            var cr = UI.Button(Root, "main.credits", UI.BC, new Vector2(0, 110), new Vector2(420, 120), new Color(0, 0, 0, 0), Pal.Dim, "Credits", 42, "info", Pal.Mute);
            cr.onClick = () => nav.Push(Scr.Credits); Mark("credits", cr.rectTransform);
            Back = null;
        }

        Tappable Tile(string key, float x, string icon, string label, System.Action act, int i)
        {
            var bg = UI.Rounded(Root, key, UI.C, new Vector2(x, -400), new Vector2(270, 290), Pal.PanelHi, 48); bg.raycastTarget = true;
            var t = bg.gameObject.AddComponent<Tappable>(); t.key = key; t.bg = bg; t.fill = Pal.PanelHi; t.onClick = act;
            UI.Outline(bg.transform, "ring", new Vector2(270, 290), Pal.Mute, 48, 3);
            t.icon = UI.Icon(bg.transform, "icon", icon, UI.C, new Vector2(0, 38), 130, Pal.White);
            t.label = UI.Text(bg.transform, "label", label, 36, Pal.Dim, UI.C, new Vector2(0, -98), new Vector2(260, 50), TextAlignmentOptions.Center, true, 3);
            tiles.Add(t.rectTransform); return t;
        }

        public override void OnShow(bool returning)
        {
            var s = GS.Ships[GS.ShipIndex]; heroImg.sprite = Sprites.Icon(s.icon); heroImg.color = s.color; heroGlow.color = Pal.A(s.color, .25f); shipLabel.text = s.name;
            int xp = GS.Xp; lvl.text = "Lv " + (1 + xp / 100); xpFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(16, 410f * (xp % 100) / 100f), 16);
            Tw.Kill((this, "bob")); Tw.Kill((this, "ping"));
            var r = Rest.Of(hero);
            Tw.Loop(k => hero.anchoredPosition = r.pos + new Vector2(0, (k - .5f) * 30f), 2.2f, Ease.InOutSine, true, (this, "bob"));
            Tw.Loop(k => { playHalo.localScale = Vector3.one * (1f + .09f * k); playHalo.GetComponent<Image>().color = Pal.A(Pal.Cyan, .45f * (1 - k)); }, 1.5f, Ease.OutQuad, false, (this, "ping"));
            if (returning) return;
            Tw.ScaleFrom(hero, .6f, 1f, .6f, Ease.OutBack);
            Intro.Rise(Marks["play"], 1, 80, .08f, .5f);
            for (int i = 0; i < tiles.Count; i++) Intro.Rise(tiles[i], 2 + i, 80, .08f, .5f);
            Intro.Rise(Marks["credits"], 5, 40, .08f, .5f);
        }
        public override void OnBackKey() { nav.Back(); }
    }

    // =====================================================================  3. MODE SELECT
    public class ModeScreen : MenuScreen
    {
        readonly List<RectTransform> cards = new List<RectTransform>(); int selected = -1; readonly List<Image> rings = new List<Image>(); readonly List<Image> checks = new List<Image>();

        public override void Build()
        {
            UI.Header(this, "SELECT MODE");
            var sub = UI.Text(Root, "sub", "How do you want to fly today?", 44, Pal.Dim, UI.TC, new Vector2(0, -205), new Vector2(900, 70), TextAlignmentOptions.Center, false); Mark("sub", sub.rectTransform);
            Card(0, "flag", "CAMPAIGN", "Clear 12 sectors and collect stars", Pal.Cyan, true);
            Card(1, "infinity", "ENDLESS", "Survive the longest · Best " + GS.BestScore.ToString("N0"), Pal.Amber, true);
            Card(2, "clock", "TIME ATTACK", "45 seconds. Highest score wins.", Pal.Pink, true);
            Card(3, "trophy", "DAILY RUN", "Reach Lv 10 to unlock", Pal.Mute, false);
            Mark("cards", UI.Place(UI.Rect(Root, "cardsMark"), UI.TC, new Vector2(0, -300), new Vector2(960, 1430)));
        }

        void Card(int i, string icon, string title, string desc, Color accent, bool unlocked)
        {
            float y = -300 - i * 370;
            var bg = UI.Rounded(Root, "mode" + i, UI.TC, new Vector2(0, y), new Vector2(960, 320), unlocked ? Pal.PanelHi : Pal.PanelLo, 52); bg.raycastTarget = true;
            var t = bg.gameObject.AddComponent<Tappable>(); t.key = "mode." + i; t.bg = bg; t.fill = bg.color;
            var ring = UI.Outline(bg.transform, "ring", new Vector2(960, 320), unlocked ? Pal.Mute : Pal.Locked, 52, 3); rings.Add(ring);
            var disc = UI.Box(bg.transform, "disc", UI.ML, new Vector2(36, 0), new Vector2(210, 210), unlocked ? Pal.A(accent, 1f) : Pal.Locked, Sprites.Circle());
            UI.Icon(disc.transform, "icon", unlocked ? icon : "lock", UI.C, Vector2.zero, 120, unlocked ? Pal.Ink : Pal.Mute);
            UI.Text(bg.transform, "title", title, 56, unlocked ? Pal.White : Pal.Mute, UI.TL, new Vector2(276, -70), new Vector2(560, 70), TextAlignmentOptions.MidlineLeft, true, 3);
            UI.Text(bg.transform, "desc", desc, 38, unlocked ? Pal.Dim : Pal.Mute, UI.TL, new Vector2(276, -150), new Vector2(560, 120), TextAlignmentOptions.TopLeft, false);
            if (unlocked) UI.Icon(bg.transform, "go", "chev", UI.MR, new Vector2(-30, 0), 90, Pal.Cyan);
            else UI.Text(bg.transform, "lv", "LOCKED", 30, Pal.Mute, UI.MR, new Vector2(-30, 0), new Vector2(170, 50), TextAlignmentOptions.MidlineRight, true, 2);
            int idx = i;
            t.onClick = () =>
            {
                if (!unlocked) { return; }
                selected = idx;
                if (idx == 0) nav.Push(Scr.Level);
                else { GS.Session.Mode = idx; GS.Session.Level = 1; App.I.flight.Begin(); }
            };
            if (!unlocked) { t.SetInteractable(false, Pal.PanelLo); t.onDenied = () => nav.Toast("Daily Challenge unlocks at Lv 10", false); t.bg.color = Pal.PanelLo; }
            cards.Add(t.rectTransform);
        }

        public override void OnShow(bool returning)
        {
            if (returning) return;
            // cards enter from the right in order of importance (top -> bottom), 70 ms stagger
            for (int i = 0; i < cards.Count; i++) Intro.SlideIn(cards[i], i + 1, 220, .07f, .5f);
            Intro.Rise(Marks["sub"], 0, 30, .07f, .45f);
        }
    }

    // =====================================================================  4. LEVEL SELECT
    public class LevelScreen : MenuScreen
    {
        readonly List<RectTransform> nodes = new List<RectTransform>(); readonly List<Image> nodeBg = new List<Image>(); readonly List<Image> nodeRing = new List<Image>();
        int sel = 4; TextMeshProUGUI infoName, infoSub, sector; Tappable start; Image selRing; RectTransform info;

        public override void Build()
        {
            UI.Header(this, "CAMPAIGN");
            var banner = UI.Rounded(Root, "banner", UI.TC, new Vector2(0, -200), new Vector2(960, 130), Pal.Panel, 40); UI.Outline(banner.transform, "ring", new Vector2(960, 130), Pal.Mute, 40, 3);
            sector = UI.Text(banner.transform, "sector", "SECTOR 1 · OUTER RIM", 38, Pal.White, UI.ML, new Vector2(36, 24), new Vector2(640, 56), TextAlignmentOptions.MidlineLeft, true, 3);
            UI.Text(banner.transform, "sub", "", 34, Pal.Dim, UI.ML, new Vector2(36, -26), new Vector2(560, 50), TextAlignmentOptions.MidlineLeft, false).name = "progress";
            UI.Icon(banner.transform, "star", "star", UI.MR, new Vector2(-250, 0), 64, Pal.Amber);
            UI.Text(banner.transform, "stars", "", 48, Pal.Amber, UI.MR, new Vector2(-30, 0), new Vector2(200, 70), TextAlignmentOptions.MidlineLeft).name = "starsTxt";
            Mark("banner", banner.rectTransform);

            for (int i = 1; i <= GS.Levels; i++)
            {
                int col = (i - 1) % 3, row = (i - 1) / 3; var pos = new Vector2((col - 1) * 310, -400 - row * 300);
                var bg = UI.Rounded(Root, "node" + i, UI.TC, pos, new Vector2(280, 280), Pal.PanelHi, 52); bg.raycastTarget = true;
                var t = bg.gameObject.AddComponent<Tappable>(); t.key = "level." + i; t.bg = bg; t.fill = Pal.PanelHi;
                var ring = UI.Outline(bg.transform, "ring", new Vector2(280, 280), Pal.Mute, 52, 3); nodeRing.Add(ring);
                int lv = i; t.onClick = () => Select(lv, true);
                nodes.Add(t.rectTransform); nodeBg.Add(bg);
            }
            selRing = UI.Outline(Root, "selRing", new Vector2(310, 310), Pal.Cyan, 60, 8); selRing.rectTransform.anchorMin = selRing.rectTransform.anchorMax = UI.TC; selRing.rectTransform.pivot = UI.TC; selRing.rectTransform.sizeDelta = new Vector2(316, 316);
            Mark("grid", UI.Place(UI.Rect(Root, "gridMark"), UI.TC, new Vector2(0, -400 - 450 + 140), new Vector2(940, 1200)));

            info = UI.Rounded(Root, "info", UI.BC, new Vector2(0, 40), new Vector2(960, 290), Pal.Panel, 52).rectTransform; UI.Outline(info, "ring", new Vector2(960, 290), Pal.Cyan, 52, 3);
            infoName = UI.Text(info, "name", "", 54, Pal.White, UI.TL, new Vector2(40, -40), new Vector2(520, 130), TextAlignmentOptions.TopLeft);
            infoSub = UI.Text(info, "sub", "", 38, Pal.Dim, UI.BL, new Vector2(40, 40), new Vector2(520, 90), TextAlignmentOptions.BottomLeft, false);
            start = UI.Button(info, "level.start", UI.MR, new Vector2(-36, 0), new Vector2(340, 210), Pal.Cyan, Pal.Ink, "START", 52, "play", null, 60);
            start.onClick = () => { GS.Session.Mode = 0; GS.Session.Level = sel; App.I.flight.Begin(); };
            start.onDenied = () => nav.Toast("Clear the previous level to unlock", false);
            Mark("info", info); Mark("start", start.rectTransform);
        }

        void Paint()
        {
            for (int i = 1; i <= GS.Levels; i++)
            {
                var bg = nodeBg[i - 1]; var t = bg.transform; bool open = GS.Unlocked(i); int stars = GS.Stars(i);
                for (int c = t.childCount - 1; c >= 0; c--) { var ch = t.GetChild(c); if (ch.name != "ring") Object.Destroy(ch.gameObject); }
                bg.color = open ? (stars > 0 ? Pal.PanelHi : Pal.Panel) : Pal.PanelLo;
                nodeRing[i - 1].color = open ? (stars > 0 ? Pal.Mute : Pal.Cyan) : Pal.Locked;
                if (open)
                {
                    UI.Text(t, "num", i.ToString(), 120, Pal.White, UI.C, new Vector2(0, 28), new Vector2(240, 140));
                    for (int s = 0; s < 3; s++) UI.Icon(t, "s" + s, s < stars ? "star" : "star_o", UI.BC, new Vector2((s - 1) * 62, 38), 56, s < stars ? Pal.Amber : Pal.Mute);
                }
                else
                {
                    UI.Icon(t, "lock", "lock", UI.C, new Vector2(0, 28), 120, Pal.Mute);
                    UI.Text(t, "num", i.ToString(), 40, Pal.Mute, UI.BC, new Vector2(0, 36), new Vector2(200, 56));
                }
            }
            var prog = Root.Find("banner/progress")?.GetComponent<TextMeshProUGUI>(); if (prog) prog.text = GS.Cleared + " / " + GS.Levels + " cleared";
            var st = Root.Find("banner/starsTxt")?.GetComponent<TextMeshProUGUI>(); if (st) st.text = GS.TotalStars + "/" + GS.Levels * 3;
        }

        void Select(int lv, bool animate)
        {
            sel = lv; bool open = GS.Unlocked(lv);
            var rt = nodes[lv - 1]; selRing.rectTransform.anchoredPosition = rt.anchoredPosition + new Vector2(0, 18);
            if (animate) { Tw.ScaleFrom(selRing.transform, 1.18f, 1f, .35f, Ease.OutBack); if (!open) Tw.Shake(rt); else Tw.ScaleFrom(rt, .92f, 1f, .35f, Ease.OutBack); }
            infoName.text = open ? "LEVEL " + lv + "\n" + GS.LevelNames[lv - 1] : "LEVEL " + lv + "\nLocked";
            infoName.fontSize = 46; infoName.alignment = TextAlignmentOptions.TopLeft;
            int stars = GS.Stars(lv);
            infoSub.text = open ? (stars > 0 ? "Best " + GS.Best(lv).ToString("N0") + "  ·  " + stars + "/3 stars" : "Not cleared yet") : "Clear level " + (lv - 1) + " to unlock";
            start.SetLabel(open ? "START" : "LOCKED"); start.SetInteractable(open, Pal.Locked);
            if (animate) { Tw.AlphaFrom(UI.Group(infoName.rectTransform), 0, 1, .25f); Tw.AlphaFrom(UI.Group(infoSub.rectTransform), 0, 1, .3f); }
            selRing.color = open ? Pal.Cyan : Pal.Pink;
        }

        public override void OnShow(bool returning)
        {
            Paint(); if (!GS.Unlocked(sel)) sel = 1;
            if (GS.Session.Level >= 1 && GS.Session.Level <= GS.Levels && GS.Unlocked(GS.Session.Level)) sel = GS.Session.Level;
            Select(sel, false);
            if (returning) return;
            Intro.Rise(Marks["banner"], 0, 40, .06f, .45f);
            for (int i = 0; i < nodes.Count; i++) Intro.Pop(nodes[i], i + 1, .045f, .45f);
            Intro.Rise(Marks["info"], 6, 120, .06f, .55f);
        }
    }
}
