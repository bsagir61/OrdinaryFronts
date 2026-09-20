using System.Collections;
using UnityEngine;

namespace OrdinaryFronts
{
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioClip paperClip;
        [SerializeField] private AudioClip confirmClip;
        [SerializeField] private AudioClip backClip;
        [SerializeField] private AudioClip windAmbience;
        [SerializeField] private AudioClip gustClip;
        [SerializeField] private AudioClip sparkClip;
        [SerializeField] private AudioClip creakClip;
        [SerializeField] private AudioClip riverAmbience;
        [SerializeField] private AudioClip cityAmbience;
        [SerializeField] private AudioClip shelterAmbience;
        [SerializeField] private AudioClip trainAmbience;

        private AudioSource ambientSource;
        private AudioSource effectsSource;
        private SettingsData settings;
        private Coroutine ambientRoutine;

        private void Awake()
        {
            EnsureSources();
        }

        private void EnsureSources()
        {
            if (ambientSource != null && effectsSource != null) return;
            AudioSource[] existing = GetComponents<AudioSource>();
            ambientSource = existing.Length > 0 ? existing[0] : gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.spatialBlend = 0f;
            effectsSource = existing.Length > 1 ? existing[1] : gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;
        }

        public void ConfigureAssets(AudioClip paper, AudioClip confirm, AudioClip back, AudioClip city, AudioClip shelter, AudioClip train)
        {
            paperClip = paper;
            confirmClip = confirm;
            backClip = back;
            cityAmbience = city;
            shelterAmbience = shelter;
            trainAmbience = train;
        }

        /// <summary>Set rüzgârı: Afsluitdijk düğümlerinin ortam sesi ve ara sahnenin bora vuruşu.</summary>
        public void ConfigureInterludeClips(AudioClip wind, AudioClip gust, AudioClip spark, AudioClip creak, AudioClip river)
        {
            windAmbience = wind;
            gustClip = gust;
            sparkClip = spark;
            creakClip = creak;
            riverAmbience = river;
        }

        public void PlayGust() { PlayOneShot(gustClip); }
        public void PlaySpark() { PlayOneShot(sparkClip); }
        public void PlayCreak() { PlayOneShot(creakClip); }

        public void ApplySettings(SettingsData value)
        {
            EnsureSources();
            settings = value ?? new SettingsData();
            settings.Clamp();
            AudioListener.volume = settings.masterVolume;
            ambientSource.volume = settings.ambientVolume * 0.42f;
            effectsSource.volume = settings.effectsVolume * 0.7f;
        }

        public void PlayPaper() { PlayOneShot(paperClip); }
        public void PlayConfirm() { PlayOneShot(confirmClip); }
        public void PlayBack() { PlayOneShot(backClip); }

        public void PlayAmbienceFor(string imageKey)
        {
            EnsureSources();
            AudioClip requested = cityAmbience;
            if (imageKey == "shelter_stairs") requested = shelterAmbience;
            else if (imageKey == "train_platform") requested = trainAmbience;
            else if (imageKey == "afsluitdijk" && windAmbience != null) requested = windAmbience;
            else if ((imageKey == "broken_bridge" || imageKey == "river_gorge") && riverAmbience != null) requested = riverAmbience;
            if (requested == ambientSource.clip && ambientSource.isPlaying) return;
            if (ambientRoutine != null) StopCoroutine(ambientRoutine);
            ambientRoutine = StartCoroutine(CrossFade(requested));
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip != null) effectsSource.PlayOneShot(clip);
        }

        private IEnumerator CrossFade(AudioClip next)
        {
            float baseVolume = (settings ?? new SettingsData()).ambientVolume * 0.42f;
            float start = ambientSource.volume;
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                ambientSource.volume = Mathf.Lerp(start, 0f, t / 0.35f);
                yield return null;
            }
            ambientSource.Stop();
            ambientSource.clip = next;
            if (next != null) ambientSource.Play();
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                ambientSource.volume = Mathf.Lerp(0f, baseVolume, t / 0.5f);
                yield return null;
            }
            ambientSource.volume = baseVolume;
            ambientRoutine = null;
        }
    }
}
