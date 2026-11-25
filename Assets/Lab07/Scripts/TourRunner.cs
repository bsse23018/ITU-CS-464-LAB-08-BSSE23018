using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;

namespace Lab07
{
    /// <summary>
    /// Developer tool (not used by players). When Capture/tour.flag exists next to the Assets folder the app plays itself:
    ///   "shots" -> saves a colour PNG + element rectangles (JSON, used to draw the annotations) for each of the 10 screens
    ///   "video" -> walks the real navigation flow with tap ripples and dumps frames that ffmpeg turns into the demo video.
    /// The UI is rendered into a RenderTexture so the result is independent of the Game-view size.
    /// </summary>
    public class TourRunner : MonoBehaviour
    {
        string dir; RenderTexture rt; Texture2D tex; int W, H, frame; string mode; string frameDir; App app; Nav nav;

        BinaryWriter audio;
        bool audioCapture;
        bool IsVideo => mode == "video" || mode == "restart-video";

        public static bool Wanted(out string mode, out string dir)
        {
            dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Capture")); mode = null;
            var f = Path.Combine(dir, "tour.flag"); if (!File.Exists(f)) return false;
            mode = File.ReadAllText(f).Trim(); return true;
        }

        public void Begin(App a, string m, string d)
        {
            app = a; nav = a.nav; mode = m; dir = d; Hide(); StartCoroutine(Run());
        }

        void Hide() { }

        IEnumerator Run()
        {
            W = IsVideo ? 540 : 1080; H = IsVideo ? 960 : 1920;
            rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32); app.cam.targetTexture = rt; tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            Time.captureFramerate = 30; Tw.FixedDt = 1f / 30f;
            if (IsVideo)
            {
                frameDir = Path.Combine(dir, mode + "_frames");
                Directory.CreateDirectory(frameDir);
                audio = new BinaryWriter(File.Create(Path.Combine(dir, mode + ".pcm")));
                audioCapture = AudioRenderer.Start();
                File.WriteAllText(Path.Combine(dir, mode + "-audio-rate.txt"), AudioSettings.outputSampleRate.ToString());
            }
            nav.Start(Scr.Title);
            yield return null; yield return null;
            if (mode == "video") yield return Video();
            else if (mode == "restart-video") yield return RestartVideo();
            else yield return Shots();
            CloseAudio();
            Tw.FixedDt = 0; Time.captureFramerate = 0; app.cam.targetTexture = null; Time.timeScale = 1;
            File.WriteAllText(Path.Combine(dir, "done.txt"), mode + " ok " + DateTime.Now);
            File.Delete(Path.Combine(dir, "tour.flag"));
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ helpers
        IEnumerator Wait(float seconds)
        {
            int n = Mathf.CeilToInt(seconds * 30f);
            for (int i = 0; i < n; i++) { yield return new WaitForEndOfFrame(); if (IsVideo) Grab(); }
        }

        void CloseAudio()
        {
            if (audioCapture) AudioRenderer.Stop();
            audioCapture = false;
            audio?.Dispose();
            audio = null;
        }

        void OnDestroy() => CloseAudio();

        void Grab()
        {
            if (audioCapture)
            {
                int channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
                int count = AudioRenderer.GetSampleCountForCaptureFrame() * channels;
                if (count >= 0)
                {
                    using var samples = new NativeArray<float>(count, Allocator.Temp);
                    if (AudioRenderer.Render(samples))
                        foreach (float sample in samples) audio.Write(sample);
                }
            }
            RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(frameDir, "f" + (frame++).ToString("D5") + ".jpg"), tex.EncodeToJPG(88));
        }

