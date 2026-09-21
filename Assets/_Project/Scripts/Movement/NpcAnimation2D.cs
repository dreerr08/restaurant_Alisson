using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AliGame.Movement
{
    /// <summary>
    /// Plays the idle or walk clip assigned below depending on whether the NpcWander2D is walking,
    /// cross-fading between them. Clips are played directly, so no Animator Controller states are needed.
    /// </summary>
    [RequireComponent(typeof(Animator), typeof(NpcWander2D))]
    public class NpcAnimation2D : MonoBehaviour
    {
        public AnimationClip idleClip;
        public AnimationClip walkClip;
        public float crossFadeTime = 0.15f;

        private const int Idle = 0;
        private const int Walk = 1;

        private readonly AnimationClipPlayable[] _playables = new AnimationClipPlayable[2];
        private readonly AnimationClip[] _clips = new AnimationClip[2];
        private readonly float[] _weights = new float[2];

        private NpcWander2D _wander;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private int _state = Idle;

        private void Awake()
        {
            _wander = GetComponent<NpcWander2D>();
            _clips[Idle] = idleClip;
            _clips[Walk] = walkClip;

            _graph = PlayableGraph.Create("NpcAnimation2D");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, 2);

            for (int i = 0; i < 2; i++)
            {
                if (_clips[i] == null) continue;
                _playables[i] = AnimationClipPlayable.Create(_graph, _clips[i]);
                _graph.Connect(_playables[i], 0, _mixer, i);
            }

            var output = AnimationPlayableOutput.Create(_graph, "Animation", GetComponent<Animator>());
            output.SetSourcePlayable(_mixer);

            _weights[Idle] = 1f;
            ApplyWeights();
            _graph.Play();
        }

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        private void Update()
        {
            int wanted = _wander.IsWalking ? Walk : Idle;
            if (wanted != _state)
            {
                _state = wanted;
                if (_clips[_state] != null) _playables[_state].SetTime(0d);
            }

            LoopCurrentClip();
            FadeWeights();
        }

        private void LoopCurrentClip()
        {
            AnimationClip clip = _clips[_state];
            if (clip == null || clip.length <= 0f) return;

            double time = _playables[_state].GetTime();
            if (time >= clip.length) _playables[_state].SetTime(time % clip.length);
        }

        private void FadeWeights()
        {
            float step = crossFadeTime > 0f ? Time.deltaTime / crossFadeTime : 1f;
            float sum = 0f;

            for (int i = 0; i < 2; i++)
            {
                _weights[i] = Mathf.MoveTowards(_weights[i], i == _state ? 1f : 0f, step);
                sum += _weights[i];
            }

            if (sum > 0f)
            {
                for (int i = 0; i < 2; i++) _weights[i] /= sum;
            }

            ApplyWeights();
        }

        private void ApplyWeights()
        {
            for (int i = 0; i < 2; i++)
                _mixer.SetInputWeight(i, _clips[i] != null ? _weights[i] : 0f);
        }
    }
}
