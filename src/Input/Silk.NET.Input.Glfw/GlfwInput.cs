// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Silk.NET.Windowing.Glfw;

namespace Silk.NET.Input.Glfw
{
    public static class GlfwInput
    {
        public static void RegisterPlatform()
        {
            GlfwWindowing.RegisterPlatform(); // just in case it's not already
            if (!InputWindowExtensions._platforms.OfType<GlfwInputPlatform>().Any())
            {
                InputWindowExtensions.Add(new GlfwInputPlatform());
            }
        }

        /// <summary>
        /// Registers this input platform and stops the first-party platforms from being
        /// discovered via reflection.
        /// </summary>
        /// <remarks>
        /// Pair this with <see cref="GlfwWindowing.Use"/> to keep every backend off the
        /// reflection path, which is what lets the unused backends be trimmed away.
        /// </remarks>
        public static void Use() // for consistency with windowing
        {
            InputWindowExtensions.ShouldLoadFirstPartyPlatforms(false);
            RegisterPlatform();
        }
    }
}
