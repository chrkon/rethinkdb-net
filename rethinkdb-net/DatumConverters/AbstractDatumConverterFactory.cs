﻿using System;
using RethinkDb.Spec;
using System.Reflection;

namespace RethinkDb.DatumConverters
{
    public abstract class AbstractDatumConverterFactory : IDatumConverterFactory
    {
        public abstract bool TryGet<T>(IDatumConverterFactory rootDatumConverterFactory, out IDatumConverter<T> datumConverter);

        // This is really ugly, using a helper class and reflection to call the generic TryGet<T> method.  But,
        // I can't see any alternative due to the generic out parameter, and I'm making the assumptions that
        // (a) non-generic version of TryGet will be less frequently used than the generic version, and (b) the
        // generic version is easier to write, so the non-generic version should be the uglier one.
        public bool TryGet(Type datumType, IDatumConverterFactory rootDatumConverterFactory, out IDatumConverter datumConverter)
        {
            var helperType = typeof(GenericHelper<>).MakeGenericType(datumType);
            var helperMethod = helperType.GetMethod("TryGet", BindingFlags.Public | BindingFlags.Static);
            var retval = (Tuple<bool, IDatumConverter>)helperMethod.Invoke(null, new object[] { this, rootDatumConverterFactory });
            datumConverter = retval.Item2;
            return retval.Item1;
        }

        private static class GenericHelper<T>
        {
            public static Tuple<bool, IDatumConverter> TryGet(IDatumConverterFactory target, IDatumConverterFactory rootDatumConverterFactory)
            {
                IDatumConverter<T> datumConverter;
                bool success = target.TryGet<T>(rootDatumConverterFactory, out datumConverter);
                return new Tuple<bool, IDatumConverter>(success, datumConverter);
            }
        }

        public Type GetBestNativeTypeForDatum(Spec.Datum datum)
        {
            // Attempt to auto-detect the best native type for a given Datum.

            switch (datum.type)
            {
                case Datum.DatumType.R_ARRAY:
                case Datum.DatumType.R_BOOL:
                case Datum.DatumType.R_JSON:
                case Datum.DatumType.R_NULL:
                case Datum.DatumType.R_NUM:
                    throw new RethinkDbRuntimeException("I don't know what the best native type for this RethinkDB type is.");
                case Datum.DatumType.R_OBJECT:
                    return typeof(object);
                case Datum.DatumType.R_STR:
                    return typeof(string);
                default:
                    throw new RethinkDbInternalErrorException("Unrecognized datum type");
            }
        }
    }
}
