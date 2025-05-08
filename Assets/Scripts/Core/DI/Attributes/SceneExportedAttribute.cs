using System;

namespace ParaMoon
{
    /// <summary>
    /// Marks a component as available for cross-scene referencing
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SceneExportedAttribute : Attribute
    {
        /// <summary>
        /// ID used to reference this object from other scenes. If empty, the GameObject name is used.
        /// </summary>
        public string Id { get; }

        public SceneExportedAttribute(string id = "")
        {
            Id = id;
        }
    }
}