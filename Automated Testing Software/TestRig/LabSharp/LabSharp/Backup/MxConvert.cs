/*
 * Lab# - Matlab interaction library for .Net
 * 
 * Copyright (C) 2005 Julien Roncaglia
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.

 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace LabSharp
{
    public static partial class MxConvert
    {
        /// <summary>
        /// Cas sp�cial pour les strings, utile car si non les strings ne sont que
        /// des char[].
        /// 
        /// </summary>
        /// <remarks>
        /// <code></code>
        /// <list type=""
        /// </remarks>
        /// <param name="array">Un mxArray de type Char</param>
        /// <returns>Les caract�res du tableau de charact�re contenu dans
        /// <paramref name="array"/> sous forme de string.</returns>
        static string ToString(MxArray array)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (array.Class != ClassID.Char)
                throw new InvalidCastException("Char mxArray required");

            return array.StringValue;
        }

        public static MxArray ToMxArray<TType>(TType value)
        {
            if (typeof(TType) == typeof(string))
            {
                return MxArray.CreateString((string)(Object)value);
            }
            return _ToMxArray<TType>(value);
        }

        public static TType FromMxArray<TType>(MxArray array)
        {
            return FromMxArray<TType>(array, false);
        }

        public static TType FromMxArray<TType>(MxArray array, bool noVectorization)
        {
            ClassID classId = array.Class;
            int ndims = array.NumberOfDimensions;

            Type genericType = typeof(TType);
            // There is two sorts of Array types : the ones that answer true to IsArray, where we could
            // get dimensions, element type and other details; and the Array class that could contain
            // any array.
            //bool isArray = genericType.IsArray;
            bool isArrayClass = genericType == typeof(Array);
            bool isObjectClass = genericType == typeof(Object);
            bool isStringClass = genericType == typeof(String);

            if (isStringClass)
            {
                return (TType)(Object)ToString(array);
            }
            else if (isArrayClass)
            {
                return (TType)(Object)_ConvertToArray(array, classId, ndims);
            }
            else if (isObjectClass)
            {
                // Try to find the C# type that match the mxArray
                if (classId == ClassID.Char)
                {
                    return (TType)(Object)ToString(array);
                }
                else if (array.NumberOfElements == 1)
                {
                    return (TType)_ConvertToBasicType(array, classId);
                }
                else
                {
                    return (TType)(Object)_ConvertToArray(array, classId, ndims);
                }
            }
            else
            {
                // Try to convert the mxArray to the specified C# type
                return _ConvertToSomeType<TType>(array, classId, ndims, noVectorization);
            }
        }

        static void ExtractTypeInfos(Type type, out Type insideType, out bool isArray,
            out bool isComplex)
        {
            isArray = type.IsArray;
            if (isArray)
            {
                insideType = type.GetElementType();
            }
            else
            {
                insideType = type;
            }
            isComplex = insideType.IsGenericType
                && (insideType.GetGenericTypeDefinition() == typeof(Complex<>));
            if (isComplex)
            {
                insideType = insideType.GetGenericArguments()[0];
			}
        }
    }
}
