using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lab07
{
    public class FlightFlow : MonoBehaviour
    {
        public const string SceneName = "Flight";
        const string ScenePath = "Assets/Lab07/Scenes/Flight.unity";
        App app;
        RectTransform overlay, fill;
        TMP_Text progress, title;
        public FlightSession Session { get; private set; }
        public bool Loading { get; private set; }

        public void Initialize(App owner)
        {
            app = owner;
            var background = UI.Box(app.overlay, "FlightLoading", UI.C, Vector2.zero, Vector2.zero, Pal.BgBottom);
            overlay = background.rectTransform;
            UI.Stretch(overlay);
            background.raycastTarget = true;
            UI.Icon(overlay, "ship", "ship1", UI.C, new Vector2(0, 380), 160, Pal.Cyan);
            title = UI.Text(overlay, "destination", "", 64, Pal.White, UI.C, new Vector2(0, 165), new Vector2(900, 110));
            var rail = UI.Rounded(overlay, "rail", UI.C, Vector2.zero, new Vector2(720, 32), Pal.PanelHi, 16);
            fill = UI.Rounded(rail.transform, "fill", UI.ML, Vector2.zero, new Vector2(720, 32), Pal.Cyan, 16).rectTransform;
            progress = UI.Text(overlay, "progress", "", 42, Pal.Cyan, UI.C, new Vector2(0, -104), new Vector2(760, 88));
            overlay.gameObject.SetActive(false);
        }

        public void Begin(bool replace = false, bool fromPause = false)
        {
            if (Loading || app.nav.Busy) return;
            if (!Application.CanStreamedLevelBeLoaded(ScenePath))
            {
                app.nav.Toast("Flight scene is missing from Build Profiles", false);
                return;
            }
            Loading = true;
            StartCoroutine(Load(replace, fromPause));
        }

        IEnumerator Load(bool replace, bool fromPause)
        {
            app.overlay.SetAsLastSibling();
            overlay.SetAsLastSibling();
            overlay.gameObject.SetActive(true);
            title.text = GS.Session.Title;
            Show(0);
            yield return null;

            var previous = SceneManager.GetSceneByPath(ScenePath);
            if (previous.isLoaded)
                yield return SceneManager.UnloadSceneAsync(previous);

            var operation = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
            operation.allowSceneActivation = false;
            while (operation.progress < .9f)
            {
                Show(operation.progress / .9f);
                yield return null;
            }

            Show(1);
            yield return new WaitForSecondsRealtime(.45f);
            operation.allowSceneActivation = true;
            yield return operation;
            var loaded = SceneManager.GetSceneByPath(ScenePath);
            Session = Array.Find(loaded.GetRootGameObjects(), root => root.GetComponent<FlightSession>())?.GetComponent<FlightSession>();
            overlay.gameObject.SetActive(false);
            Loading = false;

            if (!Session)
            {
                app.nav.Toast("Unable to start flight", false);
                yield break;
            }

            if (fromPause)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
                app.nav.GoRoot(Scr.Hud);
            }
            else if (replace) app.nav.Replace(Scr.Hud);
            else app.nav.Push(Scr.Hud, Trans.Fade);
        }

        void Show(float value)
        {
            fill.gameObject.SetActive(value > 0);
            fill.sizeDelta = new Vector2(720 * value, 32);
            progress.text = "LOADING FLIGHT  ·  " + Mathf.FloorToInt(value * 100) + "%";
        }

        public void ReturnToMenu()
        {
            if (Loading) return;
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
            Session = null;
        }
    }
}
