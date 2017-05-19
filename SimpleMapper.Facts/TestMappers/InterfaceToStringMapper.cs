using System;
using SimpleMapper.Facts.TestObjects;

namespace SimpleMapper.Facts.TestMappers
{
    public class InterfaceToStringMapper : Mapper
    {
        public static readonly Func<INamedEntity, string> InterfaceToStringConversion = x => x.Name;

        public InterfaceToStringMapper()
        {
            Configure.WithConversion(InterfaceToStringConversion);
        }
    }
}