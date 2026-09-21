using UnityEngine;

namespace AliGame.Data
{
    /// <summary>Who is speaking: name, portrait and the color of the name plate in the dialogue box.</summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Ali/Dialogue Character")]
    public class DialogueCharacterSO : ScriptableObject
    {
        [SerializeField] private string displayName = "Personagem";
        [SerializeField] private Sprite portrait;
        [SerializeField] private Color accentColor = new Color(0.435f, 0.271f, 0.165f, 1f);
        [Tooltip("Characters revealed per second for this character. 0 uses the dialogue UI's default speed.")]
        [SerializeField, Min(0f)] private float typingSpeed;

        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
        public Color AccentColor => accentColor;
        public float TypingSpeed => typingSpeed;
    }
}
