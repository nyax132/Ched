﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Ched.UI
{
    public class Recorder
    {
        public static int N_GROUND = 32;
        public static int N_AIR = 6;
        public static int N_LANES = N_GROUND + N_AIR;
        private static int TICK_INF = 999999999;

        private static Key[] KEYBOARD_YUANCON_LAYOUT = {
            Key.D6, Key.D5, Key.D4, Key.D3, Key.D2, Key.D1, Key.Z, Key.Y,
            Key.X, Key.W, Key.V, Key.U, Key.T, Key.S, Key.R, Key.Q,
            Key.P, Key.O, Key.N, Key.M, Key.L, Key.K, Key.J, Key.I,
            Key.H, Key.G, Key.F, Key.E, Key.D, Key.C, Key.B, Key.A,
            Key.OemMinus, Key.OemPlus, Key.Oem4,
            Key.Oem6, Key.Oem5, Key.Oem1
        };

        private static Key[] KEYBOARD_TASOLLER_LAYOUT = {
            Key.A, Key.D1, Key.Z, Key.Q, Key.S, Key.D2, Key.X, Key.W,
            Key.D, Key.D3, Key.C, Key.E, Key.F, Key.D4, Key.V, Key.R,
            Key.G, Key.D5, Key.B, Key.T, Key.H, Key.D6, Key.N, Key.Y,
            Key.J, Key.D7, Key.M, Key.U, Key.K, Key.D8, Key.OemComma, Key.I,
            Key.Oem2, Key.Oem7, Key.OemPeriod,
            Key.Oem1, Key.Oem6, Key.Oem4
        };

        private bool IsRecording = false;
        private Interval RecordingInterval = (0, 0);
        private YuanconHid YuanconHid = null;
        private RecordingModeType recordingMode;
        private InputModeType inputMode;

        private Interval LastFetchedInterval = (0, 0);
        private List<List<Interval>> LastFetchedData = null;

        private List<PlaybackLane> PlaybackLanes;
        private List<RecordingLane> RecordingLanes;

        public InputModeType InputMode {
            get { return inputMode; }
            set { 
                inputMode = value; 
            }
        }
        public RecordingModeType RecordingMode
        {
            get { return recordingMode; }
            set { recordingMode = value; }
        }

        public Recorder()
        {
            PlaybackLanes = new List<PlaybackLane> { };
            RecordingLanes = new List<RecordingLane> { };
            for (var i = 0; i < N_LANES; i++)
            {
                PlaybackLanes.Add(new PlaybackLane());
                RecordingLanes.Add(new RecordingLane());
            }
            YuanconHid = new YuanconHid();
            recordingMode = RecordingModeType.RECORDING_OVERWRITE;
            inputMode = InputModeType.INPUT_KEYBOARD_YUANCON;
        }

        public List<List<Interval>> GetRecordedData(int fromTick, int toTick)
        {
            Interval viewport = (fromTick, toTick);
            if (!IsRecording && LastFetchedData != null && LastFetchedInterval.Equals(viewport))
            {
                return LastFetchedData;
            }

            Interval preRecordViewport = (fromTick, Math.Min(RecordingInterval.StartTick, toTick));
            Interval duringRecordViewport = (
                Math.Max(RecordingInterval.StartTick, fromTick),
                Math.Min(RecordingInterval.EndTick, toTick)
            );
            Interval postRecordViewport = (Math.Max(RecordingInterval.EndTick, fromTick), toTick);

            var data = new List<List<Interval>> { };
            for (var i = 0; i < N_LANES; i++)
            {
                var lane = new PlaybackLane { };
                lane.AddIntervals(RecordingLanes[i].GetVisibleIntervals(duringRecordViewport));
                switch (recordingMode)
                {
                    case RecordingModeType.RECORDING_OVERWRITE:
                        lane.AddIntervals(PlaybackLanes[i].GetVisibleIntervals(preRecordViewport));
                        lane.AddIntervals(PlaybackLanes[i].GetVisibleIntervals(postRecordViewport));
                        break;
                    case RecordingModeType.RECORDING_ADD:
                        lane.AddIntervals(PlaybackLanes[i].GetVisibleIntervals(viewport));
                        break;
                }
                data.Add(lane.GetAllIntervals().ToList());
            }
            LastFetchedData = data;
            LastFetchedInterval = viewport;
            return data;
        }

        public void Start(int tick)
        {
            if (recordingMode == RecordingModeType.RECORDING_DISABLED) return;

            IsRecording = true;
            RecordingInterval = (tick, tick);
            for (var i = 0; i < RecordingLanes.Count(); i++)
            {
                RecordingLanes[i].Clear();
            }

            if (InputMode == InputModeType.INPUT_HID_YUANCON)
            {
                YuanconHid.Connect();
            }
        }

        public void Update(int tick)
        {
            if (!IsRecording) return;
            RecordingInterval.EndTick = tick;
            var ioData = DoIO();
            for (var i = 0; i < RecordingLanes.Count(); i++)
            {
                RecordingLanes[i].Update(tick, ioData[i]);
            }
        }

        private List<bool> DoIO()
        {
            switch (InputMode)
            {
                case InputModeType.INPUT_KEYBOARD_YUANCON:
                    return KEYBOARD_YUANCON_LAYOUT.Select(p => Keyboard.IsKeyDown(p)).ToList();
                case InputModeType.INPUT_KEYBOARD_TASOLLER:
                    return KEYBOARD_TASOLLER_LAYOUT.Select(p => Keyboard.IsKeyDown(p)).ToList();
                case InputModeType.INPUT_HID_YUANCON:
                    return YuanconHid.Read();
                case InputModeType.INPUT_HID_TASOLLER:
                    return KEYBOARD_YUANCON_LAYOUT.Select(p => false).ToList();
            }
            throw new ArgumentException();
        }

        public void Stop()
        {
            if (!IsRecording) return;
            IsRecording = false;

            if (InputMode == InputModeType.INPUT_HID_YUANCON)
            {
                YuanconHid.Disconnect();
            }

            for (var i = 0; i < RecordingLanes.Count(); i++)
            {
                RecordingLanes[i].Update(RecordingInterval.EndTick, false);
                switch (recordingMode)
                {
                    case RecordingModeType.RECORDING_OVERWRITE:
                        RecordingLanes[i].AddIntervals(PlaybackLanes[i].GetVisibleIntervals((0, RecordingInterval.StartTick)));
                        RecordingLanes[i].AddIntervals(PlaybackLanes[i].GetVisibleIntervals((RecordingInterval.EndTick, TICK_INF)));

                        PlaybackLanes[i] = new PlaybackLane();
                        PlaybackLanes[i].AddIntervals(RecordingLanes[i].GetAllIntervals());
                        break;
                    case RecordingModeType.RECORDING_ADD:
                        PlaybackLanes[i].AddIntervals(RecordingLanes[i].GetAllIntervals());
                        break;
                }
            }
            RecordingInterval = (0, 0);
            LastFetchedData = null;
        }

        public void Clear()
        {
            LastFetchedData = null;
            for (var i = 0; i < RecordingLanes.Count(); i++)
            {
                RecordingLanes[i].Clear();
                PlaybackLanes[i].Clear();
            }
        }

        public enum RecordingModeType
        {
            RECORDING_DISABLED,
            RECORDING_OVERWRITE,
            RECORDING_ADD
        }

        public enum InputModeType
        {
            INPUT_KEYBOARD_YUANCON,
            INPUT_KEYBOARD_TASOLLER,
            INPUT_HID_YUANCON,
            INPUT_HID_TASOLLER
        }

        private class PlaybackLane
        {
            protected List<Interval> intervals = new List<Interval>();
            private int startIndex = 0;
            private int endIndex = 0;

            public PlaybackLane() { }

            public IEnumerable<Interval> GetAllIntervals()
            {
                return intervals.AsEnumerable();
            }
            public IEnumerable<Interval> GetVisibleIntervals(Interval viewport)
            {
                if (!viewport.IsValid) return new Interval[] { };
                while (startIndex != 0 && !intervals[startIndex-1].IsBefore(viewport.StartTick)) startIndex--;
                while (startIndex < intervals.Count() && intervals[startIndex].IsBefore(viewport.StartTick)) startIndex++;
                while (endIndex != 0 && intervals[endIndex - 1].IsAfter(viewport.EndTick)) endIndex--;
                while (endIndex < intervals.Count() && !intervals[endIndex].IsAfter(viewport.EndTick)) endIndex++;

                var slice = intervals.Skip(startIndex).Take(endIndex - startIndex);
                if (slice.Count() != 0)
                {
                    var head = slice.First();
                    Interval newHead = (Math.Max(head.StartTick, viewport.StartTick), head.EndTick);
                    slice = (new[] { newHead }).Concat(slice.Skip(1));
                    var tail = slice.Last();
                    Interval newTail = (tail.StartTick, Math.Min(tail.EndTick, viewport.EndTick));
                    slice = slice.Take(slice.Count() - 1).Concat(new[] { newTail });
                }
                return slice;
            }
            public void Clear()
            {
                intervals.Clear();
                startIndex = 0;
                endIndex = 0;
            }

            public void AddIntervals(IEnumerable<Interval> other)
            {
                if (other.Count() == 0) return;

                var allIntervals = intervals.Concat(other).Where(x => x.IsValid).ToList();
                allIntervals.Sort((x, y) =>
                {
                    return x.EndTick.CompareTo(y.EndTick);
                });

                var cleanedIntervals = new List<Interval> { };
                foreach(var interval in allIntervals)
                {
                    var accumulatorInterval = interval;
                    while (cleanedIntervals.Count() != 0 && cleanedIntervals.Last().CanMerge(accumulatorInterval))
                    {
                        accumulatorInterval = cleanedIntervals.Last().Merge(accumulatorInterval);
                        cleanedIntervals.RemoveAt(cleanedIntervals.Count() - 1);
                    }
                    cleanedIntervals.Add(accumulatorInterval);
                    /*
                    if (cleanedIntervals.Count() == 0 || !cleanedIntervals.Last().CanMerge(interval))
                    {
                        cleanedIntervals.Add(interval);
                    } else {
                        cleanedIntervals[cleanedIntervals.Count()-1].MergeInplace(interval);
                    }
                    */
                }

                intervals = cleanedIntervals;
            }
        }

        private class RecordingLane: PlaybackLane
        {
            private bool recording = false;
            
            public void Update(int tick, bool active)
            {
                if (active && !recording)
                {
                    recording = true;
                    intervals.Add((tick-1, tick));
                }

                if (recording)
                {
                    intervals[intervals.Count()-1] = (intervals[intervals.Count() - 1].StartTick, tick);
                }

                if (!active && recording)
                {
                    recording = false;
                }
            }
        }

        public struct Interval
        {
            public int StartTick;
            public int EndTick;

            (int, int) Ticks
            {
                get { return (StartTick, EndTick); }
                set {
                    StartTick = value.Item1;
                    EndTick = value.Item2;
                }
            }
            public bool IsValid { get { return StartTick < EndTick; } }
            public int Duration { get { return EndTick - StartTick; } }

            public static implicit operator Interval((int, int) ticks) => new Interval
            {
                StartTick = ticks.Item1,
                EndTick = ticks.Item2
            };

            public bool Equals(Interval other) => Ticks == other.Ticks;
            public bool IsAfter(int tick) => tick < StartTick;
            public bool IsInside(int tick) => StartTick <= tick && tick < EndTick;
            public bool IsBefore(int tick) => EndTick <= tick;

            public bool CanMerge(Interval other)
            {
                if (other.EndTick < StartTick) return false;
                if (EndTick < other.StartTick) return false;
                return true;
            }
            public Interval Merge(Interval other)
            {
                if (!CanMerge(other)) throw new ArgumentException("Merging invalid intervals");
                return (Math.Min(StartTick, other.StartTick), Math.Max(EndTick, other.EndTick));
            }
            public void MergeInplace(Interval other) => Ticks = Merge(other).Ticks;

            public bool CanMask(Interval other)
            {
                if (other.EndTick <= StartTick) return false;
                if (EndTick <= other.StartTick) return false;
                return true;
            }
            public Interval Mask(Interval other)
            {
                if (!CanMask(other)) return (0, 0);
                return (Math.Max(StartTick, other.StartTick), Math.Min(EndTick, other.EndTick));
            }
            public void MaskInplace(Interval other) => Ticks = Mask(other).Ticks;
        }
    }
}
