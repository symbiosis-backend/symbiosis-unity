using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MahjongMusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool loop = true;

        private AudioClip currentClip;

        private void Awake()
        {
            if (source == null)
                source = GetComponent<AudioSource>();

            if (source != null)
            {
                source.playOnAwake = false;
                source.loop = loop;
                source.volume = volume;
            }
        }

        public void PlayLevelMusic(AudioClip clip)
        {
            if (source == null)
                return;

            if (clip == null)
            {
                StopMusic();
                return;
            }

            if (currentClip == clip && source.isPlaying)
                return;

            currentClip = clip;
            source.clip = clip;
            source.loop = loop;
            source.volume = volume;
            source.Play();
        }

        public void StopMusic()
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            currentClip = null;
        }
    }
}