using Attack.Cases;

namespace Experiments
{
    public struct Experiment
    {
        public readonly CaseChoice Choice;
        public readonly Direction Direction;
        public readonly int Rmax;
        public readonly int Frames;
        public readonly int ID;
        public readonly bool LogScreenshots;
        public readonly bool LogAttack;
        public readonly bool LogRTT;

        public Experiment
            (CaseChoice choice, Direction direction, int rmax, int frames, int id, bool logScreenshots, bool logAttack, bool logRTT)
        {
            Choice = choice;
            Direction = direction;
            Rmax = rmax;
            Frames = frames;
            ID = id;
            LogScreenshots = logScreenshots;
            LogAttack = logAttack;
            LogRTT = logRTT;

        }
    }
}
