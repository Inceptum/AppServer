using System;
using ProtoBuf;

namespace Inceptum.AppServer.Monitoring
{
    /// <summary>
    /// Host HeartBeat message. Extends HbMessage with running services list
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class HostHbMessage 
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        [ProtoMember(1)]
        public string[] Services { get;  set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostHbMessage"/> class.
        /// </summary>
        public HostHbMessage()
        {
            MachineName = Environment.MachineName;
            SendDate = DateTime.Now;
        }

        /// <summary>
        /// Gets or sets the name of the instance.
        /// </summary>
        /// <value>
        /// The name of the instance.
        /// </value>
        [ProtoMember(2)]
        public string InstanceName { get; set; }
        /// <summary>
        /// Gets the message send date.
        /// </summary>
        [ProtoMember(3)]
        public DateTime SendDate { get; internal set; }
        /// <summary>
        /// Gets or sets the HB period.
        /// </summary>
        /// <value>
        /// The period.
        /// </value>
        [ProtoMember(4)]
        public long Period { get; set; }
        /// <summary>
        /// Gets the name of the machine.
        /// </summary>
        /// <value>
        /// The name of the machine.
        /// </value>
        [ProtoMember(5)]
        public string MachineName { get; internal set; }
    }
}