using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using S = Lab07.GS.Session;

namespace Lab07
{
    public class DragInput : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Action<PointerEventData> onPointer;
        public void OnPointerDown(PointerEventData e) { onPointer?.Invoke(e); }
        public void OnDrag(PointerEventData e) { onPointer?.Invoke(e); }
    }

    // =====================================================================  GAMEPLAY HUD
    public class HudScreen : MenuScreen
    {
        class Fall { public RectTransform rt; public bool coin; }
        public static bool AutoPlay;
        public static float DurationOverride;

        RectTransform field, ship, fillRt, shipHolder;
        Image shipImg, flash, shield;
        TextMeshProUGUI scoreT, levelT, countT, boostT;
        readonly Image[] hearts = new Image[3];
        readonly List<Fall> objs = new List<Fall>();
        FlightSession session;
        GameObject feedback;
        Tappable pauseBtn;
        float spawnT, targetX, shipX, countdown;
        int lastCount;
        public int Score => session ? session.Score : 0;
        public int CoinsGot => session ? session.Coins : 0;

        public override void Build()
        {
            field = UI.Stretch(UI.Rect(Root, "field")); var catcher = UI.Box(field, "input", UI.C, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0)); UI.Stretch(catcher.rectTransform); catcher.raycastTarget = true;
            catcher.gameObject.AddComponent<DragInput>().onPointer = e =>
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(field, e.position, e.pressEventCamera, out var lp); targetX = Mathf.Clamp(lp.x, -460, 460);
            };
            shipHolder = UI.Place(UI.Rect(field, "shipHolder"), UI.BC, new Vector2(0, 330), new Vector2(170, 170));
            shield = UI.Box(shipHolder, "shield", UI.C, Vector2.zero, new Vector2(250, 250), Pal.A(Pal.Cyan, .4f), Sprites.Circle());
            shipImg = UI.Icon(shipHolder, "ship", "ship1", UI.C, Vector2.zero, 170, Pal.Cyan); ship = shipHolder; Mark("ship", ship);
            flash = UI.Box(Root, "flash", UI.C, Vector2.zero, Vector2.zero, Pal.A(Pal.Pink, 0)); UI.Stretch(flash.rectTransform); flash.raycastTarget = false;

            // top bar: pause (left), score (centre), lives (right) - grouped, aligned to the same baseline
            pauseBtn = UI.IconButton(Root, "hud.pause", "pause", UI.TL, new Vector2(40, -34), 132, Pal.PanelHi, Pal.White, 76, Pal.Mute);
            pauseBtn.onClick = Pause; Mark("pause", pauseBtn.rectTransform);
            UI.Text(Root, "scoreLabel", "SCORE", 30, Pal.Dim, UI.TC, new Vector2(0, -26), new Vector2(300, 40), TextAlignmentOptions.Center, true, 6);
            scoreT = UI.Text(Root, "score", "0", 92, Pal.White, UI.TC, new Vector2(0, -60), new Vector2(520, 110)); Mark("score", scoreT.rectTransform);
            for (int i = 0; i < 3; i++) hearts[i] = UI.Icon(Root, "heart" + i, "heart", UI.TR, new Vector2(-40 - (2 - i) * 76, -52), 64, Pal.Pink);
            Mark("hearts", UI.Place(UI.Rect(Root, "heartsMark"), UI.TR, new Vector2(-30, -42), new Vector2(240, 84)));
            var track = UI.Rounded(Root, "timeTrack", UI.TC, new Vector2(0, -200), new Vector2(820, 26), Pal.PanelLo, 13); UI.Outline(track.transform, "ring", new Vector2(820, 26), Pal.Mute, 13, 3);
            fillRt = UI.Rounded(track.transform, "fill", UI.ML, Vector2.zero, new Vector2(820, 26), Pal.Cyan, 13).rectTransform; Mark("time", track.rectTransform);
            levelT = UI.Text(Root, "level", "", 36, Pal.Dim, UI.TC, new Vector2(0, -240), new Vector2(900, 50), TextAlignmentOptions.Center, false); Mark("level", levelT.rectTransform);
            boostT = UI.Text(Root, "boost", "", 36, Pal.Cyan, UI.BC, new Vector2(0, 60), new Vector2(700, 60), TextAlignmentOptions.Center, true, 4);
            countT = UI.Text(Root, "count", "", 260, Pal.White, UI.C, new Vector2(0, 160), new Vector2(900, 320)); countT.gameObject.SetActive(false); Mark("count", countT.rectTransform);
            Modal = false;
        }

        public void Pause()
        {
            if (session && (session.Running || countdown > 0)) nav.Push(Scr.Pause);
        }

        public override void OnBackKey() => Pause();

        public override void OnShow(bool returning)
        {
            if (returning) return;
            foreach (var item in objs)
                if (item.rt) UnityEngine.Object.Destroy(item.rt.gameObject);
            objs.Clear();
            if (feedback)
            {
                feedback.SetActive(false);
                UnityEngine.Object.Destroy(feedback);
            }
            if (session != null) session.Finished -= OnFinished;
            session = App.I.flight.Session;
            if (!session) return;

            AttachFeedback();
            session.Finished += OnFinished;
            session.Prepare(DurationOverride);
            var equipped = GS.Ships[GS.ShipIndex];
            shipImg.sprite = Sprites.Icon(equipped.icon);
            shipImg.color = equipped.color;
            shield.gameObject.SetActive(session.Shield);
            boostT.text = session.BoostLabel;
            foreach (var heart in hearts) heart.color = Pal.Pink;
            scoreT.text = "0";
            levelT.text = GS.Session.Title;
            fillRt.sizeDelta = new Vector2(820, 26);
            fillRt.parent.gameObject.SetActive(session.Duration > 0);
            targetX = shipX = 0;
            spawnT = .18f;
            ship.anchoredPosition = new Vector2(0, 330);
            Tw.MoveFrom(ship, new Vector2(0, -200), .6f, Ease.OutCubic);
            countdown = AutoPlay ? 1.2f : 2.7f;
            lastCount = 0;
        }

        void AttachFeedback()
        {
            feedback = new GameObject("Flight feedback");
            feedback.SetActive(false);
            feedback.transform.SetParent(Root, false);
            var hud = feedback.AddComponent<FlightHudFeedback>();
            hud.session = session;
            hud.score = scoreT;
            hud.boost = boostT;
            hud.hearts = hearts;
            hud.shield = shield;

            feedback.AddComponent<FlightAudioFeedback>().session = session;
            var visuals = feedback.AddComponent<FlightVisualFeedback>();
            visuals.session = session;
            visuals.field = field;
            visuals.ship = ship;
            visuals.flash = flash;
            feedback.SetActive(true);
        }

        void OnFinished(bool won) => nav.Replace(Scr.Results);

        void UpdateCountdown(float dt)
        {
            countdown -= dt;
            int count = Mathf.Clamp(Mathf.CeilToInt(countdown / (AutoPlay ? .4f : .9f)), 0, 3);
            if (count == lastCount) return;
            lastCount = count;
            countT.gameObject.SetActive(true);
            countT.text = count > 0 ? count.ToString() : "GO!";
            countT.color = count > 0 ? Pal.White : Pal.Cyan;
            Tw.ScaleFrom(countT.transform, count > 0 ? 1.5f : .5f, 1f, .32f, Ease.OutBack);
            Tw.AlphaFrom(UI.Group(countT.rectTransform), 0, 1, .12f);
            if (count > 0) return;
            Tw.Alpha(UI.Group(countT.rectTransform), 0, .3f, Ease.InQuad, .28f, () => countT.gameObject.SetActive(false));
            session.Launch();
        }

        public void Tick(float dt)
        {
            if (!session || dt <= 0) return;
            if (countdown > 0)
            {
                UpdateCountdown(dt);
                return;
            }

            if (!session.Running) return;
            session.Advance(dt);
            if (session.Duration > 0)
                fillRt.sizeDelta = new Vector2(Mathf.Max(26, 820f * session.Remaining / session.Duration), 26);
            if (!session.Running) return;
            float keyboard = Input.GetAxisRaw("Horizontal");
            targetX = Mathf.Clamp(targetX + keyboard * 650f * dt, -460f, 460f);
            int lv = GS.Session.Level; float speed = 560f + lv * 45f; spawnT -= dt;
            if (spawnT <= 0)
            {
                spawnT = Mathf.Max(.32f, .62f - lv * .02f); bool coin = UnityEngine.Random.value < .62f;
                var im = UI.Icon(field, coin ? "coin" : "rock", coin ? "coin" : "rock", UI.TC, new Vector2(UnityEngine.Random.Range(-440f, 440f), 120), coin ? 96 : 118, coin ? Pal.Amber : Pal.Pink);
                if (!coin) im.rectTransform.localEulerAngles = new Vector3(0, 0, UnityEngine.Random.Range(0, 360));
                objs.Add(new Fall { rt = im.rectTransform, coin = coin });
                if (coin) session.OfferCoin();
            }
            if (AutoPlay) AutoSteer();
            float follow = Mathf.Lerp(6f, 18f, GS.Sensitivity);
            shipX += (targetX - shipX) * Mathf.Min(1f, dt * follow); ship.anchoredPosition = new Vector2(shipX, 330); ship.localRotation = Quaternion.Euler(0, 0, -(targetX - shipX) * .06f);
            float fh = field.rect.height; float shipY = -(fh - 330 - 85);
            for (int i = objs.Count - 1; i >= 0; i--)
            {
                var o = objs[i]; var p = o.rt.anchoredPosition; p.y -= speed * dt;
                if (o.coin && session.Magnet) { float dx = shipX - p.x, dy = shipY - p.y; if (new Vector2(dx, dy).magnitude < 420) { p.x += dx * dt * 6f; p.y += dy * dt * 3f; } }
                o.rt.anchoredPosition = p; if (!o.coin) o.rt.Rotate(0, 0, 90 * dt);
                bool hit = Mathf.Abs(p.x - shipX) < 100 && Mathf.Abs(p.y - shipY) < 100;
                if (hit)
                {
                    if (o.coin) session.Collect(p);
                    else session.Hit(p);
                    UnityEngine.Object.Destroy(o.rt.gameObject);
                    objs.RemoveAt(i);
                    if (!session.Running) break;
                    continue;
                }
                if (p.y < -fh - 120) { UnityEngine.Object.Destroy(o.rt.gameObject); objs.RemoveAt(i); }
            }
        }

        void AutoSteer()
        {
            float fh = field.rect.height; float shipY = -(fh - 330 - 85); float best = 99999; float tx = 0; bool found = false;
            foreach (var o in objs) { if (!o.coin) continue; var p = o.rt.anchoredPosition; float d = p.y - shipY; if (d < -60) continue; if (Mathf.Abs(d) < best) { best = Mathf.Abs(d); tx = p.x; found = true; } }
            if (!found) tx = Mathf.Sin(Time.time * .8f) * 300f;
            foreach (var o in objs)
            {
                if (o.coin) continue; var p = o.rt.anchoredPosition; float d = p.y - shipY;
                if (d > -50 && d < 520 && Mathf.Abs(p.x - tx) < 160) tx = p.x + (p.x > shipX ? -190f : 190f) * (Mathf.Abs(p.x - shipX) < 30 ? (shipX > 0 ? -1 : 1) : 1);
            }
            targetX = Mathf.Clamp(tx, -440, 440);
        }

    }

    // =====================================================================  8. PAUSE (modal)
    public class PauseScreen : MenuScreen
    {
        Image dim; RectTransform card; Tappable resume, restart, settings, quit; float quitArm; TextMeshProUGUI sub; TextMeshProUGUI scoreChip, coinChip; readonly List<RectTransform> items = new List<RectTransform>();

        public override void Build()
        {
            Modal = true;
            dim = UI.Box(Root, "dim", UI.C, Vector2.zero, Vector2.zero, Pal.A(Pal.Ink, .76f)); UI.Stretch(dim.rectTransform); dim.raycastTarget = true; Mark("dim", dim.rectTransform);
            card = UI.Rounded(Root, "card", UI.C, new Vector2(0, 0), new Vector2(880, 1160), Pal.Panel, 64).rectTransform; UI.Outline(card, "ring", new Vector2(880, 1160), Pal.Cyan, 64, 4); Mark("card", card);
            var t = UI.Text(card, "title", "PAUSED", 96, Pal.White, UI.TC, new Vector2(0, -54), new Vector2(800, 120), TextAlignmentOptions.Center, true, 8); Mark("title", t.rectTransform);
            sub = UI.Text(card, "sub", "", 40, Pal.Dim, UI.TC, new Vector2(0, -176), new Vector2(800, 56), TextAlignmentOptions.Center, false);

            var c1 = UI.Rounded(card, "chipScore", UI.TC, new Vector2(-205, -260), new Vector2(390, 120), Pal.PanelLo, 36); UI.Text(c1.transform, "l", "SCORE", 28, Pal.Dim, UI.TL, new Vector2(24, -12), new Vector2(200, 36), TextAlignmentOptions.MidlineLeft, true, 4);
            scoreChip = UI.Text(c1.transform, "v", "0", 56, Pal.White, UI.BL, new Vector2(24, 8), new Vector2(340, 70), TextAlignmentOptions.MidlineLeft);
            var c2 = UI.Rounded(card, "chipCoin", UI.TC, new Vector2(205, -260), new Vector2(390, 120), Pal.PanelLo, 36); UI.Text(c2.transform, "l", "COINS", 28, Pal.Dim, UI.TL, new Vector2(24, -12), new Vector2(200, 36), TextAlignmentOptions.MidlineLeft, true, 4);
            coinChip = UI.Text(c2.transform, "v", "0", 56, Pal.Amber, UI.BL, new Vector2(24, 8), new Vector2(340, 70), TextAlignmentOptions.MidlineLeft);
            UI.Icon(c2.transform, "ci", "coin", UI.MR, new Vector2(-20, 0), 60, Pal.Amber);
            items.Add(c1.rectTransform); items.Add(c2.rectTransform);

            resume = UI.Button(card, "pause.resume", UI.TC, new Vector2(0, -420), new Vector2(720, 170), Pal.Cyan, Pal.Ink, "RESUME", 64, "play"); resume.onClick = () => nav.Back(); Mark("resume", resume.rectTransform);
            restart = UI.Button(card, "pause.restart", UI.TC, new Vector2(0, -620), new Vector2(720, 140), Pal.PanelHi, Pal.White, "RESTART", 50, "restart", Pal.Mute);
            restart.onClick = () => App.I.flight.Begin(fromPause: true);
            settings = UI.Button(card, "pause.settings", UI.TC, new Vector2(0, -790), new Vector2(720, 140), Pal.PanelHi, Pal.White, "SETTINGS", 50, "gear", Pal.Mute); settings.onClick = () => nav.Push(Scr.Settings);
            quit = UI.Button(card, "pause.quit", UI.TC, new Vector2(0, -980), new Vector2(720, 130), new Color(0, 0, 0, 0), Pal.Pink, "QUIT TO MENU", 44, "home", Pal.Pink); quit.onClick = OnQuit; Mark("quit", quit.rectTransform);
            items.Add(resume.rectTransform); items.Add(restart.rectTransform); items.Add(settings.rectTransform); items.Add(quit.rectTransform);
        }

        void OnQuit()
        {
            if (quitArm > 0) { quitArm = 0; Time.timeScale = 1f; AudioListener.pause = false; nav.GoRoot(Scr.Main); return; }
            quitArm = 3f; quit.SetLabel("TAP AGAIN - PROGRESS LOST"); quit.label.fontSize = 36; quit.bg.color = Pal.Pink; quit.label.color = Pal.Ink; if (quit.icon) quit.icon.color = Pal.Ink; Tw.Shake(quit.rectTransform, 12, .3f);             Tw.To(k => { }, 3f, Ease.Linear, 0f, ResetQuit, (this, "quitArm"));
        }
        void ResetQuit() { quitArm = 0; quit.SetLabel("QUIT TO MENU"); quit.label.fontSize = 44; quit.bg.color = new Color(0, 0, 0, 0); quit.label.color = Pal.Pink; if (quit.icon) quit.icon.color = Pal.Pink; }

        public override void OnShow(bool returning)
        {
            Time.timeScale = 0f; AudioListener.pause = true;
            if (!returning) GameAudio.UI(UiCue.Pause);
            ResetQuit(); Tw.Kill((this, "quitArm"));
            var hud = nav.Peek(Scr.Hud) as HudScreen; sub.text = GS.Session.Title; scoreChip.text = hud != null ? hud.Score.ToString("N0") : "0"; coinChip.text = hud != null ? "+" + hud.CoinsGot : "+0";
            // modal motion: dim fades (OutQuad), card scales in with overshoot (OutBack) - signals "layer on top"
            Tw.AlphaFrom(UI.Group(Root), 0, 1, .12f);
            Tw.To(k => dim.color = Pal.A(Pal.Ink, .76f * k), .22f, Ease.OutQuad, 0f, null, (this, "dim"));
            Tw.ScaleFrom(card, .78f, 1f, .4f, Ease.OutBack); Tw.AlphaFrom(UI.Group(card), 0, 1, .2f);
            if (!returning) for (int i = 0; i < items.Count; i++) Intro.Rise(items[i], i, 30, .04f, .35f);
        }

        public override void PlayOut(Action done)
        {
            Tw.To(k => dim.color = Pal.A(Pal.Ink, .76f * (1 - k)), .2f, Ease.InQuad, 0f, null, (this, "dim"));
            Tw.Scale(card, .88f, .2f, Ease.InBack); Tw.Alpha(UI.Group(card), 0, .18f, Ease.InQuad, 0f, () => { Time.timeScale = 1f; AudioListener.pause = false; GameAudio.UI(UiCue.Resume); done?.Invoke(); });
        }
    }

    // =====================================================================  9. RESULTS
    public class ResultsScreen : MenuScreen
    {
        TextMeshProUGUI banner, scoreT, coinsT, accT, bestT; readonly Image[] stars = new Image[3]; readonly Image[] starBg = new Image[3]; RectTransform badge, bannerRt, scoreBlock, statsCard; Tappable primary, left, right;
        Image bannerBg; readonly List<RectTransform> rise = new List<RectTransform>();

        public override void Build()
        {
            bannerBg = UI.Rounded(Root, "banner", UI.TC, new Vector2(0, -60), new Vector2(920, 190), Pal.Cyan, 60); bannerRt = bannerBg.rectTransform; Rest.Of(bannerRt); Mark("banner", bannerRt);
            banner = UI.Text(bannerRt, "t", "SECTOR CLEAR", 76, Pal.Ink, UI.C, Vector2.zero, new Vector2(880, 120), TextAlignmentOptions.Center, true, 4);
            float[] sz = { 220, 290, 220 }; float[] sx = { -250, 0, 250 }; float[] sy = { -330, -290, -330 };
            for (int i = 0; i < 3; i++)
            {
                starBg[i] = UI.Icon(Root, "starO" + i, "star_o", UI.TC, new Vector2(sx[i], sy[i] - 0), sz[i], Pal.Mute);
                stars[i] = UI.Icon(Root, "star" + i, "star", UI.TC, new Vector2(sx[i], sy[i]), sz[i], Pal.Amber);
            }
            Mark("stars", UI.Place(UI.Rect(Root, "starsMark"), UI.TC, new Vector2(0, -290), new Vector2(760, 320)));
            scoreBlock = UI.Rect(Root, "scoreBlock"); UI.Place(scoreBlock, UI.TC, new Vector2(0, -640), new Vector2(900, 240)); Mark("score", scoreBlock);
            UI.Text(scoreBlock, "lbl", "SCORE", 38, Pal.Dim, UI.TC, new Vector2(0, 0), new Vector2(500, 50), TextAlignmentOptions.Center, true, 8);
            scoreT = UI.Text(scoreBlock, "val", "0", 150, Pal.White, UI.TC, new Vector2(0, -50), new Vector2(900, 180));
            badge = UI.Rounded(scoreBlock, "badge", UI.TR, new Vector2(-10, -30), new Vector2(240, 84), Pal.Amber, 42).rectTransform; UI.Text(badge, "t", "NEW BEST!", 36, Pal.Ink, UI.C, Vector2.zero, new Vector2(240, 80)); Mark("badge", badge);

            statsCard = UI.Rounded(Root, "stats", UI.TC, new Vector2(0, -920), new Vector2(920, 290), Pal.Panel, 48).rectTransform; UI.Outline(statsCard, "ring", new Vector2(920, 290), Pal.Mute, 48, 3); Mark("stats", statsCard);
            coinsT = StatRow(statsCard, 0, "coin", "Coins collected", Pal.Amber); accT = StatRow(statsCard, 1, "bolt", "Accuracy", Pal.Cyan); bestT = StatRow(statsCard, 2, "trophy", "Best score", Pal.White);

            primary = UI.Button(Root, "results.primary", UI.BC, new Vector2(0, 400), new Vector2(880, 180), Pal.Cyan, Pal.Ink, "NEXT LEVEL", 64, "next"); Mark("primary", primary.rectTransform);
            left = UI.Button(Root, "results.left", UI.BC, new Vector2(-225, 190), new Vector2(430, 150), Pal.PanelHi, Pal.White, "RETRY", 48, "restart", Pal.Mute);
            right = UI.Button(Root, "results.home", UI.BC, new Vector2(225, 190), new Vector2(430, 150), Pal.PanelHi, Pal.White, "HOME", 48, "home", Pal.Mute); Mark("secondary", UI.Place(UI.Rect(Root, "secMark"), UI.BC, new Vector2(0, 190), new Vector2(900, 150)));
            right.onClick = () => nav.GoRoot(Scr.Main);
            Back = right;
            rise.Add(scoreBlock); rise.Add(statsCard);
        }

        TextMeshProUGUI StatRow(RectTransform card, int i, string icon, string label, Color c)
        {
            float y = -30 - i * 84;
            UI.Icon(card, "i" + i, icon, UI.TL, new Vector2(40, y - 8), 56, c);
            UI.Text(card, "l" + i, label, 40, Pal.Dim, UI.TL, new Vector2(116, y - 6), new Vector2(460, 60), TextAlignmentOptions.MidlineLeft, false);
            return UI.Text(card, "v" + i, "", 46, Pal.White, UI.TR, new Vector2(-40, y - 6), new Vector2(300, 60), TextAlignmentOptions.MidlineRight);
        }

        public override void OnShow(bool returning)
        {
            bool win = S.Win; bool campaign = S.Mode == 0;
            banner.text = win ? (campaign ? "SECTOR CLEAR" : "RUN COMPLETE") : "SHIP LOST"; bannerBg.color = win ? Pal.Cyan : Pal.Pink;
            coinsT.text = "+" + S.CoinsEarned; accT.text = Mathf.RoundToInt(S.Accuracy * 100) + "%"; bestT.text = Mathf.Max(GS.BestScore, S.Score).ToString("N0");
            scoreT.text = "0"; badge.gameObject.SetActive(S.NewBest && S.Score > 0);
            // actions
            bool hasNext = win && campaign && S.Level < GS.Levels;
            primary.SetLabel(win ? (hasNext ? "NEXT LEVEL" : "PLAY AGAIN") : "TRY AGAIN");
            primary.icon.sprite = Sprites.Icon(win && hasNext ? "next" : "restart");
            float pw = primary.label.GetPreferredValues(primary.label.text).x; primary.label.rectTransform.anchoredPosition = new Vector2(36, 0); primary.icon.rectTransform.anchoredPosition = new Vector2(-pw / 2 - 20, 0);
            primary.onClick = () => { if (hasNext) GS.Session.Level++; App.I.flight.Begin(replace: true); };
            if (win) { left.SetLabel("RETRY"); left.icon.sprite = Sprites.Icon("restart"); left.onClick = () => App.I.flight.Begin(replace: true); }
            else { left.SetLabel("SHOP"); left.icon.sprite = Sprites.Icon("bag"); left.onClick = () => nav.Push(Scr.Shop); }
            float lw = left.label.GetPreferredValues(left.label.text).x; left.label.rectTransform.anchoredPosition = new Vector2(30, 0); left.icon.rectTransform.anchoredPosition = new Vector2(-lw / 2 - 12, 0);

                        // 1) banner drops in with a bounce (state: success/fail), 2) stars pop one by one (OutElastic), 3) score counts up (OutQuart), 4) details rise
            var br = Rest.Of(bannerRt); Tw.Kill((bannerRt, "pos")); bannerRt.anchoredPosition = br.pos + new Vector2(0, 260); Tw.Move(bannerRt, br.pos, .7f, Ease.OutBounce); Tw.AlphaFrom(UI.Group(bannerRt), 0, 1, .15f);
            for (int i = 0; i < 3; i++)
            {
                bool earned = i < S.Stars; stars[i].gameObject.SetActive(earned); stars[i].color = Pal.Amber; starBg[i].color = Pal.Mute; var rt = stars[i].rectTransform; int ii = i;
                Tw.AlphaFrom(UI.Group(starBg[i].rectTransform), 0, 1, .25f, Ease.OutQuad, .35f + i * .1f);
                if (earned) { rt.localScale = Vector3.zero; Tw.To(k => { }, .01f, Ease.Linear, .6f + i * .32f, () => { Tw.ScaleFrom(rt, 0f, 1f, .7f, Ease.OutElastic); }); }
            }
            Tw.Count(v => scoreT.text = v.ToString("N0"), 0, S.Score, 1.4f, Ease.OutQuart, .5f, (this, "score"));
            Intro.Rise(scoreBlock, 0, 40, .1f, .5f); Intro.Rise(statsCard, 6, 70, .1f, .5f);
            Intro.Rise(primary.rectTransform, 9, 90, .1f, .5f); Intro.Rise(left.rectTransform, 10, 90, .1f, .5f); Intro.Rise(right.rectTransform, 10, 90, .1f, .5f);
            if (badge.gameObject.activeSelf) { Tw.ScaleFrom(badge, 0f, 1f, .5f, Ease.OutBack, 1.6f); badge.localEulerAngles = new Vector3(0, 0, -8); Tw.Loop(k => badge.localScale = Vector3.one * (1f + .06f * k), .8f, Ease.InOutSine, true, (this, "badge"), 2.2f); }
        }
        public override void OnBackKey() { nav.GoRoot(Scr.Main); }
    }

    // =====================================================================  10. CREDITS
    public class CreditsScreen : MenuScreen
    {
        ScrollRect sr; RectTransform content; Tappable done;

        static readonly string[] lines =
        {
            "#NOVA RUSH", "A portrait space arcade game", "",
            "@DESIGN & CODE", "BSSE23018 · Muhammad Tayyab", "",
            "@COURSE", "ITU CS-464 Game Development", "Lab 08 · Audio & Feedback", "",
            "@VISUAL REFERENCES", "Layered card menus, one big PLAY button:", "Subway Surfers", "Neon-on-dark arcade palette:", "Geometry Dash", "Star-rating results flow:", "Angry Birds · Cut the Rope", "(inspiration only - no assets used)", "",
            "@TECH", "Unity 6000.3.24f1", "uGUI · TextMeshPro", "Own tween engine with ease curves", "",
            "@TYPEFACE", "Liberation Sans (SIL OFL 1.1)", "via TextMeshPro", "",
            "@ART & AUDIO", "Every icon, shape and sound is", "generated in code.", "Audio synthesis: tools/make_audio.py (CC0).", "",
            "@ASSISTANCE", "Code written with Claude Code (Anthropic)", "",
            "#THANKS FOR PLAYING",
        };

        public override void Build()
        {
            UI.Header(this, "CREDITS", true, false);
            var vp = UI.Rounded(Root, "viewport", UI.TC, new Vector2(0, -200), new Vector2(960, 1350), Pal.Panel, 52); UI.Outline(vp.transform, "ring", new Vector2(960, 1350), Pal.Mute, 52, 3); Mark("viewport", vp.rectTransform);
            vp.gameObject.AddComponent<RectMask2D>(); vp.raycastTarget = true;
            content = UI.Rect(vp.transform, "content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero;
            float y = -60;
            foreach (var l in lines)
            {
                if (l.Length == 0) { y -= 36; continue; }
                bool h1 = l.StartsWith("#"), h2 = l.StartsWith("@"); string txt = h1 || h2 ? l.Substring(1) : l;
                float size = h1 ? 84 : h2 ? 38 : 44; var col = h1 ? Pal.Cyan : h2 ? Pal.Amber : Pal.White; float h = h1 ? 110 : h2 ? 60 : 62;
                UI.Text(content, "line", txt, size, col, UI.TC, new Vector2(0, y), new Vector2(900, h), TextAlignmentOptions.Center, h1 || h2, h2 ? 8 : h1 ? 6 : 0).color = col;
                y -= h + (h2 ? 8 : 0);
            }
            content.sizeDelta = new Vector2(0, -y + 80);
            sr = vp.gameObject.AddComponent<ScrollRect>(); sr.content = content; sr.viewport = vp.rectTransform; sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30;
            vp.gameObject.AddComponent<StopAutoScroll>().owner = this;
            done = UI.Button(Root, "credits.done", UI.BC, new Vector2(0, 60), new Vector2(880, 150), Pal.Cyan, Pal.Ink, "BACK TO MENU", 56, "home"); done.onClick = () => nav.GoRoot(Scr.Main); Mark("done", done.rectTransform);
        }

        public void StopScroll() { Tw.Kill((this, "scroll")); }

        public override void OnShow(bool returning)
        {
            Tw.Kill((this, "scroll")); content.anchoredPosition = Vector2.zero; float dist = Mathf.Max(0, content.sizeDelta.y - 1350);
            // credits auto-scroll with Linear easing: constant speed reads as "reel", any touch cancels it
            Tw.To(k => content.anchoredPosition = new Vector2(0, dist * k), Mathf.Max(1f, dist / 110f), Ease.Linear, 1.2f, null, (this, "scroll"));
            if (!returning) { Intro.Rise(Marks["viewport"], 0, 60, .1f, .5f); Intro.Rise(done.rectTransform, 2, 90, .1f, .5f); }
        }
    }

    public class StopAutoScroll : MonoBehaviour, IBeginDragHandler, IPointerDownHandler
    {
        public CreditsScreen owner;
        public void OnBeginDrag(PointerEventData e) { owner.StopScroll(); }
        public void OnPointerDown(PointerEventData e) { owner.StopScroll(); }
    }
}
