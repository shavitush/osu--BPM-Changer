using System;

namespace NAudio.Dmo
{
    /// <summary>
    ///     DMO_PARTIAL_MEDIATYPE
    /// </summary>
    internal struct DmoPartialMediaType
    {
        private Guid subtype;
        private Guid type;

        public Guid Type
        {
            get { return type; }
            internal set { type = value; }
        }

        public Guid Subtype
        {
            get { return subtype; }
            internal set { subtype = value; }
        }
    }
}