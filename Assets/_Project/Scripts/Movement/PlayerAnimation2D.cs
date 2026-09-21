using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AliGame.Movement
{
    /// <summary>
    /// Plays the animation clips assigned below according to movement state:
    /// Idle / Run on the ground, and Jump Begin -> Fall -> Jump Land in the air.
    /// Clips are played directly (no Animator Controller states or parameters needed).
    /// </summary>
    [RequireComponent(typeof(Animator), typeof(Rigidbody2D), typeof(PlayerMovement2D))]
    public class PlayerAnimation2D : MonoBehaviour
    {
        public AnimationClip idleClip;
        public AnimationClip runClip;
        public AnimationClip jumpBeginClip;
        public AnimationClip fallClip;
        public AnimationClip jumpLandClip;
        [Tooltip("Cutting minigame: plays once when the key goes down, holding its last frame while the key stays held.")]
        public AnimationClip cutStartClip;
        [Tooltip("Cutting minigame: plays once when the key is released after a cut counted, holding its last frame.")]
        public AnimationClip cutFinalClip;

        public float runThreshold = 0.1f;
        public float airborneVelocityY = 0.1f;
        public float crossFadeTime = 0.05f;

        private enum State { Idle, Run, JumpBegin, Fall, Land, CutStart, CutFinal }

        private const int StateCount = 7;

        private Rigidbody2D _rb;
        private PlayerMovement2D _movement;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private readonly AnimationClipPlayable[] _playables = new AnimationClipPlayable[StateCount];
        private readonly AnimationClip[] _clips = new AnimationClip[StateCount];
        private readonly float[] _weights = new float[StateCount];
        private State _state = State.Idle;
        private float _stateStartTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _movement = GetComponent<PlayerMovement2D>();

            _clips[(int)State.Idle] = idleClip;
            _clips[(int)State.Run] = runClip;
            _clips[(int)State.JumpBegin] = jumpBeginClip;
            _clips[(int)State.Fall] = fallClip;
            _clips[(int)State.Land] = jumpLandClip;
            _clips[(int)State.CutStart] = cutStartClip;
            _clips[(int)State.CutFinal] = cutFinalClip;

            _graph = PlayableGraph.Create("PlayerAnimation2D");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, StateCount);

            for (int i = 0; i < StateCount; i++)
            {
                if (_clips[i] == null) continue;
                _playables[i] = AnimationClipPlayable.Create(_graph, _clips[i]);
                _graph.Connect(_playables[i], 0, _mixer, i);
            }

            var output = AnimationPlayableOutput.Create(_graph, "Animation", GetComponent<Animator>());
            output.SetSourcePlayable(_mixer);

            _weights[(int)State.Idle] = 1f;
            ApplyWeights();
            _graph.Play();
        }

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        private void Update()
        {
            Vector2 velocity = _rb.linearVelocity;
            bool grounded = _movement.IsGrounded && velocity.y <= airborneVelocityY;
            bool moving = Mathf.Abs(velocity.x) > runThreshold;
            float elapsed = Time.time - _stateStartTime;

            switch (_state)
            {
                case State.Idle:
                case State.Run:
                    if (!grounded) Enter(velocity.y > 0f ? State.JumpBegin : State.Fall);
                    else if (moving && _state != State.Run) Enter(State.Run);
                    else if (!moving && _state != State.Idle) Enter(State.Idle);
                    break;

                case State.JumpBegin:
                    if (grounded) Enter(State.Land);
                    else if (velocity.y <= 0f || elapsed >= LengthOf(State.JumpBegin)) Enter(State.Fall);
                    break;

                case State.Fall:
                    if (grounded) Enter(State.Land);
                    break;

                case State.Land:
                    if (!grounded) Enter(velocity.y > 0f ? State.JumpBegin : State.Fall);
                    else if (moving || elapsed >= LengthOf(State.Land)) Enter(moving ? State.Run : State.Idle);
                    break;

                case State.CutStart:
                case State.CutFinal:
                    break;
            }

            LoopCurrentClip();
            FadeWeights();
        }

        /// <summary>Length in seconds of the Cut Final clip (0 if it is not assigned).</summary>
        public float CutStartLength => LengthOf(State.CutStart);

        public float CutFinalLength => LengthOf(State.CutFinal);

        /// <summary>Plays the cut-start clip once and holds its last frame. Call StopCut to hand control back to movement.</summary>
        public void PlayCutStart()
        {
            if (cutStartClip != null) Enter(State.CutStart);
        }

        /// <summary>Plays the cut-finish clip once and holds its last frame.</summary>
        public void PlayCutFinal()
        {
            if (cutFinalClip != null) Enter(State.CutFinal);
        }

        /// <summary>Leaves the cut poses; the next Update picks Idle, Run or an air state again.</summary>
        public void StopCut()
        {
            if (_state == State.CutStart || _state == State.CutFinal) Enter(State.Idle);
        }

        private void Enter(State next)
        {
            _state = next;
            _stateStartTime = Time.time;

            int index = (int)next;
            if (_clips[index] != null)
            {
                _playables[index].SetTime(0d);
                _playables[index].SetSpeed(1d);
            }
        }

        private float LengthOf(State state)
        {
            AnimationClip clip = _clips[(int)state];
            return clip != null ? clip.length : 0f;
        }

        private void LoopCurrentClip()
        {
            if (_state == State.JumpBegin || _state == State.Land) return;

            if (_state == State.CutStart || _state == State.CutFinal)
            {
                HoldLastFrame((int)_state);
                return;
            }

            int index = (int)_state;
            AnimationClip clip = _clips[index];
            if (clip == null || clip.length <= 0f) return;

            double time = _playables[index].GetTime();
            if (time >= clip.length)
                _playables[index].SetTime(time % clip.length);
        }

        /// <summary>
        /// Freezes a one-shot clip on its last frame. The playable is paused (speed 0) one step before the end, because
        /// these clips are flagged as looping and one evaluation past the end would flash the first frame.
        /// </summary>
        private void HoldLastFrame(int index)
        {
            AnimationClip clip = _clips[index];
            if (clip == null || clip.length <= 0f) return;

            double last = clip.length - 0.001d;
            if (_playables[index].GetTime() + Time.deltaTime >= last)
            {
                _playables[index].SetTime(last);
                _playables[index].SetSpeed(0d);
            }
        }

        private void FadeWeights()
        {
            float step = crossFadeTime > 0f ? Time.deltaTime / crossFadeTime : 1f;
            float sum = 0f;

            for (int i = 0; i < StateCount; i++)
            {
                float target = i == (int)_state ? 1f : 0f;
                _weights[i] = Mathf.MoveTowards(_weights[i], target, step);
                sum += _weights[i];
            }

            if (sum > 0f)
            {
                for (int i = 0; i < StateCount; i++)
                    _weights[i] /= sum;
            }

            ApplyWeights();
        }

        private void ApplyWeights()
        {
            for (int i = 0; i < StateCount; i++)
                _mixer.SetInputWeight(i, _clips[i] != null ? _weights[i] : 0f);
        }
    }
}
