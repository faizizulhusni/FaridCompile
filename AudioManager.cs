using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaridCompile
{
    internal class AudioManager
    {
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AudioManager();
                }
                return instance;
            }
        }

        // Music
        private Dictionary<string, Song> musicTracks;
        private string currentMusicTrack;

        // Sound Effect
        private Dictionary<string, SoundEffect> soundEffects;
        private Dictionary<string, SoundEffectInstance> loopingSounds;

        // Volume
        private float masterVolume = 1.0f;
        private float musicVolume = 0.7f;
        private float sfxVolume = 0.8f;

        // Music settings
        private bool musicEnabled = true;
        private bool sfxEnabled = true;
        private bool musicFading = false;
        private float musicFadeTimer = 0f;
        private float musicFadeDuration = 2f;
        private string nextMusic = null;

        public AudioManager()
        {
            musicTracks = new Dictionary<string, Song>();
            soundEffects = new Dictionary<string, SoundEffect>();
            loopingSounds = new Dictionary<string, SoundEffectInstance>();
        }

        // Load music
        public void LoadMusic(string name, Song song)
        {
            if (!musicTracks.ContainsKey(name))
            {
                musicTracks.Add(name, song);
            }
        }

        public void LoadSoundEffect(string name, SoundEffect sound)
        {
            if (!soundEffects.ContainsKey(name))
            {
                soundEffects.Add(name, sound);
            }
        }

        public void PlayMusic(string trackName, bool loop = true, bool fadeIn = true)
        {
            if (!musicEnabled || !musicTracks.ContainsKey(trackName))
            {
                return;
            }

            if (currentMusicTrack == trackName && MediaPlayer.State == MediaState.Playing)
            {
                return;
            }

            try
            {
                if (fadeIn && MediaPlayer.State == MediaState.Playing)
                {
                    FadeMusicTo(trackName, musicFadeDuration);
                }
                else
                {
                    MediaPlayer.Stop();
                    MediaPlayer.Play(musicTracks[trackName]);
                    MediaPlayer.IsRepeating = loop;
                    MediaPlayer.Volume = musicVolume * masterVolume;
                    currentMusicTrack = trackName;
                }
            }
            catch (Exception ex)
            {

            }
        }

        // Stop music
        public void StopMusic(bool fadeOut = false)
        {
            if (fadeOut)
            {
                musicFading = true;
                musicFadeTimer = musicFadeDuration;
                nextMusic = null;
            }
            else
            {
                MediaPlayer.Stop();
                currentMusicTrack = null;
            }
        }

        // Pause music
        public void PauseMusic()
        {
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Pause();
            }
        }

        // Resume music
        public void ResumeMusic()
        {
            if (MediaPlayer.State == MediaState.Paused)
            {
                MediaPlayer.Resume();
            }
        }

        // Fade music
        public void FadeMusicTo(string newTrackName, float duration = 2f)
        {
            if (!musicTracks.ContainsKey(newTrackName))
            {
                return;
            }

            musicFading = true;
            musicFadeTimer = duration;
            musicFadeDuration = duration;
            nextMusic = newTrackName;
        }

        // Play music
        public void PlaySound(string soundName, float volume = 1.0f, float pitch = 0f, float pan = 0f)
        {
            if (!sfxEnabled || !soundEffects.ContainsKey(soundName))
            {
                return;
            }

            try
            {
                float finalVolume = volume * sfxVolume * masterVolume;
                soundEffects[soundName].Play(finalVolume, pitch, pan);
            }
            catch (Exception e)
            {

            }
        }

        // Play looping sound effect
        public void PlayLoopingSound(string soundName, float volume = 1.0f)
        {
            if (!sfxEnabled || !soundEffects.ContainsKey(soundName))
            {
                return;
            }

            try
            {
                if (loopingSounds.ContainsKey(soundName))
                {
                    loopingSounds[soundName].Stop();
                    loopingSounds[soundName].Dispose();
                    loopingSounds.Remove(soundName);
                }

                SoundEffectInstance instance = soundEffects[soundName].CreateInstance();
                instance.IsLooped = true;
                instance.Volume = volume * sfxVolume * masterVolume;
                instance.Play();
                loopingSounds.Add(soundName, instance);
            }
            catch (Exception e)
            {

            }
        }

        // Stop looping sound
        public void StopLoopingSound(string soundName)
        {
            if (loopingSounds.ContainsKey(soundName))
            {
                loopingSounds[soundName].Stop();
                loopingSounds[soundName].Dispose();
                loopingSounds.Remove(soundName);
            }
        }

        // Stop all looping sounds
        public void StopAllLoopingSounds()
        {
            foreach (var sound in loopingSounds.Values)
            {
                sound.Stop();
                sound.Dispose();
            }

            loopingSounds.Clear();
        }

        // Master volume
        public void SetMasterVolume(float volume)
        {
            masterVolume = MathHelper.Clamp(volume, 0f, 1f);
            UpdateVolumes();
        }

        // SFX volume
        public void SetSfxVolume(float volume)
        {
            sfxVolume = MathHelper.Clamp(volume, 0f, 1f);
            UpdateLoopingSoundsVolume();
        }

        private void UpdateVolumes()
        {
            MediaPlayer.Volume = musicVolume * masterVolume;
            UpdateLoopingSoundsVolume();
        }

        private void UpdateLoopingSoundsVolume()
        {
            foreach (var sound in loopingSounds.Values)
            {
                sound.Volume = sfxVolume * masterVolume;
            }
        }

        // Toggle music on/off
        public void ToggleMusic()
        {
            musicEnabled = !musicEnabled;
            if (musicEnabled)
            {
                MediaPlayer.Stop();
            }
            else if (currentMusicTrack != null)
            {
                PlayMusic(currentMusicTrack);
            }
        }

        // Toggle sfx on/off
        public void ToggleSfx()
        {
            sfxEnabled = !sfxEnabled;
            if (!sfxEnabled)
            {
                StopAllLoopingSounds();
            }
        }

        // Update audio manager
        public void Update(float dTime)
        {
            if (musicFading)
            {
                musicFadeTimer -= dTime;

                if (musicFadeTimer > musicFadeDuration / 2f)
                {
                    float fadeProgress = (musicFadeTimer - musicFadeDuration / 2f) / (musicFadeDuration / 2f);
                    MediaPlayer.Volume = fadeProgress * musicVolume * masterVolume;
                }

                else if (musicFadeTimer > 0f)
                {
                    if (nextMusic != null && currentMusicTrack != nextMusic)
                    {
                        MediaPlayer.Stop();
                        if (musicTracks.ContainsKey(nextMusic))
                        {
                            MediaPlayer.Play(musicTracks[nextMusic]);
                            MediaPlayer.IsRepeating = true;
                            currentMusicTrack = nextMusic;
                        }
                    }

                    float fadeProgress = 1f - (musicFadeTimer - musicFadeDuration / 2f);
                    MediaPlayer.Volume = fadeProgress * musicVolume * masterVolume;
                }
                else
                {
                    musicFading = false;
                    musicFadeTimer = 0f;

                    if (nextMusic == null)
                    {
                        MediaPlayer.Stop();
                        currentMusicTrack = null;
                    }

                    else
                    {
                        MediaPlayer.Volume = musicVolume * masterVolume;
                    }

                    nextMusic = null;
                }
            }
        }

        public void Dispose()
        {
            StopAllLoopingSounds();
            MediaPlayer.Stop();
            musicTracks.Clear();
            soundEffects.Clear();
        }

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public bool MusicEnabled => musicEnabled;
        public bool SfxEnabled => sfxEnabled;
        public string CurrentMusicTrack => currentMusicTrack;
    }
}
