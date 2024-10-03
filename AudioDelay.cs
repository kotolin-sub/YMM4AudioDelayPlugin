using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Audio.Effects;
using YukkuriMovieMaker.Plugin.Effects;
namespace YMM4AudioDelayPlugin
{
    [AudioEffect("音声遅延", ["基本"], [])]
    public class AudioDelay : AudioEffectBase
    {
        public override string Label => "音声遅延";
        [Display(Name = "左チャンネル", Description = "左チャンネルの遅延量（ミリ秒）")]
        [AnimationSlider("F2", "ms", 0, 100)]
        public Animation LeftDelay { get; } = new Animation(0, 0, 100);
        [Display(Name = "右チャンネル", Description = "右チャンネルの遅延量（ミリ秒）")]
        [AnimationSlider("F2", "ms", 0, 100)]
        public Animation RightDelay { get; } = new Animation(0, 0, 100);
        public override IAudioEffectProcessor CreateAudioEffect(TimeSpan d) => new DelayProc(this, d);
        public override IEnumerable<string> CreateExoAudioFilters(int k, ExoOutputDescription e) => [];
        protected override IEnumerable<IAnimatable> GetAnimatables() => [LeftDelay, RightDelay];
    }
    internal class DelayProc : AudioEffectProcessorBase
    {
        private readonly AudioDelay itm; private readonly TimeSpan dur;
        private readonly Queue<float> lbuf, rbuf;
        private long pos; private const int MaxMs = 100;
        public override int Hz => Input?.Hz ?? 1; public override long Duration => Input?.Duration ?? 0;
        public DelayProc(AudioDelay i, TimeSpan d)
        {
            itm = i; dur = d;
            lbuf = new Queue<float>(Hz * MaxMs / 1000);
            rbuf = new Queue<float>(Hz * MaxMs / 1000);
            pos = 0;
        }
        protected override void seek(long p) { pos = p; Input?.Seek(p); lbuf.Clear(); rbuf.Clear(); }
        protected override int read(float[] dst, int off, int cnt)
        {
            if (Input == null || Hz == 0) return 0;
            int r = Input.Read(dst, off, cnt); long tot = (long)(Hz * dur.TotalSeconds);
            for (int i = 0; i < r; i += 2)
            {
                long cur = (pos + i) / 2;
                double lms = Math.Min(itm.LeftDelay.GetValue(cur, tot, Hz), MaxMs);
                double rms = Math.Min(itm.RightDelay.GetValue(cur, tot, Hz), MaxMs);
                int ldly = (int)(lms * Hz / 1000); int rdly = (int)(rms * Hz / 1000);
                lbuf.Enqueue(dst[off + i]); rbuf.Enqueue(dst[off + i + 1]);
                dst[off + i] = lbuf.Count > ldly ? lbuf.Dequeue() : 0;
                dst[off + i + 1] = rbuf.Count > rdly ? rbuf.Dequeue() : 0;
            }
            while (lbuf.Count > Hz * MaxMs / 1000) lbuf.Dequeue();
            while (rbuf.Count > Hz * MaxMs / 1000) rbuf.Dequeue();
            pos += r; return r;
        }
    }
}
