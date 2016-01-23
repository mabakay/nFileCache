using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.Caching
{
    public class AnonymousTypeSurrogateSelector : SurrogateSelector
    {
        private ISerializationSurrogate _anonymousTypeSurrogate;

        public ISerializationSurrogate AnonymousTypeSurrogate
        {
            get
            {
                if (_anonymousTypeSurrogate == null)
                {
                    _anonymousTypeSurrogate = new AnonymousTypeSurrogate();
                }

                return _anonymousTypeSurrogate;
            }
        }

        public override ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
        {
            var surrogate = base.GetSurrogate(type, context, out selector);

            if (surrogate == null)
            {
                bool isAnonymousType = Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                                    && type.IsGenericType && type.Name.Contains("AnonymousType")
                                    && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                                    && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;

                if (isAnonymousType)
                {
                    surrogate = AnonymousTypeSurrogate;
                }
            }

            return surrogate;
        }
    }
}