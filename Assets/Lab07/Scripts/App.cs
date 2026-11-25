using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lab07
{
    /// <summary>
    /// Single bootstrap component. Builds camera, canvas (Screen Space - Camera, 1080x1920 reference, safe-area aware),
    /// event system, animated background and the Nav, then opens the Title screen.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class App : MonoBehaviour
    {
        public static App I { get; private set; }
        public FlightFlow flight;
        public Nav nav; public Camera cam; public Canvas canvas; public RectTransform safe, overlay, bgRoot;
        readonly List<RectTransform> stars = new List<RectTransform>(); readonly List<float> starSpeed = new List<float>();

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60; Screen.orientation = ScreenOrientation.Portrait;
            Time.timeScale = 1f; Input.multiTouchEnabled = false;
            QualitySettings.SetQualityLevel(Mathf.Clamp(GS.Quality * 2, 0, QualitySettings.names.Length - 1), true);

            // camera
            cam = FindFirstObjectByType<Camera>();
            if (!cam) cam = new GameObject("UI Camera", typeof(Camera)).GetComponent<Camera>();
            if (!cam.GetComponent<AudioListener>()) cam.gameObject.AddComponent<AudioListener>();
            gameObject.AddComponent<GameAudio>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Pal.BgBottom; cam.orthographic = true; cam.cullingMask = 1 << 5; cam.transform.position = new Vector3(0, 0, -10);

            // canvas
            var cgo = new GameObject("Canvas", typeof(RectTransform)); cgo.layer = 5; cgo.transform.SetParent(transform, false);
            canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 10;
            var sc = cgo.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1080, 1920);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            cgo.AddComponent<GraphicRaycaster>();

            if (!FindFirstObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            bgRoot = UI.Stretch(UI.Rect(cgo.transform, "Background")); BuildBackground();
            safe = UI.Stretch(UI.Rect(cgo.transform, "SafeArea")); safe.gameObject.AddComponent<SafeArea>();
            overlay = UI.Stretch(UI.Rect(safe, "Overlay"));
            nav = new Nav(safe, overlay);
            flight = gameObject.AddComponent<FlightFlow>();
            flight.Initialize(this);
        }

        void Start()
        {
            if (TourRunner.Wanted(out var mode, out var dir)) { gameObject.AddComponent<TourRunner>().Begin(this, mode, dir); return; }
            nav.Start(Scr.Title);
        }

        void BuildBackground()
        {
            var g = UI.Box(bgRoot, "Gradient", UI.C, Vector2.zero, Vector2.zero, Color.white, Sprites.Gradient(Pal.BgTop, Pal.BgBottom)); UI.Stretch(g.rectTransform);
            // soft glow behind the content (depth cue), plus two parallax star layers drifting slowly
            UI.Box(bgRoot, "Glow", UI.C, new Vector2(0, 250), new Vector2(1500, 1500), Pal.A(Pal.PanelHi, .45f), Sprites.Glow());
            var rnd = new System.Random(11);
            for (int i = 0; i < 46; i++)
            {
                bool near = i % 3 == 0; float s = near ? rnd.Next(7, 12) : rnd.Next(3, 7);
                var im = UI.Box(bgRoot, "star", UI.BL, new Vector2(rnd.Next(0, 1080), rnd.Next(0, 1920)), new Vector2(s, s), Pal.A(Pal.White, near ? .55f : .28f), Sprites.Circle());
                stars.Add(im.rectTransform); starSpeed.Add(near ? 38f : 16f);
            }
        }

        void Update()
        {
            float dt = Tw.FixedDt > 0 ? Tw.FixedDt : Time.unscaledDeltaTime;
            for (int i = 0; i < stars.Count; i++)
            {
                var p = stars[i].anchoredPosition; p.y -= starSpeed[i] * dt; if (p.y < -20) p.y = bgRoot.rect.height + 20; stars[i].anchoredPosition = p;
            }
            if (Input.GetKeyDown(KeyCode.Escape) && !flight.Loading && !nav.Busy && nav.Current != null) nav.Current.OnBackKey(); // Android back button
            if (!flight.Loading && !nav.Busy && nav.Current?.Id == Scr.Hud)
                (nav.Current as HudScreen)?.Tick(Time.deltaTime);
        }
    }
}
