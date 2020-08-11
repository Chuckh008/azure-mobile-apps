﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Zumo.MobileData.Internal
{
    /// <summary>
    /// Parameter validation utility methods.
    /// </summary>
    internal static class Arguments
    {
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the provided param is null
        /// </summary>
        /// <param name="param">the parameter value</param>
        /// <param name="paramName">the parameter name</param>
        internal static void IsNotNull(object param, string paramName)
        {
            if (param == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the provided param is empty.
        /// </summary>
        /// <param name="param">the parameter value</param>
        /// <param name="paramName">the parameter name</param>
        internal static void IsNotNullOrEmpty(string param, string paramName)
        {
            Arguments.IsNotNull(param, paramName);
            if (string.IsNullOrEmpty(param))
            {
                throw new ArgumentException("Parameter must not be empty", paramName);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the provided param is not an absolute Uri
        /// </summary>
        /// <param name="param">the parameter value</param>
        /// <param name="paramName">the parameter name</param>
        internal static void IsAbsoluteUri(Uri param, string paramName)
        {
            Arguments.IsNotNull(param, paramName);
            if (!param.IsAbsoluteUri)
            {
                throw new ArgumentException("Parameter must be an absolute Uri", paramName);
            }
        }
    }
}