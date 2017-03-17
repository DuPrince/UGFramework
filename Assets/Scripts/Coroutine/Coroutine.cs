using System;
using System.Collections;
using UGFramework.Pool;

namespace UGFramework.Coroutine
{
    public enum Status
    {
        NULL = 1 << 0,
        WAITING = 1 << 1,
        BREAK = 1 << 2,
        FINISH = 1 << 3,

        END = BREAK | FINISH,
    }

    /**
     * --- DOC BEGIN ---
     * --- DOC END ---
     */
    public class Coroutine : IObject 
    {
        public CoroutineGroup Group { get; set; }
        public IEnumerator Routine { get; set; }

        public int CreatedFrameCount { get; private set; }

        public Status Status { get; private set; }
        public bool IsEnd { get { return ((int)this.Status & (int)Status.END) == 1; } }

        bool _noRoutine = false;

        public Coroutine(CoroutineGroup group, IEnumerator routine)
        {
            this.Group = group;
            this.Routine = routine;    
            this.CreatedFrameCount = UnityEngine.Time.frameCount;
        }

        public void Init() 
        { 
            this.Next(); 
            this.UpdateStatus(); 
        }
        public void Dispose() { this.Status = Status.NULL; _noRoutine = false; }

        void Next()
        {
            // LogManager.Instance.Debug("Routine is null({0}) frame({1})", this.Routine.Current == null, UnityEngine.Time.frameCount);
            _noRoutine = this.Routine.MoveNext();
            var routine = this.Routine.Current as YieldInstruction;
            routine.Coroutine = this; 
        }

        void UpdateStatus()
        {
            if (_noRoutine)
            {
                Status = Status.FINISH;
                return;
            }

            var routine = this.Routine.Current as YieldInstruction;
            if (routine == null) 
            {
                Status = Status.BREAK;
                return;
            }

            Status = routine.Status;
        }

        public void LateUpdate()
        {
            if (this.IsEnd)
                return;
            
            if (this.Routine.Current is YieldInstruction == false)
            {
                throw new Exception(string.Format("Unkown class({0}), coroutine must return YieldInstruction!", this.Routine.Current.GetType().FullName));
            }

            // LateUpdate routine
            var routine = this.Routine.Current as YieldInstruction;
            routine.LateUpdate();

            if (routine.Status == Status.FINISH)
                this.Next();

            this.UpdateStatus();
        }
    }
}