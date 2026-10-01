// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Silk.NET.OpenAL.Attributes;
using Xunit;

namespace Silk.NET.OpenAL.Tests;

public class FormatHelperTests
{
    private enum AnnotatedFormat
    {
        [FormatSize(Size = 2)]
        Stereo16,

        [FormatSize]
        DefaultSized,

        Unannotated
    }

    [Fact]
    public void GetFormatSizeReadsTheAttribute()
    {
        Assert.Equal(2, FormatHelpers.GetFormatSize(AnnotatedFormat.Stereo16));
    }

    [Fact]
    public void GetFormatSizeDefaultsToOneByte()
    {
        Assert.Equal(1, FormatHelpers.GetFormatSize(AnnotatedFormat.DefaultSized));
    }

    [Fact]
    public void GetFormatSizeThrowsWhenTheAttributeIsMissing()
    {
        Assert.Throws<InvalidOperationException>
            (() => FormatHelpers.GetFormatSize(AnnotatedFormat.Unannotated));
    }

    [Fact]
    public void GetFormatSizeThrowsForAnUndefinedValue()
    {
        Assert.Throws<InvalidOperationException>
            (() => FormatHelpers.GetFormatSize((AnnotatedFormat) 9999));
    }

    [Fact]
    public void NoBuiltInBufferFormatDeclaresASize()
    {
        // Documents an upstream gap rather than endorsing it: nothing in the bindings
        // actually carries [FormatSize], so Capture's resizing overloads always throw.
        Assert.Throws<InvalidOperationException>
            (() => FormatHelpers.GetFormatSize(BufferFormat.Stereo16));
    }
}
