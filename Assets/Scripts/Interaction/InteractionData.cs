using UnityEngine;

namespace ParaMoon
{
    [System.Serializable]
    public class InteractionData
    {
        public string PromptText;
        public Sprite PromptIcon;
        public float InteractionDistance = 2.5f;
        public string InteractionSound;
        public InteractionType Type;
    }

    public enum InteractionType
    {
        Pickup,
        Use,
        Open,
        Move,
        Read,
        TalkTo,
        None
    }
}