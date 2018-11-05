namespace Unosquare.Labs.LiteLib
{
    using System;

    /// <summary>
    /// Attribute to represent a LiteLib Entity.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableAttribute"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public TableAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        /// <value>
        /// The table name.
        /// </value>
        public string Name { get; }
    }
}
