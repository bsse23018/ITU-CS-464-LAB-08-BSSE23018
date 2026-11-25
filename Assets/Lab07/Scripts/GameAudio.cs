using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Lab07
{
    public enum AudioBus { Master, Music, Sfx, Ui }
    public enum UiCue { Select, Press, Back, Open, Close, Pause, Resume, Success, Error }
    public enum FlightCue { Launch, Coin, Hit, Shield, Win, Lose }

    public class GameAudio : MonoBehaviour
    {
        const string ResourcePath = "Lab08Audio/";
        const float MusicLevel = .55f;
        static readonly string[] Parameters = { "MasterVolume", "MusicVolume", "SfxVolume", "UiVolume" };
        static readonly string[] UiFiles = { "select", "click", "back", "open", "close", "pause", "resume", "success", "error" };
        static readonly string[] FlightFiles = { "launch", "pickup", "hit", "shield", "win", "lose" };

        public static GameAudio Instance { get; private set; }
        AudioMixer mixer;
        AudioSource[] ui, sfx;
        AudioSource menuMusic, flightMusic;
        AudioClip[] uiClips, flightClips;
        Coroutine fade;
        int nextUi, nextSfx;
        float lastSelect = -1f;

        void Awake()
        {
            Instance = this;
            mixer = Resources.Load<AudioMixer>(ResourcePath + "MainMixer");
            ui = MakePool("UI", 4, true);
            sfx = MakePool("SFX", 6, false);
            uiClips = System.Array.ConvertAll(UiFiles, LoadClip);
            flightClips = System.Array.ConvertAll(FlightFiles, LoadClip);
            menuMusic = MakeSource("Menu loop", Group("Music"), true);
            flightMusic = MakeSource("Flight loop", Group("Music"), true);
            SetLoop(menuMusic, "music");
            SetLoop(flightMusic, "flight_music");
        }

        void Start()
        {
            ApplySavedVolumes();
            Music(false, true);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            AudioListener.pause = false;
        }

        static AudioClip LoadClip(string file) => Resources.Load<AudioClip>(ResourcePath + file);
        AudioMixerGroup Group(string group) => mixer.FindMatchingGroups(group)[0];

        AudioSource MakeSource(string label, AudioMixerGroup group, bool ignorePause)
        {
            var source = new GameObject(label).AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.outputAudioMixerGroup = group;
            source.playOnAwake = false;
            source.ignoreListenerPause = ignorePause;
            return source;
        }

        AudioSource[] MakePool(string group, int size, bool ignorePause)
        {
            var output = Group(group);
            var pool = new AudioSource[size];
            for (int i = 0; i < size; i++)
                pool[i] = MakeSource(group + " voice " + (i + 1), output, ignorePause);
            return pool;
        }

        void SetLoop(AudioSource source, string file)
        {
            source.clip = LoadClip(file);
            source.loop = true;
            source.volume = 0;
        }

        public static float Volume(AudioBus bus) => PlayerPrefs.GetFloat(Parameters[(int)bus], 1f);
        public static float Decibels(float value) => 20f * Mathf.Log10(Mathf.Clamp(value, .0001f, 1f));

        public void SetVolume(AudioBus bus, float value)
        {
            string parameter = Parameters[(int)bus];
            PlayerPrefs.SetFloat(parameter, Mathf.Clamp01(value));
            mixer.SetFloat(parameter, Decibels(value));
        }

        public void ApplySavedVolumes()
        {
            foreach (AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))
                mixer.SetFloat(Parameters[(int)bus], Decibels(Volume(bus)));
        }

        public static void UI(UiCue cue)
        {
            var audio = Instance;
            if (!audio) return;
            if (cue == UiCue.Select)
            {
                if (Time.unscaledTime - audio.lastSelect < .09f) return;
                audio.lastSelect = Time.unscaledTime;
            }
            audio.OneShot(audio.ui, ref audio.nextUi, audio.uiClips[(int)cue], cue == UiCue.Select ? .22f : .48f, 0f);
        }

        public void Flight(FlightCue cue)
        {
            OneShot(sfx, ref nextSfx, flightClips[(int)cue], .65f, cue == FlightCue.Coin || cue == FlightCue.Hit ? .045f : 0f);
        }

        void OneShot(AudioSource[] pool, ref int next, AudioClip clip, float gain, float pitchRange)
        {
            if (!clip) return;
            for (int i = 0; i < pool.Length; i++)
            {
                var voice = pool[next];
                next = (next + 1) % pool.Length;
                if (voice.isPlaying) continue;
                voice.pitch = 1f + Random.Range(-pitchRange, pitchRange);
                voice.PlayOneShot(clip, gain);
                return;
            }
            // Keep the playback pitch of existing effects intact when the pool is full.
            pool[next].PlayOneShot(clip, gain);
            next = (next + 1) % pool.Length;
        }

        public void Music(bool inFlight, bool instant = false)
        {
            if (fade != null) StopCoroutine(fade);
            var incoming = inFlight ? flightMusic : menuMusic;
            var outgoing = inFlight ? menuMusic : flightMusic;
            if (!incoming.isPlaying) incoming.Play();
            fade = StartCoroutine(Crossfade(incoming, outgoing, instant ? 0f : .85f));
        }

        IEnumerator Crossfade(AudioSource incoming, AudioSource outgoing, float seconds)
        {
            float fromIn = incoming.volume;
            float fromOut = outgoing.volume;
            for (float time = 0; time < seconds; time += Time.unscaledDeltaTime)
            {
                incoming.volume = Mathf.Lerp(fromIn, MusicLevel, time / seconds);
                outgoing.volume = Mathf.Lerp(fromOut, 0, time / seconds);
                yield return null;
            }
            incoming.volume = MusicLevel;
            outgoing.Stop();
            outgoing.volume = 0;
            fade = null;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) PlayerPrefs.Save();
        }
    }
}
