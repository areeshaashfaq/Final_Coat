using UnityEngine;

namespace FinalCoat{
    public abstract class EnemyState{
        protected readonly WindUpToy owner;
        protected float timeInState;
        protected EnemyState(WindUpToy owner) { this.owner = owner; }
        public abstract string Name { get; }
        public virtual void Enter() { timeInState = 0f; }
        public virtual void Tick(float dt) { timeInState += dt; }
        public virtual void FixedTick(float dt) { }
        public virtual void Exit() { }
    }

    public class PatrolState : EnemyState{
        int direction = 1;
        public PatrolState(WindUpToy owner) : base(owner) { }
        public override string Name => "Patrol";
        public override void Enter(){
            base.Enter();
            owner.Target = null;
            owner.KeySpinSpeed = 180f;
        }

        public override void Tick(float dt){
            base.Tick(dt);
            var seen = owner.FindNearestToy(owner.detectRange);
            if (seen != null){
                owner.Target = seen;
                owner.ChangeState(owner.Chase);
            }
        }

        public override void FixedTick(float dt){
            float offset = owner.transform.position.x - owner.Home.x;
            if (offset > owner.patrolDistance) direction = -1;
            else if (offset < -owner.patrolDistance) direction = 1;
            if (!owner.Walk(direction, owner.patrolSpeed)) direction = -direction;
        }
    }

    public class ChaseState : EnemyState{
        float retargetTimer;
        public ChaseState(WindUpToy owner) : base(owner) { }
        public override string Name => owner.Target != null ? $"Chase P{owner.Target.playerIndex + 1}" : "Chase";
        public override void Enter(){
            base.Enter();
            retargetTimer = 0f;
            owner.KeySpinSpeed = 540f;
        }

        public override void Tick(float dt){
            base.Tick(dt);
            retargetTimer -= dt;
            if (retargetTimer > 0f) return;
            retargetTimer = owner.retargetInterval;
            var nearest = owner.FindNearestToy(owner.loseRange);
            if (nearest == null){
                owner.ChangeState(owner.Return);
                return;
            }
            owner.Target = nearest;
        }

        public override void FixedTick(float dt){
            if (owner.Target == null) return;
            float dx = owner.Target.transform.position.x - owner.transform.position.x;
            int dir = Mathf.Abs(dx) < 0.1f ? 0 : (dx > 0f ? 1 : -1);
            owner.Walk(dir, owner.chaseSpeed);
        }
    }

    public class HitState : EnemyState{
        public HitState(WindUpToy owner) : base(owner) { }
        public override string Name => "Hit!";
        public override void Enter(){
            base.Enter();
            owner.KeySpinSpeed = 0f;
            owner.Stop();
        }

        public override void Tick(float dt){
            base.Tick(dt);
            if (timeInState >= owner.hitPause) owner.ChangeState(owner.WindDown);
        }
    }

    public class WindDownState : EnemyState{
        public WindDownState(WindUpToy owner) : base(owner) { }
        public override string Name => $"Wound down {Mathf.Max(0f, owner.windDownTime - timeInState):0.0}s";
        public override void Enter(){
            base.Enter();
            owner.KeySpinSpeed = 0f;
            owner.SetWoundDown(true);
        }

        public override void Tick(float dt){
            base.Tick(dt);
            if (timeInState >= owner.windDownTime) owner.ChangeState(owner.Return);
        }

        public override void Exit() => owner.SetWoundDown(false);
    }

    public class ReturnState : EnemyState{
        const float GiveUpTime = 6f;
        public ReturnState(WindUpToy owner) : base(owner) { }
        public override string Name => "Return";
        public override void Enter(){
            base.Enter();
            owner.Target = null;
            owner.KeySpinSpeed = 300f;
        }

        public override void FixedTick(float dt){
            float dx = owner.Home.x - owner.transform.position.x;
            if (Mathf.Abs(dx) < 0.2f){
                owner.Stop();
                owner.ChangeState(owner.Patrol);
                return;
            }
            bool walked = owner.Walk(dx > 0f ? 1 : -1, owner.returnSpeed);
            if (!walked || timeInState > GiveUpTime){
                owner.SetHome(owner.transform.position);
                owner.ChangeState(owner.Patrol);
            }
        }
    }
}