        void SavePng(string path)
        {
            RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); RenderTexture.active = null; File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        static string Rect4(RectTransform r, Camera cam, int h)
        {
            var c = new Vector3[4]; r.GetWorldCorners(c); float x0 = 1e9f, y0 = 1e9f, x1 = -1e9f, y1 = -1e9f;
            foreach (var w in c) { var p = RectTransformUtility.WorldToScreenPoint(cam, w); x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, h - p.y); y1 = Mathf.Max(y1, h - p.y); }
            return "[" + Mathf.RoundToInt(x0) + "," + Mathf.RoundToInt(y0) + "," + Mathf.RoundToInt(x1) + "," + Mathf.RoundToInt(y1) + "]";
        }

        void SaveJson(string path)
        {
            var sb = new StringBuilder("{\"w\":" + W + ",\"h\":" + H + ",\"marks\":{"); bool first = true;
            void Add(MenuScreen s, string prefix) { foreach (var kv in s.Marks) { if (!kv.Value || !kv.Value.gameObject.activeInHierarchy) continue; if (!first) sb.Append(','); first = false; sb.Append("\"" + prefix + kv.Key + "\":" + Rect4(kv.Value, app.cam, H)); } }
            var cur = nav.Current; if (cur.Modal && nav.Stack.Count > 1) Add(nav.Stack[nav.Stack.Count - 2], "under.");
            Add(cur, "");
            sb.Append("},\"targets\":["); first = true;
            foreach (var t in Tappable.All.Values)
            {
                if (!t || !t.gameObject.activeInHierarchy || !t.GetComponentInParent<CanvasGroup>() || t.name.StartsWith("title.catcher")) continue;
                if (cur.Modal && !t.transform.IsChildOf(cur.Root)) continue;
                if (!t.transform.IsChildOf(cur.Root)) continue;
                if (!first) sb.Append(','); first = false; sb.Append("{\"k\":\"" + t.key + "\",\"r\":" + Rect4(t.rectTransform, app.cam, H) + "}");
            }
            sb.Append("]}"); File.WriteAllText(path, sb.ToString());
        }

        IEnumerator Shot(string name, float wait)
        {
            yield return Wait(wait); yield return new WaitForEndOfFrame();
            var d = Path.Combine(dir, "shots"); Directory.CreateDirectory(d); SavePng(Path.Combine(d, name + "-colour.png")); SaveJson(Path.Combine(d, name + ".json"));
        }

        // ------------------------------------------------------------------ screenshot tour
        IEnumerator Shots()
        {
            HudScreen.AutoPlay = true; HudScreen.DurationOverride = 0;
            yield return Shot("01-title", 3.6f);
            nav.Jump(Scr.Main); yield return Shot("02-main", 2.2f);
            nav.Jump(Scr.Mode); yield return Shot("03-mode", 2.2f);
            nav.Jump(Scr.Level); yield return Shot("04-level", 2.4f);
            nav.Jump(Scr.Loadout); yield return Shot("05-loadout", 2.4f);
            nav.Jump(Scr.Shop); yield return Shot("06-shop", 2.4f);
            nav.Jump(Scr.Settings); yield return Shot("07-settings", 2.4f);
            GS.Session.Mode = 0; GS.Session.Level = 4; App.I.flight.Begin();
            while (App.I.flight.Loading) yield return null;
            yield return Wait(6.2f); nav.Push(Scr.Pause); yield return Shot("08-pause", 1.8f);
            Time.timeScale = 1; AudioListener.pause = false; App.I.flight.ReturnToMenu();
            GS.Session.Win = true; GS.Session.Score = 3450; GS.Session.CoinsEarned = 18; GS.Session.Stars = 3; GS.Session.Accuracy = .86f; GS.Session.NewBest = true; GS.Session.Level = 4;
            nav.Jump(Scr.Results); yield return Shot("09-results", 4.2f);
            nav.Jump(Scr.Credits); yield return Shot("10-credits", 3.6f);
        }

        // ------------------------------------------------------------------ video tour
        IEnumerator Tap(string key, float after = 1f)
        {
            if (!Tappable.All.TryGetValue(key, out var t) || !t || !t.gameObject.activeInHierarchy) { Debug.LogError("Tour could not find " + key); yield break; }
            Ripple(t.rectTransform); yield return Wait(.45f); t.Click(); yield return Wait(after);
        }

        void Ripple(RectTransform target)
        {
            var c = new Vector3[4]; target.GetWorldCorners(c); var wp = (c[0] + c[2]) * .5f;
            var go = new GameObject("tap", typeof(RectTransform), typeof(Image)); go.layer = 5; var r = (RectTransform)go.transform; r.SetParent(nav.Overlay, false);
            r.position = wp; r.sizeDelta = new Vector2(70, 70); var im = go.GetComponent<Image>(); im.sprite = Sprites.Circle(); im.raycastTarget = false; im.color = new Color(1, 1, 1, .8f);
            Tw.To(k => { r.localScale = Vector3.one * (1f + 1.8f * k); im.color = new Color(1, 1, 1, .8f * (1 - k)); }, .55f, Ease.OutCubic, 0f, () => Destroy(go));
        }

        IEnumerator Video()
        {
            UnityEngine.Random.InitState(108);
            HudScreen.AutoPlay = true; HudScreen.DurationOverride = 9f;
            for (int i = 0; i < 4; i++) GameAudio.Instance.SetVolume((AudioBus)i, 1f);
            yield return Wait(3.2f);                                    // 1 Title: logo, loading bar -> TAP TO START
            yield return Tap("title.start", 2.2f);                       // 2 Main menu
            yield return Tap("main.play", 1.8f);                         // 3 Mode select
            yield return Tap("mode.3", 1.2f);                            //   locked card -> deny shake + toast
            yield return Tap("mode.0", 2.0f);                            // 4 Level select
            yield return Tap("level.8", 1.0f);                           //   locked level -> deny
            yield return Tap("level.4", 1.0f);
            yield return Tap("level.start", 5.0f);                       //   Gameplay (countdown + HUD)
            yield return Tap("hud.pause", 2.0f);                         // 5 Pause modal
            yield return Tap("pause.settings", 1.6f);                    // 6 Settings (from pause)
            var sliders = nav.Get(Scr.Settings).Root.GetComponentsInChildren<USlider>(true);
            float[] levels = { .8f, .45f, .7f, .6f };
            for (int i = 0; i < levels.Length; i++)
            {
                var slider = sliders[i];
                float start = slider.value;
                float target = levels[i];
                Tw.To(k => slider.Set(Mathf.Lerp(start, target, k)), .5f, Ease.InOutQuad);
                yield return Wait(.65f);
            }
            PlayerPrefs.Save();
            yield return Tap("settings.shake", .9f); yield return Tap("settings.quality.High", 1.0f);
            yield return Tap("back", 1.6f);                              //   back to Pause
            yield return Tap("pause.resume", 3.0f);                      //   resume play
            yield return Wait(9.0f);                                   // 7 Results
            yield return Tap("results.primary", 3.2f);                  // Replay via loading
            yield return Tap("hud.pause", 1.2f);
            yield return Tap("pause.quit", .55f);
            yield return Tap("pause.quit", 1.5f);
            yield return Tap("main.hangar", 1.8f);                       // 8 Hangar / loadout
            yield return Tap("loadout.next", 1.2f); yield return Tap("loadout.action", 1.3f); // locked ship -> not enough coins
            yield return Tap("loadout.prev", 1.0f); yield return Tap("boost.2", 1.0f); yield return Tap("boost.0", 1.0f);
            yield return Tap("back", 1.6f);
            yield return Tap("main.shop", 1.8f);                         // 9 Shop
            yield return Tap("shop.tabs.Skins", 1.4f); yield return Tap("buy.s_ember", 1.4f);
            yield return Tap("shop.tabs.Coins", 1.4f); yield return Tap("buy.c_small", 1.6f);
            yield return Tap("shop.tabs.Boosts", 1.4f); yield return Tap("buy.b_magnet", 1.6f);
            yield return Tap("back", 1.6f);
            yield return Tap("main.credits", 5.0f);                      // 10 Credits (auto-scroll)
            yield return Tap("credits.done", 2.4f);
            AssertMix();
        }

        IEnumerator RestartVideo()
        {
            yield return Wait(3.0f);
            yield return Tap("title.start", 1f);
            nav.Push(Scr.Settings);
            yield return Wait(3.2f);
            float[] targets = { .8f, .45f, .7f, .6f };
            var sliders = nav.Get(Scr.Settings).Root.GetComponentsInChildren<USlider>(true);
            for (int i = 0; i < targets.Length; i++)
                if (Mathf.Abs(GameAudio.Volume((AudioBus)i) - targets[i]) > .002f ||
                    Mathf.Abs(sliders[i].value - targets[i]) > .002f)
                    Debug.LogError("Restart volume mismatch for " + (AudioBus)i);
            AssertMix();
        }

        void AssertMix()
        {
            var mixer = Resources.Load<UnityEngine.Audio.AudioMixer>("Lab08Audio/MainMixer");
            string[] parameters = { "MasterVolume", "MusicVolume", "SfxVolume", "UiVolume" };
            foreach (var source in GameAudio.Instance.GetComponentsInChildren<AudioSource>())
                if (!source.outputAudioMixerGroup || source.outputAudioMixerGroup.audioMixer != mixer)
                    Debug.LogError("Unrouted audio source: " + source.name);
            for (int i = 0; i < parameters.Length; i++)
                if (!mixer.GetFloat(parameters[i], out float db) ||
                    Mathf.Abs(db - GameAudio.Decibels(GameAudio.Volume((AudioBus)i))) > .02f)
                    Debug.LogError("Mixer parameter mismatch: " + parameters[i]);
        }
    }
}
