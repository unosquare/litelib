namespace Unosquare.Labs.LiteLib
{
    using System;

    /// <summary>
    /// Attribute for ignored properties.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property)]
    public class NotMappedAttribute : Attribute
    {
    }
}
