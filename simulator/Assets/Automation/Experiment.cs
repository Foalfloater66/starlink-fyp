using System;
using Attack.Cases;

namespace Automation
{

    [System.Serializable]
    public class Experiment
    {
        public CaseChoice choice;
        public Direction direction;
        public int rMax;
        public int reps;
        [NonSerialized]
        public int Frames;
        [NonSerialized]
        public int ID;
        [NonSerialized]
        public bool LogScreenshots;
        [NonSerialized]
        public bool LogVideo;
        [NonSerialized]
        public bool LogAttack;
        [NonSerialized]
        public  bool LogRTT;

        [NonSerialized] public bool LogHops;

        /// <summary>
        /// Adds crucial runtime information not included in the serialized object.
        /// </summary>
        /// <param name="id">Run ID.</param>
        /// <param name="frames">Number of frames.</param>
        /// <param name="logScreenshots">If enabled, screenshots are taken at each frame.</param>
        /// <param name="logVideo">If enabled, the simulation is recorded.</param>
        /// <param name="logAttack">If enabled, the attack's performance is logged at each frame.</param>
        /// <param name="logRTT">If enabled, the RTT of all paths is logged at each frame.</param>
        public void Build(int id, int frames, bool logScreenshots, bool logVideo, bool logAttack, bool logRTT, bool logHops)
        {
            reps = 1;
            Frames = frames;
            ID = id;
            LogScreenshots = logScreenshots;
            LogVideo = logVideo;
            LogAttack = logAttack;
            LogRTT = logRTT;
            LogHops = logHops;
        }

        public Experiment(CaseChoice choice, Direction direction, int rMax, int id, int frames, bool logScreenshots,
            bool logVideo, bool logAttack, bool logRTT, bool logHops)
        {
            this.choice = choice;
            this.direction = direction;
            this.rMax = rMax;
            this.reps = 1;
            this.ID = id;
            this.Frames = frames;
            this.LogScreenshots = logScreenshots;
            this.LogVideo = logVideo;
            this.LogAttack = logAttack;
            this.LogRTT = logRTT;
            LogHops = logHops;
        }

        public Experiment Clone()
        {
            return new Experiment(choice, direction, rMax, ID, Frames, LogScreenshots, LogVideo, LogAttack, LogRTT, LogHops);
        }
    }
}
