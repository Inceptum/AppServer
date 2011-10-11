namespace Inceptum.AppServer.Configuration
{
    /// <summary>
    /// Stores connection string. Dedicated type is requerid to simplefy IoC dependency resolving.
    /// </summary>
    public struct ConnectionString
    {
        private readonly string m_Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionString"/> struct.
        /// </summary>
        public ConnectionString(string connectionString)
            : this()
        {
            m_Value = connectionString;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="ConnectionString"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(ConnectionString connectionString)
        {
            return connectionString.ToString();
        }


        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="ConnectionString"/>.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator ConnectionString(string connectionString)
        {
            return new ConnectionString(connectionString);
        }

        /// <summary>
        /// Equalses the specified other.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public bool Equals(ConnectionString other)
        {
            return Equals(other.m_Value, m_Value);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (ConnectionString)) return false;
            return Equals((ConnectionString) obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return (m_Value != null ? m_Value.GetHashCode() : 0);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return m_Value;
        }
    }
}