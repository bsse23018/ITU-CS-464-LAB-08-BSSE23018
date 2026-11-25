using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lab07
{
    // =====================================================================  5. LOADOUT (HANGAR)
    public class LoadoutScreen : MenuScreen
    {
        int idx; Image shipImg, glow, lockImg; TextMeshProUGUI nameT, descT, lockNote; readonly Image[] fill = new Image[3]; readonly TextMeshProUGUI[] val = new TextMeshProUGUI[3];
        Tappable action; Image actIcon; readonly List<Tappable> boostChips = new List<Tappable>(); readonly List<Image> boostRings = new List<Image>(), boostChecks = new List<Image>(), dots = new List<Image>();
        readonly List<TextMeshProUGUI> boostCount = new List<TextMeshProUGUI>(); RectTransform stage, shipRt; string[] statNames = { "SPEED", "ARMOR", "HANDLING" };

        public override void Build()
        {
            UI.Header(this, "HANGAR");
            var st = UI.Rounded(Root, "stage", UI.TC, new Vector2(0, -200), new Vector2(960, 640), Pal.Panel, 56); stage = st.rectTransform; UI.Outline(stage, "ring", new Vector2(960, 640), Pal.Mute, 56, 3); Mark("stage", stage);
            glow = UI.Box(stage, "glow", UI.C, new Vector2(0, 30), new Vector2(620, 620), Pal.A(Pal.Cyan, .3f), Sprites.Glow());
            shipImg = UI.Icon(stage, "ship", "ship1", UI.C, new Vector2(0, 30), 360, Pal.Cyan); shipRt = shipImg.rectTransform; Mark("ship", shipRt);
            lockImg = UI.Icon(stage, "lock", "lock", UI.C, new Vector2(110, -80), 120, Pal.White);
            nameT = UI.Text(stage, "name", "COMET", 68, Pal.White, UI.TC, new Vector2(0, -26), new Vector2(700, 90), TextAlignmentOptions.Center, true, 8); Mark("shipName", nameT.rectTransform);
            var prev = UI.IconButton(stage, "loadout.prev", "back", UI.ML, new Vector2(22, 0), 130, Pal.PanelHi, Pal.White, 80, Pal.Mute); prev.onClick = () => Step(-1); Mark("prev", prev.rectTransform);
            var next = UI.IconButton(stage, "loadout.next", "next", UI.MR, new Vector2(-22, 0), 130, Pal.PanelHi, Pal.White, 80, Pal.Mute); next.onClick = () => Step(1); Mark("next", next.rectTransform);
            for (int i = 0; i < GS.Ships.Length; i++) dots.Add(UI.Rounded(stage, "dot" + i, UI.BC, new Vector2((i - 1) * 44, 30), new Vector2(24, 24), Pal.Mute, 12));
            lockNote = UI.Text(stage, "lockNote", "LOCKED", 34, Pal.Dim, UI.C, new Vector2(110, -170), new Vector2(240, 50), TextAlignmentOptions.Center, true, 3);

            descT = UI.Text(Root, "desc", "", 40, Pal.Dim, UI.TC, new Vector2(0, -860), new Vector2(900, 100), TextAlignmentOptions.Center, false); Mark("desc", descT.rectTransform);

            var stats = UI.Rect(Root, "stats"); UI.Place(stats, UI.TC, new Vector2(0, -975), new Vector2(960, 270)); Mark("stats", stats);
            for (int i = 0; i < 3; i++)
            {
                float y = -i * 90;
                UI.Text(stats, "l" + i, statNames[i], 36, Pal.Dim, UI.TL, new Vector2(30, y - 14), new Vector2(240, 60), TextAlignmentOptions.MidlineLeft, true, 3);
                UI.Rounded(stats, "track" + i, UI.TL, new Vector2(290, y - 28), new Vector2(520, 30), Pal.PanelLo, 15);
                fill[i] = UI.Rounded(stats, "fill" + i, UI.TL, new Vector2(290, y - 28), new Vector2(30, 30), Pal.Cyan, 15);
                val[i] = UI.Text(stats, "v" + i, "", 38, Pal.White, UI.TR, new Vector2(-30, y - 14), new Vector2(130, 60), TextAlignmentOptions.MidlineRight);
            }

            UI.Text(Root, "boostHead", "BOOST SLOT", 36, Pal.Dim, UI.TL, new Vector2(60, -1280), new Vector2(500, 50), TextAlignmentOptions.MidlineLeft, true, 4);
            for (int i = 0; i < GS.Boosts.Length; i++)
            {
                var b = GS.Boosts[i]; int bi = i; float x = (i - 1) * 322;
                var bg = UI.Rounded(Root, "boost" + i, UI.TC, new Vector2(x, -1340), new Vector2(300, 230), Pal.PanelHi, 44); bg.raycastTarget = true;
                var t = bg.gameObject.AddComponent<Tappable>(); t.key = "boost." + i; t.bg = bg; t.fill = Pal.PanelHi;
                boostRings.Add(UI.Outline(bg.transform, "ring", new Vector2(300, 230), Pal.Mute, 44, 3));
                UI.Icon(bg.transform, "icon", b.icon, UI.C, new Vector2(0, 36), 100, b.color);
                UI.Text(bg.transform, "n", b.name, 36, Pal.White, UI.BC, new Vector2(0, 54), new Vector2(280, 46));
                boostCount.Add(UI.Text(bg.transform, "cnt", "", 30, Pal.Dim, UI.BC, new Vector2(0, 14), new Vector2(280, 40), TextAlignmentOptions.Center, false));
                var chk = UI.Box(bg.transform, "chk", UI.TR, new Vector2(-12, -12), new Vector2(54, 54), Pal.Cyan, Sprites.Circle()); UI.Icon(chk.transform, "c", "check", UI.C, Vector2.zero, 36, Pal.Ink); boostChecks.Add(chk);
                t.onClick = () =>
                {
                    if (GS.BoostCount(b.id) <= 0 && bi != GS.BoostIndex) { Tw.Shake(t.rectTransform); nav.Toast(b.name + " needs a pack - buy one in the Shop", false); return; }
                    GS.BoostIndex = bi; PaintBoosts(true); Tw.ScaleFrom(t.transform, .92f, 1f, .35f, Ease.OutBack);
                };
                boostChips.Add(t);
            }
            Mark("boosts", UI.Place(UI.Rect(Root, "boostsMark"), UI.TC, new Vector2(0, -1330), new Vector2(960, 250)));

            action = UI.Button(Root, "loadout.action", UI.BC, new Vector2(0, 50), new Vector2(900, 170), Pal.Cyan, Pal.Ink, "EQUIP", 62); Mark("action", action.rectTransform);
            actIcon = UI.Icon(action.transform, "ai", "check", UI.C, Vector2.zero, 64, Pal.Ink);
            action.onClick = OnAction;
        }

        void Step(int d)
        {
            int n = (idx + d + GS.Ships.Length) % GS.Ships.Length; ShowShip(n, d);
        }

        void ShowShip(int n, int dir)
        {
            idx = n;             var rest = new Vector2(0, 30);
            // old ship exits (InQuad: accelerates away), new one eases in with OutCubic
            Tw.Move(shipRt, rest + new Vector2(-dir * 380, 0), .12f, Ease.InQuad, 0f, () =>
            {
                Paint(); shipRt.anchoredPosition = rest + new Vector2(dir * 380, 0); Tw.Move(shipRt, rest, .38f, Ease.OutCubic);
            });
            Tw.Alpha(UI.Group(shipRt), 0, .12f, Ease.InQuad, 0f, () => Tw.Alpha(UI.Group(shipRt), 1, .3f, Ease.OutQuad));
        }

        void Paint()
        {
            var s = GS.Ships[idx]; bool owned = GS.Owns(s.id);
            shipImg.sprite = Sprites.Icon(s.icon); shipImg.color = owned ? s.color : Pal.Mute; glow.color = Pal.A(owned ? s.color : Pal.Mute, .28f);
            nameT.text = s.name; descT.text = s.desc; lockImg.gameObject.SetActive(!owned); lockNote.gameObject.SetActive(!owned);
            for (int di = 0; di < dots.Count; di++) { bool a = di == idx; var dot = dots[di]; float w0 = dot.rectTransform.sizeDelta.x; Tw.Tint(dot, a ? Pal.Cyan : Pal.Mute, .2f); Tw.To(k => dot.rectTransform.sizeDelta = new Vector2(Mathf.LerpUnclamped(w0, a ? 60 : 24, k), 24), .25f, Ease.OutBack, 0f, null, (dot, "w")); }
            int[] v = { s.speed, s.armor, s.handling };
            for (int i = 0; i < 3; i++)
            {
                int ii = i; float to = Mathf.Max(30, 520f * v[i] / 10f); float from = fill[i].rectTransform.sizeDelta.x;
                fill[i].color = owned ? s.color : Pal.Mute; val[i].text = v[i] + "/10";
                Tw.To(k => fill[ii].rectTransform.sizeDelta = new Vector2(Mathf.LerpUnclamped(from, to, k), 30), .55f, Ease.OutCubic, i * .07f, null, (fill[i], "w"));
            }
            SetAction();
        }

        void SetAction()
        {
            var s = GS.Ships[idx]; bool owned = GS.Owns(s.id); bool eq = GS.ShipIndex == idx && owned;
            string text = eq ? "EQUIPPED" : owned ? "EQUIP" : "UNLOCK  " + s.price.ToString("N0");
            action.SetLabel(text); float w = action.label.GetPreferredValues(text).x;
            if (!owned) { action.SetFill(Pal.Amber); action.labelColor = Pal.Ink; actIcon.sprite = Sprites.Icon("coin"); actIcon.gameObject.SetActive(true); action.label.rectTransform.anchoredPosition = new Vector2(-34, 0); actIcon.rectTransform.anchoredPosition = new Vector2(w / 2 - 34 + 56, 0); action.SetInteractable(true); action.bg.color = Pal.Amber; action.label.color = Pal.Ink; actIcon.color = Pal.Ink; }
            else if (eq) { action.SetFill(Pal.PanelHi); actIcon.sprite = Sprites.Icon("check"); actIcon.gameObject.SetActive(true); action.label.rectTransform.anchoredPosition = new Vector2(36, 0); actIcon.rectTransform.anchoredPosition = new Vector2(-w / 2 + 36 - 52, 0); action.SetInteractable(false, Pal.PanelHi); action.label.color = Pal.White; actIcon.color = Pal.Cyan; }
            else { action.SetFill(Pal.Cyan); actIcon.gameObject.SetActive(false); action.label.rectTransform.anchoredPosition = Vector2.zero; action.labelColor = Pal.Ink; action.SetInteractable(true); }
        }

        void OnAction()
        {
            var s = GS.Ships[idx];
            if (!GS.Owns(s.id))
            {
                if (GS.Spend(s.price)) { GS.Own(s.id); GS.ShipIndex = idx; Paint(); Tw.ScaleFrom(shipRt, .7f, 1f, .6f, Ease.OutElastic); nav.Toast(s.name + " unlocked & equipped"); }
                else { Tw.Shake(action.rectTransform); nav.Toast("Need " + (s.price - GS.Coins).ToString("N0") + " more coins", false); }
            }
            else { GS.ShipIndex = idx; SetAction(); Tw.ScaleFrom(shipRt, .85f, 1f, .45f, Ease.OutBack); nav.Toast(s.name + " equipped"); }
        }

        void PaintBoosts(bool animate)
        {
            for (int i = 0; i < boostChips.Count; i++)
            {
                bool sel = i == GS.BoostIndex; int cnt = GS.BoostCount(GS.Boosts[i].id);
                boostRings[i].color = sel ? Pal.Cyan : Pal.Mute; boostRings[i].sprite = sel ? Sprites.Ring(44, 8) : Sprites.Ring(44, 3);
                boostChecks[i].gameObject.SetActive(sel); boostChips[i].bg.color = sel ? Pal.PanelHi : Pal.Panel;
                boostCount[i].text = cnt > 0 ? "x" + cnt + " owned" : "none · buy in Shop"; boostCount[i].color = cnt > 0 ? Pal.Dim : Pal.Mute;
                if (sel && animate) Tw.ScaleFrom(boostChecks[i].transform, 0f, 1f, .35f, Ease.OutBack);
            }
        }

        public override void OnShow(bool returning)
        {
            idx = GS.ShipIndex; Tw.Kill((this, "bob")); var r = Rest.Of(shipRt);
            Paint(); PaintBoosts(false);
            Tw.Loop(k => { if (shipRt.localScale.x > .95f) shipRt.anchoredPosition = new Vector2(r.pos.x, r.pos.y + (k - .5f) * 24f); }, 2f, Ease.InOutSine, true, (this, "bob"));
            if (returning) return;
            Intro.Rise(stage, 0, 60, .06f, .5f); Intro.Rise(descT.rectTransform, 1, 40, .06f, .5f); Intro.Rise(Marks["stats"], 2, 50, .06f, .5f);
            for (int i = 0; i < boostChips.Count; i++) Intro.Rise(boostChips[i].rectTransform, 3 + i, 60, .06f, .5f);
            Intro.Rise(action.rectTransform, 6, 100, .06f, .5f);
        }
    }

    // =====================================================================  6. SHOP
    public class ShopScreen : MenuScreen
    {
        string[] tabs = { "Boosts", "Skins", "Coins" }; int tab; RectTransform grid; USegment seg; readonly List<RectTransform> cards = new List<RectTransform>();

        public override void Build()
        {
            UI.Header(this, "SHOP");
            seg = USegment.Create(Root, "shop.tabs", UI.TC, new Vector2(0, -200), new Vector2(960, 120), tabs, 0, i => SwitchTab(i)); Mark("tabs", seg.GetComponent<RectTransform>());
            grid = UI.Rect(Root, "grid"); UI.Place(grid, UI.TC, new Vector2(0, -370), new Vector2(1000, 1200)); Mark("grid", grid);
            var note = UI.Text(Root, "note", "Demo store · no real payments", 32, Pal.Mute, UI.BC, new Vector2(0, 40), new Vector2(800, 50), TextAlignmentOptions.Center, false); Mark("note", note.rectTransform);
        }

        void SwitchTab(int i)
        {
            if (i == tab) return; int dir = i > tab ? 1 : -1; tab = i;
            var cg = UI.Group(grid);
            Tw.Move(grid, new Vector2(-dir * 180, -370), .14f, Ease.InQuad, 0f, () =>
            {
                Rebuild(); grid.anchoredPosition = new Vector2(dir * 180, -370); Tw.Move(grid, new Vector2(0, -370), .34f, Ease.OutCubic);
                Tw.Alpha(cg, 1, .25f); for (int c = 0; c < cards.Count; c++) Tw.ScaleFrom(cards[c], .92f, 1f, .4f, Ease.OutBack, c * .04f);
            });
            Tw.Alpha(cg, 0, .14f, Ease.InQuad);
        }

        void Rebuild()
        {
            for (int c = grid.childCount - 1; c >= 0; c--) Object.Destroy(grid.GetChild(c).gameObject);
            cards.Clear(); int n = 0;
            foreach (var it in GS.ShopItems)
            {
                if (it.tab != tabs[tab]) continue;
                int col = n % 2, row = n / 2; var item = it; n++;
                var bg = UI.Rounded(grid, "item_" + it.id, UI.TC, new Vector2((col - .5f) * 490, -row * 590), new Vector2(470, 560), Pal.Panel, 52); UI.Outline(bg.transform, "ring", new Vector2(470, 560), Pal.Mute, 52, 3);
                var disc = UI.Box(bg.transform, "disc", UI.TC, new Vector2(0, -34), new Vector2(210, 210), Pal.PanelHi, Sprites.Circle());
                UI.Icon(disc.transform, "icon", it.icon, UI.C, Vector2.zero, 120, it.color);
                UI.Text(bg.transform, "name", it.name, 44, Pal.White, UI.TC, new Vector2(0, -268), new Vector2(440, 56));
                var desc = UI.Text(bg.transform, "desc", it.desc, 34, Pal.Dim, UI.TC, new Vector2(0, -326), new Vector2(440, 48), TextAlignmentOptions.Center, false);
                bool coins = it.tab == "Coins"; bool oneTime = it.tab == "Skins"; bool owned = oneTime && GS.Owns(it.id);
                if (it.tab == "Boosts") desc.text = it.desc + " · own " + GS.BoostCount(BoostId(it.id));
                var pill = UI.Button(bg.transform, "buy." + it.id, UI.BC, new Vector2(0, 30), new Vector2(390, 110), Pal.Amber, Pal.Ink, coins ? GS.CoinPackPrice(it.id) : it.price.ToString("N0"), 46, coins ? null : "coin", null, 55);
                if (coins) pill.SetFill(it.id == "c_small" ? Pal.Cyan : Pal.White);
                if (owned) { pill.SetLabel("OWNED"); pill.SetFill(Pal.PanelLo); pill.label.color = Pal.Cyan; pill.labelColor = Pal.Cyan; if (pill.icon) { pill.icon.sprite = Sprites.Icon("check"); pill.icon.color = Pal.Cyan; } pill.interactable = false; pill.bg.color = Pal.PanelLo; }
                pill.onClick = () => Buy(item, pill, bg.rectTransform, desc);
                pill.onDenied = () => { };
                cards.Add(bg.rectTransform);
            }
        }

        static string BoostId(string shopId) => shopId == "b_shield" ? "shield" : shopId == "b_magnet" ? "magnet" : shopId == "b_x2" ? "x2" : "life";

        void Buy(ShopItem it, Tappable pill, RectTransform card, TextMeshProUGUI desc)
        {
            if (it.tab == "Coins")
            {
                GS.AddCoins(GS.CoinPackAmount(it.id)); Tw.ScaleFrom(card, .94f, 1f, .5f, Ease.OutBack); nav.Toast("+" + GS.CoinPackAmount(it.id).ToString("N0") + " coins"); return;
            }
            if (!GS.Spend(it.price))
            {
                Tw.Shake(pill.rectTransform); Tw.Shake(card, 14);
                nav.Toast("Need " + (it.price - GS.Coins).ToString("N0") + " more coins", false); return;
            }
            Tw.ScaleFrom(card, .92f, 1f, .6f, Ease.OutElastic);
            if (it.tab == "Boosts") { GS.AddBoost(BoostId(it.id), 3); desc.text = it.desc + " · own " + GS.BoostCount(BoostId(it.id)); nav.Toast(it.name + " added"); }
            else
            {
                GS.Own(it.id); pill.SetLabel("OWNED"); pill.SetFill(Pal.PanelLo); pill.label.color = Pal.Cyan; pill.labelColor = Pal.Cyan; pill.bg.color = Pal.PanelLo; pill.interactable = false;
                if (pill.icon) { pill.icon.sprite = Sprites.Icon("check"); pill.icon.color = Pal.Cyan; }
                nav.Toast(it.name + " purchased");
            }
        }

        public override void OnShow(bool returning)
        {
            Rebuild(); UI.Group(grid).alpha = 1; grid.anchoredPosition = new Vector2(0, -370);
            if (returning) return;
            Intro.Rise(Marks["tabs"], 0, 40, .06f, .45f);
            for (int c = 0; c < cards.Count; c++) Intro.Pop(cards[c], 1 + c, .06f, .5f);
        }
    }

    // =====================================================================  7. SETTINGS
    public class SettingsScreen : MenuScreen
    {
        readonly List<RectTransform> sections = new List<RectTransform>(); Tappable reset; float resetArm; UToggle shakeT; USlider sens; USegment qual;
        readonly USlider[] volume = new USlider[4];

        public override void Build()
        {
            UI.Header(this, "SETTINGS", true, false);
            // ---- audio
            var a = Section("AUDIO", -200, 4, 120);
            string[] labels = { "Master", "Music", "SFX", "UI" };
            string[] icons = { "bars", "sparkle", "bolt", "gear" };
            for (int i = 0; i < volume.Length; i++)
            {
                AudioBus bus = (AudioBus)i;
                Row(a, i, icons[i], labels[i], 120);
                volume[i] = USlider.Create(a, "settings.volume." + labels[i], UI.TR,
                    new Vector2(-150, Cy(i, 120) + 60), 330, GameAudio.Volume(bus),
                    v => GameAudio.Instance.SetVolume(bus, v), ValueLabel(a, i, 120));
            }
            // ---- gameplay
            var g = Section("GAMEPLAY", -836, 3, 120);
            Row(g, 0, "bars", "Sensitivity", 120); sens = USlider.Create(g, "settings.sens", UI.TR, new Vector2(-150, Cy(0) + 60), 330, GS.Sensitivity, v => GS.Sensitivity = v, ValueLabel(g, 0));
            Row(g, 1, "bolt", "Screen shake", 120); shakeT = UToggle.Create(g, "settings.shake", UI.TR, new Vector2(-30, Cy(1, 120) + 50), GS.Shake, v => GS.Shake = v);
            Row(g, 2, "gear", "Graphics", 120); qual = USegment.Create(g, "settings.quality", UI.TR, new Vector2(-30, Cy(2, 120) + 48), new Vector2(470, 96), new[] { "Low", "Med", "High" }, GS.Quality, i => GS.Quality = i, 38);
            // ---- data
            float y3 = -1352;
            var d = UI.Rounded(Root, "section_data", UI.TC, new Vector2(0, y3), new Vector2(960, 330), Pal.Panel, 48); UI.Outline(d.transform, "ring", new Vector2(960, 330), Pal.Mute, 48, 3); sections.Add(d.rectTransform);
            reset = UI.Button(d.transform, "settings.reset", UI.TC, new Vector2(0, -36), new Vector2(880, 120), new Color(0, 0, 0, 0), Pal.Pink, "RESET PROGRESS", 44, "restart", Pal.Pink);
            reset.onClick = OnReset; Mark("reset", reset.rectTransform);
            var cr = UI.Button(d.transform, "settings.credits", UI.BC, new Vector2(0, 36), new Vector2(880, 120), Pal.PanelHi, Pal.White, "CREDITS & THANKS", 44, "info", Pal.Mute);
            cr.onClick = () => nav.Push(Scr.Credits); Mark("credits", cr.rectTransform);
            Mark("audio", a); Mark("gameplay", g);
        }

        static float Cy(int i, int step = 140) => -130 - step * i;
        TextMeshProUGUI ValueLabel(RectTransform card, int row, int step = 140) => UI.Text(card, "val" + row, "", 38, Pal.White, UI.TR, new Vector2(-24, Cy(row, step) + 30), new Vector2(120, 60), TextAlignmentOptions.MidlineRight);

        RectTransform Section(string title, float y, int rows, int step)
        {
            float h = rows * step + 130; var card = UI.Rounded(Root, "section_" + title, UI.TC, new Vector2(0, y), new Vector2(960, h), Pal.Panel, 48); UI.Outline(card.transform, "ring", new Vector2(960, h), Pal.Mute, 48, 3);
            UI.Text(card.transform, "title", title, 34, Pal.Amber, UI.TL, new Vector2(40, -26), new Vector2(500, 50), TextAlignmentOptions.MidlineLeft, true, 6);
            sections.Add(card.rectTransform); return card.rectTransform;
        }

        void Row(RectTransform card, int i, string icon, string label, int step)
        {
            float c = Cy(i, step);
            UI.Icon(card, "i" + i, icon, UI.TL, new Vector2(40, c + 28), 56, Pal.Dim);
            UI.Text(card, "l" + i, label, 42, Pal.White, UI.TL, new Vector2(116, c + 35), new Vector2(360, 70), TextAlignmentOptions.MidlineLeft, false);
        }

        void OnReset()
        {
            if (resetArm > 0)
            {   // second tap within 3s confirms
                resetArm = 0; GS.ResetAll(); nav.Toast("Progress reset"); reset.SetLabel("RESET PROGRESS"); reset.label.color = Pal.Pink; reset.bg.color = new Color(0, 0, 0, 0); shakeT.Set(GS.Shake, true); sens.Set(GS.Sensitivity, false); qual.Select(GS.Quality, true, false);
                return;
            }
            resetArm = 3f; reset.SetLabel("TAP AGAIN TO CONFIRM"); reset.bg.color = Pal.Pink; reset.label.color = Pal.Ink; if (reset.icon) reset.icon.color = Pal.Ink;             Tw.Shake(reset.rectTransform, 12, .3f);
            Tw.To(k => { }, 3f, Ease.Linear, 0f, () => { if (resetArm > 0) { resetArm = 0; reset.SetLabel("RESET PROGRESS"); reset.label.color = Pal.Pink; reset.bg.color = new Color(0, 0, 0, 0); if (reset.icon) reset.icon.color = Pal.Pink; } }, (this, "armed"));
        }

        public override void OnShow(bool returning)
        {
            sens.Set(GS.Sensitivity, false);
            for (int i = 0; i < volume.Length; i++) volume[i].Set(GameAudio.Volume((AudioBus)i), false);
            if (returning) return;
            for (int i = 0; i < sections.Count; i++) Intro.Rise(sections[i], i, 70, .1f, .5f);
        }

        public override void OnHide() => PlayerPrefs.Save();
    }
}
