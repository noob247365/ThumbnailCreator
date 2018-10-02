using System;

namespace ThumbnailCreator
{
    #region Drawing enums

    /// <summary>
    /// Horizontal alignment for drawing text
    /// </summary>
    public enum HAlign: int
    {
        /// <summary>
        /// X position specifies the left of the text
        /// </summary>
        Left,

        /// <summary>
        /// X position specifies the center of the text
        /// </summary>
        Center,

        /// <summary>
        /// X position specifies the right of the text
        /// </summary>
        Right
    }

    /// <summary>
    /// Vertical alignment for drawing text
    /// </summary>
    public enum VAlign: int
    {
        /// <summary>
        /// Y position specifies the top of the text
        /// </summary>
        Top,

        /// <summary>
        /// Y position specifies the middle of the text
        /// </summary>
        Middle,
        
        /// <summary>
        /// Y position specifies the bottom of the text
        /// </summary>
        Bottom
    }

    #endregion
}