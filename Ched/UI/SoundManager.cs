using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ManagedBass;
using ManagedBass.Fx;

namespace Ched.UI
{
    public class SoundManager : IDisposable
    {
        readonly Dictionary<string, MediaPlayerFX> Players = new Dictionary<string, MediaPlayerFX>();

        public bool IsSupported { get; private set; } = true;

        public event EventHandler ExceptionThrown;

        public SoundManager()
        {
            if (!Bass.Init())
            {
                IsSupported = false;
                return;
            }
        }

        public void Dispose()
        {
            if (!IsSupported) return;
            Bass.Free();
        }

        public void Register(string path)
        {
            CheckSupported();
            lock (Players)
            {
                if (Players.ContainsKey(path)) return;
                var player = new MediaPlayerFX();
                bool result = false;
                Task.Run(async () => { result = await player.LoadAsync(path); }).Wait();
                if (!result) return;

                // Peek file with another handle to recover frequency, ManagedBass defaults mediaplayers to 44100hz
                int tempHandle = Bass.CreateStream(path);
                player.Frequency = Bass.ChannelGetAttribute(tempHandle, ChannelAttribute.Frequency);
                Bass.StreamFree(tempHandle);

                player.Loop = false;

                Players.Add(path, player);
            }
        }

        public void Play(string path , double volume)
        {
            Play(path, 0, 1.0, volume);
        }

        public void Play(string path, double offset, double speed, double volume)
        {
            CheckSupported();
            Task.Run(() => PlayInternal(path, offset, speed , volume))
                .ContinueWith(p =>
                {
                    if (p.Exception != null)
                    {
                        Program.DumpExceptionTo(p.Exception, "sound_exception.json");
                        ExceptionThrown?.Invoke(this, EventArgs.Empty);
                    }
                });
        }

        private void PlayInternal(string path, double offset, double speed, double volume)
        {
            lock (Players)
            {
                if (!Players.ContainsKey(path)) throw new InvalidOperationException("Sound file was not loaded");
                var player = Players[path];
                player.Position = TimeSpan.FromSeconds(offset);
                player.Tempo = speed * 100 - 100;
                player.Volume = volume;
                player.Play();
            }
        }

        public void StopAll()
        {
            CheckSupported();
            lock (Players)
            {
                foreach (var player in Players.Values)
                {
                    player.Pause();
                }
            }
        }

        public double GetDuration(string path)
        {
            Register(path);
            lock (Players)
            {
                return Players[path].Duration.TotalSeconds;
            }
        }

        protected void CheckSupported()
        {
            if (IsSupported) return;
            throw new NotSupportedException("The sound engine is not supported.");
        }
    }

    /// <summary>
    /// 音源を表すクラスです。
    /// </summary>
    [Serializable]
    public class SoundSource
    {
        public static readonly IReadOnlyCollection<string> SupportedExtensions = new string[] { ".wav", ".mp3", ".ogg" };

        /// <summary>
        /// この音源における遅延時間を取得します。
        /// この値は、タイミングよく音声が出力されるまでの秒数です。
        /// </summary>
        public double Latency { get; set; }

        public string FilePath { get; set; }

        public SoundSource()
        {
        }

        public SoundSource(string path, double latency)
        {
            FilePath = path;
            Latency = latency;
        }
    }
}
