﻿// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using Datasync.Common.Test;
using Datasync.Common.Test.Models;
using Microsoft.AspNetCore.Datasync;
using Microsoft.Datasync.Client;
using Microsoft.Datasync.Client.Utils;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Datasync.Integration.Test.Client.RemoteTable
{
    [ExcludeFromCodeCoverage(Justification = "Test suite")]
    [Collection("Integration")]
    public class RemoteTable_Patch_Tests : BaseTest
    {
        private readonly DatasyncClient client;
        private readonly IRemoteTable table;
        private readonly List<TableModifiedEventArgs> modifications = new();

        public RemoteTable_Patch_Tests()
        {
            client = GetMovieClient();
            table = client.GetRemoteTable("movies");
            table.TableModified += (sender, e) => modifications.Add(e);
        }

        #region Helper Methods
        /// <summary>
        /// Assert that the event handler was called with the right args.
        /// </summary>
        /// <param name="id"></param>
        private void AssertEventHandlerCalled(IRemoteTable table, string id)
        {
            Assert.Single(modifications);
            Assert.Equal(TableModifiedEventArgs.TableOperation.Replace, modifications[0].Operation);
            Assert.Equal(id, modifications[0].Id);
            Assert.Equal(table.Endpoint, modifications[0].TableEndpoint);
            Assert.NotNull(modifications[0].Entity);
        }
        #endregion

        [Fact]
        [Trait("Method", "UpdateItemAsync")]
        public async Task UpdateItemAsync_Basic()
        {
            // Arrange
            var id = GetRandomId();
            var original = MovieServer.GetMovieById(id)!;
            var expected = ClientMovie.From(original);
            expected.Title = "Replacement Title";
            expected.Rating = "PG-13";
            var updates = new Dictionary<string, object>()
            {
                { "Title", "Replacement Title" },
                { "Rating", "PG-13" }
            };

            // Act
            var response = await table.UpdateItemAsync(id, updates).ConfigureAwait(false);
            var stored = MovieServer.GetMovieById(id);

            // Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal<IMovie>(expected, stored);
            AssertEx.SystemPropertiesChanged(original, stored);
            AssertEx.SystemPropertiesMatch(stored, response.Value);
            AssertEx.Contains("ETag", $"\"{response.Value.GetVersion()}\"", response.Headers);
            AssertEventHandlerCalled(table, id);
        }

        [Theory]
        [InlineData("duration", 50)]
        [InlineData("duration", 370)]
        [InlineData("rating", "M")]
        [InlineData("rating", "PG-13 but not always")]
        [InlineData("title", "a")]
        [InlineData("title", "Lorem ipsum dolor sit amet, consectetur adipiscing elit accumsan.")]
        [InlineData("year", 1900)]
        [InlineData("year", 2035)]
        [Trait("Method", "UpdateItemAsync")]
        public async Task UpdateItemAsync_Validation(string propName, object propValue)
        {
            // Arrange
            var id = GetRandomId();
            var original = MovieServer.GetMovieById(id)!;
            var expected = ClientMovie.From(original);
            var entity = expected.Clone();

            switch (propName)
            {
                case "duration": entity.Duration = (int)propValue; break;
                case "rating": entity.Rating = (string)propValue; break;
                case "title": entity.Title = (string)propValue; break;
                case "year": entity.Year = (int)propValue; break;
            }

            IReadOnlyDictionary<string, object> updates = new Dictionary<string, object>()
            {
                { propName, propValue }
            };

            // Act
            var exception = await Assert.ThrowsAsync<DatasyncOperationException>(() => table.UpdateItemAsync(id, updates)).ConfigureAwait(false);
            var stored = MovieServer.GetMovieById(id).Clone();

            // Assert
            Assert.Equal(400, exception.StatusCode);
            Assert.Equal<IMovie>(expected, stored);
            Assert.Equal<ITableData>(original, stored);
            Assert.Empty(modifications);
        }

        [Fact]
        [Trait("Method", "UpdateItemAsync")]
        public async Task UpdateItemAsync_ThrowsWhenNotFound()
        {
            var updates = new Dictionary<string, object>()
            {
                { "Title", "Replacement Title" },
                { "Rating", "PG-13" }
            };

            // Act
            var exception = await Assert.ThrowsAsync<DatasyncOperationException>(() => table.UpdateItemAsync("not-found", updates)).ConfigureAwait(false);

            // Assert
            Assert.Equal(404, exception.StatusCode);
            Assert.Empty(modifications);
        }

        [Fact]
        [Trait("Method", "UpdateItemAsync")]
        public async Task UpdateItemAsync_ConditionalSuccess()
        {
            // Arrange
            var id = GetRandomId();
            var original = MovieServer.GetMovieById(id)!;
            var expected = ClientMovie.From(original);
            expected.Title = "Replacement Title";
            expected.Rating = "PG-13";
            var updates = new Dictionary<string, object>()
            {
                { "Title", "Replacement Title" },
                { "Rating", "PG-13" }
            };

            // Act
            var response = await table.UpdateItemAsync(id, updates, IfMatch.Version(expected.Version)).ConfigureAwait(false);
            var stored = MovieServer.GetMovieById(id);

            // Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal<IMovie>(expected, stored);
            AssertEx.SystemPropertiesChanged(original, stored);
            AssertEx.SystemPropertiesMatch(stored, response.Value);
            AssertEx.Contains("ETag", $"\"{response.Value.GetVersion()}\"", response.Headers);
            AssertEventHandlerCalled(table, id);
        }

        [Fact]
        [Trait("Method", "UpdateItemAsync")]
        public async Task UpdateItemAsync_ConditionalFailure()
        {
            // Arrange
            var id = GetRandomId();
            var original = MovieServer.GetMovieById(id);
            var expected = ClientMovie.From(original);
            expected.Title = "Replacement Title";
            expected.Rating = "PG-13";
            const string etag = "dGVzdA==";
            var updates = new Dictionary<string, object>()
            {
                { "Title", "Replacement Title" },
                { "Rating", "PG-13" }
            };

            // Act
            var exception = await Assert.ThrowsAsync<DatasyncConflictException<JsonDocument>>(() => table.UpdateItemAsync(id, updates, IfMatch.Version(etag))).ConfigureAwait(false);

            // Assert
            Assert.NotNull(exception.Request);
            Assert.NotNull(exception.Response);
            Assert.Equal(412, exception.StatusCode);
            Assert.Equal(MovieCount, MovieServer.GetMovieCount());
            var entity = MovieServer.GetMovieById(id);
            Assert.NotNull(entity);
            Assert.Equal<IMovie>(original, entity);
            Assert.Equal<ITableData>(original, entity);
            AssertEx.JsonDocumentMatches(entity, exception.ServerItem);
            AssertEx.ResponseHasConditionalHeaders(entity, exception.Response);
            Assert.Empty(modifications);
        }

        [Fact]
        [Trait("Method", "UpdateItemAsync")]
        public async Task UpdateItemAsync_SoftNotDeleted()
        {
            // Arrange
            var id = GetRandomId();
            var table = client.GetRemoteTable("soft");
            table.TableModified += (sender, e) => modifications.Add(e);
            var original = MovieServer.GetMovieById(id)!;
            var expected = ClientMovie.From(original);
            expected.Title = "Replacement Title";
            expected.Rating = "PG-13";
            var updates = new Dictionary<string, object>()
            {
                { "Title", "Replacement Title" },
                { "Rating", "PG-13" }
            };

            // Act
            var response = await table.UpdateItemAsync(id, updates).ConfigureAwait(false);
            var stored = MovieServer.GetMovieById(id);

            // Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal<IMovie>(expected, stored);
            AssertEx.SystemPropertiesChanged(original, stored);
            AssertEx.SystemPropertiesMatch(stored, response.Value);
            AssertEx.Contains("ETag", $"\"{response.Value.GetVersion()}\"", response.Headers);
            AssertEventHandlerCalled(table, id);
        }

        [Fact]
        [Trait("Method", "UpdateItemAsync")]
        public async Task UpdateItemAsync_SoftDeleted_ReturnsGone()
        {
            // Arrange
            var id = GetRandomId();
            var table = client.GetRemoteTable("soft");
            await MovieServer.SoftDeleteMoviesAsync(x => x.Id == id).ConfigureAwait(false);
            var original = MovieServer.GetMovieById(id);
            var expected = ClientMovie.From(original);
            expected.Title = "Replacement Title";
            expected.Rating = "PG-13";
            var updates = new Dictionary<string, object>()
            {
                { "Title", "Replacement Title" },
                { "Rating", "PG-13" }
            };

            // Act
            var exception = await Assert.ThrowsAsync<DatasyncOperationException>(() => table.UpdateItemAsync(id, updates)).ConfigureAwait(false);
            var stored = MovieServer.GetMovieById(id);

            // Assert
            Assert.Equal(410, exception.StatusCode);
            Assert.Equal<IMovie>(original, stored);
            Assert.Equal<ITableData>(original, stored);
            Assert.Empty(modifications);
        }
    }
}