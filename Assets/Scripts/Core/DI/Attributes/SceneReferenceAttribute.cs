using System;

namespace ParaMoon
{

    /// <summary>
    /// Injects a reference to an object in another scene
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SceneReferenceAttribute : Attribute
    {
        /// <summary>
        /// ID of the object to inject
        /// </summary>
        public string TargetId { get; }

        /// <summary>
        /// Name of the scene containing the object. If empty, searches all scenes.
        /// </summary>
        public string SceneName { get; }

        /// <summary>
        /// If true, won't log an error if the reference isn't found
        /// </summary>
        public bool Optional { get; }

        public SceneReferenceAttribute(string targetId, string sceneName = "", bool optional = false)
        {
            TargetId = targetId;
            SceneName = sceneName;
            Optional = optional;
        }
    }
}