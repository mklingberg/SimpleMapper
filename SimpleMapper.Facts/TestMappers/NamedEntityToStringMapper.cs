using System;
using SimpleMapper.Facts.TestObjects;

namespace SimpleMapper.Facts.TestMappers
{
    public class NamedEntityToStringMapper : Mapper
    {
        public static readonly Func<INamedEntity, string> InterfaceToStringConversion = x => x.Name;

        public NamedEntityToStringMapper()
        {
            Configure.WithConversion(InterfaceToStringConversion);
        }
    }
}