using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace AliGame.Items
{
    /// <summary>
    /// All the "juice" of the cutting minigame: camera shake, a quick full-screen color flash and a burst of chips
    /// at the cut point. Put it next to a CutMinigame; it listens to its events, so there is nothing to call by hand.
    /// The window opening (the "now!" moment) only gets a small spark, no shake or flash, so the two real beats
    /// (a good cut and a miss) are the ones that read as an impact.
    /// </summary>
    [RequireComponent(typeof(CutMinigame))]
    public class CutFeedbackFx : MonoBehaviour
    {
        [Header("Where")]
        [Tooltip("Offset from the player where the burst and flash appear to happen.")]
        [SerializeField] private Vector2 effectOffset = new Vector2(0f, 1.3f);

        [Header("Window opens (the cue to release)")]
        [SerializeField] private Color sparkColor = new Color(1f, 0.93f, 0.7f, 1f);
        [SerializeField, Min(0)] private int sparkCount = 5;
        [SerializeField, Min(0f)] private float sparkSpeed = 1.3f;
        [SerializeField, Min(0f)] private float sparkSize = 0.08f;

        [Header("Good cut")]
        [SerializeField] private CinemachineImpulseDefinition.ImpulseShapes successShakeShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        [SerializeField, Min(0f)] private float successShakeForce = 0.45f;
        [SerializeField, Min(0.02f)] private float successShakeDuration = 0.18f;
        [SerializeField, Min(0.1f)] private float successShakeFrequency = 3f;
        [SerializeField] private Color successFlashColor = new Color(1f, 0.85f, 0.55f, 0.28f);
        [SerializeField] private Color successBurstColor = new Color(0.94f, 0.66f, 0.23f, 1f);
        [SerializeField, Min(0)] private int successBurstCount = 16;
        [SerializeField, Min(0f)] private float successBurstSpeed = 4f;
        [SerializeField, Min(0f)] private float successBurstSize = 0.14f;

        [Header("Miss")]
        [SerializeField] private CinemachineImpulseDefinition.ImpulseShapes missShakeShape = CinemachineImpulseDefinition.ImpulseShapes.Recoil;
        [SerializeField, Min(0f)] private float missShakeForce = 0.22f;
        [SerializeField, Min(0.02f)] private float missShakeDuration = 0.22f;
        [SerializeField, Min(0.1f)] private float missShakeFrequency = 2f;
        [SerializeField] private Color missFlashColor = new Color(0.75f, 0.2f, 0.15f, 0.26f);
        [SerializeField] private Color missBurstColor = new Color(0.55f, 0.5f, 0.46f, 1f);
        [SerializeField, Min(0)] private int missBurstCount = 7;
        [SerializeField, Min(0f)] private float missBurstSpeed = 2f;
        [SerializeField, Min(0f)] private float missBurstSize = 0.1f;

        [Header("Finishing the recipe")]
        [Tooltip("Multiplies the good-cut burst and flash on the very last cut, for a bigger payoff.")]
        [SerializeField, Min(1f)] private float finishBurstMultiplier = 1.6f;

        [Header("Shared burst settings")]
        [SerializeField, Min(0.05f)] private float burstLifetime = 0.35f;
        [SerializeField, Min(0f)] private float burstGravity = 2.5f;
        [SerializeField, Min(0.02f)] private float flashDuration = 0.14f;

        private static Texture2D _dotTexture;

        private CutMinigame _minigame;
        private CinemachineImpulseSource _impulse;
        private ParticleSystem _particles;
        private Image _flashImage;
        private bool _flashActive;
        private float _flashStart;
        private float _flashPeakAlpha;
        private Color _flashColor;

        private void Awake()
        {
            _minigame = GetComponent<CutMinigame>();
            SetupShake();
            BuildParticles();
            BuildFlash();
        }

        private void OnEnable()
        {
            _minigame.WindowOpened += OnWindowOpened;
            _minigame.CutSucceeded += OnCutSucceeded;
            _minigame.CutMissed += OnCutMissed;
            _minigame.AllFinished += OnAllFinished;
        }

        private void OnDisable()
        {
            _minigame.WindowOpened -= OnWindowOpened;
            _minigame.CutSucceeded -= OnCutSucceeded;
            _minigame.CutMissed -= OnCutMissed;
            _minigame.AllFinished -= OnAllFinished;
        }

        private void Update()
        {
            if (!_flashActive) return;

            float t = (Time.unscaledTime - _flashStart) / flashDuration;
            if (t >= 1f)
            {
                _flashActive = false;
                _flashImage.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0f);
                return;
            }

            float alpha = _flashPeakAlpha * (1f - t);
            _flashImage.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, alpha);
        }

        private bool _finishing;

        private void OnWindowOpened()
        {
            Burst(sparkColor, sparkCount, sparkSpeed, sparkSize);
        }

        private void OnCutSucceeded(int cutsDone)
        {
            // AllFinished fires right after the last CutSucceeded; wait one frame so the bigger payoff replaces this one.
            if (cutsDone >= _minigame.RequiredCuts)
            {
                _finishing = true;
                return;
            }

            Shake(successShakeShape, successShakeForce, successShakeDuration, successShakeFrequency);
            Flash(successFlashColor);
            Burst(successBurstColor, successBurstCount, successBurstSpeed, successBurstSize);
        }

        private void OnAllFinished()
        {
            if (!_finishing) return;
            _finishing = false;

            Shake(successShakeShape, successShakeForce * finishBurstMultiplier, successShakeDuration, successShakeFrequency);
            Flash(Scale(successFlashColor, finishBurstMultiplier));
            Burst(successBurstColor, Mathf.RoundToInt(successBurstCount * finishBurstMultiplier), successBurstSpeed, successBurstSize);
        }

        private void OnCutMissed()
        {
            Shake(missShakeShape, missShakeForce, missShakeDuration, missShakeFrequency);
            Flash(missFlashColor);
            Burst(missBurstColor, missBurstCount, missBurstSpeed, missBurstSize);
        }

        private static Color Scale(Color color, float alphaMultiplier)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * alphaMultiplier));
        }

        private void Flash(Color color)
        {
            if (_flashImage == null || color.a <= 0f) return;

            _flashColor = color;
            _flashPeakAlpha = color.a;
            _flashStart = Time.unscaledTime;
            _flashActive = true;
            _flashImage.color = color;
        }

        private void Burst(Color color, int count, float speed, float size)
        {
            if (_particles == null || count <= 0) return;

            Vector3 position = EffectPosition();
            ParticleSystem.MainModule main = _particles.main;
            main.startColor = color;
            main.startSpeed = speed;
            main.startSize = size;
            main.startLifetime = burstLifetime;
            main.gravityModifier = burstGravity;

            _particles.transform.position = position;
            _particles.Emit(count);
        }

        private Vector3 EffectPosition()
        {
            Transform player = _minigame.PlayerTransform;
            Vector2 basePos = player != null ? (Vector2)player.position : (Vector2)transform.position;
            return new Vector3(basePos.x + effectOffset.x, basePos.y + effectOffset.y, 0f);
        }

        private void Shake(CinemachineImpulseDefinition.ImpulseShapes shape, float force, float duration, float frequency)
        {
            if (_impulse == null || force <= 0f) return;

            CinemachineImpulseDefinition definition = _impulse.ImpulseDefinition;
            definition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            definition.ImpulseShape = shape;
            definition.ImpulseDuration = duration;
            definition.FrequencyGain = frequency;
            definition.AmplitudeGain = 1f;
            definition.TimeEnvelope.AttackTime = 0.01f;
            definition.TimeEnvelope.SustainTime = 0f;
            definition.TimeEnvelope.DecayTime = duration;

            _impulse.GenerateImpulseWithVelocity(new Vector3(force, force, 0f));
        }

        private void SetupShake()
        {
            _impulse = gameObject.AddComponent<CinemachineImpulseSource>();

            foreach (CinemachineCamera cam in FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var listener = cam.GetComponent<CinemachineImpulseListener>();
                if (listener == null) listener = cam.gameObject.AddComponent<CinemachineImpulseListener>();

                // A listener added from code starts with Gain 0 and no channel, which silently ignores every impulse.
                listener.Gain = 1f;
                listener.ChannelMask = 1;
                listener.Use2DDistance = true;
                listener.ReactionSettings.AmplitudeGain = 1f;
                listener.ReactionSettings.FrequencyGain = 1f;
            }
        }

        private void BuildParticles()
        {
            var go = new GameObject("CutBurst");
            go.transform.SetParent(transform, false);

            _particles = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation3D = false;

            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;
            shape.arc = 360f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 30;
            renderer.material = BuildParticleMaterial();
        }

        private static Material BuildParticleMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");

            var material = new Material(shader);
            Texture2D dot = DotTexture();
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", dot);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", dot);
            return material;
        }

        /// <summary>A small soft-edged white circle, generated once and shared by every CutFeedbackFx.</summary>
        private static Texture2D DotTexture()
        {
            if (_dotTexture != null) return _dotTexture;

            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float half = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            _dotTexture = tex;
            return tex;
        }

        private void BuildFlash()
        {
            var canvasObject = new GameObject("CutFlash", typeof(Canvas), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasObject.GetComponent<CanvasGroup>().blocksRaycasts = false;

            var imageObject = new GameObject("Flash", typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            _flashImage = imageObject.GetComponent<Image>();
            _flashImage.raycastTarget = false;
            _flashImage.color = new Color(1f, 1f, 1f, 0f);

            var rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
