using System;
using System.ComponentModel;
using System.Reflection;

namespace EmbedIO.WebApi.Internal
{
    internal class AdditionalParameterInfo
    {
        private readonly TypeConverter _converter;

        public AdditionalParameterInfo(ParameterInfo parameterInfo)
        {
            Info = parameterInfo;
            _converter = TypeDescriptor.GetConverter(parameterInfo.ParameterType);

            if (parameterInfo.ParameterType.IsValueType)
                Default = Activator.CreateInstance(parameterInfo.ParameterType);
        }

        public object Default { get; }
        public ParameterInfo Info { get; }

        public object GetValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                value = null; // ignore whitespace

            // convert and add to arguments, if null use default value
            return value == null ? Default : _converter.ConvertFromString(value);
        }
    }
}