﻿// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using Datasync.Common.Test;
using Datasync.Common.Test.Models;
using Microsoft.Datasync.Client;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Datasync.Integration.Test.Client.OfflineTableOfT
{
    [ExcludeFromCodeCoverage]
    public class GetItemAsync_Tests : BaseOperationTest
    {
        public GetItemAsync_Tests(ITestOutputHelper logger) : base(logger) { }

        [Fact]
        [Trait("Method", "GetItemAsync")]
        public async Task GetItemAsync_Basic()
        {
            await InitializeAsync();

            // Arrange
            var id = GetRandomId();
            var expected = MovieServer.GetMovieById(id)!;

            // Act
            var response = await table!.GetItemAsync(id);

            // Assert
            AssertEx.SystemPropertiesMatch(expected, response);
            Assert.Equal<IMovie>(expected, response);
        }

        [Fact]
        [Trait("Method", "GetItemAsync")]
        public async Task GetItemAsync_NotFound()
        {
            await InitializeAsync();

            // Arrange
            const string id = "not-found";

            // Act
            var response = await table!.GetItemAsync(id);

            // Assert
            Assert.Null(response);
        }
    }
}
