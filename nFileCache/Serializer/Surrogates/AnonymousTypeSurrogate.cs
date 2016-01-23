using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.Serialization;

namespace System.Runtime.Caching
{
    internal class AnonymousTypeSurrogate : ISerializationSurrogate
    {
        /// <summary>
        /// Manually add objects to the <see cref="SerializationInfo"/> store.
        /// </summary>
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.CanRead)
                {
                    info.AddValue(property.Name, property.GetValue(obj, null));
                }
            }
        }

        /// <summary>
        /// Retrieves objects from the <see cref="SerializationInfo"/> store.
        /// </summary>
        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            var expandoObject = (IDictionary<string, object>)new ExpandoObject();

            var enumerator = info.GetEnumerator();
            while (enumerator.MoveNext())
            {
                expandoObject.Add(enumerator.Name, enumerator.Value);
            }

            return expandoObject;
        }
    }
}